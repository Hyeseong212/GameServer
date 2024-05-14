using System;
using System.Threading;

public class CThread
{
    private bool bStopThread;
    private Thread thread;
    public string ThreadName;

    // 생성자
    public CThread()
    {
        bStopThread = false;
        thread = null;
    }

    // 소멸자
    ~CThread()
    {
        Destroy();
    }

    // 스레드 생성 함수
    public bool Create(string name)
    {
        Console.WriteLine(name + "Created");
        ThreadName = name;
        try
        {
            thread = new Thread(ThreadFunc);
            thread.Start();
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e.GetType().Name}: {e.Message}");
            return false;
        }

        bStopThread = false;
        return true;
    }

    // 스레드 종료 함수
    public void Destroy()
    {
        bStopThread = true;
        Console.WriteLine($"{ThreadName} 스레드가 종료되었습니다");

        if (thread != null)
        {
            thread.Join(1000); // 스레드가 종료되길 기다립니다.
            thread = null;
        }
    }

    // 스레드 함수
    private void ThreadFunc()
    {
        try
        {
            while (!bStopThread)
            {
                ThreadUpdate();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e.GetType().Name}: {e.Message}");
        }
    }

    // 사용자가 구현하는 스레드 업데이트 함수
    protected virtual void ThreadUpdate()
    {
        Thread.Sleep(1000);
        Console.WriteLine($"{ThreadName} 스레드가 실행 중입니다.");
    }
}