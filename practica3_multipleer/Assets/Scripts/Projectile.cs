using FishNet.Object;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 20f;
    [SerializeField] private int _damage = 15;

    private void Update()
    {
        if (!base.IsServer) return;
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!base.IsServer) return;

        PlayerNetwork target = other.GetComponent<PlayerNetwork>();
        if (target == null) return;
        if (target.Owner == base.Owner) return;

        int newHealth = Mathf.Max(0, target.HP.Value - _damage);
        target.HP.Value = newHealth;

        base.ServerManager.Despawn(base.NetworkObject);
    }
}