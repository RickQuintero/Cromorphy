using System;
using UnityEngine;

public static class SaveSystem
{
    public const int    MAX_SLOTS        = 10;
    public const string PENDING_LOAD_KEY = "pending_load_slot";
    public const string ACTIVE_SLOT_KEY  = "active_slot";

    public static int  GetActiveSlot()        => PlayerPrefs.GetInt(ACTIVE_SLOT_KEY, -1);
    public static void SetActiveSlot(int slot) { PlayerPrefs.SetInt(ACTIVE_SLOT_KEY, slot); PlayerPrefs.Save(); }

    private const string KEY_EXISTS          = "save_{0}_exists";
    private const string KEY_CHECKPOINT      = "save_{0}_checkpoint";
    private const string KEY_SCENE           = "save_{0}_scene";
    private const string KEY_POS_X           = "save_{0}_posX";
    private const string KEY_POS_Y           = "save_{0}_posY";
    private const string KEY_POS_Z           = "save_{0}_posZ";
    private const string KEY_TIMESTAMP       = "save_{0}_timestamp";
    private const string KEY_IS_DAY          = "save_{0}_isDay";
    private const string KEY_CYCLES          = "save_{0}_cycles";
    private const string KEY_JUMP_LEVEL      = "save_{0}_jumpLevel";
    private const string KEY_TONGUE_RADIUS   = "save_{0}_tongueRadius";
    private const string KEY_CAMUFLAJE_TIME  = "save_{0}_camuflajeTime";

    [Serializable]
    public class SaveData
    {
        public bool    exists;
        public int     checkpointIndex;
        public string  sceneName;
        public Vector3 playerPosition;
        public string  timestamp;
        // World state
        public bool    isDay;
        public int     numberOfCycles;
        // Player stats
        public int     maxJumpLevel;
        public int     maxTongueRadius;
        public int     maxCamuflajeTime;
    }

    public static void Save(int slot, SaveData data)
    {
        if (!IsValidSlot(slot)) return;

        PlayerPrefs.SetInt   (Key(KEY_EXISTS,         slot), 1);
        PlayerPrefs.SetInt   (Key(KEY_CHECKPOINT,     slot), data.checkpointIndex);
        PlayerPrefs.SetString(Key(KEY_SCENE,          slot), data.sceneName);
        PlayerPrefs.SetFloat (Key(KEY_POS_X,          slot), data.playerPosition.x);
        PlayerPrefs.SetFloat (Key(KEY_POS_Y,          slot), data.playerPosition.y);
        PlayerPrefs.SetFloat (Key(KEY_POS_Z,          slot), data.playerPosition.z);
        PlayerPrefs.SetString(Key(KEY_TIMESTAMP,      slot), DateTime.Now.ToString("dd/MM/yyyy  HH:mm"));
        PlayerPrefs.SetInt   (Key(KEY_IS_DAY,         slot), data.isDay ? 1 : 0);
        PlayerPrefs.SetInt   (Key(KEY_CYCLES,         slot), data.numberOfCycles);
        PlayerPrefs.SetInt   (Key(KEY_JUMP_LEVEL,     slot), data.maxJumpLevel);
        PlayerPrefs.SetInt   (Key(KEY_TONGUE_RADIUS,  slot), data.maxTongueRadius);
        PlayerPrefs.SetInt   (Key(KEY_CAMUFLAJE_TIME, slot), data.maxCamuflajeTime);
        PlayerPrefs.Save();

        Debug.Log($"[SaveSystem] Slot {slot} saved — checkpoint {data.checkpointIndex}");
    }

    public static SaveData Load(int slot)
    {
        if (!IsValidSlot(slot) || !SlotExists(slot)) return null;

        return new SaveData
        {
            exists           = true,
            checkpointIndex  = PlayerPrefs.GetInt   (Key(KEY_CHECKPOINT,     slot)),
            sceneName        = PlayerPrefs.GetString (Key(KEY_SCENE,          slot)),
            playerPosition   = new Vector3(
                                   PlayerPrefs.GetFloat(Key(KEY_POS_X, slot)),
                                   PlayerPrefs.GetFloat(Key(KEY_POS_Y, slot)),
                                   PlayerPrefs.GetFloat(Key(KEY_POS_Z, slot))),
            timestamp        = PlayerPrefs.GetString (Key(KEY_TIMESTAMP,      slot)),
            isDay            = PlayerPrefs.GetInt   (Key(KEY_IS_DAY,          slot), 1) == 1,
            numberOfCycles   = PlayerPrefs.GetInt   (Key(KEY_CYCLES,          slot)),
            maxJumpLevel     = PlayerPrefs.GetInt   (Key(KEY_JUMP_LEVEL,      slot), 1),
            maxTongueRadius  = PlayerPrefs.GetInt   (Key(KEY_TONGUE_RADIUS,   slot), 3),
            maxCamuflajeTime = PlayerPrefs.GetInt   (Key(KEY_CAMUFLAJE_TIME,  slot), 5),
        };
    }

    public static void DeleteSlot(int slot)
    {
        if (!IsValidSlot(slot)) return;

        PlayerPrefs.DeleteKey(Key(KEY_EXISTS,         slot));
        PlayerPrefs.DeleteKey(Key(KEY_CHECKPOINT,     slot));
        PlayerPrefs.DeleteKey(Key(KEY_SCENE,          slot));
        PlayerPrefs.DeleteKey(Key(KEY_POS_X,          slot));
        PlayerPrefs.DeleteKey(Key(KEY_POS_Y,          slot));
        PlayerPrefs.DeleteKey(Key(KEY_POS_Z,          slot));
        PlayerPrefs.DeleteKey(Key(KEY_TIMESTAMP,      slot));
        PlayerPrefs.DeleteKey(Key(KEY_IS_DAY,         slot));
        PlayerPrefs.DeleteKey(Key(KEY_CYCLES,         slot));
        PlayerPrefs.DeleteKey(Key(KEY_JUMP_LEVEL,     slot));
        PlayerPrefs.DeleteKey(Key(KEY_TONGUE_RADIUS,  slot));
        PlayerPrefs.DeleteKey(Key(KEY_CAMUFLAJE_TIME, slot));
        PlayerPrefs.Save();

        Debug.Log($"[SaveSystem] Slot {slot} deleted.");
    }

    public static bool SlotExists(int slot) =>
        IsValidSlot(slot) && PlayerPrefs.GetInt(Key(KEY_EXISTS, slot), 0) == 1;

    private static string Key(string template, int slot) => string.Format(template, slot);
    private static bool IsValidSlot(int slot) => (uint)slot < (uint)MAX_SLOTS;
}
