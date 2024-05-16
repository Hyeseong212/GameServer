using GameServer.Controller;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

internal class GuildController
{
    private static GuildController instance;
    public static GuildController Instance
    {
        get
        {
            if (instance == null)
            {
                if (instance == null)
                {
                    instance = new GuildController();
                }
            }
            return instance;
        }
    }

    public ConcurrentDictionary<long, GuildSession> GuildSessions = new ConcurrentDictionary<long, GuildSession>();

    public void Init()
    {
        Console.WriteLine("GuildController init Complete");
    }
    public void ProcessGuildPacket(Socket clientSocket, byte[] data)
    {
        GuildProtocol guildProtocol = (GuildProtocol)data[0];

        if (guildProtocol == GuildProtocol.IsUserGuildEnable)
        {
            //현재 유저가 길드 가입했는지 확인
            byte[] userUID = data.Skip(1).ToArray();
            IsUserGuildEnable(clientSocket, userUID);

        }
        else if (guildProtocol == GuildProtocol.SelectGuildName)
        {
            //길드이름 받아서 길드이름 조회
            byte[] guildName = data.Skip(1).ToArray();
            GuildFindWithName(clientSocket, guildName);
        }
        else if (guildProtocol == GuildProtocol.SelectGuildCrew)
        {
            //현재 가입한 길드의 길드원들 조회
        }
        else if (guildProtocol == GuildProtocol.CreateGuild)
        {
            //길드 생성
            byte[] guildNamebyte = data.Skip(1).ToArray();
            CreateGuild(guildNamebyte, clientSocket);
        }
        else if (guildProtocol == GuildProtocol.SelectGuildUid)
        {
            //길드UID 받아서 길드이름 조회
            byte[] guildUID = data.Skip(1).ToArray();
            SelectFromGuildUidToName(clientSocket, guildUID);
        }
    }
    private void GuildFindWithName(Socket clientSocket, byte[] data)
    {
        string guildName = Encoding.UTF8.GetString(data);
        Task<List<GuildInfo>> CheckUser = MySQLController.Instance.SelectGuildNameFromGuildName(guildName);
        CheckUser.ContinueWith((antecedent) =>
        {
            string jsonguildInfo = JsonConvert.SerializeObject(antecedent.Result);
            int length = 0x01 + Utils.GetLength(jsonguildInfo);

            var sendData = new Packet();

            sendData.push((byte)Protocol.Guild);
            sendData.push(length);
            sendData.push((byte)GuildProtocol.SelectGuildName);
            sendData.push(jsonguildInfo);

            ClientController.Instance.SendToClient(clientSocket, sendData);
        });
    }

    private void IsUserGuildEnable(Socket clientsocket, byte[] userUID)
    {
        long uidval = BitConverter.ToInt64(userUID);

        Task<long> CheckUser = MySQLController.Instance.CheckUserGuildEnable(uidval);

        CheckUser.ContinueWith((antecedent) =>
        {
            int length = 0x01 + Utils.GetLength(antecedent.Result);

            var sendData = new Packet();

            sendData.push((byte)Protocol.Guild);
            sendData.push(length);
            sendData.push((byte)GuildProtocol.IsUserGuildEnable);
            sendData.push(antecedent.Result);

            ClientController.Instance.SendToClient(clientsocket, sendData);
        });
    }
    private void SelectFromGuildUidToName(Socket clientsocket, byte[] guildUID)
    {
        try
        {
            long uidval = BitConverter.ToInt64(guildUID);

            Task<string> CheckGuildName = MySQLController.Instance.SelectGuildNameFromGuildUid(uidval);

            CheckGuildName.ContinueWith((antecedent) =>
            {
                int length = 0x01 + Utils.GetLength(antecedent.Result);

                var sendData = new Packet();

                sendData.push((byte)Protocol.Guild);
                sendData.push(length);
                sendData.push((byte)GuildProtocol.SelectGuildUid);
                sendData.push(antecedent.Result);

                ClientController.Instance.SendToClient(clientsocket, sendData);
            });
        }
        catch (Exception ex)
        {

        }
    }
    private void CreateGuild(byte[] data, Socket clientSocket)
    {
        long userUID = BitConverter.ToInt64(data);

        byte[] guildNamebyte = data.Skip(8).ToArray();

        string guildName = Encoding.UTF8.GetString(guildNamebyte);

        Task<UserEntity> CheckUser = MySQLController.Instance.UserSelect(userUID);

        CheckUser.ContinueWith((antecedent01) =>
        {
            GuildCrew guildCrew = new GuildCrew();
            guildCrew.crewUid = antecedent01.Result.UserUID;
            guildCrew.crewName = antecedent01.Result.UserName;
            Task<long> CheckGuildName = MySQLController.Instance.CreateGuild(guildName, guildCrew);
            CheckGuildName.ContinueWith((antecedent02) =>
            {
                Packet packet = new Packet();
                int length = 0x01 + 0x01;
                if (antecedent02.Result == long.MinValue)
                {
                    packet.push((byte)Protocol.Guild);
                    packet.push(length);
                    packet.push((byte)GuildProtocol.CreateGuild);
                    packet.push((byte)ResponseType.Fail);
                }
                else
                {
                    packet.push((byte)Protocol.Guild);
                    packet.push(length);
                    packet.push((byte)GuildProtocol.CreateGuild);
                    packet.push((byte)ResponseType.Success);

                    UserEntity userEntity = new UserEntity();

                    userEntity.UserUID = antecedent01.Result.UserUID;
                    userEntity.Userid = antecedent01.Result.Userid;
                    userEntity.UserPW = antecedent01.Result.UserPW;
                    userEntity.UserName = antecedent01.Result.UserName;
                    userEntity.guildUID = antecedent02.Result;

                    Task<bool> CheckGuildName = MySQLController.Instance.UpdateUserInfo(userUID, userEntity);
                }
                ClientController.Instance.SendToClient(clientSocket, packet);
            });
        });
    }
    public void GuildSessionCheck(UserEntity user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        // guildUID가 0인 경우 처리
        if (user.guildUID == 0)
        {
            Console.WriteLine("User is not part of any guild.");
            return;
        }

        // 해당 guildUID에 대한 GuildSession을 가져오거나 새로운 세션을 생성
        GuildSessions.AddOrUpdate(user.guildUID,
            (key) =>
            {
                // 새 세션 생성
                var newSession = new GuildSession
                {
                    guildUid = user.guildUID
                };
                newSession.onlineGuildCrews.Add(user);
                return newSession;
            },
            (key, existingSession) =>
            {
                // 기존 세션에 유저 추가
                if (!existingSession.onlineGuildCrews.Contains(user))
                {
                    existingSession.onlineGuildCrews.Add(user);
                }
                return existingSession;
            });

        Console.WriteLine($"User {user.UserName} added to guild session with UID: {user.guildUID}");
    }
    public void RemoveUserFromGuildSession(UserEntity user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        // guildUID가 0인 경우 처리
        if (user.guildUID == 0)
        {
            Console.WriteLine("User is not part of any guild.");
            return;
        }

        // 해당 guildUID에 대한 GuildSession을 가져옴
        if (GuildSessions.TryGetValue(user.guildUID, out GuildSession existingSession))
        {
            // 기존 세션에서 유저 제거
            var userToRemove = existingSession.onlineGuildCrews.FirstOrDefault(u => u.UserUID == user.UserUID);
            if (userToRemove != null)
            {
                existingSession.onlineGuildCrews.Remove(userToRemove);
                Console.WriteLine($"User {user.UserName} removed from guild session with UID: {user.guildUID}");
            }
            else
            {
                Console.WriteLine($"User {user.UserName} not found in guild session with UID: {user.guildUID}");
            }
        }
        else
        {
            Console.WriteLine($"Guild session with UID: {user.guildUID} not found.");
        }
    }
    public List<UserEntity> GetOnlineGuildMembers(long guildUID)
    {
        if (guildUID == 0)
        {
            Console.WriteLine("Invalid guild UID.");
            return new List<UserEntity>();
        }

        // 해당 guildUID에 대한 GuildSession을 가져옴
        if (GuildSessions.TryGetValue(guildUID, out GuildSession existingSession))
        {
            return new List<UserEntity>(existingSession.onlineGuildCrews);
        }
        else
        {
            Console.WriteLine($"Guild session with UID: {guildUID} not found.");
            return new List<UserEntity>();
        }
    }
}
