// Uncomment the line below to switch back to real Steam matchmaking. Steam
// ties lobby membership to a SteamID, and one Steam account can't represent
// two distinct lobby members — that blocked testing 3+ "players" locally
// with multiple .exe copies under one account. Disabled for now; the local
// stand-in below (a shared JSON file, since all instances are one machine)
// keeps the exact same class/method surface so LobbyUIController and
// AutoTestHarness don't need to change either way.
// #define USE_STEAM_LOBBY

using System;
using System.Collections.Generic;
using System.IO;
#if USE_STEAM_LOBBY
using System.Net;
using System.Net.Sockets;
using Steamworks;
#endif
using UnityEngine;

public class SteamLobbyClient : MonoBehaviour
{
#if USE_STEAM_LOBBY
    private const string KeyName = "name";
    private const string KeyScenario = "scenario";
    private const string KeyMaxPlayers = "max_players";
    private const string KeyStatus = "status";
    private const string KeyHost = "unity_host";
    private const string KeyPort = "unity_port";
    private const string KeyJoinCode = "join_code";
    private const ushort GamePort = 7777;

    // I/O and 0/1 excluded to avoid ambiguity when read aloud or typed.
    private const string CodeLetters = "ABCDEFGHJKLMNPQRSTUVWXYZ";

    private CSteamID currentLobby;

    private Callback<LobbyCreated_t> lobbyCreatedCallback;
    private Callback<LobbyEnter_t> lobbyEnteredCallback;
    private Callback<LobbyMatchList_t> lobbyMatchListCallback;

    private Action<CreateSessionResponse> onCreateSuccess;
    private Action<string> onCreateError;
    private Action<JoinSessionResponse> onJoinSuccess;
    private Action<string> onJoinError;

    private string pendingSessionName;
    private string pendingScenario;
    private int pendingMaxPlayers;
    private string pendingJoinCode;

    void Awake()
    {
        lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        lobbyEnteredCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyMatchListCallback = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
    }

    public void CreateSession(
        string sessionName,
        int maxPlayers,
        string scenario,
        Action<CreateSessionResponse> onSuccess,
        Action<string> onError)
    {
        if (!SteamManager.Initialized)
        {
            onError?.Invoke("Steam is not running. Launch Steam and try again.");
            return;
        }

        onCreateSuccess = onSuccess;
        onCreateError = onError;
        pendingSessionName = sessionName;
        pendingScenario = scenario;
        pendingMaxPlayers = maxPlayers;

        // Public (not FriendsOnly) so a join code works for any Steam
        // account, not just the host's Steam friends.
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, maxPlayers);
    }

    public void JoinSession(string code, Action<JoinSessionResponse> onSuccess, Action<string> onError)
    {
        if (!SteamManager.Initialized)
        {
            onError?.Invoke("Steam is not running. Launch Steam and try again.");
            return;
        }

        onJoinSuccess = onSuccess;
        onJoinError = onError;
        pendingJoinCode = code.Trim().ToUpperInvariant();

        SteamMatchmaking.AddRequestLobbyListStringFilter(KeyJoinCode, pendingJoinCode, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.RequestLobbyList();
    }

    public void GetSessionStatus(Action<SessionStatusResponse> onSuccess, Action<string> onError)
    {
        if (!SteamManager.Initialized)
        {
            onError?.Invoke("Steam is not running.");
            return;
        }

        var lobby = currentLobby;
        var localId = SteamUser.GetSteamID();
        var owner = SteamMatchmaking.GetLobbyOwner(lobby);

        int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobby);
        var players = new List<SessionPlayerDto>(memberCount);

        for (int i = 0; i < memberCount; i++)
        {
            var memberId = SteamMatchmaking.GetLobbyMemberByIndex(lobby, i);

            players.Add(new SessionPlayerDto
            {
                id = unchecked((int)memberId.m_SteamID),
                display_name = memberId == localId ? SteamFriends.GetPersonaName() : SteamFriends.GetFriendPersonaName(memberId),
                is_host = memberId == owner,
            });
        }

        string status = SteamMatchmaking.GetLobbyData(lobby, KeyStatus);
        int.TryParse(SteamMatchmaking.GetLobbyData(lobby, KeyMaxPlayers), out int maxPlayers);
        int.TryParse(SteamMatchmaking.GetLobbyData(lobby, KeyPort), out int port);

        onSuccess?.Invoke(new SessionStatusResponse
        {
            name = SteamMatchmaking.GetLobbyData(lobby, KeyName),
            status = string.IsNullOrEmpty(status) ? "lobby" : status,
            players = players.ToArray(),
            max_players = maxPlayers,
            unity_host = SteamMatchmaking.GetLobbyData(lobby, KeyHost),
            unity_port = port,
            is_host = owner == localId,
        });
    }

    public void LeaveSession()
    {
        if (currentLobby.IsValid())
        {
            SteamMatchmaking.LeaveLobby(currentLobby);
        }

        currentLobby = CSteamID.Nil;
    }

    public void InvitePanel()
    {
        if (currentLobby.IsValid())
        {
            SteamFriends.ActivateGameOverlayInviteDialog(currentLobby);
        }
    }

    public void StartSession(Action<StartSessionResponse> onSuccess, Action<string> onError)
    {
        var lobby = currentLobby;

        if (SteamMatchmaking.GetLobbyOwner(lobby) != SteamUser.GetSteamID())
        {
            onError?.Invoke("Only the host can start the game.");
            return;
        }

        if (SteamMatchmaking.GetNumLobbyMembers(lobby) < 3)
        {
            onError?.Invoke("At least 3 players are needed to start.");
            return;
        }

        string hostAddress = GetLocalIPv4();

        SteamMatchmaking.SetLobbyData(lobby, KeyHost, hostAddress);
        SteamMatchmaking.SetLobbyData(lobby, KeyPort, GamePort.ToString());
        SteamMatchmaking.SetLobbyData(lobby, KeyStatus, "in_progress");

        onSuccess?.Invoke(new StartSessionResponse
        {
            status = "in_progress",
            unity_host = hostAddress,
            unity_port = GamePort,
        });
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            onCreateError?.Invoke($"Failed to create lobby: {callback.m_eResult}");
            onCreateSuccess = null;
            onCreateError = null;
            return;
        }

        currentLobby = new CSteamID(callback.m_ulSteamIDLobby);

        string joinCode = GenerateJoinCode();

        SteamMatchmaking.SetLobbyData(currentLobby, KeyName, pendingSessionName);
        SteamMatchmaking.SetLobbyData(currentLobby, KeyScenario, pendingScenario);
        SteamMatchmaking.SetLobbyData(currentLobby, KeyMaxPlayers, pendingMaxPlayers.ToString());
        SteamMatchmaking.SetLobbyData(currentLobby, KeyStatus, "lobby");
        SteamMatchmaking.SetLobbyData(currentLobby, KeyJoinCode, joinCode);

        onCreateSuccess?.Invoke(new CreateSessionResponse
        {
            code = joinCode,
            player_token = string.Empty,
            is_host = true,
        });

        onCreateSuccess = null;
        onCreateError = null;
    }

    private void OnLobbyMatchList(LobbyMatchList_t callback)
    {
        // Not something we initiated (e.g. a stray callback from elsewhere).
        if (pendingJoinCode == null)
        {
            return;
        }

        pendingJoinCode = null;

        if (callback.m_nLobbiesMatching == 0)
        {
            onJoinError?.Invoke("No session was found with that code.");
            onJoinSuccess = null;
            onJoinError = null;
            return;
        }

        SteamMatchmaking.JoinLobby(SteamMatchmaking.GetLobbyByIndex(0));
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        currentLobby = new CSteamID(callback.m_ulSteamIDLobby);

        // CreateLobby also auto-enters the creator, firing this same callback;
        // OnLobbyCreated already reported success for that case, so only react
        // here when this came from an explicit JoinSession() call.
        if (onJoinSuccess == null && onJoinError == null)
        {
            return;
        }

        if ((EChatRoomEnterResponse)callback.m_EChatRoomEnterResponse != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            onJoinError?.Invoke($"Could not join session: {(EChatRoomEnterResponse)callback.m_EChatRoomEnterResponse}");
            onJoinSuccess = null;
            onJoinError = null;
            return;
        }

        onJoinSuccess?.Invoke(new JoinSessionResponse
        {
            code = SteamMatchmaking.GetLobbyData(currentLobby, KeyJoinCode),
            player_token = string.Empty,
            is_host = SteamMatchmaking.GetLobbyOwner(currentLobby) == SteamUser.GetSteamID(),
        });

        onJoinSuccess = null;
        onJoinError = null;
    }

    private static string GetLocalIPv4()
    {
        foreach (var address in Dns.GetHostAddresses(Dns.GetHostName()))
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                return address.ToString();
            }
        }

        return "127.0.0.1";
    }
#else
    // ---- Steam auth disabled: local-only stand-in ----
    // Every .exe instance on this machine reads/writes the same JSON file
    // under Application.persistentDataPath, which is how separate processes
    // "see" each other's sessions without any Steam identity involved.

    [Serializable]
    private class PlayerRecord
    {
        public string id;
        public string display_name;
        public bool is_host;
    }

    [Serializable]
    private class SessionRecord
    {
        public string code;
        public string name;
        public string scenario;
        public int max_players;
        public string status;
        public string unity_host;
        public int unity_port;
        public List<PlayerRecord> players = new List<PlayerRecord>();
    }

    [Serializable]
    private class SessionFile
    {
        public List<SessionRecord> sessions = new List<SessionRecord>();
    }

    private const string CodeLetters = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private static string FilePath => Path.Combine(Application.persistentDataPath, "local_sessions.json");

    private string localPlayerId;
    private string localDisplayName;
    private string currentCode;

    void Awake()
    {
        // UnityEngine.Random can't be called from a field initializer.
        localPlayerId = Guid.NewGuid().ToString();
        localDisplayName = "Player" + UnityEngine.Random.Range(1000, 9999);
    }

    public void CreateSession(
        string sessionName,
        int maxPlayers,
        string scenario,
        Action<CreateSessionResponse> onSuccess,
        Action<string> onError)
    {
        string code = GenerateJoinCode();
        var file = ReadFile();

        var record = new SessionRecord
        {
            code = code,
            name = sessionName,
            scenario = scenario,
            max_players = maxPlayers,
            status = "lobby",
            unity_host = string.Empty,
            unity_port = 0,
        };
        record.players.Add(new PlayerRecord { id = localPlayerId, display_name = localDisplayName, is_host = true });

        file.sessions.Add(record);
        WriteFile(file);

        currentCode = code;
        onSuccess?.Invoke(new CreateSessionResponse { code = code, player_token = string.Empty, is_host = true });
    }

    public void JoinSession(string code, Action<JoinSessionResponse> onSuccess, Action<string> onError)
    {
        code = code.Trim().ToUpperInvariant();
        var file = ReadFile();
        var record = file.sessions.Find(s => s.code == code);

        if (record == null)
        {
            onError?.Invoke("No session was found with that code.");
            return;
        }

        if (record.status != "lobby")
        {
            onError?.Invoke("That session has already started.");
            return;
        }

        if (record.players.Count >= record.max_players)
        {
            onError?.Invoke("That session is full.");
            return;
        }

        record.players.Add(new PlayerRecord { id = localPlayerId, display_name = localDisplayName, is_host = false });
        WriteFile(file);

        currentCode = code;
        onSuccess?.Invoke(new JoinSessionResponse { code = code, player_token = string.Empty, is_host = false });
    }

    public void GetSessionStatus(Action<SessionStatusResponse> onSuccess, Action<string> onError)
    {
        var file = ReadFile();
        var record = file.sessions.Find(s => s.code == currentCode);

        if (record == null)
        {
            onError?.Invoke("Session no longer exists.");
            return;
        }

        bool isHost = record.players.Exists(p => p.id == localPlayerId && p.is_host);
        var players = record.players.ConvertAll(p => new SessionPlayerDto
        {
            id = p.id.GetHashCode(),
            display_name = p.display_name,
            is_host = p.is_host,
        });

        onSuccess?.Invoke(new SessionStatusResponse
        {
            name = record.name,
            status = record.status,
            players = players.ToArray(),
            max_players = record.max_players,
            unity_host = record.unity_host,
            unity_port = record.unity_port,
            is_host = isHost,
        });
    }

    public void StartSession(Action<StartSessionResponse> onSuccess, Action<string> onError)
    {
        var file = ReadFile();
        var record = file.sessions.Find(s => s.code == currentCode);

        if (record == null)
        {
            onError?.Invoke("Session no longer exists.");
            return;
        }

        bool isHost = record.players.Exists(p => p.id == localPlayerId && p.is_host);
        if (!isHost)
        {
            onError?.Invoke("Only the host can start the game.");
            return;
        }

        if (record.players.Count < 3)
        {
            onError?.Invoke("At least 3 players are needed to start.");
            return;
        }

        // Same-machine only (no Steam relay involved), so loopback is always
        // correct here — also sidesteps the Windows Firewall/wrong-adapter
        // pitfalls of guessing a real LAN IP.
        record.unity_host = "127.0.0.1";
        record.unity_port = 7777;
        record.status = "in_progress";
        WriteFile(file);

        onSuccess?.Invoke(new StartSessionResponse
        {
            status = "in_progress",
            unity_host = record.unity_host,
            unity_port = record.unity_port,
        });
    }

    public void LeaveSession()
    {
        var file = ReadFile();
        var record = file.sessions.Find(s => s.code == currentCode);

        if (record != null)
        {
            record.players.RemoveAll(p => p.id == localPlayerId);

            if (record.players.Count == 0)
            {
                file.sessions.Remove(record);
            }

            WriteFile(file);
        }

        currentCode = null;
    }

    public void InvitePanel()
    {
        GUIUtility.systemCopyBuffer = currentCode ?? string.Empty;
    }

    private static string GenerateJoinCode()
    {
        var rng = new System.Random();
        char l1 = CodeLetters[rng.Next(CodeLetters.Length)];
        char l2 = CodeLetters[rng.Next(CodeLetters.Length)];
        string digits = rng.Next(0, 1000).ToString("D3");
        return $"{l1}{l2}{digits}";
    }

    private static SessionFile ReadFile()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new SessionFile();
            }

            string json = File.ReadAllText(FilePath);
            return string.IsNullOrEmpty(json) ? new SessionFile() : JsonUtility.FromJson<SessionFile>(json);
        }
        catch
        {
            return new SessionFile();
        }
    }

    private static void WriteFile(SessionFile file)
    {
        string json = JsonUtility.ToJson(file);

        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                File.WriteAllText(FilePath, json);
                return;
            }
            catch (IOException)
            {
                System.Threading.Thread.Sleep(20);
            }
        }
    }
#endif
}
