using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

// Starts Netcode for GameObjects networking. Normally triggered by
// LobbyUIController once a Steam lobby's "start session" call returns a
// host/port to connect to. Also supports an explicit "-mode host|client|server
// -host <ip> -port <port>" command line for scripted/local testing (see
// AutoTestHarness) without going through the in-game menu at all.
[RequireComponent(typeof(NetworkManager))]
public class GameBootstrap : MonoBehaviour
{
    private static readonly Vector3[] SpawnPoints =
    {
        new(0, 1, 0),
        new(3, 1, 0),
        new(-3, 1, 0),
        new(0, 1, 3),
        new(0, 1, -3),
        new(3, 1, 3),
        new(-3, 1, -3),
        new(3, 1, -3),
        new(-3, 1, 3),
        new(6, 1, 0),
        new(-6, 1, 0),
        new(0, 1, 6),
    };

    private NetworkManager networkManager;

    void Awake()
    {
        // GameBootstrap lives on the same GameObject as NetworkManager, so grab
        // it directly rather than via NetworkManager.Singleton — the singleton
        // isn't guaranteed to be assigned yet when this Awake runs.
        networkManager = GetComponent<NetworkManager>();
        networkManager.NetworkConfig.ConnectionApproval = true;
        networkManager.ConnectionApprovalCallback = ApprovalCheck;
        networkManager.OnServerStarted += () => Debug.Log("GameBootstrap: server started.");
        networkManager.OnClientConnectedCallback += id => Debug.Log($"GameBootstrap: client connected ({id}).");
        networkManager.OnClientDisconnectCallback += id => Debug.Log($"GameBootstrap: client disconnected ({id}).");
    }

    void Start()
    {
        // Only auto-start when launched with an explicit "-mode" flag (the
        // scripted test path). Launched bare — by Steam, or by double-clicking
        // the exe — there's no such flag, so this stays idle and waits for the
        // in-game menu (LobbyUIController) to call BeginNetworking() once a
        // player creates or joins a session.
        var args = ParseArgs();

        if (!args.ContainsKey("mode"))
        {
            return;
        }

        string address = args.TryGetValue("host", out var host) ? host : "127.0.0.1";
        ushort port = args.TryGetValue("port", out var portStr) && ushort.TryParse(portStr, out var parsedPort)
            ? parsedPort
            : (ushort)7777;
        string mode = args.TryGetValue("mode", out var m) ? m.ToLowerInvariant() : "host";

        BeginNetworking(mode, address, port);
    }

    // Starts networking in the given role. Called either by Start() (the
    // command-line test path) or by LobbyUIController once the in-game menu's
    // "start session" call returns a unity_host/unity_port to connect to.
    public void BeginNetworking(string mode, string address, ushort port)
    {
        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("GameBootstrap: no UnityTransport found on the NetworkManager.");
            return;
        }

        if (mode == "client")
        {
            transport.SetConnectionData(address, port);
            networkManager.StartClient();
        }
        else if (mode == "server")
        {
            transport.SetConnectionData("0.0.0.0", port, "0.0.0.0");
            networkManager.StartServer();
        }
        else
        {
            transport.SetConnectionData("0.0.0.0", port, "0.0.0.0");
            networkManager.StartHost();
        }
    }

    private void ApprovalCheck(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        int index = networkManager.ConnectedClientsIds.Count % SpawnPoints.Length;

        response.Approved = true;
        response.CreatePlayerObject = true;
        response.Position = SpawnPoints[index];
        response.Rotation = Quaternion.identity;
    }

    private static Dictionary<string, string> ParseArgs()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var rawArgs = Environment.GetCommandLineArgs();

        for (int i = 0; i < rawArgs.Length; i++)
        {
            if (rawArgs[i].StartsWith("-") && i + 1 < rawArgs.Length)
            {
                result[rawArgs[i].TrimStart('-')] = rawArgs[i + 1];
            }
        }

        return result;
    }
}
