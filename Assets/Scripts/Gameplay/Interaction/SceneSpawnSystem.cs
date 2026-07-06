using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneSpawnSystem
{
    private static string nextSpawnPointName;
    private static bool initialized;

    public static bool HasPendingSpawnPoint => !string.IsNullOrWhiteSpace(nextSpawnPointName);
    public static bool LastLoadUsedSpawn { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeOnLoad()
    {
        EnsureInitialized();
    }

    public static void SetNextSpawnPoint(string spawnPointName)
    {
        EnsureInitialized();
        nextSpawnPointName = string.IsNullOrWhiteSpace(spawnPointName) ? null : spawnPointName;
        LastLoadUsedSpawn = false;
    }

    public static bool PlacePlayerAt(string spawnPointName)
    {
        if (string.IsNullOrWhiteSpace(spawnPointName))
        {
            return false;
        }

        GameObject spawnObject = GameObject.Find(spawnPointName);
        if (spawnObject == null)
        {
            Debug.LogWarning($"[SceneSpawnSystem] Spawn point '{spawnPointName}' was not found.");
            return false;
        }

        Transform player = FindPlayer();
        if (player == null)
        {
            Debug.LogWarning("[SceneSpawnSystem] Player was not found.");
            return false;
        }

        Vector3 targetPosition = spawnObject.transform.position;
        targetPosition.z = player.position.z;

        if (player.TryGetComponent(out Rigidbody2D body))
        {
            body.position = new Vector2(targetPosition.x, targetPosition.y);
            player.position = targetPosition;
            Physics2D.SyncTransforms();
        }
        else
        {
            player.position = targetPosition;
        }

        return true;
    }

    private static void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        initialized = true;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrWhiteSpace(nextSpawnPointName))
        {
            LastLoadUsedSpawn = false;
            return;
        }

        string spawnPointName = nextSpawnPointName;
        nextSpawnPointName = null;
        LastLoadUsedSpawn = true;
        PlacePlayerAt(spawnPointName);
    }

    private static Transform FindPlayer()
    {
        GameObject playerObject = null;

        try
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
        }

        if (playerObject == null)
        {
            playerObject = GameObject.Find("Player");
        }

        return playerObject != null ? playerObject.transform : null;
    }
}
