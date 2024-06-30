using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace GameServer.Controller
{

    internal class ClientController
    {
        private static ClientController instance;
        public static ClientController Instance
        {
            get
            {
                if (instance == null)
                {
                    if (instance == null)
                    {
                        instance = new ClientController();
                    }
                }
                return instance;
            }
        }
        public ConcurrentDictionary<UserEntity, Socket> connectedClients = new ConcurrentDictionary<UserEntity, Socket>();
        
        public void Init()
        {
            Console.WriteLine("ClientController init Complete");
        }
        public void RegisterUserConnection(long userUID, Socket user)
        {
            Task<UserEntity> CheckUser = MySQLController.Instance.UserSelect(userUID);
            CheckUser.ContinueWith((antecedent) =>
            {
                if(antecedent.Result.guildUID != 0) 
                {
                    GuildController.Instance.AddUserToGuildSession(antecedent.Result);
                }
                connectedClients[antecedent.Result] = user;
            });
        }
        public void DisconnectUserConnection(long userUID)
        {
            Task<UserEntity> CheckUser = MySQLController.Instance.UserSelect(userUID);
            CheckUser.ContinueWith((antecedent) =>
            {

                foreach (var kvp in connectedClients)
                {
                    if (kvp.Key.UserUID == userUID)
                    {
                        GuildController.Instance.RemoveUserFromGuildSession(antecedent.Result);
                        Console.WriteLine($"{CheckUser.Result.UserName} : disconnected.");
                        connectedClients.TryRemove(kvp.Key, out _);
                        break;
                    }
                }
            });
        }
        public void RemoveClient(Socket clientSocket)
        {
            foreach (var kvp in connectedClients)
            {
                if (kvp.Value == clientSocket)
                {
                    GuildController.Instance.RemoveUserFromGuildSession(kvp.Key);
                    Console.WriteLine($"Client with UID : {kvp.Key.UserUID} disconnected.");
                    connectedClients.TryRemove(kvp.Key, out _);
                    break;
                }
            }
        }

        public void SendToClient(Socket clientSocket, Packet packet)
        {
            if (ServerController.Instance.eventArgsPool.TryDequeue(out SocketAsyncEventArgs sendEventArg))
            {
                sendEventArg.SetBuffer(packet.buffer, 0, packet.position);
                sendEventArg.UserToken = clientSocket;

                if (!clientSocket.SendAsync(sendEventArg))
                {
                    ServerController.Instance.IO_Completed(this, sendEventArg);
                }
            }
        }

        public void SendToAllClients(Packet packet)
        {
            foreach (Socket clientSocket in connectedClients.Values)
            {
                SendToClient(clientSocket, packet);
            }
        }

        public void SendToSelectedClients(long[] users, Packet packet)
        {
            foreach (long userId in users)
            {
                // UserEntity에서 UserUID가 userId와 일치하는 것을 찾음
                UserEntity userEntity = connectedClients.Keys.FirstOrDefault(u => u.UserUID == userId);
                if (userEntity != null && connectedClients.TryGetValue(userEntity, out Socket clientSocket))
                {
                    SendToClient(clientSocket, packet);
                }
                else
                {
                    Console.WriteLine($"Client with UID : {userId} not found or disconnected.");
                }
            }
        }

    }
}
