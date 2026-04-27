using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

public class SimplePlayerSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject _playerPrefab;

    private void Start()
    {
        InstanceFinder.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
    }

    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState != RemoteConnectionState.Started) return;
        NetworkObject player = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity);
        InstanceFinder.ServerManager.Spawn(player, conn);
        Debug.Log($"Player spawned for connection {conn.ClientId}");
    }
}