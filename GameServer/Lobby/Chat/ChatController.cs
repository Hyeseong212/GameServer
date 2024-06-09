using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Controller
{
    internal class ChatController
    {
        private static ChatController instance;
        public static ChatController Instance
        {
            get
            {
                if (instance == null)
                {
                    if (instance == null)
                    {
                        instance = new ChatController();
                    }
                }
                return instance;
            }
        }
        public void Init()
        {
            Console.WriteLine("ChatController Init Complete");
        }
        public void ProcessChatPacket(Socket clientSocket, byte[] data)
        {
            // uid걸러내기
            ChatStatus chatstatus = (ChatStatus)data[0];

            if(chatstatus == ChatStatus.ENTIRE)
            {
                byte[] uidandText = data.Skip(1).ToArray();
                EntireChat(uidandText);
            }
            else if(chatstatus == ChatStatus.WHISPER) //로그인 시스템 필요 현재 소켓과 IP로 구분하는 구조를 소켓과 해당 클라이언트에서 접속한 유저의 유저UID로 바꾸어야됨
            {
                byte[] uidandText = data.Skip(1).ToArray();
                WhisperChat(uidandText);
            }
            else if (chatstatus == ChatStatus.GUILD)
            {
                byte[] uidandText = data.Skip(1).ToArray();
                GuildChat(uidandText);
            }
            else
            {
            }
        }

        public void EntireChat(byte[] uidandText)
        {
            byte[] uid = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                uid[i] = uidandText[i];
            }
            long uidval = BitConverter.ToInt64(uid);

            Task<UserEntity> CheckUser = MySQLController.Instance.UserSelect(uidval);

            CheckUser.ContinueWith((antecedent) =>
            {
                string receivedText = Encoding.UTF8.GetString(uidandText, uid.Length, uidandText.Length - uid.Length);

                int length = 0x01 + Encoding.UTF8.GetBytes(CheckUser.Result.UserName).Length + Encoding.UTF8.GetBytes(" : ").Length + (uidandText.Length - uid.Length);

                var sendData = new Packet();
                sendData.push((byte)Protocol.Chat);
                sendData.push(length);
                sendData.push((byte)ChatStatus.ENTIRE);
                sendData.push(CheckUser.Result.UserName);
                sendData.push(" : ");
                sendData.push(receivedText);

                Console.WriteLine($"{Encoding.UTF8.GetString(sendData.buffer, 6, sendData.position)}");

                string str = "";
                for (int i = 5; i < sendData.position; i++)
                {
                    if (i != sendData.position - 1) str += sendData.buffer[i] + "|";
                    else str += sendData.buffer[i];
                }
                ClientController.Instance.SendToAllClients(sendData);
            });
        }
        public void WhisperChat(byte[] uidandText)
        {
            byte[] senduid = new byte[8];
            byte[] receivinguid = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                senduid[i] = uidandText[i];
            }
            for (int i = 8; i < 16; i++)
            {
                receivinguid[i - 8] = uidandText[i];
            }
            long sendUseruidval = BitConverter.ToInt64(senduid);
            long receivingUseruidval = BitConverter.ToInt64(receivinguid);

            //여기서 uid로 DB체크한번 하고~
            Task<UserEntity> CheckUser = MySQLController.Instance.UserSelect(sendUseruidval);

            CheckUser.ContinueWith((antecedent) =>
            {
                string receivedText = Encoding.UTF8.GetString(uidandText, senduid.Length + receivinguid.Length, uidandText.Length - senduid.Length - receivinguid.Length);

                int length =0x01+ Encoding.UTF8.GetBytes(CheckUser.Result.UserName.ToString()).Length + Encoding.UTF8.GetBytes(" : ").Length + (uidandText.Length - senduid.Length - receivinguid.Length);

                var sendData = new Packet();
                sendData.push((byte)Protocol.Chat);
                sendData.push(length);
                sendData.push((byte)ChatStatus.WHISPER);
                sendData.push(CheckUser.Result.UserName.ToString());
                sendData.push(" : ");
                sendData.push(receivedText);

                long[] uids = new long[2];
                uids[0] = sendUseruidval;
                uids[1] = receivingUseruidval;
                Console.WriteLine($"{Encoding.UTF8.GetString(sendData.buffer, 6, sendData.position)}");
                ClientController.Instance.SendToSelectedClients(uids, sendData);
            });
        }
        public void GuildChat(byte[] uidandText) 
        {
            byte[] sendUseruid = new byte[8];
            byte[] guilduid = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                sendUseruid[i] = uidandText[i];
            }
            for (int i = 8; i < 16; i++)
            {
                guilduid[i-8] = uidandText[i];
            }
            long sendUseruidval = BitConverter.ToInt64(sendUseruid);
            long Guilduidval = BitConverter.ToInt64(guilduid);
            string receivedText = Encoding.UTF8.GetString(uidandText, sendUseruid.Length + guilduid.Length, uidandText.Length - sendUseruid.Length - guilduid.Length);

            List<UserEntity> onlineUsers = GuildController.Instance.GetOnlineGuildMembers(Guilduidval);

            long[] userUids = new long[onlineUsers.Count];

            for(int i = 0; i < onlineUsers.Count ; i++)
            {
                userUids[i] = onlineUsers[i].UserUID;
            }

            Task<UserEntity> CheckUser = MySQLController.Instance.UserSelect(sendUseruidval);

            CheckUser.ContinueWith((antecedent) =>
            {

                int length = 0x01 + Encoding.UTF8.GetBytes(CheckUser.Result.UserName.ToString()).Length + Encoding.UTF8.GetBytes(" : ").Length + (uidandText.Length - sendUseruid.Length - guilduid.Length); ;

                Packet sendData = new Packet();
                sendData.push((byte)Protocol.Chat);
                sendData.push(length);
                sendData.push((byte)ChatStatus.GUILD);
                sendData.push(CheckUser.Result.UserName.ToString());
                sendData.push(" : ");
                sendData.push(receivedText);

                Console.WriteLine($"{Encoding.UTF8.GetString(sendData.buffer, 6, sendData.position)}");

                ClientController.Instance.SendToSelectedClients(userUids, sendData);
            });
        }
    }
}
