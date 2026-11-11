using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// STOMP over WebSocket 연결 하나를 유지합니다. 처음 사용할 때 싱글턴 오브젝트를 만들고 씬이 바뀌어도 유지합니다.
/// 기능은 연결하기 전에 Subscribe로 받을 destination을 등록하고, 연결이 비정상으로 끊기면 정해진 횟수만큼 다시 연결합니다.
/// </summary>
public class StompClient : SingletonMonoBehaviour<StompClient>
{
    private const int MaxReconnectAttempts = 3;

    private StompConnection connection;

    // 연결할 때마다 이 순서대로 구독하고, 받은 body를 destination별 콜백으로 넘긴다
    private readonly List<string> listDestination = new List<string>();
    private readonly Dictionary<string, Action<string>> dicMessageHandler = new Dictionary<string, Action<string>>();

    private string webSocketUrl;
    private string stompHost;
    private string accessToken;

    private int reconnectAttemptCount;
    private bool reportedReconnectFailure;
    private Coroutine corReconnect;

    public StompConnectionState State => connection.State;

    public event Action<StompConnectionState> OnStateChangedEvent = delegate { };
    public event Action OnDisconnectRequestedEvent = delegate { }; // 이미 끊겨 있어도 Disconnect를 부르면 항상 호출된다
    public event Action<string> OnErrorEvent = delegate { };
    public event Action<int, int> OnReconnectScheduledEvent = delegate { }; // 이번 시도 차례, 최대 시도 횟수
    public event Action OnReconnectFailedEvent = delegate { };

    protected override void OnAwakeSingleton()
    {
        DontDestroyOnLoad(gameObject);

        connection = new StompConnection(new NativeWebSocketStompSocket(), new StompFrameCodec());
        connection.StateChanged += OnConnectionStateChanged;
        connection.MessageReceived += OnConnectionMessageReceived;
        connection.TransportErrorReceived += OnConnectionErrorReceived;
        connection.ProtocolErrorReceived += OnConnectionErrorReceived;
    }

    protected override void OnDestroySingleton()
    {
        connection.StateChanged -= OnConnectionStateChanged;
        connection.MessageReceived -= OnConnectionMessageReceived;
        connection.TransportErrorReceived -= OnConnectionErrorReceived;
        connection.ProtocolErrorReceived -= OnConnectionErrorReceived;
        connection.Dispose();
    }

    // 연결할 때마다 구독할 destination과 그 destination으로 온 body를 받을 콜백을 등록합니다. 연결하기 전에 등록해야 합니다.
    public void Subscribe(string destination, Action<string> onMessage)
    {
        if (State != StompConnectionState.Disconnected)
        {
            throw new InvalidOperationException("연결하기 전에 구독할 destination을 등록해야 합니다.");
        }

        if (dicMessageHandler.ContainsKey(destination))
        {
            dicMessageHandler[destination] += onMessage;
            return;
        }

        listDestination.Add(destination);
        dicMessageHandler.Add(destination, onMessage);
    }

    // 이미 연결 중이거나 연결되어 있으면 아무것도 하지 않습니다. 재연결을 기다리는 중이면 기다리지 않고 바로 연결합니다.
    public void Connect(string webSocketUrl, string stompHost, string accessToken)
    {
        if (State != StompConnectionState.Disconnected)
        {
            return;
        }

        this.webSocketUrl = webSocketUrl;
        this.stompHost = stompHost;
        this.accessToken = accessToken;
        reconnectAttemptCount = 0;
        reportedReconnectFailure = false;

        StopReconnect();
        StartConnection();
    }

    // DISCONNECT를 보낸 뒤 소켓을 닫습니다. 이렇게 끊은 연결은 다시 연결하지 않습니다.
    public void Disconnect()
    {
        StopReconnect();
        accessToken = null;
        OnDisconnectRequestedEvent.Invoke();
        _ = DisconnectAsync();
    }

    public Task SendAsync(string destination, string body)
    {
        return connection.SendAsync(destination, body);
    }

    private void StartConnection()
    {
        if (State != StompConnectionState.Disconnected)
        {
            return;
        }

        _ = ConnectAsync();
    }

    // 연결 Task는 소켓이 닫힐 때까지 끝나지 않으므로 기다리지 않고, 예외만 오류 이벤트로 바꾼다
    private async Task ConnectAsync()
    {
        try
        {
            await connection.ConnectAsync(webSocketUrl, stompHost, accessToken, listDestination);
        }
        catch (Exception exception)
        {
            // 기다리는 동안 플레이를 끝내 싱글턴이 파괴됐을 수 있다
            if (this == null)
            {
                return;
            }

            OnErrorEvent.Invoke(exception.Message);
        }
    }

    // 종료 중의 오류는 StompConnection이 이미 오류 이벤트로 알렸으므로 로그만 남긴다
    private async Task DisconnectAsync()
    {
        try
        {
            await connection.DisconnectAsync();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[StompClient] 연결 종료 중 오류가 발생했습니다. {exception.Message}");
        }
    }

    private void StartReconnect()
    {
        if (corReconnect != null || string.IsNullOrEmpty(accessToken))
        {
            return;
        }

        if (reconnectAttemptCount >= MaxReconnectAttempts)
        {
            if (reportedReconnectFailure == false)
            {
                reportedReconnectFailure = true;
                OnReconnectFailedEvent.Invoke();
            }

            return;
        }

        corReconnect = StartCoroutine(ReconnectAfterDelay());
    }

    private void StopReconnect()
    {
        if (corReconnect != null)
        {
            StopCoroutine(corReconnect);
            corReconnect = null;
        }
    }

    // 1초, 2초, 4초 뒤에 다시 연결한다. 전투 배속에 영향을 받지 않도록 실제 시간으로 기다린다
    private IEnumerator ReconnectAfterDelay()
    {
        reconnectAttemptCount++;
        OnReconnectScheduledEvent.Invoke(reconnectAttemptCount, MaxReconnectAttempts);

        yield return new WaitForSecondsRealtime(1 << (reconnectAttemptCount - 1));

        // 연결이 바로 실패해 다음 재연결을 예약할 수 있도록 먼저 비운다
        corReconnect = null;
        StartConnection();
    }

    private void OnConnectionStateChanged(StompConnectionState state)
    {
        if (state == StompConnectionState.Connected)
        {
            reconnectAttemptCount = 0;
            reportedReconnectFailure = false;
        }

        OnStateChangedEvent.Invoke(state);

        // 비정상 종료 뒤에만 다시 연결한다. 서버가 거절한 연결은 다시 시도해도 같은 결과이므로 시도하지 않는다
        if (state == StompConnectionState.Disconnected
            && connection.IsNormalDisconnectRequested == false
            && connection.IsRejectedByServer == false)
        {
            StartReconnect();
        }
    }

    // 구독한 기능의 콜백이 예외를 던져도 연결은 유지하고 오류만 알린다
    private void OnConnectionMessageReceived(string destination, string body)
    {
        if (dicMessageHandler.TryGetValue(destination, out Action<string> onMessage) == false)
        {
            OnErrorEvent.Invoke($"구독하지 않은 destination의 메시지입니다. ({destination})");
            return;
        }

        try
        {
            onMessage.Invoke(body);
        }
        catch (Exception exception)
        {
            OnErrorEvent.Invoke(exception.Message);
        }
    }

    private void OnConnectionErrorReceived(string errorMessage)
    {
        OnErrorEvent.Invoke(errorMessage);
    }
}
