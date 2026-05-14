using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameCycleUI : MonoBehaviour
{
    private GameObject _root;
    private GameObject _overlay;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _bodyText;
    private TextMeshProUGUI _hudText;
    private PlayerNetwork _localPlayer;

    private void Start()
    {
        BuildUi();
        SetRootActive(false);
    }

    private void Update()
    {
        if (Application.isBatchMode)
            return;

        if (!FishNet.InstanceFinder.IsClientStarted)
        {
            SetRootActive(false);
            _localPlayer = null;
            return;
        }

        SetRootActive(true);

        if (_localPlayer == null || !_localPlayer.IsOwner)
            _localPlayer = FindLocalPlayer();

        if (_localPlayer == null)
        {
            ShowOverlay("Подключение", "Ожидание появления игрока...");
            _hudText.gameObject.SetActive(false);
            return;
        }

        switch (_localPlayer.RoundState.Value)
        {
            case GameRoundState.WaitingForPlayers:
                ShowWaiting();
                break;
            case GameRoundState.InProgress:
                ShowMatchHud();
                break;
            case GameRoundState.ShowingResults:
                ShowResults();
                break;
        }
    }

    private void BuildUi()
    {
        _root = new GameObject("Practice4CycleUI");
        DontDestroyOnLoad(_root);

        Canvas canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _root.AddComponent<GraphicRaycaster>();

        _overlay = new GameObject("Overlay");
        _overlay.transform.SetParent(_root.transform, false);
        RectTransform overlayRect = _overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = _overlay.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.64f);

        _titleText = CreateText("Title", _overlay.transform, 46, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform titleRect = _titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.15f, 0.58f);
        titleRect.anchorMax = new Vector2(0.85f, 0.72f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        _bodyText = CreateText("Body", _overlay.transform, 30, FontStyles.Normal, TextAlignmentOptions.Center);
        RectTransform bodyRect = _bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0.15f, 0.28f);
        bodyRect.anchorMax = new Vector2(0.85f, 0.58f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;

        _hudText = CreateText("HUD", _root.transform, 24, FontStyles.Bold, TextAlignmentOptions.TopRight);
        RectTransform hudRect = _hudText.rectTransform;
        hudRect.anchorMin = new Vector2(0.68f, 0.78f);
        hudRect.anchorMax = new Vector2(0.98f, 0.96f);
        hudRect.offsetMin = Vector2.zero;
        hudRect.offsetMax = Vector2.zero;
    }

    private void ShowWaiting()
    {
        int connected = _localPlayer.ConnectedPlayers.Value;
        int required = _localPlayer.RequiredPlayers.Value;
        string status = connected >= required
            ? "Игроки набраны. Матч скоро начнется."
            : "Ждем остальных игроков.";

        ShowOverlay("Лобби", $"Ожидание игроков: {connected}/{required}\n{status}");
        _hudText.gameObject.SetActive(false);
    }

    private void ShowMatchHud()
    {
        _overlay.SetActive(false);
        _hudText.gameObject.SetActive(true);

        int seconds = Mathf.CeilToInt(_localPlayer.MatchTimeRemaining.Value);
        _hudText.text =
            $"Время: {seconds}с\n" +
            $"Счет: {_localPlayer.Score.Value}\n" +
            $"Игроки: {_localPlayer.ConnectedPlayers.Value}/{_localPlayer.RequiredPlayers.Value}";
    }

    private void ShowResults()
    {
        string results = _localPlayer.ResultsText.Value;

        if (string.IsNullOrWhiteSpace(results))
            results = BuildLocalResultsText();

        ShowOverlay("Результаты", $"{results}\n\nВозврат в лобби...");
        _hudText.gameObject.SetActive(false);
    }

    private void ShowOverlay(string title, string body)
    {
        _overlay.SetActive(true);
        _titleText.text = title;
        _bodyText.text = body;
    }

    private void SetRootActive(bool active)
    {
        if (_root != null && _root.activeSelf != active)
            _root.SetActive(active);
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        int fontSize,
        FontStyles style,
        TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static PlayerNetwork FindLocalPlayer()
    {
        foreach (PlayerNetwork player in FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None))
        {
            if (player.IsOwner)
                return player;
        }

        return null;
    }

    private static string BuildLocalResultsText()
    {
        PlayerNetwork[] players = FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);

        if (players.Length == 0)
            return "Нет результатов.";

        StringBuilder builder = new StringBuilder("Итоговый счет:");

        foreach (PlayerNetwork player in players)
        {
            string nickname = string.IsNullOrWhiteSpace(player.Nickname.Value)
                ? $"Player {player.OwnerId}"
                : player.Nickname.Value;

            builder.AppendLine();
            builder.Append($"{nickname}: {player.Score.Value}");
        }

        return builder.ToString();
    }
}

