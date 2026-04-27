using FishNet.Object;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    [SerializeField] private Vector3 _offset = new(0f, 4f, -2f);
    [SerializeField] private GameObject _cameraView; // цель, на которую смотрит камера

    private Camera _cam;

    public override void OnStartNetwork()
    {
        // Не используем IsOwner здесь – перенесём проверку в LateUpdate
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main; // запасной вариант
    }

    private void LateUpdate()
    {
        if (!base.IsOwner) return; // работает и для хоста, и для клиента
        if (_cam == null) return;
        if (_cameraView == null) return;

        _cam.transform.position = transform.root.position + _offset;
        _cam.transform.LookAt(_cameraView.transform.position);
    }
}