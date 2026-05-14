using UnityEngine;

public sealed class GameRuntimeBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeServices()
    {
        if (FindAnyObjectByType<GameRuntimeBootstrap>() != null)
            return;

        GameObject go = new GameObject("Practice4Runtime");
        DontDestroyOnLoad(go);

        go.AddComponent<GameRuntimeBootstrap>();
        go.AddComponent<GameFlowManager>();

        if (!Application.isBatchMode)
            go.AddComponent<GameCycleUI>();
    }
}
