using System.Net.Sockets;
using System.Net;
using System.Text;
using GameServer.Controller;

namespace GameServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ServerController.Instance.Start();
        }
    }
}
