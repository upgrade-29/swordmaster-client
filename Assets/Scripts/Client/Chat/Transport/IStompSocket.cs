using System;
using System.Threading.Tasks;

// STOMP 해석 없이 WebSocket 연결과 텍스트 송수신만 제공하는 transport 경계다.
public interface IStompSocket
{
    bool IsOpen { get; }

    event Action Opened;
    event Action<string> MessageReceived;
    event Action<string> ErrorReceived;
    event Action Closed;

    // 지정한 WebSocket 주소 연결을 시작한다.
    Task ConnectAsync(string url);

    // 이미 구성된 STOMP frame 문자열을 WebSocket text message로 보낸다.
    Task SendTextAsync(string message);

    // transport 종료를 요청한다.
    Task CloseAsync();
}
