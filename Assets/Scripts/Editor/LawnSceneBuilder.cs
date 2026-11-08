using System.IO;
using DonutQuest.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DonutQuest.EditorTools
{
    /// <summary>
    /// Меню: Donut Quest → Создать сцену 01_Lawn.
    /// Собирает тестовую лужайку: землю, куст с зоной сохранения, свет, камеру и Корру.
    /// Сначала собери префаб Корры (Donut Quest → Собрать префаб Корры), иначе её не будет в сцене.
    /// Пересоздание перезаписывает сцену — ручные правки в ней пропадут.
    /// </summary>
    public static class LawnSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/01_Lawn.unity";
        const string MaterialsDir = "Assets/Materials";
        const string KorraPrefabPath = "Assets/Prefabs/Korra.prefab";
        const string CatPrefabPath = "Assets/Prefabs/Cat.prefab";

        [MenuItem("Donut Quest/Создать сцену 01_Lawn")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Материалы сохраняем ассетами. Материал, созданный только в памяти,
            // в сцену не запишется, и после перезапуска Unity объект станет розовым.
            var grass = GetOrCreateMaterial("Grass", new Color(0.36f, 0.62f, 0.27f));
            var leaves = GetOrCreateMaterial("BushLeaves", new Color(0.18f, 0.42f, 0.18f));

            // Земля: длинная полоса 60 × 6 м. Plane — 10 × 10 м при масштабе 1.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(25f, 0f, 0f);
            ground.transform.localScale = new Vector3(6f, 1f, 0.6f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = grass;

            // Куст стоит позади дорожки (Z = 1.2), чтобы не загораживать путь.
            // Пока это сфера, позже заменим моделью из Kenney Nature Kit.
            var bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.name = "Bush";
            bush.transform.position = new Vector3(8f, 0.6f, 1.2f);
            bush.transform.localScale = new Vector3(1.8f, 1.2f, 1.2f);
            bush.GetComponent<MeshRenderer>().sharedMaterial = leaves;
            Object.DestroyImmediate(bush.GetComponent<Collider>());

            // Зона сохранения — невидимый триггер на дорожке перед кустом.
            var zone = new GameObject("SaveZone_01");
            zone.transform.position = new Vector3(8f, 1f, 0f);
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2f, 2f, 2f);
            zone.AddComponent<SaveZone>();

            // Свет низкого вечернего солнца из SPEC (раздел 5).
            var sun = new GameObject("Sun");
            sun.transform.rotation = Quaternion.Euler(25f, -40f, 0f);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.85f, 0.65f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;

            // Корра
            Transform korra = null;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KorraPrefabPath);
            if (prefab != null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.position = new Vector3(0f, 0.1f, 0f);
                korra = go.transform;
            }
            else
            {
                Debug.LogWarning("[Donut Quest] Нет префаба Корры. Сначала: Donut Quest → Собрать префаб Корры.");
            }

            // Кот рядом с кустом (патрулирует туда-обратно)
            var catPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CatPrefabPath);
            if (catPrefab != null)
            {
                var cat = (GameObject)PrefabUtility.InstantiatePrefab(catPrefab);
                cat.transform.position = new Vector3(10.5f, 0.1f, 0f);
            }
            else
            {
                Debug.LogWarning("[Donut Quest] Нет префаба Кота. Сначала: Donut Quest → Собрать префаб Кота.");
            }

            // Камера сбоку
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.98f, 0.80f, 0.60f); // тёплое вечернее небо
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 1.5f, -7f);
            var follow = camGo.AddComponent<SideCamera>();
            follow.target = korra;

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            // Прописываем сцены в Build Settings: меню — индекс 0 (с неё стартует билд),
            // уровень — индекс 1. Без этого SceneManager.LoadScene("01_Lawn") в билде упадёт.
            MainMenuSceneBuilder.AddToBuildSettings("Assets/Scenes/00_MainMenu.unity", 0);
            MainMenuSceneBuilder.AddToBuildSettings(ScenePath, 1);

            Debug.Log($"[Donut Quest] Сцена готова: {ScenePath}. Жми Play.");
        }

        static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            Directory.CreateDirectory(MaterialsDir);
            mat = new Material(Shader.Find("Standard")) { color = color };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
