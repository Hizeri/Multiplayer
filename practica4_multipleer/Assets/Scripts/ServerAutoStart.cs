using System;
using System.Collections;
using FishNet;
using FishNet.Transporting;
using UnityEngine;

public sealed class ServerAutoStart : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInBatchMode()
    {
        if (!Application.isBatchMode)
            return;

        if (FindAnyObjectByType<ServerAutoStart>() != null)
            return;

        GameObject go = new GameObject("ServerAutoStart");
        DontDestroyOnLoad(go);
        go.AddComponent<ServerAutoStart>();
    }

    private IEnumerator Start()
    {
        while (InstanceFinder.NetworkManager == null || InstanceFinder.ServerManager == null)
            yield return null;

        if (NetworkLaunchArgs.TryGetUShort("port", out ushort port))
            InstanceFinder.TransportManager.Transport.SetPort(port);

        InstanceFinder.TransportManager.Transport.SetServerBindAddress("0.0.0.0", IPAddressType.IPv4);

        if (!InstanceFinder.IsServerStarted && !InstanceFinder.ServerManager.GetStartOnHeadless())
        {
            Debug.Log("[Server] Headless mode detected. Starting FishNet server.");
            InstanceFinder.ServerManager.StartConnection();
        }
    }
}

internal static class NetworkLaunchArgs
{
    public static string GetString(params string[] keys)
    {
        string[] args = Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; i++)
        {
            string current = args[i];

            foreach (string key in keys)
            {
                string dashKey = "-" + key;
                string doubleDashKey = "--" + key;

                if (string.Equals(current, dashKey, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(current, doubleDashKey, StringComparison.OrdinalIgnoreCase))
                {
                    return i + 1 < args.Length ? args[i + 1] : string.Empty;
                }

                string dashPrefix = dashKey + "=";
                string doubleDashPrefix = doubleDashKey + "=";

                if (current.StartsWith(dashPrefix, StringComparison.OrdinalIgnoreCase))
                    return current.Substring(dashPrefix.Length);

                if (current.StartsWith(doubleDashPrefix, StringComparison.OrdinalIgnoreCase))
                    return current.Substring(doubleDashPrefix.Length);
            }
        }

        return string.Empty;
    }

    public static bool TryGetUShort(string key, out ushort value)
    {
        return ushort.TryParse(GetString(key), out value);
    }
}
