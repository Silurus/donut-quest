using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DonutQuest.EditorTools
{
    /// <summary>
    /// Сборка Корры из модели в один клик.
    /// Меню: Donut Quest → Собрать префаб Корры.
    ///
    /// Что делает:
    /// 1. Берёт corgi.glb из Assets/Models/Corra.
    /// 2. Создаёт Animator Controller, где на каждый клип модели есть состояние
    ///    с тем же именем (Run, Jump, Sitting).
    /// 3. Собирает Assets/Prefabs/Corra.prefab: корень с физикой и вводом,
    ///    внутри — модель с Animator и CorraAnimator.
    ///
    /// Запускать из Unity, из-под рута проекта. Повторный запуск пересобирает префаб.
    /// </summary>
    public static class CorraPrefabBuilder
    {
        const string ModelPath = "Assets/Models/Corra/corgi.glb";
        const string ControllerPath = "Assets/Models/Corra/CorraAnimator.controller";
        const string PrefabPath = "Assets/Prefabs/Corra.prefab";

        /// <summary>
        /// Поворот модели вокруг Y. glTF — правая система координат, Unity — левая:
        /// при импорте ось Z переворачивается, и собака, смотревшая в glTF на +Z,
        /// в Unity смотрит на -Z. Разворот на 180° ставит её носом вперёд.
        /// Если в игре Корра бежит задом наперёд — поменяй здесь 180f на 0f.
        /// </summary>
        const float ModelYaw = 180f;

        [MenuItem("Donut Quest/Собрать префаб Корры")]
        public static void Build()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                EditorUtility.DisplayDialog("Нет модели",
                    $"Не нашёл {ModelPath}.\n" +
                    "Положи corgi.glb в Assets/Models/Corra/ и повтори.",
                    "Понял");
                return;
            }

            var clips = LoadClips(ModelPath);
            if (clips.Count == 0)
            {
                EditorUtility.DisplayDialog("Нет анимаций",
                    "В модели не нашлось ни одного клипа.\n\n" +
                    "Выбери corgi.glb в Project, в инспекторе включи:\n" +
                    "• Animation → Import Animation\n" +
                    "• Animation → Animation Method → Mecanim\n" +
                    "Потом нажми Apply и запусти сборку снова.",
                    "Понял");
                return;
            }

            var controller = BuildController(clips);
            var prefab = BuildPrefab(model, controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            Debug.Log($"[Donut Quest] Префаб готов: {PrefabPath}. Клипы: {string.Join(", ", clips.ConvertAll(c => c.name))}");
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
            var sm = ctrl.layers[0].stateMachine;

            AnimatorState first = null, sit = null;
            foreach (var clip in clips)
            {
                var st = sm.AddState(clip.name);
                st.motion = clip;
                if (first == null) first = st;
                if (sit == null && clip.name.ToLowerInvariant().Contains("sit")) sit = st;
            }

            sm.defaultState = sit != null ? sit : first;
            return ctrl;
        }

        static GameObject BuildPrefab(GameObject model, AnimatorController controller)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

            var root = new GameObject("Corra");

            var cc = root.AddComponent<CharacterController>();
            cc.height = 0.6f;
            cc.radius = 0.16f;
            cc.center = new Vector3(0f, 0.3f, 0f);
            cc.slopeLimit = 50f;
            cc.stepOffset = 0.15f;

            root.AddComponent<CorraInput>();
            root.AddComponent<CorraController>();

            // Модель — дочерний объект: её локальные оси не мешают повороту персонажа.
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.name = "Model";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(0f, ModelYaw, 0f);

            var animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            if (visual.GetComponent<CorraAnimator>() == null)
                visual.AddComponent<CorraAnimator>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}