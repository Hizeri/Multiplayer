using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerView : NetworkBehaviour
{
    [SerializeField] private PlayerNetwork _playerNetwork; // ссылка на компонент с данными
    [SerializeField] private TMP_Text _nicknameText;       // ссылка на текстовое поле для ника
    [SerializeField] private TMP_Text _hpText;             // ссылка на текстовое поле для HP

    public override void OnNetworkSpawn()
    {
        // Подписываемся на изменения сетевых переменных
        _playerNetwork.Nickname.OnValueChanged += OnNicknameChanged;
        _playerNetwork.HP.OnValueChanged += OnHpChanged;

        // Инициализируем текст текущими значениями (на случай, если они уже установлены)
        OnNicknameChanged(default, _playerNetwork.Nickname.Value);
        OnHpChanged(0, _playerNetwork.HP.Value);
    }

    public override void OnNetworkDespawn()
    {
        // Обязательно отписываемся при исчезновении объекта
        _playerNetwork.Nickname.OnValueChanged -= OnNicknameChanged;
        _playerNetwork.HP.OnValueChanged -= OnHpChanged;
    }

    private void OnNicknameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        _nicknameText.text = newValue.ToString();
    }

    private void OnHpChanged(int oldValue, int newValue)
    {
        _hpText.text = $"HP: {newValue}";
    }
}