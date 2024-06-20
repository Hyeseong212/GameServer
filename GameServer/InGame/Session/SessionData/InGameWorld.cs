using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

internal class InGameWorld
{
    public InGameSession m_inGameSession;
    public List<Character> usersCharacter;
    public InGameWorld(InGameSession inGameSession)
    {
        m_inGameSession = inGameSession;
        usersCharacter = new List<Character>();
    }

    public void UpdatePlayerTR(byte[] data)
    {
        long userUID = BitConverter.ToInt64(data,0);
        for (int i = 0; i < usersCharacter.Count; i++)
        {
            if (usersCharacter[i].uid == userUID)
            {
                usersCharacter[i].m_position = new Vector3(BitConverter.ToSingle(data, 8), BitConverter.ToSingle(data, 12), BitConverter.ToSingle(data, 16));
                usersCharacter[i].m_quaternion = new Quaternion(BitConverter.ToSingle(data, 20), BitConverter.ToSingle(data, 24), BitConverter.ToSingle(data, 28), BitConverter.ToSingle(data, 32));

                // 위치 로그 출력
                Console.WriteLine($"User {usersCharacter[i].uid} Position: X={usersCharacter[i].m_position.X}, Y={usersCharacter[i].m_position.Y}, Z={usersCharacter[i].m_position.Z}");
                //Console.WriteLine($"User {usersCharacter[i].uid} Rotation: X={usersCharacter[i].m_quaternion.x}, Y={usersCharacter[i].m_quaternion.y}, Z={usersCharacter[i].m_quaternion.z}, W={usersCharacter[i].m_quaternion.w}");
            }
        }
    }
}