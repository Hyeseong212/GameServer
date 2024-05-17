using System.Net.Sockets;

public class ConnectedUsers
{
    public long userId;
    public Socket client;
    public ConnectedUsers(Socket socket)
    {
        userId = long.MinValue;
        client = socket;
    }
}
[Serializable]
public class UserEntity
{
    public long UserUID;
    public string UserName;
    public string Userid;
    public string UserPW;
    public long guildUID;
    public UserEntity()
    {
        UserUID = long.MinValue;
        UserName = string.Empty;
        Userid = string.Empty;
        UserPW = string.Empty;
        guildUID = 0;
    }
}
[Serializable]
public class LoginInfo
{
    public string ID;
    public string PW;
    public LoginInfo()
    {
        ID = string.Empty;
        PW = string.Empty;
    }
}
[Serializable]
public class SignUpInfo
{
    public string id;
    public string pw;
    public string name;
    public SignUpInfo()
    {
        id = string.Empty;
        pw = string.Empty;
        name = string.Empty;
    }
}
[Serializable]
public class GuildInfo
{
    public long guildUid;
    public string guildName;
    public List<GuildCrew> guildCrews;
    public long guildLeader;
    public List<long> guildRequest;
    public GuildInfo()
    {
        guildUid = long.MinValue;
        guildName = string.Empty;
        guildCrews = new List<GuildCrew>();
        guildLeader = long.MinValue;
        guildRequest = new List<long>();
    }
}
[Serializable]
public class GuildCrew
{
    public long crewUid;
    public string crewName;
    public GuildCrew()
    {
        crewUid = long.MinValue;
        crewName = string.Empty;
    }
}
public class GuildSession
{
    public long guildUid;
    public List<UserEntity> onlineGuildCrews;
    public List<UserEntity> signupRequests;
    public GuildSession()
    {
        guildUid = long.MinValue;
        onlineGuildCrews = new List<UserEntity>();
        signupRequests = new List<UserEntity>();
    }
}