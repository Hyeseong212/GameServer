using System.Net.Sockets;
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

    public void Init()
    {
        Console.WriteLine("GuildController init Complete");
    }
    public void ProcessGuildPacket(Socket clientSocket, byte[] data)
    {
        GuildProtocol guildProtocol = (GuildProtocol)data[0];

        if (guildProtocol == GuildProtocol.IsUserGuildEnable)
        {
            //현재 길드 
        }
        else if (guildProtocol == GuildProtocol.SelectGuildName) 
        {
            //길드이름 받아서 길드이름 조회
        }
        else if (guildProtocol == GuildProtocol.SelectGuildCrew)
        {
            //현재 가입한 길드의 길드원들 조회
        }
        else if(guildProtocol == GuildProtocol.CreateGuild)
        {
            //길드 생성
        }
    }
}
