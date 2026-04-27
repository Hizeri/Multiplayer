using FishNet;
using FishNet.Object;
using System.Collections;
using UnityEngine;

public class PickupManager : MonoBehaviour
{
    [SerializeField] private GameObject _healthPickupPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _respawnDelay = 10f;

    private void Start()
    {
        StartCoroutine(WaitForServerAndSpawn());
    }

    private IEnumerator WaitForServerAndSpawn()
    {
        while (!InstanceFinder.IsServer) yield return null;
        Debug.Log("PickupManager: Server is active, spawning pickups");
        SpawnAll();
    }

    private void SpawnAll()
    {
        Debug.Log($"PickupManager: SpawnAll, points count = {_spawnPoints.Length}");
        foreach (var point in _spawnPoints)
        {
            if (point == null) continue;
            SpawnPickup(point.position);
        }
    }

    public void OnPickedUp(Vector3 position)
    {
        if (!InstanceFinder.IsServer) return;
        StartCoroutine(RespawnAfterDelay(position));
    }

    private IEnumerator RespawnAfterDelay(Vector3 position)
    {
        yield return new WaitForSeconds(_respawnDelay);
        SpawnPickup(position);
    }

    private void SpawnPickup(Vector3 position)
    {
        if (!InstanceFinder.IsServer) return;
        GameObject go = Instantiate(_healthPickupPrefab, position, Quaternion.identity);
        NetworkObject netObj = go.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("PickupManager: HealthPickup prefab missing NetworkObject component!");
            return;
        }
        HealthPickup pickup = go.GetComponent<HealthPickup>();
        if (pickup != null) pickup.Init(this, position);
        InstanceFinder.ServerManager.Spawn(netObj);
        Debug.Log($"Pickup spawned at {position}");
    }
}