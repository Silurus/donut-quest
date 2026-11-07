using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DonutQuest.Gameplay;

namespace DonutQuest.EditorTools
{
    /// <summary>
    /// Меню: Donut Quest → Создать сцену 00_MainMenu.
    /// Собирает сцену главного меню: камера, скрипт MainMenu.
    /// EventSystem не нужен: меню на IMGUI, а навигация идёт через InputAction самого MainMenu.
    /// </summary>
    public static class MainMenuSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/00_MainMenu.unity";

        [MenuItem("Donut Quest/Создать сцену 00_MainMenu")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Фон меню — тот же тёплый вечерний цвет, что и в уровне,
            // чтобы переход из меню в игру не мигал.
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.20f, 0.13f, 0.10f);
            camGo.transform.position = new Vector3(0f, 1.5f, -10f);
            camGo.AddComponent<AudioListener>();

            var menuGo = new GameObject("MainMenu");
            var menu = menuGo.AddComponent<MainMenu>();
            menu.levelSceneName = "01_Lawn";

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            AddToBuildSettings(ScenePath, 0);

            Debug.Log($"[Donut Quest] Сцена меню готова: {ScenePath}. Жми Play.");
            EditorSceneManager.OpenScene(ScenePath);
        }

        /// <summary>
        /// Добавляет сцену в Build Settings на нужную позицию, не дублируя.
        /// Порядок важен: индекс 0 — сцена, с которой стартует билд.
        /// </summary>
        internal static void AddToBuildSettings(string scenePath, int index)
        {
            var scenes = EditorBuildSettings.scenes;

            // Убираем старую запись, чтобы не было дублей при повторном запуске.
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes.Length + 1);
            foreach (var s in scenes)
                if (s.path != scenePath) list.Add(s);

            index = Mathf.Clamp(index, 0, list.Count);
            list.Insert(index, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
