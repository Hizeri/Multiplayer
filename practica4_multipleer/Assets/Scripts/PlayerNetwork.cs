using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    // Синхронизируемый ник – его увидят все клиенты
    public readonly SyncVar<string> Nickname = new SyncVar<string>("Player");

    public readonly SyncVar<int> HP = new SyncVar<int>(100);
    public readonly SyncVar<bool> IsAlive = new SyncVar<bool>(true);
    public readonly SyncVar<int> Ammo = new SyncVar<int>(20);
    public readonly SyncVar<int> Score = new SyncVar<int>(0);
    public readonly SyncVar<float> RespawnTime = new SyncVar<float>(0f);
    public readonly SyncVar<GameRoundState> RoundState = new SyncVar<GameRoundState>(GameRoundState.WaitingForPlayers);
    public readonly SyncVar<int> ConnectedPlayers = new SyncVar<int>(0);
    public readonly SyncVar<int> RequiredPlayers = new SyncVar<int>(2);
    public readonly SyncVar<float> MatchTimeRemaining = new SyncVar<float>(60f);
    public readonly SyncVar<string> ResultsText = new SyncVar<string>(string.Empty);

    private Coroutine _respawnRoutine;

    public bool CanAct => RoundState.Value == GameRoundState.InProgress && IsAlive.Value;

    public override void OnStartNetwork()
    {
        HP.OnChange += OnHpChanged;
        IsAlive.OnChange += OnIsAliveChanged;
        RespawnTime.OnChange += OnRespawnTimeChanged;

        // Устанавливаем ник только для локального игрока
        if (base.Owner.IsLocalClient)
        {
            StartCoroutine(SetNicknameDelayed());
        }
    }

    private IEnumerator SetNicknameDelayed()
    {
        yield return null; // Ждём один кадр для полной инициализации

        string nickname = ConnectionUI.PlayerNickname;
        string safe = string.IsNullOrWhiteSpace(nickname) ? $"Player_{OwnerId}" : nickname.Trim();

        if (base.IsServerInitialized)
        {
            // === ХОСТ (сервер + клиент): Устанавливаем ник напрямую в SyncVar ===
            Nickname.Value = safe;
            Debug.Log($"Host: ник синхронизирован: {Nickname.Value}");
        }
        else
        {
            // === КЛИЕНТ: Отправляем RPC на сервер ===
            SetNicknameServerRpc(safe);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetNicknameServerRpc(string nickname)
    {
        Nickname.Value = nickname;
        Debug.Log($"ServerRpc: ник установлен для {OwnerId}: {Nickname.Value}");
    }

    public override void OnStopNetwork()
    {
        HP.OnChange -= OnHpChanged;
        IsAlive.OnChange -= OnIsAliveChanged;
        RespawnTime.OnChange -= OnRespawnTimeChanged;

        if (_respawnRoutine != null)
        {
            StopCoroutine(_respawnRoutine);
            _respawnRoutine = null;
        }
    }

    private void OnHpChanged(int prev, int next, bool asServer)
    {
        if (!asServer) return;
        if (next <= 0 && IsAlive.Value)
        {
            IsAlive.Value = false;
            if (_respawnRoutine != null)
                StopCoroutine(_respawnRoutine);

            _respawnRoutine = StartCoroutine(RespawnRoutine());
        }
    }

    private void OnIsAliveChanged(bool prev, bool next, bool asServer)
    {
        if (next == false)
            HidePlayer();
        else
            ShowPlayer();
    }

    private void OnRespawnTimeChanged(float oldValue, float newValue, bool asServer) { }

    public void ApplyRoundState(
        GameRoundState state,
        int connectedPlayers,
        int requiredPlayers,
        float matchTimeRemaining,
        string resultsText)
    {
        if (!base.IsServerInitialized)
            return;

        RoundState.Value = state;
        ConnectedPlayers.Value = connectedPlayers;
        RequiredPlayers.Value = requiredPlayers;
        MatchTimeRemaining.Value = Mathf.Max(0f, matchTimeRemaining);
        ResultsText.Value = resultsText ?? string.Empty;
    }

    public void AddScore(int amount)
    {
        if (!base.IsServerInitialized)
            return;

        Score.Value += amount;
    }

    public void ResetForRound(bool resetScore)
    {
        if (!base.IsServerInitialized)
            return;

        if (_respawnRoutine != null)
        {
            StopCoroutine(_respawnRoutine);
            _respawnRoutine = null;
        }

        HP.Value = 100;
        IsAlive.Value = true;
        Ammo.Value = 20;
        RespawnTime.Value = 0f;

        if (resetScore)
            Score.Value = 0;

        ShowPlayer();
    }

    private void HidePlayer()
    {
        foreach (var r in GetComponentsInChildren<MeshRenderer>()) r.enabled = false;
        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;
        CharacterController cc = GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
    }

    private void ShowPlayer()
    {
        foreach (var r in GetComponentsInChildren<MeshRenderer>()) r.enabled = true;
        Collider col = GetComponent<Collider>();
        if (col) col.enabled = true;
        CharacterController cc = GetComponent<CharacterController>();
        if (cc) cc.enabled = true;
    }

    private IEnumerator RespawnRoutine()
    {
        float timer = 3f;
        while (timer > 0)
        {
            if (RoundState.Value != GameRoundState.InProgress)
            {
                RespawnTime.Value = 0f;
                _respawnRoutine = null;
                yield break;
            }

            timer -= Time.deltaTime;
            RespawnTime.Value = timer;
            yield return null;
        }

        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
        if (spawnPoints.Length == 0) spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        if (spawnPoints.Length == 0)
        {
            _respawnRoutine = null;
            yield break;
        }
        int idx = Random.Range(0, spawnPoints.Length);
        Vector3 newPos = spawnPoints[idx].transform.position;

        TeleportPlayerObservers(newPos);
        if (base.IsServerInitialized) transform.position = newPos;

        HP.Value = 100;
        IsAlive.Value = true;
        Ammo.Value = 20;
        RespawnTime.Value = 0f;
        _respawnRoutine = null;
    }

    [ObserversRpc(BufferLast = true)]
    private void TeleportPlayerObservers(Vector3 spawnPosition)
    {
        if (!base.IsServerInitialized && base.IsOwner)
            transform.position = spawnPosition;
    }
}
