using System;
using System.Collections;
using System.Net;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

// Uses NGO reliable named messages for the turn-based board protocol. No cell needs
// a NetworkObject: only commands and public match results cross the network.
public class GameNgoClient : MonoBehaviour
{
    const string CommandChannel = "Battleship.Command.v1";
    const string StateChannel = "Battleship.State.v1";
    const string ErrorChannel = "Battleship.Error.v1";
    enum Command : byte { Fleet, Shot, Pause, FinishAck }
    public TextMeshProUGUI statusText;
    public event Action<FleetSnapshot> StateChanged;
    public event Action<string> StatusChanged;
    public event Action<bool> ConnectionChanged;
    public FleetSnapshot State { get; private set; }
    public string Status { get; private set; } = "Создайте комнату или подключитесь к хосту.";
    public bool Connected => manager != null && manager.IsConnectedClient;
    public bool Busy => manager != null;
    public ulong LocalId => manager != null ? manager.LocalClientId : ulong.MaxValue;
    public bool CanShoot => Connected && State.Started && !State.Finished && !State.Paused && State.Turn == LocalId && !shotPending;
    NetworkManager manager;
    FleetMatch match;
    bool shotPending, ending, intentionalStop, finishAck;
    Coroutine connectTimeout;

    void Start()
    {
        SetStatus(Status);
        UIRuntimePolish.ShowConnectionMenu(this);
    }

    public void Host(string portText) => Begin(true, "127.0.0.1", portText);
    public void Join(string address, string portText) => Begin(false, address, portText);

    void Begin(bool host, string address, string portText)
    {
        if (Busy) return;
        if (!ushort.TryParse(portText, out ushort port) || port == 0)
        { SetStatus("Укажите порт от 1 до 65535."); return; }
        address = address.Trim();
        if (!host && (!IPAddress.TryParse(address, out IPAddress ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork))
        { SetStatus("Введите IPv4-адрес хоста, например 192.168.1.10."); return; }
        if (PlacementData.ShipCells.Count != 20)
        { SetStatus("Сначала расставьте флот из 20 клеток."); return; }
        if (NetworkManager.Singleton != null)
        { SetStatus("Другой NetworkManager уже активен. Завершите предыдущую сессию."); return; }
        intentionalStop = ending = finishAck = shotPending = false;
        State = default;
        match = host ? new FleetMatch() : null;
        GameObject root = new GameObject("Battleship NGO NetworkManager");
        UnityTransport transport = root.AddComponent<UnityTransport>();
        manager = root.AddComponent<NetworkManager>();
        manager.NetworkConfig = new NetworkConfig
        {
            NetworkTransport = transport,
            EnableSceneManagement = false,
            ConnectionApproval = true,
            ForceSamePrefabs = false,
            ProtocolVersion = 1,
            PlayerPrefab = null
        };
        transport.SetConnectionData(address, port, host ? "0.0.0.0" : null);
        manager.ConnectionApprovalCallback = Approve;
        manager.OnServerStarted += RegisterMessages;
        manager.OnClientStarted += RegisterMessages;
        manager.OnClientConnectedCallback += OnConnected;
        manager.OnClientDisconnectCallback += OnDisconnected;
        manager.OnTransportFailure += OnTransportFailure;
        SetStatus(host ? "Создание комнаты…" : "Подключение к хосту…");
        try
        {
            bool started = host ? manager.StartHost() : manager.StartClient();
            if (manager == null) return;
            if (!started) { StopSession("Не удалось запустить соединение. Проверьте адрес и порт."); return; }
            if (!Connected) connectTimeout = StartCoroutine(WaitForConnection());
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            StopSession("Не удалось подключиться. Проверьте адрес, порт и доступность хоста.");
        }
    }

    void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // Reserve the second slot at approval time, including pending connections.
        bool approved = match != null && match.AddPlayer(request.ClientNetworkId);
        response.Approved = approved;
        response.CreatePlayerObject = false;
        response.Pending = false;
        response.Reason = approved ? "" : "Комната рассчитана на двух игроков.";
    }

    void RegisterMessages()
    {
        manager.CustomMessagingManager.RegisterNamedMessageHandler(CommandChannel, ReceiveCommand);
        manager.CustomMessagingManager.RegisterNamedMessageHandler(StateChannel, ReceiveState);
        manager.CustomMessagingManager.RegisterNamedMessageHandler(ErrorChannel, ReceiveError);
    }

    void OnConnected(ulong id)
    {
        if (manager == null) return;
        if (manager.IsServer) match.AddPlayer(id);
        if (id == manager.LocalClientId)
        {
            if (connectTimeout != null) { StopCoroutine(connectTimeout); connectTimeout = null; }
            ConnectionChanged?.Invoke(true);
            using (var writer = new FastBufferWriter(256, Allocator.Temp))
            {
                writer.WriteValueSafe((byte)Command.Fleet);
                writer.WriteValueSafe(PlacementData.ShipCells.Count);
                foreach (Vector2Int cell in PlacementData.ShipCells)
                { writer.WriteValueSafe(cell.x); writer.WriteValueSafe(cell.y); }
                SendCommand(writer);
            }
        }
        if (manager.IsServer) BroadcastState();
    }

    public void Shoot(int x, int y)
    {
        if (!CanShoot) return;
        shotPending = true;
        using (var writer = new FastBufferWriter(32, Allocator.Temp))
        {
            writer.WriteValueSafe((byte)Command.Shot);
            writer.WriteValueSafe(x); writer.WriteValueSafe(y); writer.WriteValueSafe(State.Revision);
            SendCommand(writer);
        }
    }

    public void SetPaused(bool paused)
    {
        if (!Connected || State.Finished) return;
        using (var writer = new FastBufferWriter(8, Allocator.Temp))
        {
            writer.WriteValueSafe((byte)Command.Pause); writer.WriteValueSafe(paused);
            SendCommand(writer);
        }
    }

    void SendCommand(FastBufferWriter writer)
    {
        if (manager.IsServer)
        {
            using (var reader = new FastBufferReader(writer, Allocator.Temp))
                ReceiveCommand(manager.LocalClientId, reader);
        }
        else manager.CustomMessagingManager.SendNamedMessage(CommandChannel, NetworkManager.ServerClientId, writer, NetworkDelivery.ReliableSequenced);
    }

    void ReceiveCommand(ulong sender, FastBufferReader reader)
    {
        if (manager == null || !manager.IsServer || !manager.ConnectedClients.ContainsKey(sender)) return;
        try
        {
            reader.ReadValueSafe(out byte kind);
            string error = null;
            switch ((Command)kind)
            {
                case Command.Fleet:
                    reader.ReadValueSafe(out int count);
                    if (count != 20) { SendError(sender, "Во флоте должно быть ровно 20 клеток."); return; }
                    var cells = new Vector2Int[count];
                    for (int i = 0; i < count; i++)
                    {
                        reader.ReadValueSafe(out int x); reader.ReadValueSafe(out int y);
                        cells[i] = new Vector2Int(x, y);
                    }
                    if (!match.SubmitFleet(sender, cells, out error)) SendError(sender, error);
                    break;
                case Command.Shot:
                    reader.ReadValueSafe(out int shotX); reader.ReadValueSafe(out int shotY); reader.ReadValueSafe(out int revision);
                    if (!match.Shoot(sender, shotX, shotY, revision, out error)) SendError(sender, error);
                    break;
                case Command.Pause:
                    reader.ReadValueSafe(out bool paused);
                    match.SetPause(sender, paused);
                    break;
                case Command.FinishAck:
                    if (match.Finished && sender != NetworkManager.ServerClientId) finishAck = true;
                    return;
                default: SendError(sender, "Неизвестная команда."); return;
            }
            BroadcastState();
        }
        catch (Exception exception) when (exception is OverflowException || exception is ArgumentException || exception is IndexOutOfRangeException)
        { SendError(sender, "Повреждённое сетевое сообщение."); }
    }

    void BroadcastState()
    {
        foreach (ulong id in manager.ConnectedClientsIds)
        {
            var state = new FleetSnapshot
            {
                Revision = match.Revision, ShotSequence = match.ShotSequence,
                MyReady = match.Ready(id), OtherReady = match.OtherReady(id),
                Started = match.Started, Finished = match.Finished, Paused = match.Paused,
                OwnPause = match.OwnPause(id), Turn = match.Turn, Winner = match.Winner,
                Shooter = match.LastShooter, X = match.LastX, Y = match.LastY, Hit = match.LastHit
            };
            if (id == manager.LocalClientId) { ApplyState(state); continue; }
            using (var writer = new FastBufferWriter(128, Allocator.Temp))
            {
                writer.WriteValueSafe(state.Revision); writer.WriteValueSafe(state.ShotSequence);
                writer.WriteValueSafe(state.MyReady); writer.WriteValueSafe(state.OtherReady);
                writer.WriteValueSafe(state.Started); writer.WriteValueSafe(state.Finished);
                writer.WriteValueSafe(state.Paused); writer.WriteValueSafe(state.OwnPause);
                writer.WriteValueSafe(state.Turn); writer.WriteValueSafe(state.Winner); writer.WriteValueSafe(state.Shooter);
                writer.WriteValueSafe(state.X); writer.WriteValueSafe(state.Y); writer.WriteValueSafe(state.Hit);
                manager.CustomMessagingManager.SendNamedMessage(StateChannel, id, writer, NetworkDelivery.ReliableSequenced);
            }
        }
    }

    void ReceiveState(ulong sender, FastBufferReader reader)
    {
        if (sender != NetworkManager.ServerClientId || manager.IsServer) return;
        FleetSnapshot state = default;
        reader.ReadValueSafe(out state.Revision); reader.ReadValueSafe(out state.ShotSequence);
        reader.ReadValueSafe(out state.MyReady); reader.ReadValueSafe(out state.OtherReady);
        reader.ReadValueSafe(out state.Started); reader.ReadValueSafe(out state.Finished);
        reader.ReadValueSafe(out state.Paused); reader.ReadValueSafe(out state.OwnPause);
        reader.ReadValueSafe(out state.Turn); reader.ReadValueSafe(out state.Winner); reader.ReadValueSafe(out state.Shooter);
        reader.ReadValueSafe(out state.X); reader.ReadValueSafe(out state.Y); reader.ReadValueSafe(out state.Hit);
        ApplyState(state);
    }

    void ApplyState(FleetSnapshot state)
    {
        if (state.Revision < State.Revision) return;
        State = state;
        shotPending = false;
        StateChanged?.Invoke(state);
        SetStatus(state.Finished ? (state.Winner == LocalId ? "Победа!" : "Поражение") :
            !state.MyReady ? "Проверка расстановки…" : !state.OtherReady ? "Ожидание второго игрока…" :
            state.Paused ? (state.OwnPause ? "Вы приостановили матч" : "Соперник приостановил матч") :
            state.Turn == LocalId ? "Ваш ход" : "Ход противника");
        if (!state.Finished || ending) return;
        ending = true;
        if (!manager.IsServer)
        {
            using (var writer = new FastBufferWriter(1, Allocator.Temp))
            { writer.WriteValueSafe((byte)Command.FinishAck); SendCommand(writer); }
        }
        StartCoroutine(FinishMatch(state.Winner == LocalId));
    }

    void SendError(ulong id, string error)
    {
        error = error ?? "Действие отклонено хостом.";
        if (id == manager.LocalClientId) { shotPending = false; SetStatus(error); return; }
        using (var writer = new FastBufferWriter(1024, Allocator.Temp))
        {
            writer.WriteValueSafe(error);
            manager.CustomMessagingManager.SendNamedMessage(ErrorChannel, id, writer, NetworkDelivery.ReliableSequenced);
        }
    }

    void ReceiveError(ulong sender, FastBufferReader reader)
    {
        if (sender != NetworkManager.ServerClientId) return;
        reader.ReadValueSafe(out string error);
        shotPending = false;
        SetStatus(error);
    }

    IEnumerator WaitForConnection()
    {
        yield return new WaitForSecondsRealtime(15);
        connectTimeout = null;
        if (!Connected) StopSession("Хост не отвечает. Проверьте IPv4, UDP-порт и брандмауэр.");
    }

    IEnumerator FinishMatch(bool won)
    {
        yield return new WaitForSecondsRealtime(2);
        float deadline = Time.realtimeSinceStartup + 5;
        while (manager != null && manager.IsServer && !finishAck && Time.realtimeSinceStartup < deadline)
            yield return null;
        StopSession("");
        SceneManager.LoadScene(won ? "WIn" : "Defeat");
    }

    void OnDisconnected(ulong id)
    {
        if (intentionalStop || manager == null) return;
        if (manager.IsServer && (match == null || !match.HasPlayer(id))) return;
        if (ending) { finishAck = true; return; }
        string reason = manager.DisconnectReason;
        StopSession(string.IsNullOrEmpty(reason) ? "Соединение закрыто. Создайте комнату или подключитесь снова." : reason);
    }

    void OnTransportFailure() => StopSession("Ошибка транспорта. Порт может быть занят или недоступен.");
    public void Cancel() => StopSession("Соединение завершено.");

    void StopSession(string message)
    {
        intentionalStop = true;
        if (connectTimeout != null) { StopCoroutine(connectTimeout); connectTimeout = null; }
        NetworkManager old = manager;
        manager = null;
        if (old != null)
        {
            old.OnServerStarted -= RegisterMessages;
            old.OnClientStarted -= RegisterMessages;
            old.OnClientConnectedCallback -= OnConnected;
            old.OnClientDisconnectCallback -= OnDisconnected;
            old.OnTransportFailure -= OnTransportFailure;
            old.Shutdown();
            Destroy(old.gameObject);
        }
        match = null;
        shotPending = false;
        State = default;
        ConnectionChanged?.Invoke(false);
        if (!string.IsNullOrEmpty(message)) SetStatus(message);
    }

    void SetStatus(string text)
    {
        Status = text;
        if (statusText != null) statusText.text = text;
        StatusChanged?.Invoke(text);
    }

    void OnDestroy()
    {
        StateChanged = null;
        StatusChanged = null;
        ConnectionChanged = null;
        StopSession("");
    }
}
