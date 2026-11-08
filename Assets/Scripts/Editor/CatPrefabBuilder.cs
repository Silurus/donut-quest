using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using DonutQuest.Gameplay;

namespace DonutQuest.EditorTools
{
    /// <summary>
    /// Меню: Donut Quest → Собрать префаб Кота.
    /// Создаёт prefab Assets/Prefabs/Cat.prefab с Animator + CatBehaviour.
    ///
    /// Требования: Cat.fbx должен быть в проекте; у него должны быть клипы Idle и Walk.
    /// Откроет сцену 01_Lawn и поставит кота в точку (8.5, 0.1, 0) рядом с кустом.
    /// </summary>
    public static class CatPrefabBuilder
    {
        const string FbxPath     = "Assets/Models/Cat/Cat.fbx";
        const string PrefabPath  = "Assets/Prefabs/Cat.prefab";
        const string ControllerPath = "Assets/Models/Cat/CatAnimator.controller";

        const string idleClip = "CatArmature|Idle";
        const string walkClip = "CatArmature|Walking";

        [MenuItem("Donut Quest/Собрать префаб Кота")]
        public static void Build()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (model == null)
            {
                Debug.LogError($"[CatPrefabBuilder] Не найдена модель: {FbxPath}");
                return;
            }

            // ---- Animator Controller ----
            var ctrl = BuildController(model);
            if (ctrl == null) return;

            // ---- Префаб ----
            var root = new GameObject("Cat");

            // Модель дочерним объектом (как у Корры).
            var modelGo = (GameObject)PrefabUtility.InstantiatePrefab(model);
            modelGo.name = "Model";
            modelGo.transform.SetParent(root.transform, false);
            // Поворот: Cat.fbx от Quaternius смотрит по +Z, нам нужно +X (вправо).
            modelGo.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

            var anim = modelGo.GetComponent<Animator>();
            if (anim == null) anim = modelGo.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;

            var cat = root.AddComponent<CatBehaviour>();
            // Имена клипов из Cat.fbx.meta (CatArmature|Idle, CatArmature|Walking).
            cat.idleClip = idleClip;
            cat.walkClip = walkClip;

            // Коллайдер для OverlapSphere (IBarkReactive).
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.35f, 0f);
            col.radius = 0.2f;
            col.height = 0.7f;

            // Сохраняем и подчищаем.
            System.IO.Directory.CreateDirectory("Assets/Prefabs");
            bool saved = false;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out saved);
            Object.DestroyImmediate(root);

            if (!saved)
            {
                Debug.LogError("[CatPrefabBuilder] Не удалось сохранить префаб");
                return;
            }

            Debug.Log($"[CatPrefabBuilder] ✓ Префаб сохранён: {PrefabPath}");

            // Добавляем в открытую сцену рядом с кустом, если там уже есть SaveZone_01.
            PlaceCatInScene(prefab);
        }

        static AnimatorController BuildController(GameObject model)
        {
            var clips = GetClips(model, "Idle", "Walk");
            if (clips[0] == null || clips[1] == null)
            {
                Debug.LogError("[CatPrefabBuilder] Не найдены клипы Idle и Walk в Cat.fbx. " +
                               "Открой модель в инспекторе → Animation → убедись, что клипы есть.");
                return null;
            }

            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var root = ctrl.layers[0].stateMachine;

            foreach (var clip in clips)
            {
                var state = root.AddState(clip.name);
                state.motion = clip;
                if (clip.name == "Idle")
                    root.defaultState = state;
            }

            return ctrl;
        }

        static AnimationClip[] GetClips(GameObject model, params string[] names)
        {
            var allClips = AssetDatabase.LoadAllAssetsAtPath(FbxPath);
            var result = new AnimationClip[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                foreach (var a in allClips)
                    if (a is AnimationClip c && c.name == names[i])
                        { result[i] = c; break; }
            }
            return result;
        }

        static void PlaceCatInScene(GameObject prefab)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "01_Lawn") return;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            // Рядом с кустом-зоной сохранения.
            go.transform.position = new Vector3(10.5f, 0.1f, 0f);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[CatPrefabBuilder] Кот добавлен в сцену 01_Lawn в позицию (10.5, 0.1, 0). Сохрани сцену.");
        }
    }
}
