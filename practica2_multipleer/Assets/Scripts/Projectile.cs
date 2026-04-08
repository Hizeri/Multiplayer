using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 20f;
    [SerializeField] private int _damage = 15;

    private void Update()
    {
        if (!IsServer || !IsSpawned) return;
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !IsSpawned) return;

        PlayerNetwork target = other.GetComponent<PlayerNetwork>();
        if (target == null) return;

        // Не наносим урон самому себе
        if (target.OwnerClientId == OwnerClientId) return;

        int newHp = Mathf.Max(0, target.HP.Value - _damage);
        target.HP.Value = newHp;

        StartCoroutine(DestroyProjectile());
    }

    private IEnumerator DestroyProjectile()
    {
        yield return null; // ждём 1 кадр
        if (IsSpawned)
            NetworkObject.Despawn(true);
    }
}