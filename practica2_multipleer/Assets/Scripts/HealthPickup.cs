using Unity.Netcode;
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
        if (!IsServer) return;

        PlayerNetwork player = other.GetComponent<PlayerNetwork>();
        if (player == null) return;

        // Мёртвый не подбирает
        if (!player.IsAlive.Value) return;

        // При полном здоровье не лечим
        if (player.HP.Value >= 100) return;

        int newHp = Mathf.Min(100, player.HP.Value + _healAmount);
        player.HP.Value = newHp;

        // Сообщаем менеджеру, что аптечку подобрали
        if (_manager != null)
            _manager.OnPickedUp(_spawnPosition);

        // Уничтожаем аптечку
        NetworkObject.Despawn(true);
    }
}