using GameServer.Controller;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

class ServerController
{
    private static ServerController instance;
    public static ServerController Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new ServerController(IPAddress.Any, 9000);
            }
            return instance;
        }
    }

    private Socket listenSocket;
    private const int maxConnections = 1000;
    private int activeConnections = 0;

    public ConcurrentQueue<SocketAsyncEventArgs> eventArgsPool = new ConcurrentQueue<SocketAsyncEventArgs>();
    private SemaphoreSlim maxConnectionsSemaphore = new SemaphoreSlim(maxConnections);


    public ServerController(IPAddress ip, int port)
    {
        listenSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        listenSocket.Bind(new IPEndPoint(ip, port));
        listenSocket.Listen(100);

        for (int i = 0; i < maxConnections; i++)
        {
            SocketAsyncEventArgs eventArg = new SocketAsyncEventArgs();
            eventArg.Completed += IO_Completed;
            byte[] buffer = new byte[Packet.buffersize];
            eventArg.SetBuffer(buffer, 0, buffer.Length);
            eventArgsPool.Enqueue(eventArg);
        }
    }

    public void Start()
    {
        ChatController.Instance.Init();
        ClientController.Instance.Init();
        GuildController.Instance.Init();
        LoginController.Instance.Init();
        MatchController.Instance.Init();
        MySQLController.Instance.Init();

        //모든 컨트롤러 이니셜라이징 작업

        Console.WriteLine("\r\nServer is listening...");
        Accept();
        Console.ReadLine();
    }

    private void Accept()
    {
        SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs();
        acceptEventArg.Completed += AcceptCompleted;

        if (!listenSocket.AcceptAsync(acceptEventArg))
        {
            AcceptCompleted(this, acceptEventArg);
        }
    }

    private void AcceptCompleted(object sender, SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success)
        {
            Socket clientSocket = e.AcceptSocket;
            Console.WriteLine($"Client connected: {clientSocket.RemoteEndPoint}");

            Interlocked.Increment(ref activeConnections);
            maxConnectionsSemaphore.Wait();

            if (eventArgsPool.TryDequeue(out SocketAsyncEventArgs receiveEventArg))
            {
                receiveEventArg.UserToken = clientSocket;

                if (!clientSocket.ReceiveAsync(receiveEventArg))
                {
                    IO_Completed(this, receiveEventArg);
                }
            }
        }

        e.AcceptSocket = null;
        Accept();
    }

    public void IO_Completed(object sender, SocketAsyncEventArgs e)
    {
        Socket clientSocket = (Socket)e.UserToken;

        if (e.LastOperation == SocketAsyncOperation.Receive)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                HandlePacket(clientSocket, e.Buffer, e.Offset, e.BytesTransferred);

                if (!clientSocket.ReceiveAsync(e))
                {
                    IO_Completed(this, e);
                }
            }
            else
            {
                clientSocket.Close();
                ClientController.Instance.RemoveClient(clientSocket);
                ReleaseEventArgs(e);
            }
        }
        else if (e.LastOperation == SocketAsyncOperation.Send)
        {
            if (e.SocketError == SocketError.Success)
            {
                // 전송 성공 후 추가 작업 가능

                // 이벤트 인스턴스를 다시 풀에 반환
                ReleaseEventArgs(e);
            }
            else
            {
                // 전송 실패 처리
                clientSocket.Close();
                ClientController.Instance.RemoveClient(clientSocket);
                ReleaseEventArgs(e);
            }
        }
    }

    private void HandlePacket(Socket clientSocket, byte[] buffer, int offset, int count)
    {
        byte protocol = buffer[0];
        byte[] lengthBytes = new byte[4];

        try
        {
            for (int i = 0; i < 4; i++)
            {
                lengthBytes[i] = buffer[i + 1];
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        int length = BitConverter.ToInt32(lengthBytes, 0);
        count -= 5;
        byte[] realData = new byte[length];

        try
        {
            for (int i = 0; i < count; i++)
            {
                realData[i] = buffer[i + 5];
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        switch (protocol)
        {
            case (byte)Protocol.Login:
                LoginController.Instance.ProcessLoginPacket(clientSocket, realData);
                break;
            case (byte)Protocol.Chat:
                ChatController.Instance.ProcessChatPacket(clientSocket, realData);
                break;
            case (byte)Protocol.Guild:
                GuildController.Instance.ProcessGuildPacket(clientSocket, realData);
                break;
            case (byte)Protocol.Match:
                MatchController.Instance.ProcessMatchPacket(clientSocket, realData);
                break;
            default:
                break;
        }
    }
    private void ReleaseEventArgs(SocketAsyncEventArgs e)
    {
        e.UserToken = null;
        maxConnectionsSemaphore.Release();
        eventArgsPool.Enqueue(e);
    }
    public void TransferSocketToGameSession(Socket clientSocket)
    {

    }
}
