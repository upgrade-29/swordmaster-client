using System;
using System.Collections.Generic;
using System.Text;

// 서버 계약에 필요한 STOMP frame을 만들고 NUL 경계 수신 데이터를 누적 해석한다.
public sealed class StompFrameCodec
{
    private const char FrameTerminator = '\0';
    private const int MaximumBufferedTextLength = 256 * 1024;

    private readonly StringBuilder _receivedText = new StringBuilder();

    // WebSocket 연결 뒤 transport가 아닌 STOMP header로 인증 정보를 전달하는 CONNECT frame을 만든다.
    public string CreateConnect(string host, string accessToken)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("STOMP host가 필요합니다.", nameof(host));
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException("Access Token이 필요합니다.", nameof(accessToken));
        }

        return CreateOutgoingFrame(
            StompCommand.Connect,
            new[]
            {
                new KeyValuePair<string, string>("accept-version", "1.2"),
                new KeyValuePair<string, string>("host", host),
                new KeyValuePair<string, string>("Authorization", $"Bearer {accessToken}"),
                new KeyValuePair<string, string>("heart-beat", "0,0")
            },
            string.Empty);
    }

    // 연결 완료 뒤 service가 관리할 destination과 subscription ID를 frame으로 만든다.
    public string CreateSubscribe(string destination, string subscriptionId)
    {
        return CreateOutgoingFrame(
            StompCommand.Subscribe,
            new[]
            {
                new KeyValuePair<string, string>("id", subscriptionId),
                new KeyValuePair<string, string>("destination", destination)
            },
            string.Empty);
    }

    // 호출한 기능이 만든 JSON body를 destination으로 보내는 SEND frame을 만든다.
    public string CreateSend(string destination, string body)
    {
        return CreateOutgoingFrame(
            StompCommand.Send,
            new[]
            {
                new KeyValuePair<string, string>("destination", destination),
                new KeyValuePair<string, string>("content-type", "application/json")
            },
            body);
    }

    // 연결을 끝내기 전에 STOMP 종료 의사를 먼저 전달하는 frame을 만든다.
    public string CreateDisconnect()
    {
        return CreateOutgoingFrame(StompCommand.Disconnect, Array.Empty<KeyValuePair<string, string>>(), string.Empty);
    }

    // WebSocket message 경계와 무관하게 NUL로 완결된 수신 frame만 순서대로 꺼낸다.
    public IReadOnlyList<StompFrame> AppendReceivedText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<StompFrame>();
        }

        if (ContainsOnlyHeartbeat(_receivedText) == true)
        {
            _receivedText.Clear();
        }

        if (_receivedText.Length == 0)
        {
            text = text.TrimStart('\r', '\n');
            if (text.Length == 0)
            {
                return Array.Empty<StompFrame>();
            }
        }

        if (_receivedText.Length + text.Length > MaximumBufferedTextLength)
        {
            Reset();
            throw new InvalidOperationException("STOMP 수신 버퍼가 허용 크기를 초과했습니다.");
        }

        _receivedText.Append(text);
        List<StompFrame> frames = new List<StompFrame>();
        int frameEndIndex;

        while ((frameEndIndex = IndexOfFrameTerminator(_receivedText)) >= 0)
        {
            string rawFrame = _receivedText.ToString(0, frameEndIndex).TrimStart('\r', '\n');
            _receivedText.Remove(0, frameEndIndex + 1);

            if (string.IsNullOrWhiteSpace(rawFrame))
            {
                continue;
            }

            frames.Add(ParseIncomingFrame(rawFrame));
        }

        return frames;
    }

    // 새 연결은 이전 연결의 미완성 frame과 결합되지 않도록 수신 버퍼를 비운다.
    public void Reset()
    {
        _receivedText.Clear();
    }

    // 지원 범위 밖 command가 전송되지 않도록 최소 송신 frame 형식을 한 곳에서 구성한다.
    private static string CreateOutgoingFrame(
        StompCommand command,
        IReadOnlyList<KeyValuePair<string, string>> headers,
        string body)
    {
        if (IsOutgoingCommand(command) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(command));
        }

        StringBuilder frame = new StringBuilder(command.ToString().ToUpperInvariant());

        foreach (KeyValuePair<string, string> header in headers)
        {
            if (string.IsNullOrWhiteSpace(header.Key) || header.Value == null)
            {
                throw new ArgumentException("STOMP header가 올바르지 않습니다.", nameof(headers));
            }

            frame.Append('\n');
            frame.Append(EscapeHeader(header.Key));
            frame.Append(':');
            frame.Append(EscapeHeader(header.Value));
        }

        frame.Append("\n\n");
        frame.Append(body);
        frame.Append(FrameTerminator);
        return frame.ToString();
    }

    // 수신으로 허용한 CONNECTED, MESSAGE, ERROR만 구조화된 frame으로 변환한다.
    // 현재 MVP는 NUL 종료 text frame만 지원하므로 content-length 기반 binary body의 NUL 포함은 지원하지 않는다.
    private static StompFrame ParseIncomingFrame(string rawFrame)
    {
        int crlfHeaderBodySeparatorIndex = rawFrame.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        int lfHeaderBodySeparatorIndex = rawFrame.IndexOf("\n\n", StringComparison.Ordinal);
        int headerBodySeparatorIndex = crlfHeaderBodySeparatorIndex >= 0
            ? crlfHeaderBodySeparatorIndex
            : lfHeaderBodySeparatorIndex;
        int separatorLength = crlfHeaderBodySeparatorIndex >= 0 ? 4 : 2;
        string headerSection = headerBodySeparatorIndex >= 0 ? rawFrame.Substring(0, headerBodySeparatorIndex) : rawFrame;
        string body = headerBodySeparatorIndex >= 0 ? rawFrame.Substring(headerBodySeparatorIndex + separatorLength) : string.Empty;
        string[] headerLines = headerSection.Replace("\r\n", "\n").Split('\n');

        if (headerLines.Length == 0 || TryParseIncomingCommand(headerLines[0], out StompCommand command) == false)
        {
            throw new FormatException("지원하지 않는 수신 STOMP command입니다.");
        }

        Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.Ordinal);

        for (int index = 1; index < headerLines.Length; index++)
        {
            int separatorIndex = headerLines[index].IndexOf(':');

            if (separatorIndex <= 0)
            {
                throw new FormatException("STOMP header 형식이 올바르지 않습니다.");
            }

            string name = UnescapeHeader(headerLines[index].Substring(0, separatorIndex));
            string value = UnescapeHeader(headerLines[index].Substring(separatorIndex + 1));
            headers[name] = value;
        }

        return new StompFrame(command, headers, body);
    }

    // 누적 버퍼에서 아직 처리하지 않은 다음 frame 끝을 찾는다.
    private static int IndexOfFrameTerminator(StringBuilder value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] == FrameTerminator)
            {
                return index;
            }
        }

        return -1;
    }

    // frame 경계 밖의 CRLF/LF heartbeat는 다음 frame의 command에 결합하지 않도록 따로 소비한다.
    private static bool ContainsOnlyHeartbeat(StringBuilder value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] != '\r' && value[index] != '\n')
            {
                return false;
            }
        }

        return true;
    }

    // 수신 전용 command를 생성 API에서 사용하지 못하게 제한한다.
    private static bool IsOutgoingCommand(StompCommand command)
    {
        return command == StompCommand.Connect
            || command == StompCommand.Subscribe
            || command == StompCommand.Send
            || command == StompCommand.Disconnect;
    }

    // 서버 계약 밖 command는 상위 계층으로 전달하지 않고 protocol 오류로 구분한다.
    private static bool TryParseIncomingCommand(string value, out StompCommand command)
    {
        if (string.Equals(value, "CONNECTED", StringComparison.Ordinal) == true)
        {
            command = StompCommand.Connected;
            return true;
        }

        if (string.Equals(value, "MESSAGE", StringComparison.Ordinal) == true)
        {
            command = StompCommand.Message;
            return true;
        }

        if (string.Equals(value, "ERROR", StringComparison.Ordinal) == true)
        {
            command = StompCommand.Error;
            return true;
        }

        command = default;
        return false;
    }

    // header 구분 문자가 값에 섞여도 frame 구조가 깨지지 않게 STOMP 1.2 규칙으로 이스케이프한다.
    private static string EscapeHeader(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace(":", "\\c");
    }

    // 수신 header를 원래 값으로 복원하면서 지원하지 않는 escape는 protocol 오류로 처리한다.
    private static string UnescapeHeader(string value)
    {
        StringBuilder unescaped = new StringBuilder(value.Length);

        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] != '\\')
            {
                unescaped.Append(value[index]);
                continue;
            }

            if (index + 1 >= value.Length)
            {
                throw new FormatException("STOMP header escape 형식이 올바르지 않습니다.");
            }

            index++;
            switch (value[index])
            {
                case 'r':
                    unescaped.Append('\r');
                    break;
                case 'n':
                    unescaped.Append('\n');
                    break;
                case 'c':
                    unescaped.Append(':');
                    break;
                case '\\':
                    unescaped.Append('\\');
                    break;
                default:
                    throw new FormatException("지원하지 않는 STOMP header escape입니다.");
            }
        }

        return unescaped.ToString();
    }
}
