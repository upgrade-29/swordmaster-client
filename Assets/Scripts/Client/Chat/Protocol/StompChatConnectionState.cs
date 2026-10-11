// WebSocket 연결과 STOMP 인증 완료를 구분하는 최소 연결 상태다.
public enum StompChatConnectionState
{
    Disconnected,
    WebSocketConnecting,
    WebSocketConnected,
    StompConnecting,
    Connected,
    Disconnecting
}
