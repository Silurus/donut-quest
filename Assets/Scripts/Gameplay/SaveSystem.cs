using System;
using System.IO;
using UnityEngine;

namespace DonutQuest.Gameplay
{
    /// <summary>
    /// Что именно лежит в сейве. Поля публичные — так требует JsonUtility.
    /// Версия нужна, чтобы старые сейвы не ломали игру после изменений.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public float posX, posY, posZ;
        public int cucumbers;
        public int cheese;
        public float playTimeSeconds;
        public string savedAtUtc;

        public Vector3 Position => new Vector3(posX, posY, posZ);
    }

    /// <summary>
    /// Один слот сохранения в Application.persistentDataPath/save.json.
    /// На Windows это %USERPROFILE%/AppData/LocalLow/&lt;Company&gt;/&lt;Product&gt;/save.json,
    /// на Steam Deck — ~/.config/unity3d/&lt;Company&gt;/&lt;Product&gt;/save.json.
    /// Никаких PlayerPrefs: файл проще бэкапить и отлаживать.
    /// </summary>
    public static class SaveSystem
    {
        const string FileName = "save.json";

        public static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>Есть ли сохранение. Для пункта меню «Продолжить».</summary>
        public static bool HasSave => File.Exists(Path);

        public static bool Save(SaveData data, out string error)
        {
            error = null;
            try
            {
                data.savedAtUtc = DateTime.UtcNow.ToString("o");
                // Пишем сначала во временный файл, потом подменяем —
                // иначе выключение Steam Deck посреди записи оставит битый сейв.
                string tmp = Path + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
                if (File.Exists(Path)) File.Replace(tmp, Path, null);
                else File.Move(tmp, Path);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                Debug.LogError($"[Save] Не удалось сохранить: {e}");
                return false;
            }
        }

        public static SaveData Load()
        {
            if (!HasSave) return null;
            try
            {
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(Path));
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Сейв повреждён: {e}");
                return null;
            }
        }

        public static void Delete()
        {
            if (File.Exists(Path)) File.Delete(Path);
        }
    }
}
