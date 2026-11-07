using UnityEngine;

namespace DonutQuest.Gameplay
{
    /// <summary>
    /// Передача сейва из меню в уровень.
    /// Меню кладёт данные сюда перед загрузкой сцены, KorraController их забирает.
    /// Статическое поле переживает загрузку сцены — объект-носитель не нужен.
    /// </summary>
    public static class SaveLoadBridge
    {
        static SaveData _pending;

        /// <summary>Есть ли сохранение, которое нужно применить на уровне.</summary>
        public static bool HasPending => _pending != null;

        public static void Put(SaveData data) => _pending = data;

        /// <summary>Забрать данные. Возвращает null, если грузиться нечего.</summary>
        public static SaveData Take()
        {
            var d = _pending;
            _pending = null;
            return d;
        }

        /// <summary>Забыть висящий сейв (новая игра).</summary>
        public static void Discard() => _pending = null;
    }
}
