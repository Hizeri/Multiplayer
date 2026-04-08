using Unity.Netcode;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    [SerializeField] private PlayerNetwork _playerNetwork;
    [SerializeField] private int _damage = 10;
    [SerializeField] private float _attackRange = 2f; // дальность атаки

    private void Update()
    {
        // Атаковать может только локальный владелец (наш игрок)
        if (!IsOwner) return;

        // При нажатии пробела
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Бросаем луч вперёд от текущей позиции
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, _attackRange))
            {
                // Проверяем, есть ли на объекте, в который попали, компонент PlayerNetwork
                PlayerNetwork target = hit.collider.GetComponent<PlayerNetwork>();
                if (target != null)
                {
                    // Пытаемся атаковать эту цель
                    TryAttack(target);
                }
            }
        }
    }

    public void TryAttack(PlayerNetwork target)
    {
        // Дополнительная проверка: не атакуем null и только если мы владелец
        if (!IsOwner || target == null)
            return;

        // Отправляем запрос на сервер
        DealDamageServerRpc(target.NetworkObjectId, _damage);
    }

    [ServerRpc]
    private void DealDamageServerRpc(ulong targetObjectId, int damage)
    {
        // Ищем объект цели по его NetworkObjectId
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId, out NetworkObject targetObject))
            return;

        PlayerNetwork targetPlayer = targetObject.GetComponent<PlayerNetwork>();
        // Запрещаем урон самому себе
        if (targetPlayer == null || targetPlayer == _playerNetwork)
            return;

        // Вычисляем новое здоровье (не ниже 0)
        int nextHp = Mathf.Max(0, targetPlayer.HP.Value - damage);
        targetPlayer.HP.Value = nextHp;
    }
}