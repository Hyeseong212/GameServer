﻿public enum Protocol
{
    ///<summary>
    ///로그인 서버 관련 요청 패킷
    ///</summary>
    Login = 0x00,
    ///<summary>
    ///채팅 서버 관련 요청 패킷
    ///</summary>
    Chat = 0x16,
    ///<summary>
    ///길드 서버 관련 요청 패킷
    ///</summary>
    Guild = 0x17,
    ///<summary>
    ///매칭 서버 관련 요청 패킷
    ///</summary>
    Match = 0x32
}
public enum GuildProtocol
{
    ///<summary>
    ///유저 uid로 길드 유무검색 하는 프로토콜
    ///</summary>
    IsUserGuildEnable = 0x00,

    ///<summary>
    ///길드이름으로 검색하는 프로토콜
    ///</summary>
    SelectGuildName = 0x01,

    ///<summary>
    ///현재 가입한 길드에서 길드원 조회하는 프로토콜
    ///</summary>
    SelectGuildCrew = 0x02,

    ///<summary>
    ///길드 생성 프로토콜
    ///</summary>
    CreateGuild = 0x03,
    ///<summary>
    ///길드uid로 길드 검색하는 프로토콜
    ///</summary>
    SelectGuildUid = 0x04
}
public enum ChatStatus
{
    ///<summary>
    ///전체 채팅
    ///</summary>
    ENTIRE = 0x00,

    ///<summary>
    ///귓속말
    ///</summary>
    WHISPER = 0x01,

    ///<summary>
    ///길드
    ///</summary>
    GUILD = 0x02
}
public enum LoginRequestType
{
    ///<summary>
    /// 신규 회원가입 요청
    ///</summary>
    SignupRequest = 0x00,

    ///<summary>
    /// 기존 회원의 로그인 요청
    ///</summary>
    LoginRequest = 0x01,

    ///<summary>
    /// 기존 회원의 로그아웃 요청
    ///</summary>
    LogoutRequest = 0x02,

    ///<summary>
    /// 회원 삭제 요청
    ///</summary>
    DeleteRequest = 0x03,

    ///<summary>
    /// 회원 정보 수정 요청
    ///</summary>
    UpdateRequest = 0x04,

}

public enum ResponseType
{
    Success = 0x00,
    Fail = 0x01,
}