using FishNet;
using FishNet.Transporting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private TMP_InputField _serverAddressInput;

    private bool _waitingForClientStart;
    private bool _waitingForServerStart;
    private bool _waitingForHostClientStart;

    public static string PlayerNickname { get; private set; } = "Player";
    public static string ServerAddress { get; private set; } = "localhost";
    public static ushort ServerPort { get; private set; } = 7770;

    private void Awake()
    {
        EnsureServerAddressInput();
        ApplyAddressDefault();
    }

    public void StartAsHost()
    {
        SaveNickname();
        ConfigurePortFromArgs();
        ServerPort = InstanceFinder.TransportManager.Transport.GetPort();
        SetClientAddress("localhost");
        UnsubscribeFromConnectionState();
        SubscribeToConnectionState();

        _waitingForServerStart = true;
        _waitingForHostClientStart = true;

        bool serverStarted = InstanceFinder.ServerManager.StartConnection();
        bool clientStarted = InstanceFinder.ClientManager.StartConnection();

        Debug.Log($"Host start requested: server={serverStarted}, client={clientStarted}");
        if (!serverStarted || !clientStarted)
            Debug.LogWarning("Host connection start returned false.");
    }

    public void StartAsClient()
    {
        SaveNickname();
        ConfigureClientAddress();
        UnsubscribeFromConnectionState();
        SubscribeToConnectionState();

        _waitingForClientStart = true;

        bool started = InstanceFinder.ClientManager.StartConnection(ServerAddress, ServerPort);
        Debug.Log($"Client start requested: {started}");
        if (!started)
            Debug.LogWarning("Client connection start returned false.");
    }

    private void SaveNickname()
    {
        string rawValue = _nicknameInput != null ? _nicknameInput.text : string.Empty;
        PlayerNickname = string.IsNullOrWhiteSpace(rawValue) ? "Player" : rawValue.Trim();
        Debug.Log($"Nickname saved: {PlayerNickname}");
    }

    private void ConfigureClientAddress()
    {
        string address = _serverAddressInput != null ? _serverAddressInput.text : string.Empty;

        if (string.IsNullOrWhiteSpace(address))
            address = NetworkLaunchArgs.GetString("address", "connectAddress", "ip", "host");

        if (string.IsNullOrWhiteSpace(address))
            address = PlayerPrefs.GetString("Practice4ServerAddress", "localhost");

        address = address.Trim();
        SplitAddressAndPort(address, out string host, out ushort? port);

        ServerAddress = string.IsNullOrWhiteSpace(host) ? "localhost" : host;
        PlayerPrefs.SetString("Practice4ServerAddress", ServerAddress);
        SetClientAddress(ServerAddress);

        if (port.HasValue)
            InstanceFinder.TransportManager.Transport.SetPort(port.Value);
        else
            ConfigurePortFromArgs();

        ServerPort = InstanceFinder.TransportManager.Transport.GetPort();
        Debug.Log($"Client target saved: {ServerAddress}:{ServerPort}");
    }

    private void ConfigurePortFromArgs()
    {
        if (NetworkLaunchArgs.TryGetUShort("port", out ushort port))
            InstanceFinder.TransportManager.Transport.SetPort(port);
    }

    private static void SetClientAddress(string address)
    {
        if (InstanceFinder.TransportManager != null && InstanceFinder.TransportManager.Transport != null)
            InstanceFinder.TransportManager.Transport.SetClientAddress(address);
    }

    private static void SplitAddressAndPort(string value, out string host, out ushort? port)
    {
        host = value;
        port = null;

        if (string.IsNullOrWhiteSpace(value))
            return;

        int colonIndex = value.LastIndexOf(':');
        bool singleColon = colonIndex > 0 && value.IndexOf(':') == colonIndex;

        if (!singleColon)
            return;

        string portText = value.Substring(colonIndex + 1);
        if (!ushort.TryParse(portText, out ushort parsedPort))
            return;

        host = value.Substring(0, colonIndex);
        port = parsedPort;
    }

    private void EnsureServerAddressInput()
    {
        if (_serverAddressInput != null || _nicknameInput == null)
            return;

        RectTransform nicknameRect = _nicknameInput.GetComponent<RectTransform>();
        if (nicknameRect == null || nicknameRect.parent == null)
            return;

        GameObject inputObject = Instantiate(_nicknameInput.gameObject, nicknameRect.parent);
        inputObject.name = "ServerAddressInput";

        RectTransform addressRect = inputObject.GetComponent<RectTransform>();
        addressRect.anchoredPosition = nicknameRect.anchoredPosition + new Vector2(0f, -42f);

        _serverAddressInput = inputObject.GetComponent<TMP_InputField>();
        _serverAddressInput.text = string.Empty;
        SetPlaceholderText(_serverAddressInput, "IP или DNS сервера");

        ShiftButtonsBelowAddress(nicknameRect.parent, nicknameRect.anchoredPosition.y);
    }

    private void ApplyAddressDefault()
    {
        if (_serverAddressInput == null)
            return;

        string address = NetworkLaunchArgs.GetString("address", "connectAddress", "ip", "host");

        if (string.IsNullOrWhiteSpace(address))
            address = PlayerPrefs.GetString("Practice4ServerAddress", "localhost");

        _serverAddressInput.text = address;
    }

    private static void SetPlaceholderText(TMP_InputField input, string value)
    {
        if (input.placeholder is TMP_Text placeholder)
            placeholder.text = value;
    }

    private static void ShiftButtonsBelowAddress(Transform parent, float nicknameY)
    {
        foreach (Button button in parent.GetComponentsInChildren<Button>(includeInactive: true))
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null && rect.anchoredPosition.y < nicknameY)
                rect.anchoredPosition += new Vector2(0f, -45f);
        }
    }

    private void SubscribeToConnectionState()
    {
        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;

        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnServerConnectionState += OnServerConnectionState;
    }

    private void UnsubscribeFromConnectionState()
    {
        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.OnClientConnectionState -= OnClientConnectionState;

        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnServerConnectionState -= OnServerConnectionState;
    }

    private void OnDestroy()
    {
        UnsubscribeFromConnectionState();
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (!_waitingForClientStart && !_waitingForHostClientStart)
            return;

        Debug.Log($"Client state: {args.ConnectionState}");

        if (args.ConnectionState == LocalConnectionState.Started)
        {
            _waitingForClientStart = false;
            _waitingForHostClientStart = false;
            TryHideUI();
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            _waitingForClientStart = false;
            _waitingForHostClientStart = false;
            Debug.LogWarning("Client stopped before starting. Check IP, port, and firewall.");
        }
    }

    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
        if (!_waitingForServerStart)
            return;

        Debug.Log($"Server state: {args.ConnectionState}");

        if (args.ConnectionState == LocalConnectionState.Started)
        {
            _waitingForServerStart = false;
            TryHideUI();
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            _waitingForServerStart = false;
            Debug.LogWarning("Server stopped before starting.");
        }
    }

    private void TryHideUI()
    {
        if (_waitingForClientStart || _waitingForServerStart || _waitingForHostClientStart)
            return;

        gameObject.SetActive(false);
    }
}
