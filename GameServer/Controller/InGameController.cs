using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Controller
{
    public class InGameControllerThread : CThread
    {
        // 클라이언트에게 보내거나 고속 계산이 필요할때 가져다 쓰자.
        protected override void ThreadUpdate()
        {
            Console.WriteLine($"{ThreadName} 스레드가 오버라이드되어 동작 중입니다.");
            Thread.Sleep(50);
        }
    }
    internal class InGameSession
    {
        public string sessionName;
        public List<long> users;
        public InGameSession()
        {
            sessionName = "";
            users = new List<long>();
        }
    }
    internal class InGameController
    {
        public InGameControllerThread inGameControllerThread;

        public string session;

        //여기서 
        internal InGameController(string SessionName)
        {
            session = SessionName;
            inGameControllerThread = new InGameControllerThread();
            inGameControllerThread.Create($"InGameThread-{SessionName}");
        }
    }


}
