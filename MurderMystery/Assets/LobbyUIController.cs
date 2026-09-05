using System.Collections;
using System.Text;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;

// The in-game menu: main menu (create/join/profile/settings/credits), create/
// join a session via SteamLobbyClient, the lobby itself, and the hand-off to
// GameBootstrap.BeginNetworking() once the host starts the game. This is the
// full replacement for game-app's Blade views — no browser or Laravel
// backend involved at all.
public class LobbyUIController : MonoBehaviour
{
    [SerializeField] private SteamLobbyClient steamLobbyClient;
    [SerializeField] private GameBootstrap gameBootstrap;

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject createPanel;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject profilePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Main Menu")]
    [SerializeField] private Button createSessionButton;
    [SerializeField] private Button joinSessionButton;
    [SerializeField] private Button profileButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;

    [Header("Create Panel")]
    [SerializeField] private InputField sessionNameField;
    [SerializeField] private InputField maxPlayersField;
    [SerializeField] private CycleSelector scenarioSelector;
    [SerializeField] private Button createButton;
    [SerializeField] private Button createBackButton;
    [SerializeField] private Text createErrorText;

    [Header("Join Panel")]
    [SerializeField] private InputField sessionCodeField;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button joinBackButton;
    [SerializeField] private Text joinErrorText;

    [Header("Lobby Panel")]
    [SerializeField] private Text roomTitleText;
    [SerializeField] private Text roomCodeText;
    [SerializeField] private Text playerListText;
    [SerializeField] private Text statusText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button inviteButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Text lobbyErrorText;

    [Header("Profile Panel")]
    [SerializeField] private Text profileNameText;
    [SerializeField] private Button profileBackButton;

    [Header("Settings Panel")]
    [SerializeField] private Button settingsBackButton;

    [Header("Credits Panel")]
    [SerializeField] private Button creditsBackButton;

    private string sessionCode;
    private bool isHost;
    private bool polling;

    void Awake()
    {
        createSessionButton.onClick.AddListener(ShowCreatePanel);
        joinSessionButton.onClick.AddListener(ShowJoinPanel);
        profileButton.onClick.AddListener(ShowProfilePanel);
        settingsButton.onClick.AddListener(() => SwitchTo(settingsPanel));
        creditsButton.onClick.AddListener(() => SwitchTo(creditsPanel));

        createBackButton.onClick.AddListener(ShowMainMenu);
        joinBackButton.onClick.AddListener(ShowMainMenu);
        profileBackButton.onClick.AddListener(ShowMainMenu);
        settingsBackButton.onClick.AddListener(ShowMainMenu);
        creditsBackButton.onClick.AddListener(ShowMainMenu);

        createButton.onClick.AddListener(OnCreateClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
        startButton.onClick.AddListener(OnStartClicked);
        inviteButton.onClick.AddListener(() => steamLobbyClient.InvitePanel());
        leaveButton.onClick.AddListener(OnLeaveClicked);

        scenarioSelector.Setup(
            new[] { "Random", "The Manor Murder", "Death on the Cruise" },
            new[] { "random", "manor", "cruise" });

        ShowMainMenu();
    }

    private void SwitchTo(GameObject panel)
    {
        mainMenuPanel.SetActive(false);
        panel.SetActive(true);
    }

    private void ShowMainMenu()
    {
        polling = false;
        mainMenuPanel.SetActive(true);
        createPanel.SetActive(false);
        joinPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        profilePanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
    }

    private void ShowCreatePanel()
    {
        SwitchTo(createPanel);
        createErrorText.text = string.Empty;
    }

    private void ShowJoinPanel()
    {
        SwitchTo(joinPanel);
        joinErrorText.text = string.Empty;
    }

    private void ShowProfilePanel()
    {
        SwitchTo(profilePanel);
        string name = SteamManager.Initialized ? SteamFriends.GetPersonaName() : "Unknown Detective";
        profileNameText.text = string.Join(" ", name.ToUpperInvariant().ToCharArray());
    }

    private void ShowLobbyPanel()
    {
        createPanel.SetActive(false);
        joinPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        lobbyErrorText.text = string.Empty;
        roomCodeText.text = sessionCode;
        startButton.gameObject.SetActive(isHost);
        polling = true;
        StartCoroutine(PollLoop());
    }

    private void OnCreateClicked()
    {
        string sessionName = string.IsNullOrWhiteSpace(sessionNameField.text) ? "New Session" : sessionNameField.text;
        int maxPlayers = int.TryParse(maxPlayersField.text, out int parsed) ? Mathf.Clamp(parsed, 3, 12) : 6;
        string scenario = scenarioSelector.CurrentValue;

        createButton.interactable = false;
        steamLobbyClient.CreateSession(
            sessionName,
            maxPlayers,
            scenario,
            response =>
            {
                createButton.interactable = true;
                sessionCode = response.code;
                isHost = response.is_host;
                ShowLobbyPanel();
            },
            error =>
            {
                createButton.interactable = true;
                createErrorText.text = error;
            });
    }

    private void OnJoinClicked()
    {
        string code = sessionCodeField.text.Trim().ToUpperInvariant();

        joinButton.interactable = false;
        steamLobbyClient.JoinSession(
            code,
            response =>
            {
                joinButton.interactable = true;
                sessionCode = response.code;
                isHost = response.is_host;
                ShowLobbyPanel();
            },
            error =>
            {
                joinButton.interactable = true;
                joinErrorText.text = error;
            });
    }

    private void OnStartClicked()
    {
        startButton.interactable = false;
        steamLobbyClient.StartSession(
            response => { }, // the poll loop below reacts to status == in_progress
            error =>
            {
                startButton.interactable = true;
                lobbyErrorText.text = error;
            });
    }

    private void OnLeaveClicked()
    {
        steamLobbyClient.LeaveSession();
        ShowMainMenu();
    }

    private IEnumerator PollLoop()
    {
        while (polling)
        {
            steamLobbyClient.GetSessionStatus(OnStatus, error => lobbyErrorText.text = error);
            yield return new WaitForSeconds(2.5f);
        }
    }

    private void OnStatus(SessionStatusResponse status)
    {
        isHost = status.is_host;
        startButton.gameObject.SetActive(isHost);

        if (!string.IsNullOrEmpty(status.name))
        {
            roomTitleText.text = string.Join(" ", status.name.ToUpperInvariant().ToCharArray());
        }

        var sb = new StringBuilder();
        foreach (var player in status.players)
        {
            sb.AppendLine(player.display_name + (player.is_host ? "   <color=#C9A34A>HOST</color>" : string.Empty));
        }

        playerListText.text = sb.ToString();

        if (status.status == "lobby")
        {
            statusText.text = $"Waiting for players... ({status.players.Length}/{status.max_players})";
            startButton.interactable = status.players.Length >= 3;
        }
        else if (status.status == "in_progress")
        {
            polling = false;
            statusText.text = "Connecting...";
            lobbyPanel.SetActive(false);
            gameBootstrap.BeginNetworking(isHost ? "host" : "client", status.unity_host, (ushort)status.unity_port);
        }
    }
}
