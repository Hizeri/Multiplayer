using FishNet.Object;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 20f;
    [SerializeField] private int _damage = 15;

    private void Update()
    {
        if (!base.IsServerInitialized) return;
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!base.IsServerInitialized) return;

        PlayerNetwork target = other.GetComponent<PlayerNetwork>();
        if (target == null) return;
        if (target.Owner == base.Owner) return;
        if (!target.IsAlive.Value) return;
        if (target.RoundState.Value != GameRoundState.InProgress) return;

        PlayerNetwork shooter = GetOwnerPlayer();
        if (shooter == null || !shooter.CanAct) return;

        int previousHealth = target.HP.Value;
        int newHealth = Mathf.Max(0, target.HP.Value - _damage);
        target.HP.Value = newHealth;

        if (previousHealth > 0 && newHealth <= 0)
            shooter.AddScore(1);

        base.ServerManager.Despawn(base.NetworkObject);
    }

    private PlayerNetwork GetOwnerPlayer()
    {
        if (base.Owner == null)
            return null;

        foreach (NetworkObject nob in base.Owner.Objects)
        {
            if (nob == null)
                continue;

            PlayerNetwork player = nob.GetComponent<PlayerNetwork>();
            if (player != null)
                return player;
        }

        return null;
    }
}
