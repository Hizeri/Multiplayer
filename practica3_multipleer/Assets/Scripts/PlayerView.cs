using TMPro;
using FishNet.Object;
using UnityEngine;

public class PlayerView : NetworkBehaviour
{
    [SerializeField] private PlayerNetwork _playerNetwork;
    [SerializeField] private TMP_Text _nicknameText;
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private TMP_Text _timerText;

    private float _respawnTimer = 0f;
    private bool _isDead = false;

    public override void OnStartNetwork()
    {
        _playerNetwork.Nickname.OnChange += OnNicknameChanged;
        _playerNetwork.HP.OnChange += OnHpChanged;
        _playerNetwork.RespawnTime.OnChange += OnRespawnTimeChanged;
        _playerNetwork.IsAlive.OnChange += OnIsAliveChanged;

        // Установить начальные значения
        OnNicknameChanged("", _playerNetwork.Nickname.Value, true);
        OnHpChanged(0, _playerNetwork.HP.Value, true);
        OnIsAliveChanged(true, _playerNetwork.IsAlive.Value, true);
    }

    public override void OnStopNetwork()
    {
        _playerNetwork.Nickname.OnChange -= OnNicknameChanged;
        _playerNetwork.HP.OnChange -= OnHpChanged;
        _playerNetwork.RespawnTime.OnChange -= OnRespawnTimeChanged;
        _playerNetwork.IsAlive.OnChange -= OnIsAliveChanged;
    }

    private void OnNicknameChanged(string oldValue, string newValue, bool asServer)
    {
        _nicknameText.text = newValue;
    }

    private void OnHpChanged(int oldValue, int newValue, bool asServer)
    {
        _hpText.text = $"HP: {newValue}";
    }

    private void OnIsAliveChanged(bool oldValue, bool newValue, bool asServer)
    {
        // Скрываем ник и HP при смерти
        _nicknameText.gameObject.SetActive(newValue);
        _hpText.gameObject.SetActive(newValue);
    }

    private void OnRespawnTimeChanged(float oldValue, float newValue, bool asServer)
    {
        if (!base.IsOwner) return;

        if (newValue > 0)
        {
            _respawnTimer = newValue;
            _isDead = true;
            if (_timerText != null) _timerText.gameObject.SetActive(true);
        }
        else
        {
            _isDead = false;
            if (_timerText != null) _timerText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!base.IsOwner) return;

        if (_isDead && _timerText != null)
        {
            int seconds = Mathf.CeilToInt(_respawnTimer);
            _timerText.text = $"Respawn: {seconds}";
            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer <= 0)
            {
                _timerText.gameObject.SetActive(false);
                _isDead = false;
            }
        }
    }
}