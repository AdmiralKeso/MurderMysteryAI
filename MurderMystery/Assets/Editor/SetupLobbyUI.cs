using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One-off setup tool: builds the full in-game menu (main menu, create/join,
// lobby, profile, settings, credits) styled to match game-app's old
// menu.css theme (dark gothic murder-mystery: Cinzel + Crimson Text,
// blood/gold/parchment palette) and wires it to LobbyUIController. Run via
// Tools > MurderMystery > Setup Lobby UI, or in batch mode with
// -executeMethod SetupLobbyUI.Run. Requires Setup Networking to have run
// first (it looks up the existing GameBootstrap in the scene).
public static class SetupLobbyUI
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    private static Font cinzelFont;
    private static Font crimsonFont;
    private static Font crimsonItalicFont;

    private static readonly Color BgBlack = new Color32(11, 10, 8, 255);
    private static readonly Color Blood = new Color32(122, 16, 16, 255);
    private static readonly Color BloodFill = new Color32(28, 12, 12, 255);
    private static readonly Color BloodBright = new Color32(168, 23, 26, 255);
    private static readonly Color Gold = new Color32(201, 163, 74, 255);
    private static readonly Color GoldDim = new Color32(138, 114, 58, 255);
    private static readonly Color Parchment = new Color32(232, 224, 207, 255);
    private static readonly Color Fog = new Color(232f / 255f, 224f / 255f, 207f / 255f, 0.65f);
    private static readonly Color BorderDim = new Color32(58, 51, 42, 255);
    private static readonly Color FieldFill = new Color32(16, 14, 12, 255);
    private static readonly Color ButtonFill = new Color32(24, 21, 18, 255);
    private static readonly Color RowFill = new Color32(20, 18, 16, 255);

    private struct LayoutCursor
    {
        public float Y;
    }

    [MenuItem("MurderMystery/Setup Lobby UI")]
    public static void Run()
    {
        cinzelFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Cinzel.ttf");
        crimsonFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/CrimsonText-Regular.ttf");
        crimsonItalicFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/CrimsonText-Italic.ttf");

        if (cinzelFont == null || crimsonFont == null || crimsonItalicFont == null)
        {
            Debug.LogError("SetupLobbyUI: font assets not found under Assets/Fonts/. Expected Cinzel.ttf, CrimsonText-Regular.ttf, CrimsonText-Italic.ttf.");
            return;
        }

        var scene = EditorSceneManager.OpenScene(ScenePath);

        var gameBootstrap = Object.FindObjectOfType<GameBootstrap>();
        if (gameBootstrap == null)
        {
            Debug.LogError("SetupLobbyUI: no GameBootstrap found in the scene. Run Setup Networking first.");
            return;
        }

        foreach (var name in new[] { "LobbyCanvas", "LobbyController", "SteamManager" })
        {
            var existing = GameObject.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }

        new GameObject("SteamManager", typeof(SteamManager));

        EnsureEventSystem();
        var canvasGo = BuildCanvas();
        BuildBackground(canvasGo.transform);

        var controllerGo = new GameObject("LobbyController");
        var steamLobbyClient = controllerGo.AddComponent<SteamLobbyClient>();
        var lobbyController = controllerGo.AddComponent<LobbyUIController>();
        var autoTestHarness = controllerGo.AddComponent<AutoTestHarness>();

        var mainMenuPanel = BuildPanel(canvasGo.transform, "MainMenuPanel");
        var createPanel = BuildPanel(canvasGo.transform, "CreatePanel");
        var joinPanel = BuildPanel(canvasGo.transform, "JoinPanel");
        var lobbyPanel = BuildPanel(canvasGo.transform, "LobbyPanel");
        var profilePanel = BuildPanel(canvasGo.transform, "ProfilePanel");
        var settingsPanel = BuildPanel(canvasGo.transform, "SettingsPanel");
        var creditsPanel = BuildPanel(canvasGo.transform, "CreditsPanel");

        // Main menu
        var cursor = new LayoutCursor();
        AddTitle(mainMenuPanel.transform, "TitleText", "The Last Witness", ref cursor);
        AddDivider(mainMenuPanel.transform, ref cursor);
        var createSessionButton = AddThemedButton(mainMenuPanel.transform, "CreateSessionButton", "Create Session", ref cursor, true);
        var joinSessionButton = AddThemedButton(mainMenuPanel.transform, "JoinSessionButton", "Join Session", ref cursor, false);
        var profileButton = AddThemedButton(mainMenuPanel.transform, "ProfileButton", "Profile", ref cursor, false);
        var settingsButton = AddThemedButton(mainMenuPanel.transform, "SettingsButton", "Settings", ref cursor, false);
        var creditsButton = AddThemedButton(mainMenuPanel.transform, "CreditsButton", "Credits", ref cursor, false);

        // Create panel
        cursor = new LayoutCursor();
        AddBackLink(createPanel.transform, "BackButtonTop", ref cursor, out var createBackButton);
        AddTitle(createPanel.transform, "TitleText", "Create Session", ref cursor);
        AddSubtitle(createPanel.transform, "SubtitleText", "Set the scene before your guests arrive.", ref cursor);
        AddDivider(createPanel.transform, ref cursor);
        var sessionNameField = AddInputField(createPanel.transform, "SessionNameField", "Session name", ref cursor);
        var maxPlayersField = AddInputField(createPanel.transform, "MaxPlayersField", "Max players (3-12)", ref cursor);
        var scenarioSelector = AddCycleSelector(createPanel.transform, "ScenarioSelector", ref cursor);
        var createButton = AddThemedButton(createPanel.transform, "CreateButton", "Create Session", ref cursor, true);
        var createErrorText = AddErrorText(createPanel.transform, "ErrorText", ref cursor);

        // Join panel
        cursor = new LayoutCursor();
        AddBackLink(joinPanel.transform, "BackButtonTop", ref cursor, out var joinBackButton);
        AddTitle(joinPanel.transform, "TitleText", "Join Session", ref cursor);
        AddSubtitle(joinPanel.transform, "SubtitleText", "Enter the invitation code to step into the mystery.", ref cursor);
        AddDivider(joinPanel.transform, ref cursor);
        var sessionCodeField = AddInputField(joinPanel.transform, "SessionCodeField", "e.g. AB123", ref cursor);
        var joinButton = AddThemedButton(joinPanel.transform, "JoinButton", "Join Session", ref cursor, true);
        var joinErrorText = AddErrorText(joinPanel.transform, "ErrorText", ref cursor);

        // Lobby panel
        cursor = new LayoutCursor();
        var lobbyTitle = AddTitle(lobbyPanel.transform, "TitleText", " ", ref cursor);
        AddSubtitle(lobbyPanel.transform, "SubtitleText", "Share the code below to invite your guests.", ref cursor);
        var roomCodeText = AddRoomCode(lobbyPanel.transform, "RoomCodeText", ref cursor);
        AddDivider(lobbyPanel.transform, ref cursor);
        var playerListText = AddBodyText(lobbyPanel.transform, "PlayerListText", string.Empty, ref cursor, Parchment, 4, TextAnchor.UpperLeft);
        playerListText.supportRichText = true;
        var statusText = AddBodyText(lobbyPanel.transform, "StatusText", string.Empty, ref cursor, Fog, 1.4f, TextAnchor.UpperCenter);
        statusText.fontStyle = FontStyle.Italic;
        var startButton = AddThemedButton(lobbyPanel.transform, "StartButton", "Start Game", ref cursor, true);
        var inviteButton = AddThemedButton(lobbyPanel.transform, "InviteButton", "Invite Friend", ref cursor, false);
        var leaveButton = AddBackLink(lobbyPanel.transform, "LeaveButton", ref cursor, out var leaveButtonComponent);
        var lobbyErrorText = AddErrorText(lobbyPanel.transform, "ErrorText", ref cursor);

        // Profile panel
        cursor = new LayoutCursor();
        AddBackLink(profilePanel.transform, "BackButtonTop", ref cursor, out var profileBackButton);
        var profileNameText = AddTitle(profilePanel.transform, "NameText", "Unknown Detective", ref cursor);
        AddSubtitle(profilePanel.transform, "SubtitleText", "Case history and standing.", ref cursor);
        AddDivider(profilePanel.transform, ref cursor);
        AddStatRow(profilePanel.transform, "SessionsPlayedRow", "Sessions Played", "0", ref cursor);
        AddStatRow(profilePanel.transform, "CasesSolvedRow", "Cases Solved", "0", ref cursor);
        AddStatRow(profilePanel.transform, "TimesMurdererRow", "Times as Murderer", "0", ref cursor);

        // Settings panel
        cursor = new LayoutCursor();
        AddBackLink(settingsPanel.transform, "BackButtonTop", ref cursor, out var settingsBackButton);
        AddTitle(settingsPanel.transform, "TitleText", "Settings", ref cursor);
        AddSubtitle(settingsPanel.transform, "SubtitleText", "Audio, video, and control options.", ref cursor);
        AddDivider(settingsPanel.transform, ref cursor);
        AddFieldLabel(settingsPanel.transform, "MasterVolumeLabel", "Master Volume", ref cursor);
        var masterVolumeSlider = AddSlider(settingsPanel.transform, "MasterVolumeSlider", ref cursor);
        AddFieldLabel(settingsPanel.transform, "EffectsVolumeLabel", "Effects Volume", ref cursor);
        var effectsVolumeSlider = AddSlider(settingsPanel.transform, "EffectsVolumeSlider", ref cursor);
        AddFieldLabel(settingsPanel.transform, "MusicVolumeLabel", "Music Volume", ref cursor);
        var musicVolumeSlider = AddSlider(settingsPanel.transform, "MusicVolumeSlider", ref cursor);
        var screenTypeSelector = AddCycleSelector(settingsPanel.transform, "ScreenTypeSelector", ref cursor);
        var resolutionSelector = AddCycleSelector(settingsPanel.transform, "ResolutionSelector", ref cursor);
        var saveSettingsButton = AddThemedButton(settingsPanel.transform, "SaveButton", "Save Settings", ref cursor, true);

        var settingsController = settingsPanel.AddComponent<SettingsController>();
        var settingsSo = new SerializedObject(settingsController);
        settingsSo.FindProperty("masterVolumeSlider").objectReferenceValue = masterVolumeSlider;
        settingsSo.FindProperty("effectsVolumeSlider").objectReferenceValue = effectsVolumeSlider;
        settingsSo.FindProperty("musicVolumeSlider").objectReferenceValue = musicVolumeSlider;
        settingsSo.FindProperty("screenTypeSelector").objectReferenceValue = screenTypeSelector;
        settingsSo.FindProperty("resolutionSelector").objectReferenceValue = resolutionSelector;
        settingsSo.FindProperty("saveButton").objectReferenceValue = saveSettingsButton;
        settingsSo.ApplyModifiedPropertiesWithoutUndo();

        // Credits panel
        cursor = new LayoutCursor();
        AddBackLink(creditsPanel.transform, "BackButtonTop", ref cursor, out var creditsBackButton);
        AddTitle(creditsPanel.transform, "TitleText", "Credits", ref cursor);
        AddSubtitle(creditsPanel.transform, "SubtitleText", "Written, designed, and investigated by AdmiralKeso.", ref cursor);
        AddDivider(creditsPanel.transform, ref cursor);
        AddCreditsBlock(creditsPanel.transform, "DesignBlock", "Design & Development", "AdmiralKeso", ref cursor);
        AddCreditsBlock(creditsPanel.transform, "BuiltWithBlock", "Built With", "Unity, Netcode for GameObjects, Steamworks.NET", ref cursor);

        createPanel.SetActive(false);
        joinPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        profilePanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);

        var so = new SerializedObject(lobbyController);
        so.FindProperty("steamLobbyClient").objectReferenceValue = steamLobbyClient;
        so.FindProperty("gameBootstrap").objectReferenceValue = gameBootstrap;
        so.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanel;
        so.FindProperty("createPanel").objectReferenceValue = createPanel;
        so.FindProperty("joinPanel").objectReferenceValue = joinPanel;
        so.FindProperty("lobbyPanel").objectReferenceValue = lobbyPanel;
        so.FindProperty("profilePanel").objectReferenceValue = profilePanel;
        so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        so.FindProperty("creditsPanel").objectReferenceValue = creditsPanel;
        so.FindProperty("createSessionButton").objectReferenceValue = createSessionButton;
        so.FindProperty("joinSessionButton").objectReferenceValue = joinSessionButton;
        so.FindProperty("profileButton").objectReferenceValue = profileButton;
        so.FindProperty("settingsButton").objectReferenceValue = settingsButton;
        so.FindProperty("creditsButton").objectReferenceValue = creditsButton;
        so.FindProperty("sessionNameField").objectReferenceValue = sessionNameField;
        so.FindProperty("maxPlayersField").objectReferenceValue = maxPlayersField;
        so.FindProperty("scenarioSelector").objectReferenceValue = scenarioSelector;
        so.FindProperty("createButton").objectReferenceValue = createButton;
        so.FindProperty("createBackButton").objectReferenceValue = createBackButton;
        so.FindProperty("createErrorText").objectReferenceValue = createErrorText;
        so.FindProperty("sessionCodeField").objectReferenceValue = sessionCodeField;
        so.FindProperty("joinButton").objectReferenceValue = joinButton;
        so.FindProperty("joinBackButton").objectReferenceValue = joinBackButton;
        so.FindProperty("joinErrorText").objectReferenceValue = joinErrorText;
        so.FindProperty("roomTitleText").objectReferenceValue = lobbyTitle;
        so.FindProperty("roomCodeText").objectReferenceValue = roomCodeText;
        so.FindProperty("playerListText").objectReferenceValue = playerListText;
        so.FindProperty("statusText").objectReferenceValue = statusText;
        so.FindProperty("startButton").objectReferenceValue = startButton;
        so.FindProperty("inviteButton").objectReferenceValue = inviteButton;
        so.FindProperty("leaveButton").objectReferenceValue = leaveButtonComponent;
        so.FindProperty("lobbyErrorText").objectReferenceValue = lobbyErrorText;
        so.FindProperty("profileNameText").objectReferenceValue = profileNameText;
        so.FindProperty("profileBackButton").objectReferenceValue = profileBackButton;
        so.FindProperty("settingsBackButton").objectReferenceValue = settingsBackButton;
        so.FindProperty("creditsBackButton").objectReferenceValue = creditsBackButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        var autoTestSo = new SerializedObject(autoTestHarness);
        autoTestSo.FindProperty("steamLobbyClient").objectReferenceValue = steamLobbyClient;
        autoTestSo.FindProperty("gameBootstrap").objectReferenceValue = gameBootstrap;
        autoTestSo.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("SetupLobbyUI: lobby menu UI configured.");
    }

    // Approximates CSS letter-spacing (legacy UI.Text has no such property)
    // by inserting a thin space between characters, and uppercases to match
    // text-transform: uppercase on the original nav/buttons/labels.
    private static string Spaced(string s)
    {
        return string.Join(" ", s.ToUpperInvariant().ToCharArray());
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static GameObject BuildCanvas()
    {
        var go = new GameObject("LobbyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        // Match by height, not width: on an ultrawide monitor, matching width
        // blows the UI up far taller than the screen (e.g. 2.7x on a 3440px-
        // wide display vs the 1280px reference), pushing content off-screen.
        // A menu is fundamentally a vertical column, so height is what matters.
        scaler.matchWidthOrHeight = 1f;

        return go;
    }

    private static void BuildBackground(Transform parent)
    {
        var go = new GameObject("Background", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = BgBlack;
    }

    private static GameObject BuildPanel(Transform parent, string name)
    {
        // Unlike a bordered card, the original page has no wrapper box around
        // the whole menu — content floats directly on the dark background,
        // with only individual elements (buttons, fields, room code) framed.
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(480, 640);
        rect.anchoredPosition = Vector2.zero;

        return go;
    }

    private static RectTransform AddRow(Transform parent, string name, ref LayoutCursor cursor, float height)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.sizeDelta = new Vector2(-20, height);
        rect.anchoredPosition = new Vector2(0, -cursor.Y - 10);

        cursor.Y += height + 14;

        return rect;
    }

    private static Text AddTitle(Transform parent, string name, string content, ref LayoutCursor cursor)
    {
        var row = AddRow(parent, name, ref cursor, 48);
        var text = row.gameObject.AddComponent<Text>();
        text.font = cinzelFont;
        text.fontStyle = FontStyle.Bold;
        text.fontSize = 34;
        text.text = Spaced(content);
        text.color = Parchment;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        return text;
    }

    private static void AddSubtitle(Transform parent, string name, string content, ref LayoutCursor cursor)
    {
        var row = AddRow(parent, name, ref cursor, 26);
        var text = row.gameObject.AddComponent<Text>();
        text.font = crimsonItalicFont;
        text.fontSize = 16;
        text.text = content;
        text.color = Fog;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
    }

    private static void AddDivider(Transform parent, ref LayoutCursor cursor)
    {
        var row = AddRow(parent, "Divider", ref cursor, 12);
        var go = new GameObject("Line", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(row, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(120, 1);
        go.GetComponent<Image>().color = GoldDim;
    }

    private static Text AddRoomCode(Transform parent, string name, ref LayoutCursor cursor)
    {
        var row = AddRow(parent, name, ref cursor, 44);
        AddFramedBackground(row, GoldDim, RowFill);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(row, false);
        StretchFill(textGo.GetComponent<RectTransform>(), 4, 4);
        var text = textGo.GetComponent<Text>();
        text.font = cinzelFont;
        text.fontSize = 20;
        text.color = Gold;
        text.alignment = TextAnchor.MiddleCenter;
        return text;
    }

    private static Text AddErrorText(Transform parent, string name, ref LayoutCursor cursor)
    {
        var row = AddRow(parent, name, ref cursor, 24);
        var text = row.gameObject.AddComponent<Text>();
        text.font = crimsonFont;
        text.fontSize = 15;
        text.color = BloodBright;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        return text;
    }

    private static Text AddBodyText(Transform parent, string name, string initial, ref LayoutCursor cursor, Color color, float heightMultiplier, TextAnchor alignment)
    {
        var row = AddRow(parent, name, ref cursor, 26 * heightMultiplier);
        var text = row.gameObject.AddComponent<Text>();
        text.font = crimsonFont;
        text.fontSize = 16;
        text.text = initial;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void AddFieldLabel(Transform parent, string name, string label, ref LayoutCursor cursor)
    {
        var row = AddRow(parent, name, ref cursor, 20);
        var text = row.gameObject.AddComponent<Text>();
        text.font = cinzelFont;
        text.fontSize = 12;
        text.text = Spaced(label);
        text.color = GoldDim;
        text.alignment = TextAnchor.LowerLeft;
    }

    private static void AddStatRow(Transform parent, string name, string label, string value, ref LayoutCursor cursor)
    {
        var row = AddRow(parent, name, ref cursor, 40);
        AddFramedBackground(row, BorderDim, RowFill);

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(row, false);
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(0.6f, 1);
        labelRect.offsetMin = new Vector2(14, 0);
        labelRect.offsetMax = Vector2.zero;
        var labelText = labelGo.GetComponent<Text>();
        labelText.font = crimsonFont;
        labelText.fontSize = 16;
        labelText.text = label;
        labelText.color = Fog;
        labelText.alignment = TextAnchor.MiddleLeft;

        var valueGo = new GameObject("Value", typeof(RectTransform), typeof(Text));
        valueGo.transform.SetParent(row, false);
        var valueRect = valueGo.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0.6f, 0);
        valueRect.anchorMax = new Vector2(1, 1);
        valueRect.offsetMin = Vector2.zero;
        valueRect.offsetMax = new Vector2(-14, 0);
        var valueText = valueGo.GetComponent<Text>();
        valueText.font = cinzelFont;
        valueText.fontSize = 16;
        valueText.text = value;
        valueText.color = Gold;
        valueText.alignment = TextAnchor.MiddleRight;
    }

    private static void AddCreditsBlock(Transform parent, string name, string header, string body, ref LayoutCursor cursor)
    {
        var headerRow = AddRow(parent, name + "Header", ref cursor, 22);
        var headerText = headerRow.gameObject.AddComponent<Text>();
        headerText.font = cinzelFont;
        headerText.fontSize = 14;
        headerText.text = Spaced(header);
        headerText.color = Gold;
        headerText.alignment = TextAnchor.MiddleLeft;

        var bodyRow = AddRow(parent, name + "Body", ref cursor, 28);
        var bodyText = bodyRow.gameObject.AddComponent<Text>();
        bodyText.font = crimsonFont;
        bodyText.fontSize = 16;
        bodyText.text = body;
        bodyText.color = Fog;
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
    }

    // Two stacked Images (a border-colored one behind a slightly inset,
    // OPAQUE fill-colored one) approximate a CSS 1px border box without
    // needing sliced border sprites. The fill must be fully opaque — any
    // transparency lets the border color underneath bleed through the whole
    // box instead of just the visible edge ring.
    private static void AddFramedBackground(Transform row, Color borderColor, Color fillColor)
    {
        var border = row.gameObject.AddComponent<Image>();
        border.color = borderColor;

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(row, false);
        StretchFill(fillGo.GetComponent<RectTransform>(), 1, 1);
        var fillColorOpaque = fillColor;
        fillColorOpaque.a = 1f;
        fillGo.GetComponent<Image>().color = fillColorOpaque;
    }

    private static InputField AddInputField(Transform parent, string name, string placeholder, ref LayoutCursor cursor)
    {
        var row = AddRow(parent, name, ref cursor, 40);
        AddFramedBackground(row, BorderDim, FieldFill);
        var inputField = row.gameObject.AddComponent<InputField>();
        inputField.targetGraphic = row.GetComponent<Image>();

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(row, false);
        StretchFill(textGo.GetComponent<RectTransform>(), 12, 6);
        var text = textGo.GetComponent<Text>();
        text.font = crimsonFont;
        text.fontSize = 17;
        text.color = Parchment;
        text.alignment = TextAnchor.MiddleLeft;
        text.supportRichText = false;

        var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
        placeholderGo.transform.SetParent(row, false);
        StretchFill(placeholderGo.GetComponent<RectTransform>(), 12, 6);
        var placeholderText = placeholderGo.GetComponent<Text>();
        placeholderText.font = crimsonItalicFont;
        placeholderText.fontSize = 17;
        placeholderText.text = placeholder;
        placeholderText.color = Fog;
        placeholderText.alignment = TextAnchor.MiddleLeft;

        inputField.textComponent = text;
        inputField.placeholder = placeholderText;

        return inputField;
    }

    private static Button AddThemedButton(Transform parent, string name, string label, ref LayoutCursor cursor, bool primary)
    {
        var row = AddRow(parent, name, ref cursor, 46);
        AddFramedBackground(row, primary ? Blood : BorderDim, primary ? BloodFill : ButtonFill);

        var button = row.gameObject.AddComponent<Button>();
        button.targetGraphic = row.GetComponent<Image>();
        var colors = button.colors;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        button.colors = colors;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(row, false);
        StretchFill(textGo.GetComponent<RectTransform>(), 0, 0);
        var text = textGo.GetComponent<Text>();
        text.font = cinzelFont;
        text.fontSize = 15;
        text.text = Spaced(label);
        text.color = Parchment;
        text.alignment = TextAnchor.MiddleCenter;

        return button;
    }

    // Text-only, borderless link — matches the original's plain "back to
    // menu" link style rather than a full boxed button.
    private static Text AddBackLink(Transform parent, string name, ref LayoutCursor cursor, out Button button)
    {
        var row = AddRow(parent, name, ref cursor, 22);
        var text = row.gameObject.AddComponent<Text>();
        text.font = cinzelFont;
        text.fontSize = 12;
        text.text = Spaced(name.StartsWith("Leave") ? "Leave Session" : "Back to Menu");
        text.color = Fog;
        text.alignment = TextAnchor.MiddleLeft;

        button = row.gameObject.AddComponent<Button>();
        button.targetGraphic = text;

        return text;
    }

    private static CycleSelector AddCycleSelector(Transform parent, string name, ref LayoutCursor cursor)
    {
        var row = AddRow(parent, name, ref cursor, 40);
        AddFramedBackground(row, BorderDim, ButtonFill);
        var button = row.gameObject.AddComponent<Button>();
        button.targetGraphic = row.GetComponent<Image>();
        var selector = row.gameObject.AddComponent<CycleSelector>();

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(row, false);
        StretchFill(textGo.GetComponent<RectTransform>(), 12, 0);
        var text = textGo.GetComponent<Text>();
        text.font = cinzelFont;
        text.fontSize = 14;
        text.color = Parchment;
        text.alignment = TextAnchor.MiddleCenter;

        var selectorSo = new SerializedObject(selector);
        selectorSo.FindProperty("valueText").objectReferenceValue = text;
        selectorSo.FindProperty("button").objectReferenceValue = button;
        selectorSo.ApplyModifiedPropertiesWithoutUndo();

        return selector;
    }

    private static Slider AddSlider(Transform parent, string name, ref LayoutCursor cursor)
    {
        var row = AddRow(parent, name, ref cursor, 24);
        var slider = row.gameObject.AddComponent<Slider>();

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(row, false);
        StretchFill(background.GetComponent<RectTransform>(), 0, 8);
        background.GetComponent<Image>().color = FieldFill;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(row, false);
        StretchFill(fillArea.GetComponent<RectTransform>(), 4, 8);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.sizeDelta = new Vector2(10, 0);
        fill.GetComponent<Image>().color = Gold;

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(row, false);
        StretchFill(handleArea.GetComponent<RectTransform>(), 8, 0);

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(14, 24);
        handle.GetComponent<Image>().color = Parchment;

        slider.targetGraphic = handle.GetComponent<Image>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.value = 80;

        return slider;
    }

    private static void StretchFill(RectTransform rect, float horizontalPadding, float verticalPadding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontalPadding, verticalPadding);
        rect.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
    }
}
