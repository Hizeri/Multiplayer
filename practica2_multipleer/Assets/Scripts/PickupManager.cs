using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PickupManager : MonoBehaviour
{
    [SerializeField] private GameObject _healthPickupPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _respawnDelay = 10f;

    private bool _isServerActive = false;

    private void Start()
    {
        // Запускаем корутину, которая ждёт активации сервера
        StartCoroutine(WaitForServerAndSpawn());
    }

    private IEnumerator WaitForServerAndSpawn()
    {
        // Ждём, пока NetworkManager появится
        while (NetworkManager.Singleton == null)
            yield return null;

        // Ждём, пока NetworkManager станет сервером (т.е. нажата кнопка HOST)
        while (!NetworkManager.Singleton.IsServer)
            yield return null;

        _isServerActive = true;
        SpawnAll();
    }

    private void SpawnAll()
    {
        if (!_isServerActive) return;

        foreach (var point in _spawnPoints)
        {
            if (point == null) continue;
            SpawnPickup(point.position);
        }
    }

    public void OnPickedUp(Vector3 position)
    {
        if (!_isServerActive) return;
        StartCoroutine(RespawnAfterDelay(position));
    }

    private IEnumerator RespawnAfterDelay(Vector3 position)
    {
        yield return new WaitForSeconds(_respawnDelay);
        if (_isServerActive)
            SpawnPickup(position);
    }

    private void SpawnPickup(Vector3 position)
    {
        if (!_isServerActive) return;

        GameObject go = Instantiate(_healthPickupPrefab, position, Quaternion.identity);
        HealthPickup pickup = go.GetComponent<HealthPickup>();
        if (pickup != null)
            pickup.Init(this, position);
        else
            Debug.LogError("HealthPickup component missing on prefab!");

        go.GetComponent<NetworkObject>().Spawn();
    }
}