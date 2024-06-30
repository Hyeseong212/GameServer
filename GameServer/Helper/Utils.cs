using System.Text;
using System.Security.Cryptography;
internal class Utils
{
    public static int GetLength(int data)
    {
        return BitConverter.GetBytes(data).Length;
    }
    public static int GetLength(float data)
    {
        return BitConverter.GetBytes(data).Length;
    }
    public static int GetLength(string data)
    {
        return Encoding.UTF8.GetBytes(data).Length;
    }
    public static int GetLength(long data)
    {
        return BitConverter.GetBytes(data).Length;
    }
    public static int GetLength(bool data)
    {
        return BitConverter.GetBytes(data).Length;
    }
    public static int GetLength(byte data)
    {
        return 1;
    }

    public static string ComputeSha256Hash(string rawData)
    {
        // SHA256 객체 생성
        using (SHA256 sha256Hash = SHA256.Create())
        {
            // 입력 문자열을 바이트 배열로 변환하고 해시를 계산
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

            // 바이트 배열을 16진수 문자열로 변환
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
