using GameServer.Controller;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;

internal class MatchController
{
    public class MatchThread : CThread
    {
        private MatchController matchController;

        public MatchThread(MatchController matchController)
        {
            this.matchController = matchController;
        }

        protected override void ThreadUpdate()
        {
            matchController.TryMatchNormalQueue();
            matchController.TryMatchRankQueue();
            Thread.Sleep(50);
        }
    }

    private static MatchController instance;
    public static MatchController Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new MatchController();
            }
            return instance;
        }
    }

    RatingRange ratingRange = new RatingRange();

    private Queue<PlayerInfo> normalQueue;
    private Dictionary<Tier, Queue<PlayerInfo>> rankQueues;
    private const int MatchSize = 1; // 매칭에 필요한 최소 플레이어 수 (예: 2명)
    private Dictionary<long, List<PlayerInfo>> pendingMatches = new Dictionary<long, List<PlayerInfo>>();
    private Dictionary<long, GameType> matchTypes = new Dictionary<long, GameType>();

    public MatchThread matchThread;

    public void Init()
    {
        Console.WriteLine($"{this.ToString()} init Complete");
        normalQueue = new Queue<PlayerInfo>();
        rankQueues = new Dictionary<Tier, Queue<PlayerInfo>>();
        foreach (Tier tier in Enum.GetValues(typeof(Tier)))
        {
            rankQueues[tier] = new Queue<PlayerInfo>();
        }
        matchThread = new MatchThread(this);
        matchThread.Create(this.ToString());
    }

    public void ProcessMatchPacket(Socket clientSocket, byte[] data)
    {
        if (data[0] == (byte)MatchProtocol.MatchStart)
        {
            if (data[1] == (byte)GameType.Normal)
            {
                byte[] UserUid = data.Skip(2).ToArray();
                InsertUserNormalQueue(clientSocket, UserUid);
            }
            else if (data[1] == (byte)GameType.Rank)
            {
                byte[] UserUid = data.Skip(2).ToArray();
                InsertUserRankQueue(clientSocket, UserUid);
            }
        }
        else if ((data[0] == (byte)MatchProtocol.MatchStop))
        {
            //여기에 매치 빼는 작업
            if (data[1] == (byte)GameType.Normal)
            {
                byte[] UserUid = data.Skip(2).ToArray();
                DeleteUserNormalQueue(clientSocket, UserUid);
            }
            else if (data[1] == (byte)GameType.Rank)
            {
                byte[] UserUid = data.Skip(2).ToArray();
                DeleteUserRankQueue(clientSocket, UserUid);
            }
        }
        else if ((data[0] == (byte)MatchProtocol.GameAccept))
        {
            byte[] UserUid = data.Skip(1).ToArray();
            HandleGameAccept(clientSocket, BitConverter.ToInt64(UserUid));
        }

    }

    private void HandleGameAccept(Socket clientSocket, long userUid)
    {
        foreach (var match in pendingMatches)
        {
            var players = match.Value;
            var player = players.FirstOrDefault(p => p.UserUID == userUid);
            if (player != null)
            {
                player.HasAccepted = true;
                if (players.All(p => p.HasAccepted))
                {
                    var gameSession = StartGameSession(match.Key, players, matchTypes[match.Key]);
                    foreach (var matchedPlayer in players)
                    {
                        SendGameRoomIP(matchedPlayer.Socket, gameSession.GameRoomEndPoint);
                    }
                }
                break;
            }
        }
    }


    private void DeleteUserNormalQueue(Socket clientSocket, byte[] data)
    {
        long userUID = BitConverter.ToInt64(data, 0);

        lock (normalQueue)
        {
            var tempQueue = new Queue<PlayerInfo>();
            while (normalQueue.Count > 0)
            {
                var playerInfo = normalQueue.Dequeue();
                if (playerInfo.UserUID != userUID)
                {
                    tempQueue.Enqueue(playerInfo);
                }
                else
                {
                    Console.WriteLine($"User {userUID} removed from normal queue.");
                }
            }

            normalQueue = tempQueue;
        }
    }

    private void InsertUserNormalQueue(Socket clientSocket, byte[] data)
    {
        long userUID = BitConverter.ToInt64(data, 0);

        Task<PlayerRating> playerInfo = MySQLController.Instance.SelectPlayerRating(userUID);

        playerInfo.ContinueWith((antedecent) =>
        {
            PlayerInfo playerInfo = new PlayerInfo
            {
                UserUID = userUID,
                rating = antedecent.Result.rating,
                Socket = clientSocket
            };

            lock (normalQueue)
            {
                normalQueue.Enqueue(playerInfo);
                Console.WriteLine($"User {userUID} added to normal queue.");
            }
        });
    }

    public void TryMatchNormalQueue()
    {
        lock (normalQueue)
        {
            if (normalQueue.Count >= MatchSize)
            {
                long matchId = DateTime.Now.Ticks;
                pendingMatches[matchId] = new List<PlayerInfo>();

                for (int i = 0; i < MatchSize; i++)
                {
                    pendingMatches[matchId].Add(normalQueue.Dequeue());
                }

                matchTypes[matchId] = GameType.Normal;

                Console.WriteLine("Match found:");
                foreach (var player in pendingMatches[matchId])
                {
                    Console.WriteLine($"User {player.UserUID} matched.");
                }

                // 매칭된 플레이어들에게 응답을 보내고 게임 세션 생성
                foreach (var player in pendingMatches[matchId])
                {
                    Console.WriteLine("!!노말 게임 매칭됨!!");
                    SendMatchResponse(player.Socket);
                }
            }
        }
    }

    private void InsertUserRankQueue(Socket clientSocket, byte[] data)
    {
        long userUID = BitConverter.ToInt64(data, 0);

        Task<PlayerRating> playerInfoTask = MySQLController.Instance.SelectPlayerRating(userUID);

        playerInfoTask.ContinueWith((task) =>
        {
            PlayerRating playerRating = task.Result;
            PlayerInfo playerInfo = new PlayerInfo
            {
                UserUID = userUID,
                rating = playerRating.rating,
                Socket = clientSocket
            };

            Tier tier = RatingRange.GetTier(playerInfo.rating);
            lock (rankQueues[tier])
            {
                rankQueues[tier].Enqueue(playerInfo);
            }

            Console.WriteLine($"User {userUID} added to {tier} rank queue.");
        });
    }

    private void DeleteUserRankQueue(Socket clientSocket, byte[] data)
    {
        long userUID = BitConverter.ToInt64(data, 0);

        foreach (var queue in rankQueues.Values)
        {
            lock (queue)
            {
                var tempQueue = new Queue<PlayerInfo>();
                while (queue.Count > 0)
                {
                    var playerInfo = queue.Dequeue();
                    if (playerInfo.UserUID != userUID)
                    {
                        tempQueue.Enqueue(playerInfo);
                    }
                    else
                    {
                        Console.WriteLine($"User {userUID} removed from rank queue.");
                    }
                }

                while (tempQueue.Count > 0)
                {
                    queue.Enqueue(tempQueue.Dequeue());
                }
            }
        }
    }

    public void TryMatchRankQueue()
    {
        foreach (var tierQueue in rankQueues)
        {
            lock (tierQueue.Value)
            {
                if (tierQueue.Value.Count >= MatchSize)
                {
                    long matchId = DateTime.Now.Ticks;
                    pendingMatches[matchId] = new List<PlayerInfo>();

                    for (int i = 0; i < MatchSize; i++)
                    {
                        pendingMatches[matchId].Add(tierQueue.Value.Dequeue());
                    }

                    matchTypes[matchId] = GameType.Rank;

                    Console.WriteLine($"Match found in {tierQueue.Key} tier:");
                    foreach (var player in pendingMatches[matchId])
                    {
                        Console.WriteLine($"User {player.UserUID} matched.");
                    }

                    // 매칭된 플레이어들에게 응답을 보냄
                    foreach (var player in pendingMatches[matchId])
                    {
                        Console.WriteLine("!!랭크 게임 매칭됨!!");
                        SendMatchResponse(player.Socket);
                    }
                }
            }
        }
    }

    private void SendMatchResponse(Socket clientSocket)
    {
        Packet packet = new Packet();

        int length = 0x01;

        packet.push((byte)Protocol.Match);
        packet.push(length);
        packet.push((byte)MatchProtocol.GameMatched);

        ClientController.Instance.SendToClient(clientSocket, packet);
    }
    private void SendGameRoomIP(Socket clientSocket, IPEndPoint gameRoomEndPoint)
    {
        Packet packet = new Packet();

        int length = 0x01 + 0x04 + 0x04;

        packet.push((byte)Protocol.Match);
        packet.push(length); // Protocol byte + IP address length (4 bytes) + port length (2 bytes)
        packet.push((byte)MatchProtocol.GameRoomIP);

        byte[] ipBytes = gameRoomEndPoint.Address.GetAddressBytes();
        packet.push(ipBytes);

        byte[] portBytes = BitConverter.GetBytes(gameRoomEndPoint.Port); // Port는 2 bytes
        packet.push(gameRoomEndPoint.Port);

        ClientController.Instance.SendToClient(clientSocket, packet);
    }


    private InGameSession StartGameSession(long matchId, List<PlayerInfo> matchedPlayers, GameType gameType)
    {
        var gameSession = SessionManager.Instance.InGameSessionCreate(matchedPlayers, gameType);

        // 메인 서버에서 해당 소켓을 해제하고 인게임 세션으로 전달
        foreach (var player in matchedPlayers)
        {
            ServerController.Instance.TransferSocketToGameSession(player.Socket);
        }

        // 매칭 완료 후 pendingMatches에서 해당 매칭 제거
        pendingMatches.Remove(matchId);
        matchTypes.Remove(matchId);

        return gameSession;
    }


}

