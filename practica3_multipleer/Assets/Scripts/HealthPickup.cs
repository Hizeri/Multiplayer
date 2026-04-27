using FishNet.Object;
using UnityEngine;

public class HealthPickup : NetworkBehaviour
{
    [SerializeField] private int _healAmount = 30;

    private PickupManager _manager;
    private Vector3 _spawnPosition;

    public void Init(PickupManager manager, Vector3 spawnPos)
    {
        _manager = manager;
        _spawnPosition = spawnPos;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!base.IsServer) return;

        PlayerNetwork player = other.GetComponent<PlayerNetwork>();
        if (player == null) return;
        if (!player.IsAlive.Value) return;
        if (player.HP.Value >= 100) return;

        int newHealth = Mathf.Min(100, player.HP.Value + _healAmount);
        player.HP.Value = newHealth;

        if (_manager != null)
            _manager.OnPickedUp(_spawnPosition);

        base.ServerManager.Despawn(base.NetworkObject);
    }
}