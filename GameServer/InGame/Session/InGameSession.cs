using Org.BouncyCastle.Bcpg;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

internal class InGameSession
{
    public long SessionId { get; private set; }

    public GameType GameType { get; private set; }
    public IPEndPoint GameRoomEndPoint { get; private set; }
    public List<PlayerInfo> users;//접속한 플레이어관리
    private Socket listenSocket;
    private bool isRunning;
    private Thread sessionThread;
    private SemaphoreSlim maxConnectionsSemaphore = new SemaphoreSlim(2); // 세션 당 최대 연결 수
    private ConcurrentQueue<SocketAsyncEventArgs> eventArgsPool = new ConcurrentQueue<SocketAsyncEventArgs>();
    private ManualResetEvent sessionEndedEvent = new ManualResetEvent(false);
    private InGameWorld world;//인게임 세계 객체
    private SessionInfoMng sessionInfoMng;


    public InGameSession(long sessionId, GameType gameType)
    {
        SessionId = sessionId;
        GameType = gameType;
        users = new List<PlayerInfo>();
        world = new InGameWorld(this);
        sessionInfoMng = new SessionInfoMng(this);

        listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // SocketAsyncEventArgs 초기화
        for (int i = 0; i < 100; i++)
        {
            SocketAsyncEventArgs eventArg = new SocketAsyncEventArgs();
            eventArg.Completed += IO_Completed;
            byte[] buffer = new byte[4096];
            eventArg.SetBuffer(buffer, 0, buffer.Length);
            eventArgsPool.Enqueue(eventArg);
        }
    }

    public void StartSession()
    {
        isRunning = true;
        sessionEndedEvent.Reset();

        // 모든 네트워크 인터페이스에서 수신 대기
        string localIP = GetLocalIPAddress(); // 로컬 IP 주소로 설정
        listenSocket.Bind(new IPEndPoint(IPAddress.Parse(localIP), 0)); // 특정 IP 주소와 임의의 포트 사용
        listenSocket.Listen(100);
        GameRoomEndPoint = listenSocket.LocalEndPoint as IPEndPoint;

        Console.WriteLine($"Session {SessionId} started on IP {localIP}, port {GameRoomEndPoint.Port}");
        Console.WriteLine($"Listening on {listenSocket.LocalEndPoint.ToString()}");

        // 리스닝 및 업데이트를 하나의 쓰레드에서 처리
        sessionThread = new Thread(RunSession);
        sessionThread.Start();
    }


    public void StopSession()
    {
        isRunning = false;
        sessionEndedEvent.Set();
        Console.WriteLine($"Session {SessionId} stopped.");
        sessionThread.Join(); // 스레드 종료 대기
    }

    public void AddPlayer(PlayerInfo player)
    {
        users.Add(player);

        InGamePlayerInfo inGamePlayerInfo = new InGamePlayerInfo();
        inGamePlayerInfo.userUID = player.UserUID;
        inGamePlayerInfo.playerNumber = sessionInfoMng.inGamePlayerInfos.Count + 1;
        sessionInfoMng.inGamePlayerInfos.Add(inGamePlayerInfo);

        Character character = new Character();
        character.uid = player.UserUID;
        character.playerNum = inGamePlayerInfo.playerNumber;
        world.usersCharacter.Add(character);


        //Console.WriteLine($"Player {player.UserUID} added to session {SessionId}");

        // 클라이언트 소켓을 인게임 세션으로 넘기고, ReceiveAsync를 호출하여 인게임 세션에서 패킷을 받도록 함
        SocketAsyncEventArgs receiveEventArg = new SocketAsyncEventArgs();
        receiveEventArg.UserToken = player.Socket;
        receiveEventArg.SetBuffer(new byte[1024], 0, 1024);
        receiveEventArg.Completed += IO_Completed;

        if (!player.Socket.ReceiveAsync(receiveEventArg))
        {
            IO_Completed(this, receiveEventArg);
        }
    }

    private void RunSession()
    {
        // 비동기 소켓 연결 수락 시작
        Accept();

        // 20Hz 업데이트 타이머
        Timer updateTimer = new Timer(UpdateGameWorld, null, 0, 25); // 50ms 간격으로 업데이트 (20Hz)

        // 세션이 종료될 때까지 대기
        sessionEndedEvent.WaitOne();

        // 세션 종료 시 타이머 정리
        updateTimer.Dispose();
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
            Console.WriteLine($"Client connected to session {SessionId}: {clientSocket.RemoteEndPoint}");

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
        else
        {
            Console.WriteLine($"Error accepting client: {e.SocketError}");
        }

        e.AcceptSocket = null;
        if (isRunning)
        {
            Accept();
        }
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
                //clientSocket.Close();
                RemoveClient(clientSocket);
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
                RemoveClient(clientSocket);
                ReleaseEventArgs(e);
            }
        }
    }

    public void HandlePacket(Socket clientSocket, byte[] buffer, int offset, int count)
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
            case (byte)InGameProtocol.SessionInfo:
                sessionInfoMng.ProcessSessionInfoPacket(realData, clientSocket);
                break;
            case (byte)InGameProtocol.CharacterTr:
                world.UpdatePlayerTR(realData);
                break;
            default:
                break;
        }
    }

    private void RemoveClient(Socket clientSocket)
    {
        // 소켓이 닫히기 전에 EndPoint를 저장
        if(clientSocket == null)
        {
            return;
        }

        //string clientEndPoint = clientSocket.RemoteEndPoint?.ToString();

        // 소켓 닫기
        clientSocket.Close();

        // 사용자 리스트에서 제거
        users.RemoveAll(user => user.Socket == clientSocket);

        if(users.Count <= 0 )
        {
            SessionManager.Instance.RemoveSession(this.SessionId);
        }

        // 로그 출력
        //Console.WriteLine($"Client removed from session {SessionId}: {clientEndPoint}");
    }

    private void ReleaseEventArgs(SocketAsyncEventArgs e)
    {
        e.UserToken = null;
        maxConnectionsSemaphore.Release();
        eventArgsPool.Enqueue(e);
    }

    private void UpdateGameWorld(object state)
    {
        UpdateCharacterTR();
    }
    private void UpdateCharacterTR()
    {
        if (sessionInfoMng.isAllPlayerReady)
        {
            //모든유저가 완료되었을때실행

            foreach (var user in world.usersCharacter)
            {
                Packet characterTR = new Packet();
                // 위치와 회전 데이터를 패킷에 추가

                int length = 0x01 + Utils.GetLength(user.uid) + Utils.GetLength(user.playerNum) + 
                    Utils.GetLength(user.m_position.X) + Utils.GetLength(user.m_position.Y) + Utils.GetLength(user.m_position.Z) + 
                    Utils.GetLength(user.m_quaternion.X) + Utils.GetLength(user.m_quaternion.Y) + Utils.GetLength(user.m_quaternion.Z) + Utils.GetLength(user.m_quaternion.W);
                characterTR.push((byte)InGameProtocol.CharacterTr);
                characterTR.push(length);
                characterTR.push((byte)SessionInfo.TransformInfo);
                characterTR.push(user.uid);
                characterTR.push(user.playerNum);
                characterTR.push(user.m_position.X);
                characterTR.push(user.m_position.Y);
                characterTR.push(user.m_position.Z);
                characterTR.push(user.m_quaternion.X);
                characterTR.push(user.m_quaternion.Y);
                characterTR.push(user.m_quaternion.Z);
                characterTR.push(user.m_quaternion.W);
                SendToAllClient(characterTR);

                //Console.WriteLine($"User {user.uid} Position: X={user.m_position.X}, Y={user.m_position.Y}, Z={user.m_position.Z}");
            }
            // 클라이언트에 데이터 전송
        }
    }

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }
    //public void SendToClient(Socket clientSocket, Packet packet)
    //{
    //    SocketAsyncEventArgs sendEventArg;
    //    if (!eventArgsPool.TryDequeue(out sendEventArg))
    //    {
    //        // 풀에 사용 가능한 EventArgs가 없을 경우 새로 생성
    //        sendEventArg = new SocketAsyncEventArgs();
    //        sendEventArg.Completed += IO_Completed;
    //        sendEventArg.SetBuffer(new byte[4096], 0, 4096); // 또는 필요한 버퍼 크기 설정
    //    }

    //    sendEventArg.SetBuffer(packet.buffer, 0, packet.position);
    //    sendEventArg.UserToken = clientSocket;

    //    if (!clientSocket.SendAsync(sendEventArg))
    //    {
    //        IO_Completed(this, sendEventArg);
    //    }
    //    else
    //    {
    //        // 비동기 전송 시작이 성공적으로 이루어진 경우
    //        Console.WriteLine("Data sent asynchronously.");
    //    }
    //}
    public void SendToClient(Socket clientSocket, Packet packet)
    {
        if (eventArgsPool.TryDequeue(out SocketAsyncEventArgs sendEventArg))
        {
            sendEventArg.SetBuffer(packet.buffer, 0, packet.position);
            sendEventArg.UserToken = clientSocket;

            if (!clientSocket.SendAsync(sendEventArg))
            {
                IO_Completed(this, sendEventArg);
            }
        }
    }
    public void SendToAllClient(Packet packet)
    {
        foreach (var user in users)
        {
            SendToClient(user.Socket, packet);
        }
    }
}
