using System;
using System.Collections;
using UnityEngine;

// Dev-only automated smoke test for the Steam-backed lobby flow. Launch with
// "-autotest host" to verify SteamManager initializes against the running
// Steam client and that CreateSession/GetSessionStatus round-trip correctly
// through real Steam matchmaking calls.
//
// Note: unlike the old Laravel-backed test, this can only exercise a single
// Steam identity — one Steam account can't represent two distinct lobby
// members on one machine, so verifying an actual second player joining (and
// therefore the "start game" / BeginNetworking hand-off) needs a real second
// Steam account, ideally on a second machine.
public class AutoTestHarness : MonoBehaviour
{
    [SerializeField] private SteamLobbyClient steamLobbyClient;
    [SerializeField] private GameBootstrap gameBootstrap;

    void Start()
    {
        var args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-autotest");

        if (index < 0 || index + 1 >= args.Length || args[index + 1] != "host")
        {
            return;
        }

        StartCoroutine(RunSmokeTest());
    }

    private IEnumerator RunSmokeTest()
    {
        Debug.Log($"AutoTestHarness: SteamManager.Initialized = {SteamManager.Initialized}");

        if (!SteamManager.Initialized)
        {
            Debug.LogError("AutoTestHarness: Steam is not initialized — is the Steam client running and logged in, with steam_appid.txt present?");
            yield break;
        }

        Debug.Log("AutoTestHarness: creating session...");

        string code = null;
        bool failed = false;

        steamLobbyClient.CreateSession(
            "AutoTest Session",
            6,
            "random",
            resp => code = resp.code,
            err =>
            {
                Debug.LogError($"AutoTestHarness: create failed: {err}");
                failed = true;
            });

        yield return new WaitUntil(() => code != null || failed);
        if (failed)
        {
            yield break;
        }

        Debug.Log($"AutoTestHarness: session {code} created. Reading status back...");

        // Steam lobby data can take a moment to be locally readable right
        // after creation; give it a beat before polling.
        yield return new WaitForSeconds(1f);

        SessionStatusResponse status = null;

        steamLobbyClient.GetSessionStatus(
            resp => status = resp,
            err =>
            {
                Debug.LogError($"AutoTestHarness: status fetch failed: {err}");
                failed = true;
            });

        yield return new WaitUntil(() => status != null || failed);
        if (failed)
        {
            yield break;
        }

        Debug.Log($"AutoTestHarness: status={status.status} players={status.players.Length}/{status.max_players} is_host={status.is_host}");

        foreach (var player in status.players)
        {
            Debug.Log($"AutoTestHarness: player '{player.display_name}' is_host={player.is_host}");
        }

        Debug.Log("AutoTestHarness: smoke test complete. A real second Steam account is needed to verify Start Game / BeginNetworking end-to-end.");
    }
}
