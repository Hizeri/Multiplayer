using Unity.Netcode;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    [SerializeField] private Vector3 _offset = new(0f, 4f, -2f);
    [SerializeField] private GameObject _camera;

    private Camera _cam;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
        _cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (_cam == null) return;
        _cam.transform.position = transform.position + _offset;
        _cam.transform.LookAt(_camera.transform.position);
    }
}