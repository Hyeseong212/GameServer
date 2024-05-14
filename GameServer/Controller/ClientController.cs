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
        public ConcurrentQueue<SocketAsyncEventArgs> eventArgsPool = new ConcurrentQueue<SocketAsyncEventArgs>();
        public ConcurrentDictionary<long, Socket> connectedClients = new ConcurrentDictionary<long, Socket>();
        
        public void Init()
        {
            Console.WriteLine("ClientController init Complete");
        }
        public void RegisterUserConnection(long userUID, Socket user)
        {
            //여기서 DB작업 추가
            Task<UserEntity> CheckUser = MySQLController.Instance.UserSelect(userUID);
            CheckUser.ContinueWith((antecedent) =>
            {
                connectedClients[userUID] = user;
            });
        }
        public void DisconnectUserConnection(long userUID)
        {
            Task<UserEntity> CheckUser = MySQLController.Instance.UserSelect(userUID);
            CheckUser.ContinueWith((antecedent) =>
            {
                foreach (var kvp in connectedClients)
                {
                    if (kvp.Key == userUID)
                    {
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

                    Console.WriteLine($"Client with UID : {kvp.Key} disconnected.");
                    connectedClients.TryRemove(kvp.Key, out _);
                    break;
                }
            }
        }

        public void SendToClient(Socket clientSocket, Packet packet)
        {
            if (eventArgsPool.TryDequeue(out SocketAsyncEventArgs sendEventArg))
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
            foreach (long user in users)
            {
                if (connectedClients.TryGetValue(user, out Socket clientSocket))
                {
                    SendToClient(clientSocket, packet);
                }
                else
                {
                    Console.WriteLine($"Client with UID : {user} not found or disconnected.");
                }
            }
        }

    }
}
