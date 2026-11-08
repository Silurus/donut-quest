using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using DonutQuest.Gameplay;

namespace DonutQuest.EditorTools
{
    /// <summary>
    /// Сборка кота из модели в один клик.
    /// Меню: Donut Quest → Собрать префаб Кота.
    ///
    /// Что делает:
    /// 1. Берёт cat.glb из Assets/Models/Cat.
    /// 2. Создаёт Animator Controller со всеми клипами модели.
    ///    Дефолтное состояние — Idle (или первый клип, если Idle нет).
    /// 3. Собирает Assets/Prefabs/Cat.prefab: корень с CatBehaviour + коллайдером,
    ///    внутри — модель с Animator.
    ///
    /// Анимации в cat.glb: Idle, WalkClean, SitDown, SittingIdle, StandUp.
    /// CatBehaviour использует idleClip="Idle", walkClip="WalkClean".
    ///
    /// Повторный запуск пересобирает префаб.
    /// </summary>
    public static class CatPrefabBuilder
    {
        const string ModelPath      = "Assets/Models/Cat/cat.glb";
        const string ControllerPath = "Assets/Models/Cat/CatAnimator.controller";
        const string PrefabPath     = "Assets/Prefabs/Cat.prefab";

        /// <summary>
        /// Поворот модели вокруг Y. Если кот бежит задом наперёд — поменяй на 180f.
        /// </summary>
        const float ModelYaw = 0f;

        [MenuItem("Donut Quest/Собрать префаб Кота")]
        public static void Build()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                EditorUtility.DisplayDialog("Нет модели",
                    $"Не нашёл {ModelPath}.\n" +
                    "Положи cat.glb в Assets/Models/Cat/ и повтори.",
                    "Понял");
                return;
            }

            var clips = LoadClips(ModelPath);
            if (clips.Count == 0)
            {
                EditorUtility.DisplayDialog("Нет анимаций",
                    "В модели не нашлось ни одного клипа.\n\n" +
                    "Выбери cat.glb в Project, в инспекторе включи:\n" +
                    "• Animation → Import Animation\n" +
                    "• Animation → Animation Method → Mecanim\n" +
                    "Потом нажми Apply и запусти сборку снова.",
                    "Понял");
                return;
            }

            var controller = BuildController(clips);
            var prefab     = BuildPrefab(model, controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            Debug.Log($"[Donut Quest] Кот готов: {PrefabPath}. Клипы: {string.Join(", ", clips.ConvertAll(c => c.name))}");
        }

        static List<AnimationClip> LoadClips(string path)
        {
            var result = new List<AnimationClip>();
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    result.Add(clip);
            return result;
        }

        static AnimatorController BuildController(List<AnimationClip> clips)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ControllerPath));
            AssetDatabase.DeleteAsset(ControllerPath);

            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var sm   = ctrl.layers[0].stateMachine;

            AnimatorState first = null, idle = null;
            foreach (var clip in clips)
            {
                var st = sm.AddState(clip.name);
                st.motion = clip;
                if (first == null) first = st;
                if (idle == null && clip.name.ToLowerInvariant() == "idle") idle = st;
            }

            sm.defaultState = idle != null ? idle : first;
            return ctrl;
        }

        static GameObject BuildPrefab(GameObject model, AnimatorController controller)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

            var root = new GameObject("Cat");

            // Коллайдер для OverlapSphere KorraBark (IBarkReactive).
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.35f, 0f);
            col.radius = 0.2f;
            col.height = 0.7f;

            var cat = root.AddComponent<CatBehaviour>();
            // Имена клипов как в cat.glb (DreamNoms — Somali Cat Animated).
            cat.idleClip = "Idle";
            cat.walkClip = "WalkClean";

            // Модель — дочерний объект.
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.name = "Model";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(0f, ModelYaw, 0f);

            var animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
