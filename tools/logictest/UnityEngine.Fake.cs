// Минимальная подмена UnityEngine для офлайн-тестов логики без редактора Unity.
// НЕ попадает в игру: лежит вне Assets.
//
// Зачем: SaveSystem.cs и SaveData — обычный C#, и их можно прогнать под Mono,
// если подменить Application.persistentDataPath, Debug и JsonUtility.
// Так проверяется атомарная запись файла и обработка битого сейва — без GPU и Unity.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace UnityEngine
{
    public static class Application
    {
        public static string persistentDataPath = Path.Combine(Path.GetTempPath(), "dq-logictest");
    }

    public static class Debug
    {
        public static readonly List<string> Errors = new List<string>();
        public static void Log(object m) { Console.WriteLine(m); }
        public static void LogError(object m) { Errors.Add(m?.ToString() ?? ""); }
        public static void LogWarning(object m) { Console.WriteLine("WARN: " + m); }
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "({0}, {1}, {2})", x, y, z);
    }

    /// <summary>
    /// Замена JsonUtility: тот же контракт (публичные поля), но чистый C#.
    /// Плоские типы int/float/string — ровно то, что лежит в SaveData.
    /// </summary>
    public static class JsonUtility
    {
        public static string ToJson(object o, bool pretty) => Write(o, pretty, 0);

        public static T FromJson<T>(string json) => (T)ParseValue(json.Trim(), typeof(T));

        static string Write(object o, bool pretty, int depth)
        {
            if (o == null) return "null";
            var t = o.GetType();
            if (t == typeof(string)) return Quote((string)o);
            if (t == typeof(int)) return ((int)o).ToString(CultureInfo.InvariantCulture);
            if (t == typeof(float)) return ((float)o).ToString("R", CultureInfo.InvariantCulture);
            if (t == typeof(bool)) return (bool)o ? "true" : "false";

            string pad = pretty ? new string(' ', (depth + 1) * 2) : "";
            string endPad = pretty ? new string(' ', depth * 2) : "";
            string nl = pretty ? "\n" : "";
            var sb = new StringBuilder("{");
            if (pretty) sb.Append(nl);

            var fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                if (pretty) sb.Append(pad);
                sb.Append(Quote(fields[i].Name)).Append(':');
                if (pretty) sb.Append(' ');
                sb.Append(Write(fields[i].GetValue(o), pretty, depth + 1));
                if (i < fields.Length - 1) sb.Append(',');
                if (pretty) sb.Append(nl);
            }
            if (pretty) sb.Append(endPad);
            return sb.Append('}').ToString();
        }

        static string Quote(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        static object ParseValue(string s, Type t)
        {
            s = s.Trim();
            if (t == typeof(string)) return Unquote(s);
            if (t == typeof(int)) return int.Parse(s, CultureInfo.InvariantCulture);
            if (t == typeof(float)) return float.Parse(s, CultureInfo.InvariantCulture);

            var obj = Activator.CreateInstance(t);
            var body = s.Trim().TrimStart('{').TrimEnd('}');
            foreach (var pair in SplitFields(body))
            {
                string key = Unquote(pair.Key);
                var f = t.GetField(key, BindingFlags.Public | BindingFlags.Instance);
                if (f != null) f.SetValue(obj, ParseValue(pair.Value, f.FieldType));
            }
            return obj;
        }

        static string Unquote(string s)
        {
            s = s.Trim();
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
                s = s.Substring(1, s.Length - 2);
            return s.Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        /// <summary>Разбирает тело объекта на пары ключ/значение верхнего уровня.</summary>
        static IEnumerable<KeyValuePair<string, string>> SplitFields(string body)
        {
            int i = 0;
            while (i < body.Length)
            {
                while (i < body.Length && (char.IsWhiteSpace(body[i]) || body[i] == ',')) i++;
                if (i >= body.Length) yield break;

                int start = i;
                while (i < body.Length && body[i] != ':') i++;
                string key = body.Substring(start, i - start).Trim();
                i++; // пропускаем ':'

                while (i < body.Length && char.IsWhiteSpace(body[i])) i++;
                int vStart = i;
                int depth = 0;
                bool inStr = false;
                while (i < body.Length)
                {
                    char c = body[i];
                    if (inStr)
                    {
                        if (c == '\\') i++;
                        else if (c == '"') inStr = false;
                    }
                    else if (c == '"') inStr = true;
                    else if (c == '{' || c == '[') depth++;
                    else if (c == '}' || c == ']') { if (depth == 0) break; depth--; }
                    else if (c == ',' && depth == 0) break;
                    i++;
                }
                yield return new KeyValuePair<string, string>(key, body.Substring(vStart, i - vStart).Trim());
            }
        }
    }
}
