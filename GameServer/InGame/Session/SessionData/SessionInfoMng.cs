using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

internal class SessionInfoMng
{
    public InGameSession m_inGameSession;
    public List<InGamePlayerInfo> inGamePlayerInfos;
    public SessionInfoMng(InGameSession inGameSession)
    {
        m_inGameSession = inGameSession;
        inGamePlayerInfos = new List<InGamePlayerInfo>();
    }
    public void ProcessSessionInfoPacket(byte[] realData, Socket client)
    {
        if ((SessionInfo)realData[0] == SessionInfo.SessionSyncOK)
        {
            CheckAllPlayerSyncOK(realData);
        }
        else if ((SessionInfo)realData[0] == SessionInfo.PlayerNum)
        {
            SendPlayerNumber(realData, client);
        }
    }
    private void CheckAllPlayerSyncOK(byte[] userUid)
    {
        long Useruid = BitConverter.ToInt64(userUid.Skip(1).ToArray());
        for (int i = 0; i < inGamePlayerInfos.Count; i++)
        {
            if (inGamePlayerInfos[i].userUID == Useruid)
            {
                inGamePlayerInfos[i].isConnected = true;
            }
        }
        // 여기에 모든 플레이어가 모두 연결되었는지 체크하는 로직
        bool allPlayersConnected = true;
        for (int i = 0; i < inGamePlayerInfos.Count; i++)
        {
            if (!inGamePlayerInfos[i].isConnected)
            {
                allPlayersConnected = false;
                break;
            }
        }

        if (allPlayersConnected)
        {
            // 모든 플레이어가 연결된 경우 수행할 동작
            OnAllPlayersConnected();
        }
    }
    private void OnAllPlayersConnected()
    {
        Packet packet = new Packet();

        int length = 0x01;

        packet.push((byte)InGameProtocol.SessionInfo);
        packet.push(length);
        packet.push((byte)SessionInfo.SessionSyncOK);

        m_inGameSession.SendToAllClient(packet);
    }
    private void SendPlayerNumber(byte[] uid, Socket client)
    {
        long useruid = BitConverter.ToInt64(uid.Skip(1).ToArray());
        for (int i = 0; i < inGamePlayerInfos.Count; i++)
        {
            if (inGamePlayerInfos[i].userUID == useruid)
            {
                Packet packet = new Packet();

                int length = 0x01 + Utils.GetLength(inGamePlayerInfos[i].playerNumber);

                packet.push(((byte)InGameProtocol.SessionInfo));
                packet.push(length);
                packet.push(((byte)SessionInfo.PlayerNum));
                packet.push(inGamePlayerInfos[i].playerNumber);

                m_inGameSession.SendToClient(client, packet);
            }
        }
    }
}
