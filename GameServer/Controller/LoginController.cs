using GameServer.Controller;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Org.BouncyCastle.Bcpg;

internal class LoginController
{
    private static LoginController instance;
    public static LoginController Instance
    {
        get
        {
            if (instance == null)
            {
                if (instance == null)
                {
                    instance = new LoginController();
                }
            }
            return instance;

        }
    }
    public void Init()
    {
        Console.WriteLine("LoginController init Complete");
    }
    public void ProcessLoginPacket(Socket clientSocket, byte[] realData)
    {
        //여기서 데이터 구분하고
        LoginRequestType loginRequestType = (LoginRequestType)realData[0];
        if (loginRequestType == LoginRequestType.SignupRequest)
        {
            //회원 가입
            SignUp(realData, clientSocket);
        }
        else if (loginRequestType == LoginRequestType.LoginRequest)
        {
            Login(realData, clientSocket);
        }
        else if(loginRequestType == LoginRequestType.LogoutRequest)
        {
            //로그아웃 작업
            Logout(realData, clientSocket);
        }
        else if (loginRequestType == LoginRequestType.DeleteRequest)
        {
            //회원 삭제 작업
        }
        else if(loginRequestType == LoginRequestType.UpdateRequest)
        {
            //회원정보 수정 작업
        }
    }
    private void SignUp(byte[] dataPacket , Socket clientSocket)
    { 
        byte[] NewdataPacket = new byte[dataPacket.Length - 1];
        for (int i = 0; i < NewdataPacket.Length; i++)
        {
            NewdataPacket[i] = (byte)dataPacket[i + 1];
        }
        string signUPInfoJSON = Encoding.UTF8.GetString(NewdataPacket);
        SignUpInfo signUPInfo = JsonConvert.DeserializeObject<SignUpInfo>(signUPInfoJSON);

        Task<bool> CheckUser = MySQLController.Instance.SignUpToDatabase(signUPInfo);
        CheckUser.ContinueWith((antecedent) =>
        {
            if (antecedent.Result)//회원가입이 성공한경우
            {
                int length = 0x01 + 0x01;

                Packet packet = new Packet();
                packet.push((byte)Protocol.Login);
                packet.push(length);
                packet.push((byte)LoginRequestType.SignupRequest);
                packet.push((byte)ResponseType.Success);

                ClientController.Instance.SendToClient(clientSocket, packet);
            }
            else//회원 가입이 실패한경우는 UserID중복밖에 없다
            {
                int length = 0x01 + 0x01;

                Packet packet = new Packet();
                packet.push((byte)Protocol.Login);
                packet.push(length);
                packet.push((byte)LoginRequestType.SignupRequest);
                packet.push((byte)ResponseType.Fail);

                ClientController.Instance.SendToClient(clientSocket, packet);
            }
        });
    }
    private void Logout(byte[] uid, Socket clientSocket)
    {
        long uidval = BitConverter.ToInt64(uid, 1);

        int length = 0x01+ 0x01;

        Packet packet = new Packet();
        packet.push((byte)Protocol.Login);
        packet.push(length);
        packet.push((byte)LoginRequestType.LogoutRequest);
        packet.push((byte)ResponseType.Success);

        ClientController.Instance.SendToClient(clientSocket, packet);

        ClientController.Instance.DisconnectUserConnection(uidval);
    }
    private void Login(byte[] dataPacket, Socket socket)
    {
        byte[] NewdataPacket = new byte[dataPacket.Length - 1];
        for(int i = 0; i < NewdataPacket.Length; i++) 
        {
            NewdataPacket[i] = (byte)dataPacket[i + 1];
        }
        string stridAndPW = Encoding.UTF8.GetString(NewdataPacket);
        LoginInfo idAndPW = JsonConvert.DeserializeObject<LoginInfo>(stridAndPW);
        
        Task<UserEntity> CheckUser = MySQLController.Instance.CheckUserIdInDatabase(idAndPW.ID);
        CheckUser.ContinueWith((antecedent) =>
        {
            if(antecedent.Result.Userid == idAndPW.ID)
            {
                if (antecedent.Result.UserPW == idAndPW.PW)
                {
                    Console.WriteLine($"{CheckUser.Result.UserName} connected.");

                    string strUserEntity = JsonConvert.SerializeObject(antecedent.Result);

                    int length = 0x01 + 0x01 + Utils.GetLength(strUserEntity);

                    ClientController.Instance.RegisterUserConnection(CheckUser.Result.UserUID, socket);

                    Packet packet = new Packet();
                    packet.push((byte)Protocol.Login);
                    packet.push(length);
                    packet.push((byte)LoginRequestType.LoginRequest);
                    packet.push((byte)ResponseType.Success);
                    packet.push(strUserEntity);

                    ClientController.Instance.SendToClient(socket, packet);
                }
                else
                {
                    Console.WriteLine($"password is incorrect");

                    int length = 0x01 + 0x01;

                    Packet packet = new Packet();
                    packet.push((byte)Protocol.Login);
                    packet.push(length);
                    packet.push((byte)LoginRequestType.LoginRequest);
                    packet.push((byte)ResponseType.Fail);
                    ClientController.Instance.SendToClient(socket, packet);
                }
            }
            else
            {
                Console.WriteLine($"id is incorrect");

                int length = 0x01 + 0x01;

                Packet packet = new Packet();
                packet.push((byte)Protocol.Login);
                packet.push(length);
                packet.push((byte)LoginRequestType.LoginRequest);
                packet.push((byte)ResponseType.Fail);
                ClientController.Instance.SendToClient(socket, packet);
            }
        });
    }
}
