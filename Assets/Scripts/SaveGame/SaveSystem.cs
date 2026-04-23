using System;
using UnityEngine;

/// <summary>
/// Gestiona el guardado y carga de partidas usando PlayerPrefs.
/// Soporta hasta MAX_SLOTS slots independientes.
/// Extensible: añade campos a SaveData cuando implementes vida, inventario, etc.
/// </summary>
public static class SaveSystem
{
    public const int MAX_SLOTS = 10;

    // ── Claves base de PlayerPrefs ────────────────────────────────────────────
    private const string KEY_EXISTS      = "save_{0}_exists";
    private const string KEY_CHECKPOINT  = "save_{0}_checkpoint";
    private const string KEY_SCENE       = "save_{0}_scene";
    private const string KEY_POS_X       = "save_{0}_posX";
    private const string KEY_POS_Y       = "save_{0}_posY";
    private const string KEY_POS_Z       = "save_{0}_posZ";
    private const string KEY_TIMESTAMP   = "save_{0}_timestamp";

    // ── Datos de una partida ──────────────────────────────────────────────────
    [Serializable]
    public class SaveData
    {
        public bool   exists;
        public int    checkpointIndex;   // índice del último checkpoint alcanzado
        public string sceneName;         // escena donde se guardó
        public Vector3 playerPosition;
        public string timestamp;         // fecha/hora legible

        // ── Futuro: añade aquí vida, inventario, ítems, etc. ─────────────────
        // public int health;
        // public List<string> inventory;
    }

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>Guarda una partida en el slot indicado (0-based).</summary>
    public static void Save(int slot, SaveData data)
    {
        if (!IsValidSlot(slot)) return;

        PlayerPrefs.SetInt   (Key(KEY_EXISTS,     slot), 1);
        PlayerPrefs.SetInt   (Key(KEY_CHECKPOINT, slot), data.checkpointIndex);
        PlayerPrefs.SetString(Key(KEY_SCENE,      slot), data.sceneName);
        PlayerPrefs.SetFloat (Key(KEY_POS_X,      slot), data.playerPosition.x);
        PlayerPrefs.SetFloat (Key(KEY_POS_Y,      slot), data.playerPosition.y);
        PlayerPrefs.SetFloat (Key(KEY_POS_Z,      slot), data.playerPosition.z);
        PlayerPrefs.SetString(Key(KEY_TIMESTAMP,  slot), DateTime.Now.ToString("dd/MM/yyyy  HH:mm"));
        PlayerPrefs.Save();

        Debug.Log($"[SaveSystem] Partida guardada en slot {slot} — checkpoint {data.checkpointIndex}");
    }

    /// <summary>Carga los datos del slot indicado. Devuelve null si está vacío.</summary>
    public static SaveData Load(int slot)
    {
        if (!IsValidSlot(slot) || !SlotExists(slot)) return null;

        return new SaveData
        {
            exists           = true,
            checkpointIndex  = PlayerPrefs.GetInt   (Key(KEY_CHECKPOINT, slot)),
            sceneName        = PlayerPrefs.GetString (Key(KEY_SCENE,      slot)),
            playerPosition   = new Vector3(
                                   PlayerPrefs.GetFloat(Key(KEY_POS_X, slot)),
                                   PlayerPrefs.GetFloat(Key(KEY_POS_Y, slot)),
                                   PlayerPrefs.GetFloat(Key(KEY_POS_Z, slot))),
            timestamp        = PlayerPrefs.GetString (Key(KEY_TIMESTAMP,  slot))
        };
    }

    /// <summary>Borra el slot indicado.</summary>
    public static void DeleteSlot(int slot)
    {
        if (!IsValidSlot(slot)) return;

        PlayerPrefs.DeleteKey(Key(KEY_EXISTS,     slot));
        PlayerPrefs.DeleteKey(Key(KEY_CHECKPOINT, slot));
        PlayerPrefs.DeleteKey(Key(KEY_SCENE,      slot));
        PlayerPrefs.DeleteKey(Key(KEY_POS_X,      slot));
        PlayerPrefs.DeleteKey(Key(KEY_POS_Y,      slot));
        PlayerPrefs.DeleteKey(Key(KEY_POS_Z,      slot));
        PlayerPrefs.DeleteKey(Key(KEY_TIMESTAMP,  slot));
        PlayerPrefs.Save();

        Debug.Log($"[SaveSystem] Slot {slot} borrado.");
    }

    /// <summary>Devuelve true si el slot tiene datos guardados.</summary>
    public static bool SlotExists(int slot) =>
        IsValidSlot(slot) && PlayerPrefs.GetInt(Key(KEY_EXISTS, slot), 0) == 1;

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static string Key(string template, int slot) => string.Format(template, slot);
    private static bool IsValidSlot(int slot) => slot >= 0 && slot < MAX_SLOTS;
}
