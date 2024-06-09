using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

internal class SessionManager
{
    private static SessionManager instance;
    public static SessionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new SessionManager();
            }
            return instance;
        }
    }

    private ConcurrentDictionary<long, InGameSession> createdSessions = new ConcurrentDictionary<long, InGameSession>();

    public void Init()
    {
        Console.WriteLine($"{this.ToString()} init Complete");
    }

    public void InGameSessionCreate(List<PlayerInfo> users, GameType gameType)
    {
        long sessionId = DateTime.Now.Ticks;
        InGameSession newSession = new InGameSession(sessionId, gameType);
        createdSessions.TryAdd(sessionId, newSession);

        // 새로운 세션 시작
        newSession.StartSession();

        foreach (var player in users)
        {
            newSession.AddPlayer(player);
        }

        Console.WriteLine($"In-game session created for matched players. GameType: {gameType}");
    }

    public InGameSession GetSession(long sessionId)
    {
        if (createdSessions.TryGetValue(sessionId, out InGameSession session))
        {
            return session;
        }
        return null;
    }
}
