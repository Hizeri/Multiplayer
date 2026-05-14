using System.Collections;
using System.Collections.Generic;
using System.Text;
using FishNet;
using FishNet.Connection;
using FishNet.Transporting;
using UnityEngine;

public sealed class GameFlowManager : MonoBehaviour
{
    private const float StateSyncInterval = 0.25f;

    [SerializeField] private int _requiredPlayers = 2;
    [SerializeField] private float _matchDuration = 60f;
    [SerializeField] private float _resultsDuration = 5f;
    [SerializeField] private float _lobbyStartDelay = 2f;

    private GameRoundState _state = GameRoundState.WaitingForPlayers;
    private float _matchTimeRemaining;
    private float _nextStateSyncTime;
    private Coroutine _startMatchRoutine;
    private Coroutine _resetRoutine;
    private bool _subscribedToServerEvents;
    private string _resultsText = string.Empty;

    private IEnumerator Start()
    {
        ApplyLaunchOverrides();
        _matchTimeRemaining = _matchDuration;

        while (InstanceFinder.NetworkManager == null || InstanceFinder.ServerManager == null)
            yield return null;

        InstanceFinder.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        _subscribedToServerEvents = true;

        SyncPlayers(force: true);
        RefreshLobbyState();
    }

    private void OnDestroy()
    {
        if (_subscribedToServerEvents && InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
    }

    private void Update()
    {
        if (!InstanceFinder.IsServerStarted)
            return;

        if (_state == GameRoundState.InProgress)
        {
            _matchTimeRemaining -= Time.deltaTime;

            if (_matchTimeRemaining <= 0f)
            {
                EndMatch();
                return;
            }
        }

        SyncPlayers(force: false);
    }

    private void ApplyLaunchOverrides()
    {
        if (int.TryParse(NetworkLaunchArgs.GetString("requiredPlayers"), out int requiredPlayers))
            _requiredPlayers = Mathf.Max(1, requiredPlayers);

        if (float.TryParse(NetworkLaunchArgs.GetString("matchSeconds"), out float matchSeconds))
            _matchDuration = Mathf.Max(5f, matchSeconds);
    }

    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Started ||
            args.ConnectionState == RemoteConnectionState.Stopped)
        {
            StartCoroutine(RefreshAfterConnectionChange());
        }
    }

    private IEnumerator RefreshAfterConnectionChange()
    {
        yield return null;
        yield return null;
        RefreshLobbyState();
    }

    private void RefreshLobbyState()
    {
        if (!InstanceFinder.IsServerStarted)
            return;

        if (_state == GameRoundState.WaitingForPlayers)
        {
            if (GetConnectedPlayersCount() >= _requiredPlayers)
                ScheduleMatchStart();
            else
                CancelScheduledStart();
        }

        SyncPlayers(force: true);
    }

    private void ScheduleMatchStart()
    {
        if (_startMatchRoutine != null)
            return;

        _startMatchRoutine = StartCoroutine(StartMatchAfterDelay());
    }

    private IEnumerator StartMatchAfterDelay()
    {
        float timer = _lobbyStartDelay;

        while (timer > 0f)
        {
            if (_state != GameRoundState.WaitingForPlayers ||
                GetConnectedPlayersCount() < _requiredPlayers)
            {
                _startMatchRoutine = null;
                yield break;
            }

            timer -= Time.deltaTime;
            yield return null;
        }

        _startMatchRoutine = null;
        StartMatch();
    }

    private void CancelScheduledStart()
    {
        if (_startMatchRoutine == null)
            return;

        StopCoroutine(_startMatchRoutine);
        _startMatchRoutine = null;
    }

    private void StartMatch()
    {
        if (_state != GameRoundState.WaitingForPlayers)
            return;

        if (GetConnectedPlayersCount() < _requiredPlayers)
            return;

        if (_resetRoutine != null)
        {
            StopCoroutine(_resetRoutine);
            _resetRoutine = null;
        }

        _state = GameRoundState.InProgress;
        _matchTimeRemaining = _matchDuration;
        _resultsText = string.Empty;

        foreach (PlayerNetwork player in GetPlayers())
            player.ResetForRound(resetScore: true);

        Debug.Log("[Server] Match started.");
        SyncPlayers(force: true);
    }

    private void EndMatch()
    {
        if (_state != GameRoundState.InProgress)
            return;

        _state = GameRoundState.ShowingResults;
        _matchTimeRemaining = 0f;
        _resultsText = BuildResultsText();

        Debug.Log("[Server] Match ended. Showing results.");
        SyncPlayers(force: true);

        if (_resetRoutine != null)
            StopCoroutine(_resetRoutine);

        _resetRoutine = StartCoroutine(ResetToLobbyAfterDelay());
    }

    private IEnumerator ResetToLobbyAfterDelay()
    {
        yield return new WaitForSeconds(_resultsDuration);
        _resetRoutine = null;
        ResetToLobby();
    }

    private void ResetToLobby()
    {
        if (!InstanceFinder.IsServerStarted)
            return;

        foreach (PlayerNetwork player in GetPlayers())
            player.ResetForRound(resetScore: true);

        _state = GameRoundState.WaitingForPlayers;
        _matchTimeRemaining = _matchDuration;
        _resultsText = string.Empty;

        Debug.Log("[Server] Lobby reset. Waiting for players.");
        SyncPlayers(force: true);
        RefreshLobbyState();
    }

    private void SyncPlayers(bool force)
    {
        if (!InstanceFinder.IsServerStarted)
            return;

        if (!force && Time.time < _nextStateSyncTime)
            return;

        _nextStateSyncTime = Time.time + StateSyncInterval;

        int connectedPlayers = GetConnectedPlayersCount();
        float timer = _state == GameRoundState.InProgress ? _matchTimeRemaining : _matchDuration;

        foreach (PlayerNetwork player in GetPlayers())
        {
            player.ApplyRoundState(
                _state,
                connectedPlayers,
                _requiredPlayers,
                timer,
                _resultsText);
        }
    }

    private int GetConnectedPlayersCount()
    {
        return InstanceFinder.ServerManager == null ? 0 : InstanceFinder.ServerManager.Clients.Count;
    }

    private string BuildResultsText()
    {
        List<PlayerNetwork> players = new List<PlayerNetwork>(GetPlayers());
        players.Sort((a, b) => b.Score.Value.CompareTo(a.Score.Value));

        if (players.Count == 0)
            return "Нет подключенных игроков.";

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Итоговый счет:");

        for (int i = 0; i < players.Count; i++)
        {
            PlayerNetwork player = players[i];
            string nickname = string.IsNullOrWhiteSpace(player.Nickname.Value)
                ? $"Player {player.OwnerId}"
                : player.Nickname.Value;

            builder.AppendLine($"{i + 1}. {nickname}: {player.Score.Value}");
        }

        return builder.ToString().TrimEnd();
    }

    private static PlayerNetwork[] GetPlayers()
    {
        return FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);
    }
}

