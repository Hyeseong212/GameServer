using System.Numerics;

internal class Character
{
    public long uid;//사용하는 유저
    public string characterName;//어떤캐릭터인지
    public float HP;//캐릭터 HP
    public float MP;//캐릭터 MP
    public Vector3 m_position;// 캐릭터 위치
    public Quaternion m_quaternion;// 캐릭터 로테이션
    public bool isHit;//캐릭터가 맞았는지

    public Character()
    {
        uid = 0;
        characterName = "";
        HP = 0;
        MP = 0;
        m_position = Vector3.Zero;
        m_quaternion = Quaternion.Identity;
        isHit = false;
    }
}
