using GameServer.Controller;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;


public class GuildUpdate : CThread
{
    // 클라이언트에게 보내거나 고속 계산이 필요할때 가져다 쓰자.
    protected override void ThreadUpdate()
    {
        Console.WriteLine($"{ThreadName} 스레드가 오버라이드되어 동작 중입니다.");
        Thread.Sleep(60 * 1000);
    }
}
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

        if (guildProtocol == GuildProtocol.SelectGuildName)
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
        else if (guildProtocol == GuildProtocol.RequestJoinGuild)
        {
            //길드UID 받아서 길드이름 조회
            byte[] uSerUid = data.Skip(1).ToArray();
            RequestJoinGuild(clientSocket, uSerUid);
        }
        else if (guildProtocol == GuildProtocol.RequestJoinOK)
        {
            //길드UID 받아서 길드이름 조회
            byte[] UserUid = data.Skip(1).ToArray();
            UserRequestOK(clientSocket, UserUid);
        }
    }
    private void UserRequestOK(Socket clientSocket, byte[] userUidbyte)
    {
        long userUID = BitConverter.ToInt64(userUidbyte, 0);
        long GuildUID = BitConverter.ToInt64(userUidbyte, 8);

        Task<UserEntity> CheckUser = MySQLController.Instance.UserSelect(userUID);

        CheckUser.ContinueWith((antecedent01) =>
        {
            //UserUpdate해서 길드 정보넣기
            antecedent01.Result.guildUID = GuildUID;
            Task.Run(() => MySQLController.Instance.UpdateUserInfo(antecedent01.Result.UserUID, antecedent01.Result));

            //길드정보에 RequestUser에서 빼고 GuildCrew로 넣고 업데이트
            Task<GuildInfo> GuildInfo = MySQLController.Instance.SelectGuildInfo(GuildUID);
            GuildInfo.ContinueWith((antecedent02) =>
            {
                for (int i = 0; i < antecedent02.Result.guildRequest.Count; i++)//해당 유저 요청삭제
                {
                    if (antecedent02.Result.guildRequest[i].UserUID == antecedent01.Result.UserUID)
                    {
                        antecedent02.Result.guildRequest.RemoveAt(i);
                    }
                }
                GuildCrew guildCrew = new GuildCrew();
                guildCrew.crewUid = antecedent01.Result.UserUID;
                guildCrew.crewName = antecedent01.Result.UserName;

                antecedent02.Result.guildCrews.Add(guildCrew);

                Task.Run(() => MySQLController.Instance.UpdateGuild(GuildUID, antecedent02.Result));

                //client에게 guild정보 다시알려주기

                string jsonguildInfo = JsonConvert.SerializeObject(antecedent02.Result);

                int length = 0x01 + Utils.GetLength(jsonguildInfo);

                var sendData = new Packet();

                sendData.push((byte)Protocol.Guild);
                sendData.push(length);
                sendData.push((byte)GuildProtocol.RequestJoinOK);
                sendData.push(jsonguildInfo);

                ClientController.Instance.SendToClient(clientSocket, sendData);
            });

        });
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

            Task<GuildInfo> CheckGuildName = MySQLController.Instance.SelectGuildInfoFromGuildUid(uidval);

            CheckGuildName.ContinueWith((antecedent) =>
            {
                string jsonguildInfo = JsonConvert.SerializeObject(antecedent.Result);

                int length = 0x01 + Utils.GetLength(jsonguildInfo);

                var sendData = new Packet();

                sendData.push((byte)Protocol.Guild);    
                sendData.push(length);
                sendData.push((byte)GuildProtocol.SelectGuildUid);
                sendData.push(jsonguildInfo);

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
    private void CreateGuildSession(long guildUID)
    {
        var newSession = new GuildSession
        {
            guildUid = guildUID
        };

        // 길드 세션을 GuildSessions 딕셔너리에 추가
        GuildSessions[guildUID] = newSession;

        Task<GuildInfo> SelectGuildInfo = MySQLController.Instance.SelectGuildInfo(guildUID);
        SelectGuildInfo.ContinueWith((antecedent) =>
        {
            for (int i = 0; i < antecedent.Result.guildRequest.Count; i++)//DB에서 가입요청들어온 uid의 갯수만큼
            {
                GuildSessions[guildUID].signupRequests.Add(antecedent.Result.guildRequest[i]);//그uid로 유저 정보찾기
            }
        });

        // 길드 세션이 만들어질 때 메시지 출력
        Console.WriteLine($"New guild session created with UID: {guildUID}");
    }
    private void DestroyGuildSession(long guildUID)
    {
        if (guildUID == 0)
        {
            Console.WriteLine("Invalid guild UID.");
            return;
        }

        // GuildSessions 딕셔너리에서 해당 guildUID의 세션 제거
        if (GuildSessions.TryRemove(guildUID, out _))
        {
            Console.WriteLine($"Guild session with UID: {guildUID} has been destroyed.");
        }
        else
        {
            Console.WriteLine($"Guild session with UID: {guildUID} not found.");
        }
    }
    public void AddUserToGuildSession(UserEntity user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        if (!CheckGuildSession(user.guildUID, out GuildSession guildSession))
        {
            // 세션이 없으면 새로 생성
            CreateGuildSession(user.guildUID);

            // 생성한 세션을 다시 체크
            if (!CheckGuildSession(user.guildUID, out guildSession))
            {
                Console.WriteLine("Failed to create guild session.");
                return;
            }
        }

        // 기존 세션에 유저 추가
        if (!guildSession.onlineGuildCrews.Contains(user))
        {
            guildSession.onlineGuildCrews.Add(user);
            Console.WriteLine($"User {user.UserName} added to guild session with UID: {user.guildUID}");
        }
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
                if(existingSession.onlineGuildCrews.Count == 0)
                {
                    DestroyGuildSession(user.guildUID);
                }
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
    private void RequestJoinGuild(Socket clientSocket, byte[] data)
    {
        byte[] sendUseruid = new byte[8];
        byte[] guilduid = new byte[8];
        for (int i = 0; i < 8; i++)
        {
            sendUseruid[i] = data[i];
        }
        for (int i = 8; i < 16; i++)
        {
            guilduid[i - 8] = data[i];
        }
        long sendUseruidval = BitConverter.ToInt64(sendUseruid);
        long guildUIDval = BitConverter.ToInt64(guilduid);

        //그전에 MysqlDB바꾸어야됨

        Task<UserEntity> checkUser = MySQLController.Instance.UserSelect(sendUseruidval);
        checkUser.ContinueWith(antecedent =>
        {
            Task.Run(() => MySQLController.Instance.JoinGuildRequest(guildUIDval, antecedent.Result));

            GuildSession guildSession = new GuildSession();
            if (CheckGuildSession(guildUIDval, out guildSession))//길드세션있는지 확인
            {
                guildSession.signupRequests.Add(antecedent.Result);
            }
            else if (!CheckGuildSession(guildUIDval, out guildSession))
            {
                Console.WriteLine("this guild Session is Not Online");
            }
        });
    }

    private bool CheckGuildSession(long guildUID, out GuildSession guildSession)
    {
        // 길드 UID가 유효하지 않은 경우 처리
        guildSession = new GuildSession();
        if (guildUID == 0)
        {
            //Console.WriteLine("Invalid guild UID.");
            return false;
        }

        // 해당 guildUID에 대한 GuildSession을 가져옴
        if (GuildSessions.TryGetValue(guildUID, out GuildSession findedGuildSession))
        {
            guildSession = findedGuildSession;
            return true;
        }
        else
        {
            //Console.WriteLine($"Guild session with UID: {guildUID} not found.");
            return false;
        }
    }
}
