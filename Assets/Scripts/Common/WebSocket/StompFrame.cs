using System;
using System.Collections.Generic;

// 채팅 MVP가 송수신하는 최소 STOMP command 집합을 표현한다.
public enum StompCommand
{
    Connect,
    Connected,
    Subscribe,
    Send,
    Disconnect,
    Message,
    Error
}

// command, header, body를 분리한 최소 STOMP frame 표현이다.
public sealed class StompFrame
{
    public StompFrame(StompCommand command, IReadOnlyDictionary<string, string> headers, string body)
    {
        Command = command;
        Headers = headers ?? throw new ArgumentNullException(nameof(headers));
        Body = body ?? string.Empty;
    }

    public StompCommand Command { get; }

    public IReadOnlyDictionary<string, string> Headers { get; }

    public string Body { get; }

    public bool TryGetHeader(string name, out string value)
    {
        return Headers.TryGetValue(name, out value);
    }
}
