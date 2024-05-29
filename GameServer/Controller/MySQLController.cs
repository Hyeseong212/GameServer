using GameServer.Controller;
using Google.Protobuf.WellKnownTypes;
using MySql.Data.MySqlClient;
using Mysqlx.Session;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;



internal class MySQLController
{
    private static MySQLController instance;
    public static MySQLController Instance
    {
        get
        {
            if (instance == null)
            {
                if (instance == null)
                {
                    instance = new MySQLController();
                }
            }
            return instance;
        }
    }


    private StringBuilder sBuilder;
    private MySqlConnection dbconn;

    //private string dbIP = "192.168.219.100";
    private string dbIP = "192.168.123.1";
    //private string dbIP = "127.0.0.1";

    private string connStr;


    public MySQLController()
    {
        sBuilder = new StringBuilder();
        var ip = dbIP;
        var port = "3306";
        var id = "root";
        var pwd = "1234";
        //connStr = string.Format("Server={0};Port={1};Database=Mobility;Uid={2};Pwd={3};charset=utf8;SSL Mode=Required", ip, port, id, pwd);
        connStr = string.Format("Server={0};Port={1};Database=MyGameDB;Uid={2};Pwd={3};charset=utf8", ip, port, id, pwd);
        dbconn = new MySqlConnection(connStr);
    }
    public void Init()
    {
        Console.WriteLine("MysqlController Init Complete");
        Task.Run(() => dbOpenAsyncTest());
    }
    //void형 비동기작업
    public async Task dbOpenAsyncTest()
    {
        await Task.Run(() =>
        {
            using (MySqlConnection dbconn = new MySqlConnection(connStr))
            {
                try
                {
                    dbconn.Open();
                    dbconn.Close();
                    Console.WriteLine("DB Connection OK");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        });
    }
    public async Task<UserEntity> UserSelect(long uid)
    {
        var row = new UserEntity();
        string query = @"
        SELECT `UserUID`,
               `UserName`,
               `UserID`,
               `UserPW`,
               `GuildUID`
        FROM `mygamedb`.`usertable`
        WHERE `UserUID` = @UserUID";

        using (MySqlConnection dbconn = new MySqlConnection(connStr))
        {
            await dbconn.OpenAsync(); // 비동기적으로 연결을 엽니다.

            try
            {
                MySqlCommand command = new MySqlCommand(query, dbconn);
                command.Parameters.AddWithValue("@UserUID", uid); // 매개변수 추가

                using (var reader = await command.ExecuteReaderAsync()) // 비동기적으로 데이터를 읽어옵니다.
                {
                    if (await reader.ReadAsync())
                    {
                        row.UserUID = reader.GetInt64("UserUID");
                        row.UserName = reader.GetString("UserName");
                        row.Userid = reader.GetString("UserID");
                        row.UserPW = reader.GetString("UserPW");
                        row.guildUID = long.Parse(reader["GuildUID"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // 로그를 기록하거나 추가적인 예외 처리를 여기에 작성할 수 있습니다.
            }
        }
        return row;
    }
    public async Task<UserEntity> CheckUserIdInDatabase(string userId)
    {
        var row = new UserEntity();

        // 매개변수화된 쿼리 작성
        string query = @"
        SELECT `UserUID`,
               `UserName`,
               `UserID`,
               `UserPW`,
               `GuildUID`
        FROM `mygamedb`.`usertable`
        WHERE `UserID` = @UserID
    ";

        using (MySqlConnection dbconn = new MySqlConnection(connStr))
        {
            await dbconn.OpenAsync();

            // 명령어에 매개변수화된 쿼리와 연결
            using (MySqlCommand command = new MySqlCommand(query, dbconn))
            {
                // 쿼리의 매개변수로 사용자 아이디 값을 추가
                command.Parameters.AddWithValue("@UserID", userId);

                try
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            row.UserUID = long.Parse(reader["UserUID"].ToString());
                            row.UserName = reader["UserName"].ToString();
                            row.Userid = reader["UserID"].ToString();
                            row.UserPW = reader["UserPW"].ToString();
                            row.guildUID = long.Parse(reader["GuildUID"].ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return row;  // 오류가 발생해도 빈 `UserEntity` 객체를 반환합니다.
                }
            }
        }

        return row;  // 존재하지 않을 경우 비어 있는 `UserEntity` 객체를 반환
    }
    public async Task<bool> SignUpToDatabase(SignUpInfo signupInfo)
    {
        // UserID 중복 여부 확인 쿼리
        string checkUserIdQuery = "SELECT COUNT(*) FROM usertable WHERE UserID = @UserID";
        // 새로운 사용자를 삽입하는 쿼리
        string insertUserQuery = @"
        INSERT INTO usertable (UserUID, UserName, UserID, UserPW)
        VALUES (@UserUID, @UserName, @UserID, @UserPW)";

        using (MySqlConnection dbconn = new MySqlConnection(connStr))
        {
            await dbconn.OpenAsync();

            // UserID 중복 여부를 확인
            using (MySqlCommand checkCmd = new MySqlCommand(checkUserIdQuery, dbconn))
            {
                checkCmd.Parameters.AddWithValue("@UserID", signupInfo.id);
                int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                // UserID가 이미 존재한다면 false를 반환
                if (count > 0)
                {
                    return false;
                }
            }

            // UserID가 존재하지 않는다면, 새로운 고유한 UserUID 생성
            long newUID = DateTime.Now.Ticks;

            // 새로운 사용자를 데이터베이스에 삽입
            using (MySqlCommand insertCmd = new MySqlCommand(insertUserQuery, dbconn))
            {
                insertCmd.Parameters.AddWithValue("@UserUID", newUID);
                insertCmd.Parameters.AddWithValue("@UserName", signupInfo.name);
                insertCmd.Parameters.AddWithValue("@UserID", signupInfo.id);
                insertCmd.Parameters.AddWithValue("@UserPW", signupInfo.pw);
                insertCmd.Parameters.AddWithValue("@GuildUID", 0);

                int affectedRows = await insertCmd.ExecuteNonQueryAsync();

                // 삽입이 성공하여 정확히 하나의 행이 영향을 받았다면 true를 반환
                return affectedRows == 1;
            }
        }
    }
    /// <summary>
    /// uid로 유저가 길드에 가입해있는지 체크
    /// </summary>
    /// <param name="guildName"></param>
    /// <returns></returns>
    public async Task<long> CheckUserGuildEnable(long userUID)
    {
        // 유저의 길드 가입 여부를 확인하는 쿼리
        string checkGuildQuery = "SELECT GuildUID FROM mygamedb.usertable WHERE UserUID = @UserUID";

        using (MySqlConnection dbconn = new MySqlConnection(connStr))
        {
            await dbconn.OpenAsync();

            // 유저의 길드 가입 여부를 확인
            using (MySqlCommand checkCmd = new MySqlCommand(checkGuildQuery, dbconn))
            {
                checkCmd.Parameters.AddWithValue("@UserUID", userUID);

                var result = await checkCmd.ExecuteScalarAsync();

                // GuildUID가 NULL이 아니면 해당 값을 반환, NULL이면 long.MinValue 반환
                if (result != DBNull.Value && Convert.ToInt64(result) != 0)
                {
                    return Convert.ToInt64(result);
                }
                else
                {
                    return long.MinValue;
                }
            }
        }
    }
    /// <summary>
    /// 길드uid로 길드 정보 찾기
    /// </summary>
    /// <param name="guildUid"></param>
    /// <returns></returns>
    public async Task<GuildInfo> SelectGuildInfoFromGuildUid(long guildUid)
    {
        // 길드 UID로 길드 정보를 조회하는 쿼리
        string selectGuildNameQuery = @"
    SELECT Guild_uid, Guild_Name, Guild_crews, Guild_leader, Guild_JoinRequestUser
    FROM mygamedb.guildtable 
    WHERE Guild_uid = @GuildUid";

        using (MySqlConnection dbconn = new MySqlConnection(connStr))
        {
            await dbconn.OpenAsync();

            // 길드 정보를 조회
            using (MySqlCommand selectCmd = new MySqlCommand(selectGuildNameQuery, dbconn))
            {
                selectCmd.Parameters.AddWithValue("@GuildUid", guildUid);

                using (var reader = await selectCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var guildInfo = new GuildInfo
                        {
                            guildUid = reader.GetInt64("Guild_uid"),
                            guildName = reader.GetString("Guild_Name"),
                            guildCrews = JsonConvert.DeserializeObject<List<GuildCrew>>(reader.GetString("Guild_crews")),
                            guildLeader = reader.GetInt64("Guild_leader"),
                            guildRequest = JsonConvert.DeserializeObject<List<UserEntity>>(reader.GetString("Guild_JoinRequestUser"))
                        };

                        return guildInfo;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 길드 이름으로 길드 정보를 조회하여 반환
    /// </summary>
    /// <param name="guildName"></param>
    /// <returns></returns>
    public async Task<List<GuildInfo>> SelectGuildNameFromGuildName(string guildName)
    {
        // 길드 이름으로 길드 정보를 조회하는 쿼리
        string selectGuildNameQuery = @"
        SELECT Guild_uid, Guild_Name, Guild_crews, Guild_leader ,Guild_JoinRequestUser
        FROM mygamedb.guildtable 
        WHERE Guild_Name = @GuildName";

        List<GuildInfo> guildInfos = new List<GuildInfo>();

        using (MySqlConnection dbconn = new MySqlConnection(connStr))
        {
            await dbconn.OpenAsync();

            // 길드 정보를 조회
            using (MySqlCommand selectCmd = new MySqlCommand(selectGuildNameQuery, dbconn))
            {
                selectCmd.Parameters.AddWithValue("@GuildName", guildName);

                using (var reader = await selectCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var guildInfo = new GuildInfo
                        {
                            guildUid = reader.GetInt64("Guild_uid"),
                            guildName = reader.GetString("Guild_Name"),
                            guildCrews = JsonConvert.DeserializeObject<List<GuildCrew>>(reader.GetString("Guild_crews")),
                            guildLeader = reader.GetInt64("Guild_leader"),
                            guildRequest = JsonConvert.DeserializeObject<List<UserEntity>>(reader.GetString("Guild_JoinRequestUser"))
                        };

                        guildInfos.Add(guildInfo); 
                    }
                }
            }
        }

        return guildInfos;
    }
    /// <summary>
    /// 길드 생성
    /// </summary>
    /// <param name="guildName"></param>
    /// <returns></returns>
    public async Task<long> CreateGuild(string guildName, GuildCrew guildCrew)
    {
        // 새로운 길드를 삽입하는 쿼리
        string insertGuildQuery = @"
    INSERT INTO mygamedb.guildtable 
    (Guild_uid, Guild_Name, Guild_crews, Guild_leader, Guild_JoinRequestUser) 
    VALUES (@GuildUid, @GuildName, @GuildCrews, @GuildLeader, @GuildJoinRequestUser)";

        using (MySqlConnection dbconn = new MySqlConnection(connStr))
        {
            await dbconn.OpenAsync();

            // 새로운 길드를 생성
            long newGuildUid = DateTime.Now.Ticks;
            List<GuildCrew> guildCrews = new List<GuildCrew> { guildCrew };
            string guildCrewsJson = JsonConvert.SerializeObject(guildCrews);
            List<UserEntity> joinGuildRequest = new List<UserEntity>();
            string joinGuildRequestJson = JsonConvert.SerializeObject(joinGuildRequest);

            using (MySqlCommand insertCmd = new MySqlCommand(insertGuildQuery, dbconn))
            {
                insertCmd.Parameters.AddWithValue("@GuildUid", newGuildUid);
                insertCmd.Parameters.AddWithValue("@GuildName", guildName);
                insertCmd.Parameters.AddWithValue("@GuildCrews", guildCrewsJson);
                insertCmd.Parameters.AddWithValue("@GuildLeader", guildCrew.crewUid);
                insertCmd.Parameters.AddWithValue("@GuildJoinRequestUser", joinGuildRequestJson);

                int affectedRows = await insertCmd.ExecuteNonQueryAsync();

                // 삽입이 성공하여 정확히 하나의 행이 영향을 받았다면 newGuildUid를 반환
                if (affectedRows == 1)
                {
                    return newGuildUid;
                }
                else
                {
                    return long.MinValue; // long.MinValue
                }
            }
        }
    }
    /// <summary>
    /// 길드UID로 길드정보 업데이트
    /// </summary>
    /// <param name="guildUID"></param>
    /// <param name="guildInfo"></param>
    /// <returns></returns>
    public async Task<bool> UpdateGuild(long guildUID, GuildInfo guildInfo)
    {
        // 길드를 업데이트하는 쿼리
        string updateGuildQuery = @"
    UPDATE mygamedb.guildtable 
    SET Guild_Name = @GuildName, 
        Guild_crews = @GuildCrews, 
        Guild_leader = @GuildLeader, 
        Guild_JoinRequestUser = @GuildJoinRequestUser 
    WHERE Guild_uid = @GuildUid";

        using (MySqlConnection dbconn = new MySqlConnection(connStr))
        {
            await dbconn.OpenAsync();

            // 길드 정보를 JSON으로 직렬화
            string guildCrewsJson = JsonConvert.SerializeObject(guildInfo.guildCrews);
            string joinGuildRequestJson = JsonConvert.SerializeObject(guildInfo.guildRequest);

            using (MySqlCommand updateCmd = new MySqlCommand(updateGuildQuery, dbconn))
            {
                updateCmd.Parameters.AddWithValue("@GuildUid", guildUID);
                updateCmd.Parameters.AddWithValue("@GuildName", guildInfo.guildName);
                updateCmd.Parameters.AddWithValue("@GuildCrews", guildCrewsJson);
                updateCmd.Parameters.AddWithValue("@GuildLeader", guildInfo.guildLeader);
                updateCmd.Parameters.AddWithValue("@GuildJoinRequestUser", joinGuildRequestJson);

                int affectedRows = await updateCmd.ExecuteNonQueryAsync();

                // 업데이트가 성공하여 하나 이상의 행이 영향을 받았다면 true를 반환
                return affectedRows > 0;
            }
        }
    }

    /// <summary>
    /// userUID를 받아서 받은 userEntity로 Update쿼리 날리기
    /// </summary>
    /// <param name="guildName"></param>
    /// <returns></returns>
    public async Task<bool> UpdateUserInfo(long userUID, UserEntity userEntity)
    {
        // 유저 정보를 업데이트하는 쿼리
        string updateUserQuery = @"
        UPDATE mygamedb.usertable 
        SET UserName = @UserName, 
            UserID = @Userid, 
            UserPW = @UserPW,
            GuildUID = @GuildUID 
        WHERE UserUID = @UserUID";

        using (MySqlConnection dbconn = new MySqlConnection(connStr))
        {
            await dbconn.OpenAsync();

            using (MySqlCommand updateCmd = new MySqlCommand(updateUserQuery, dbconn))
            {
                updateCmd.Parameters.AddWithValue("@UserName", userEntity.UserName);
                updateCmd.Parameters.AddWithValue("@Userid", userEntity.Userid);
                updateCmd.Parameters.AddWithValue("@UserPW", userEntity.UserPW);
                updateCmd.Parameters.AddWithValue("@GuildUID", userEntity.guildUID);
                updateCmd.Parameters.AddWithValue("@UserUID", userUID);

                int affectedRows = await updateCmd.ExecuteNonQueryAsync();

                // 업데이트가 성공하여 정확히 하나의 행이 영향을 받았다면 true를 반환
                return affectedRows == 1;
            }
        }
    }
    /// <summary>
    /// 유저가 길드 가입 요청을 보내는 메서드
    /// </summary>
    /// <param name="guildUID"></param>
    /// <param name="requestedUser"></param>
    /// <returns></returns>
    public async Task<bool> JoinGuildRequest(long guildUID, UserEntity requestedUser)
    {
        // 길드 UID로 길드 정보를 조회하는 쿼리
        string selectGuildQuery = @"
    SELECT Guild_JoinRequestUser
    FROM mygamedb.guildtable 
    WHERE Guild_uid = @GuildUid";

        // 길드 요청 목록을 업데이트하는 쿼리
        string updateGuildQuery = @"
    UPDATE mygamedb.guildtable
    SET Guild_JoinRequestUser = @GuildJoinRequestUser
    WHERE Guild_uid = @GuildUid";

        using (MySqlConnection dbconn = new MySqlConnection(connStr))
        {
            await dbconn.OpenAsync();

            List<UserEntity> requestList = new List<UserEntity>();

            // 길드 정보를 조회하여 현재 요청 목록을 가져옴
            using (MySqlCommand selectCmd = new MySqlCommand(selectGuildQuery, dbconn))
            {
                selectCmd.Parameters.AddWithValue("@GuildUid", guildUID);

                using (var reader = await selectCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        string jsonRequestList = reader["Guild_JoinRequestUser"].ToString();
                        if (!string.IsNullOrEmpty(jsonRequestList))
                        {
                            requestList = JsonConvert.DeserializeObject<List<UserEntity>>(jsonRequestList);
                        }
                    }
                }
            }

            // 요청 목록에 새로운 유저 추가
            requestList.Add(requestedUser);
            string updatedRequestListJson = JsonConvert.SerializeObject(requestList);

            // 요청 목록을 업데이트
            using (MySqlCommand updateCmd = new MySqlCommand(updateGuildQuery, dbconn))
            {
                updateCmd.Parameters.AddWithValue("@GuildJoinRequestUser", updatedRequestListJson);
                updateCmd.Parameters.AddWithValue("@GuildUid", guildUID);

                int affectedRows = await updateCmd.ExecuteNonQueryAsync();

                // 업데이트가 성공하여 정확히 하나의 행이 영향을 받았다면 true를 반환
                return affectedRows == 1;
            }
        }
    }
    public async Task<GuildInfo> SelectGuildInfo(long guildUID)
    {
        var guildInfo = new GuildInfo();
        string query = @"
        SELECT `Guild_uid`,
               `Guild_Name`,
               `Guild_crews`,
               `Guild_leader`,
               `Guild_JoinRequestUser`
        FROM `guildtable`
        WHERE `Guild_uid` = @GuildUID";

        using (MySqlConnection dbconn = new MySqlConnection(connStr))
        {
            await dbconn.OpenAsync(); // 비동기적으로 연결을 엽니다.

            try
            {
                MySqlCommand command = new MySqlCommand(query, dbconn);
                command.Parameters.AddWithValue("@GuildUID", guildUID);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        guildInfo.guildUid = reader.GetInt64("Guild_uid");
                        guildInfo.guildName = reader.GetString("Guild_Name");

                        // Guild_crews 역직렬화
                        string crewsJson = reader.GetString("Guild_crews");
                        if (!string.IsNullOrEmpty(crewsJson))
                        {
                            guildInfo.guildCrews = JsonConvert.DeserializeObject<List<GuildCrew>>(crewsJson);
                        }

                        guildInfo.guildLeader = reader.GetInt64("Guild_leader");

                        // Guild_JoinRequestUser 역직렬화
                        string requestsJson = reader["Guild_JoinRequestUser"] as string;
                        if (!string.IsNullOrEmpty(requestsJson))
                        {
                            guildInfo.guildRequest = JsonConvert.DeserializeObject<List<UserEntity>>(requestsJson);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // 로그를 기록하거나 추가적인 예외 처리를 여기에 작성할 수 있습니다.
            }
        }
        return guildInfo;
    }
}
