using FishNet;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

public class SimpleSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject _playerPrefab;

    private void Start()
    {
        InstanceFinder.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
    }

    private void OnDestroy()
    {
        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
    }

    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        Debug.Log($"[Server] Connection {conn.ClientId} state: {args.ConnectionState}");

        if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            conn.OnLoadedStartScenes -= OnLoadedStartScenes;
            return;
        }

        if (args.ConnectionState != RemoteConnectionState.Started) return;

        if (!conn.LoadedStartScenes(asServer: true))
        {
            conn.OnLoadedStartScenes += OnLoadedStartScenes;
            return;
        }

        SpawnPlayer(conn);
    }

    private void OnLoadedStartScenes(NetworkConnection conn, bool asServer)
    {
        if (!asServer)
            return;

        conn.OnLoadedStartScenes -= OnLoadedStartScenes;
        SpawnPlayer(conn);
    }

    private void SpawnPlayer(NetworkConnection conn)
    {
        Vector3 spawnPosition = GetSpawnPosition();
        NetworkObject player = Instantiate(_playerPrefab, spawnPosition, Quaternion.identity);
        InstanceFinder.ServerManager.Spawn(player, conn);
        Debug.Log($"Player spawned for connection {conn.ClientId}");
    }

    private static Vector3 GetSpawnPosition()
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
        if (spawnPoints.Length == 0)
            spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");

        if (spawnPoints.Length == 0)
            return Vector3.zero;

        return spawnPoints[Random.Range(0, spawnPoints.Length)].transform.position;
    }
}
