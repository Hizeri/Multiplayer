using Unity.Netcode;
using UnityEngine;

public class PlayerShooting : NetworkBehaviour
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _cooldown = 0.5f;
    [SerializeField] private int _maxAmmo = 10;

    private float _lastShotTime;
    private int _currentAmmo;
    private PlayerNetwork _playerNetwork;

    public override void OnNetworkSpawn()
    {
        _currentAmmo = _maxAmmo;
        _playerNetwork = GetComponent<PlayerNetwork>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Не стреляем, если мёртв
        if (_playerNetwork != null && !_playerNetwork.IsAlive.Value) return;

        if (Input.GetButtonDown("Fire1")) // левая кнопка мыши
        {
            ShootServerRpc(_firePoint.position, _firePoint.forward);
        }
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 pos, Vector3 dir, ServerRpcParams rpcParams = default)
    {
        if (_playerNetwork.HP.Value <= 0) return;
        if (_currentAmmo <= 0) return;
        if (Time.time < _lastShotTime + _cooldown) return;

        _lastShotTime = Time.time;
        _currentAmmo--;

        GameObject proj = Instantiate(_projectilePrefab, pos + dir * 0.5f, Quaternion.LookRotation(dir));
        NetworkObject netObj = proj.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(rpcParams.Receive.SenderClientId);
    }
}