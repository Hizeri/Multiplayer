using FishNet.Object;
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

    public override void OnStartNetwork()
    {
        _currentAmmo = _maxAmmo;
        _playerNetwork = GetComponent<PlayerNetwork>();
        if (_playerNetwork != null)
            _playerNetwork.Ammo.Value = _currentAmmo;
    }

    private void Update()
    {
        if (!base.IsOwner) return;
        if (_playerNetwork != null && !_playerNetwork.IsAlive.Value) return;

        if (Input.GetButtonDown("Fire1"))
        {
            ShootServerRpc(_firePoint.position, _firePoint.forward);
        }
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 pos, Vector3 dir)
    {
        if (_playerNetwork.HP.Value <= 0) return;
        if (_currentAmmo <= 0) return;
        if (Time.time < _lastShotTime + _cooldown) return;

        _lastShotTime = Time.time;
        _currentAmmo--;
        _playerNetwork.Ammo.Value = _currentAmmo;

        GameObject proj = Instantiate(_projectilePrefab, pos + dir * 0.5f, Quaternion.LookRotation(dir));
        base.ServerManager.Spawn(proj, base.Owner);
    }
}