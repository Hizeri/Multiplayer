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
    public readonly SyncVar<float> RespawnTime = new SyncVar<float>(0f);

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

        if (base.IsServer)
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
    }

    private void OnHpChanged(int prev, int next, bool asServer)
    {
        if (!asServer) return;
        if (next <= 0 && IsAlive.Value)
        {
            IsAlive.Value = false;
            StartCoroutine(RespawnRoutine());
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
            timer -= Time.deltaTime;
            RespawnTime.Value = timer;
            yield return null;
        }

        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
        if (spawnPoints.Length == 0) spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        if (spawnPoints.Length == 0) yield break;
        int idx = Random.Range(0, spawnPoints.Length);
        Vector3 newPos = spawnPoints[idx].transform.position;

        TeleportPlayerObservers(newPos);
        if (base.IsServerInitialized) transform.position = newPos;

        HP.Value = 100;
        IsAlive.Value = true;
        Ammo.Value = 20;
    }

    [ObserversRpc(BufferLast = true)]
    private void TeleportPlayerObservers(Vector3 spawnPosition)
    {
        if (!base.IsServerInitialized && base.IsOwner)
            transform.position = spawnPosition;
    }
}