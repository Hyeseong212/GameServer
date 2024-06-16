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
    public List<UserEntity> guildRequest;
    public GuildInfo()
    {
        guildUid = long.MinValue;
        guildName = string.Empty;
        guildCrews = new List<GuildCrew>();
        guildLeader = long.MinValue;
        guildRequest = new List<UserEntity>();
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
public enum Tier
{
    UNRANKED,
    BRONZE,
    SILVER,
    GOLD,
    PLATINUM,
    DIAMOND
}
public class RatingRange
{
    static readonly float BRONZE_MAX = 1000.0f;
    static readonly float SILVER_MIN = 1000.0f;
    static readonly float SILVER_MAX = 1500.0f;
    static readonly float GOLD_MIN = 1500.0f;
    static readonly float GOLD_MAX = 2000.0f;
    static readonly float PLATINUM_MIN = 2000.0f;
    static readonly float PLATINUM_MAX = 2500.0f;
    static readonly float DIAMOND_MIN = 2500.0f;



    public static Tier GetTier(float rating)
    {
        if (rating < BRONZE_MAX)
        {
            return Tier.BRONZE;
        }
        else if (rating >= SILVER_MIN && rating < SILVER_MAX)
        {
            return Tier.SILVER;
        }
        else if (rating >= GOLD_MIN && rating < GOLD_MAX)
        {
            return Tier.GOLD;
        }
        else if (rating >= PLATINUM_MIN && rating < PLATINUM_MAX)
        {
            return Tier.PLATINUM;
        }
        else if (rating >= DIAMOND_MIN)
        {
            return Tier.DIAMOND;
        }
        else
        {
            throw new ArgumentException("Invalid rating: " + rating);
        }
    }
}
internal class PlayerInfo
{
    public long UserUID { get; set; }
    public float rating { get; set; }
    public Socket Socket { get; set; }
    public bool HasAccepted { get; set; } = false;
}

public class PlayerRating
{
    public long UserUID;
    public float rating;
    public PlayerRating()
    {
        UserUID = 0;
        rating = 0;
    }
}
//인게임관련 여기에 접속한 플레이어정보 기입 
public class InGamePlayerInfo
{
    public long userUID;
    public int playerNumber;
    public bool isConnected;
    public InGamePlayerInfo()
    {
        isConnected = false;
    }
}