using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour

{


    // Ќик должен быть виден всем клиентам, но мен€ть его может только сервер.
    public NetworkVariable<FixedString32Bytes> Nickname = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // HP тоже читает каждый клиент, но измен€етс€ только на сервере.
    public NetworkVariable<int> HP = new(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        // ≈сли этот объект принадлежит локальному игроку (т.е. нам)
        if (IsOwner)
        {
            // ќтправл€ем на сервер ник, который мы ввели в меню (хранитс€ в ConnectionUI.PlayerNickname)
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
        }

        // ƒл€ отладки: выведем в консоль ник и здоровье при по€влении
        Debug.Log($"Player spawned: Nickname = {Nickname.Value}, HP = {HP.Value}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitNicknameServerRpc(string nickname)
    {
        // —ервер провер€ет и нормализует ник
        string safeValue = string.IsNullOrWhiteSpace(nickname) ? $"Player_{OwnerClientId}" : nickname.Trim();
        Nickname.Value = safeValue;

        // —ервер может сразу вывести подтверждение
        Debug.Log($"Server set nickname for client {OwnerClientId} to: {safeValue}");
    }
}