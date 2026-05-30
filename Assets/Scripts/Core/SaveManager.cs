using UnityEngine;

/// <summary>
/// Global save manager. Pure C# singleton, no MonoBehaviour, survives scene reloads. [3]
/// </summary>
public class SaveManager
{
    private static SaveManager instance;
    public static SaveManager Instance
    {
        get
        {
            if (instance == null) instance = new SaveManager();
            return instance;
        }
    }

    public Vector3 LastCheckpointPosition { get; private set; }

    private const string CheckpointXKey = "CheckpointX";
    private const string CheckpointYKey = "CheckpointY";
    private const string CheckpointZKey = "CheckpointZ";
    private const string HasSavedKey = "HasSavedCheckpoint";

    // ========================================================
    // Store system save integration: inventory/wallet/equipment/shop stock persistence
    // ========================================================
    private const string StoreDataKey = "StoreData";

    private SaveManager()
    {
        LoadCheckpoint();
    }

    /// <summary>
    /// Called when checkpoint is triggered. Auto-write to local disk. [3]
    /// </summary>
    public void SaveCheckpoint(Vector3 position)
    {
        // ========================================================
        // Security guard: prevent battle scene coordinates from polluting the save.
        // Battle stage is at X=2000, Y=2000. If X and Y are both > 1500,
        // it means the player triggered a save from the battle area - reject it. [3]
        // ========================================================
        if (position.x > 1500f && position.y > 1500f)
        {
            Debug.LogWarning($"[SaveSystem] WARNING: Position {position} is in battle area. Save rejected by security guard.");
            return;
        }

        LastCheckpointPosition = position;
        PlayerPrefs.SetFloat(CheckpointXKey, position.x);
        PlayerPrefs.SetFloat(CheckpointYKey, position.y);
        PlayerPrefs.SetFloat(CheckpointZKey, position.z);
        PlayerPrefs.SetInt(HasSavedKey, 1);

        // Capture store system data (inventory, wallet, equipment, shop stock)
        CaptureStoreData();

        PlayerPrefs.Save();
        Debug.Log($"[SaveSystem] Checkpoint saved at: {position}");
    }

    public void LoadCheckpoint()
    {
        if (PlayerPrefs.GetInt(HasSavedKey, 0) == 1)
        {
            float x = PlayerPrefs.GetFloat(CheckpointXKey);
            float y = PlayerPrefs.GetFloat(CheckpointYKey);
            float z = PlayerPrefs.GetFloat(CheckpointZKey);
            LastCheckpointPosition = new Vector3(x, y, z);
        }
        else
        {
            // Default spawn point (first time player)
            LastCheckpointPosition = new Vector3(0f, 0f, 0f);
        }

        // Restore store system data (inventory, wallet, equipment, shop stock)
        RestoreStoreData();

        // ========================================================
        // Startup self-test log: show what position was loaded from disk! [3]
        // ========================================================
        Debug.Log($"<color=red><b>[SaveCheck] Loaded: {LastCheckpointPosition}</b></color>");
    }

    /// <summary>
    /// Capture store system data and persist to PlayerPrefs
    /// </summary>
    private void CaptureStoreData()
    {
        try
        {
            var storeSaveService = UnityEngine.Object.FindObjectOfType<StoreAndInventory.StoreSaveService>();
            if (storeSaveService == null)
            {
                Debug.LogWarning("[SaveManager] StoreSaveService not found, skipping store capture.");
                return;
            }

            string json = storeSaveService.CaptureAllJson();
            if (!string.IsNullOrEmpty(json))
            {
                PlayerPrefs.SetString(StoreDataKey, json);
                Debug.Log($"[SaveManager] Store data captured ({json.Length} chars).");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Capture failed: {e.Message}");
        }
    }

    /// <summary>
    /// Restore store system data from PlayerPrefs
    /// </summary>
    private void RestoreStoreData()
    {
        try
        {
            if (!PlayerPrefs.HasKey(StoreDataKey))
            {
                Debug.Log("[SaveManager] No store data in PlayerPrefs, using defaults.");
                return;
            }

            string json = PlayerPrefs.GetString(StoreDataKey, "");
            if (string.IsNullOrEmpty(json))
            {
                Debug.Log("[SaveManager] Store data empty, using defaults.");
                return;
            }

            var storeSaveService = UnityEngine.Object.FindObjectOfType<StoreAndInventory.StoreSaveService>();
            if (storeSaveService == null)
            {
                Debug.LogWarning("[SaveManager] StoreSaveService not found, cannot restore.");
                return;
            }

            if (storeSaveService.ApplyAllJson(json, out string error))
            {
                Debug.Log("[SaveManager] Store data restored.");
            }
            else
            {
                Debug.LogWarning($"[SaveManager] Restore failed: {error}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Restore exception: {e.Message}");
        }
    }
}
