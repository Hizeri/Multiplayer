using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    // Ник
    public NetworkVariable<FixedString32Bytes> Nickname = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // HP
    public NetworkVariable<int> HP = new(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Новая переменная: жив ли игрок (только сервер меняет)
    public NetworkVariable<bool> IsAlive = new(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
        }

        // Сервер подписывается на изменение HP
        if (IsServer)
        {
            HP.OnValueChanged += OnHpChangedForDeath;
        }

        // Подписка на изменение IsAlive (для всех клиентов – чтобы скрыть/показать модель)
        IsAlive.OnValueChanged += OnIsAliveChanged;

        Debug.Log($"Player spawned: Nickname = {Nickname.Value}, HP = {HP.Value}, IsAlive = {IsAlive.Value}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitNicknameServerRpc(string nickname)
    {
        string safeValue = string.IsNullOrWhiteSpace(nickname) ? $"Player_{OwnerClientId}" : nickname.Trim();
        Nickname.Value = safeValue;
        Debug.Log($"Server set nickname for client {OwnerClientId} to: {safeValue}");
    }

    // Серверная реакция на изменение HP
    private void OnHpChangedForDeath(int oldHp, int newHp)
    {
        if (newHp <= 0 && IsAlive.Value)
        {
            IsAlive.Value = false;
            // Запускаем респавн через 3 секунды
            Invoke(nameof(RespawnPlayer), 3f);
        }
    }

    // Серверный респавн
    private void RespawnPlayer()
    {
        if (!IsServer) return;

        // Поиск всех точек с тегом "Respawn"
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");

       int index = Random.Range(0, spawnPoints.Length);
        Vector3 spawn = spawnPoints[index].transform.position;

        RespawnPlayerClientRpc(spawn);
        HP.Value = 100;
        IsAlive.Value = true;


    }

    

    // Визуальная реакция на IsAlive (отключаем рендер и CharacterController)
    private void OnIsAliveChanged(bool oldValue, bool newValue)
    {
        

        // Включаем/выключаем CharacterController (чтобы не мешал физике)
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = newValue;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
            HP.OnValueChanged -= OnHpChangedForDeath;
        IsAlive.OnValueChanged -= OnIsAliveChanged;
    }

    [ClientRpc]
    private void RespawnPlayerClientRpc(Vector3 spawn)
    {
        if (IsOwner)
        {
            transform.position = spawn;


        }
    }
}