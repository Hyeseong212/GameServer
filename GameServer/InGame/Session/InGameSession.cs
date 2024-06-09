using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

internal class InGameSession
{
    public long SessionId { get; private set; }
    public GameType GameType { get; private set; }
    private List<PlayerInfo> users;
    private bool isRunning;
    private Thread sessionThread;
    private SemaphoreSlim maxConnectionsSemaphore = new SemaphoreSlim(2); // 세션 당 최대 연결 수
    private ConcurrentQueue<SocketAsyncEventArgs> eventArgsPool = new ConcurrentQueue<SocketAsyncEventArgs>();

    public InGameSession(long sessionId, GameType gameType)
    {
        SessionId = sessionId;
        GameType = gameType;
        users = new List<PlayerInfo>();

        // SocketAsyncEventArgs 초기화
        for (int i = 0; i < 100; i++)
        {
            SocketAsyncEventArgs eventArg = new SocketAsyncEventArgs();
            eventArg.Completed += IO_Completed;
            byte[] buffer = new byte[1024];
            eventArg.SetBuffer(buffer, 0, buffer.Length);
            eventArgsPool.Enqueue(eventArg);
        }
    }

    public void StartSession()
    {
        isRunning = true;
        Console.WriteLine($"Session {SessionId} started.");

        // 리스닝 및 업데이트를 하나의 쓰레드에서 처리
        sessionThread = new Thread(RunSession);
        sessionThread.Start();
    }

    public void StopSession()
    {
        isRunning = false;
        Console.WriteLine($"Session {SessionId} stopped.");
    }

    public void AddPlayer(PlayerInfo player)
    {
        users.Add(player);
        Console.WriteLine($"Player {player.UserUID} added to session {SessionId}");

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
        // 20Hz 업데이트 타이머
        Timer updateTimer = new Timer(UpdateGameWorld, null, 0, 50); // 50ms 간격으로 업데이트 (20Hz)

        while (isRunning)
        {
            Thread.Sleep(100); // 쓰레드 과부하 방지를 위해 잠시 대기
        }

        updateTimer.Dispose();
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
                RemoveClient(clientSocket);
                ReleaseEventArgs(e);
            }
        }
        else if (e.LastOperation == SocketAsyncOperation.Send)
        {
            if (e.SocketError != SocketError.Success)
            {
                Console.WriteLine("Error while sending data.");
                clientSocket.Close();
                RemoveClient(clientSocket);
                ReleaseEventArgs(e);
            }
        }
    }

    public void HandlePacket(Socket clientSocket, byte[] buffer, int offset, int count)
    {
        // 패킷 처리 로직 추가
        Console.WriteLine($"Received data from client in session {SessionId}: {BitConverter.ToString(buffer, offset, count)}");
    }

    private void RemoveClient(Socket clientSocket)
    {
        users.RemoveAll(user => user.Socket == clientSocket);
        Console.WriteLine($"Client removed from session {SessionId}: {clientSocket.RemoteEndPoint}");
    }

    private void ReleaseEventArgs(SocketAsyncEventArgs e)
    {
        e.UserToken = null;
        maxConnectionsSemaphore.Release();
        eventArgsPool.Enqueue(e);
    }

    private void UpdateGameWorld(object state)
    {
        // 20Hz로 클라이언트로 데이터 전송
        foreach (var user in users)
        {
            // 각 사용자에게 업데이트 데이터 전송
            // NetworkStream.Write 등을 이용하여 데이터 전송 로직 추가
        }

        // Console.WriteLine($"Session {SessionId} world updated.");
    }
}
