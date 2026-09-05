using System.IO;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-off setup tool: builds the NetworkManager + Player prefab wiring for
// Netcode for GameObjects. Run via Tools > MurderMystery > Setup Networking,
// or in batch mode with -executeMethod SetupNetworking.Run.
public static class SetupNetworking
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string PrefabDirectory = "Assets/Prefabs";
    private const string PrefabPath = PrefabDirectory + "/Player.prefab";

    [MenuItem("MurderMystery/Setup Networking")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);

        var playerPrefab = BuildPlayerPrefab();
        BuildNetworkManager(playerPrefab);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("SetupNetworking: NetworkManager + Player prefab configured.");
    }

    private static GameObject BuildPlayerPrefab()
    {
        if (!Directory.Exists(PrefabDirectory))
        {
            Directory.CreateDirectory(PrefabDirectory);
        }

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Player";

        var rb = cube.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        cube.AddComponent<NetworkObject>();
        cube.AddComponent<ClientNetworkTransform>();
        cube.AddComponent<PlayerMovement>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(cube, PrefabPath, out bool success);
        Object.DestroyImmediate(cube);

        if (!success)
        {
            Debug.LogError("SetupNetworking: failed to save Player prefab.");
        }

        return prefab;
    }

    private static void BuildNetworkManager(GameObject playerPrefab)
    {
        var existing = Object.FindObjectOfType<NetworkManager>();
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        var go = new GameObject("NetworkManager");
        var networkManager = go.AddComponent<NetworkManager>();
        var transport = go.AddComponent<UnityTransport>();
        go.AddComponent<GameBootstrap>();

        networkManager.NetworkConfig ??= new NetworkConfig();
        networkManager.NetworkConfig.NetworkTransport = transport;
        networkManager.NetworkConfig.PlayerPrefab = playerPrefab;
        networkManager.NetworkConfig.ConnectionApproval = true;

        transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
    }
}
