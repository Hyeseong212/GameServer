using GameServer.Controller;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    private const int MatchSize = 2; // 매칭에 필요한 최소 플레이어 수 (예: 2명)

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
            Console.WriteLine($"{BitConverter.ToInt64(UserUid)} accept Match");
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
                List<PlayerInfo> matchedPlayers = new List<PlayerInfo>();
                for (int i = 0; i < MatchSize; i++)
                {
                    matchedPlayers.Add(normalQueue.Dequeue());
                }

                Console.WriteLine("Match found:");
                foreach (var player in matchedPlayers)
                {
                    Console.WriteLine($"User {player.UserUID} matched.");
                }

                // 매칭된 플레이어들에게 응답을 보내고 게임 세션 생성
                foreach (var player in matchedPlayers)
                {
                    Console.WriteLine("!!노말 게임 매칭됨!!");
                    SendMatchResponse(player.Socket, matchedPlayers);
                }
                GameMatched(matchedPlayers, GameType.Normal);
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
                    List<PlayerInfo> matchedPlayers = new List<PlayerInfo>();
                    for (int i = 0; i < MatchSize; i++)
                    {
                        matchedPlayers.Add(tierQueue.Value.Dequeue());
                    }

                    Console.WriteLine($"Match found in {tierQueue.Key} tier:");
                    foreach (var player in matchedPlayers)
                    {
                        Console.WriteLine($"User {player.UserUID} matched.");
                    }

                    // 매칭된 플레이어들에게 응답을 보내고 게임 세션 생성
                    foreach (var player in matchedPlayers)
                    {
                        Console.WriteLine("!!랭크 게임 매칭됨!!");
                        SendMatchResponse(player.Socket, matchedPlayers);
                    }
                    GameMatched(matchedPlayers, GameType.Rank);
                }
            }
        }
    }

    private void SendMatchResponse(Socket clientSocket, List<PlayerInfo> matchedPlayers)
    {
        Packet packet = new Packet();

        int length = 0x01;

        packet.push((byte)Protocol.Match);
        packet.push(length);
        packet.push((byte)MatchProtocol.GameMatched);

        ClientController.Instance.SendToClient(clientSocket, packet);
    }

    private void GameMatched(List<PlayerInfo> matchedPlayers, GameType gameType)
    {
        // 새로운 인게임 세션 생성 및 매칭된 플레이어 추가
        SessionManager.Instance.InGameSessionCreate(matchedPlayers, gameType);

    }
}

