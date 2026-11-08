using System.Collections;
using UnityEngine;

namespace DonutQuest.Gameplay
{
    /// <summary>
    /// Поведение кота. Состояния:
    ///
    ///   Idle     — стоит или сидит на месте; если есть PatrolEnd — ходит туда-обратно.
    ///   Scared   — реакция на лай: испуг + анимация (Walk как спринт убегания),
    ///              опционально спаун какашки, опционально сброс DropObject.
    ///   Frozen   — после испуга кот застыл или убежал за сцену; не реагирует повторно.
    ///
    /// IBarkReactive: при лае Корры в радиусе KorraBark кот переходит в Scared.
    ///
    /// Зависимости: Animator с клипами Walk / Idle (Quaternius Cat.fbx).
    /// DropObject и PoopSpawnPoint — опциональные инспекторные ссылки.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CatBehaviour : MonoBehaviour, IBarkReactive
    {
        public enum CatState { Idle, Scared, Frozen }

        [Header("Патруль (опционально)")]
        [Tooltip("Точка разворота. Если null — кот стоит на месте.")]
        public Transform patrolEnd;
        public float patrolSpeed = 1.2f;

        [Header("Реакция на лай")]
        [Tooltip("Скорость убегания.")]
        public float scaredSpeed = 5f;
        [Tooltip("Сколько метров убегает, потом замирает.")]
        public float scaredRunDistance = 8f;
        [Tooltip("Кот убегает вправо (+X) или влево (-X) — true = вправо.")]
        public bool escapeRight = true;

        [Header("Предмет, который бросает кот (опционально)")]
        [Tooltip("GameObject горшка, ящика и т.п. Если null — ничего не роняет.")]
        public GameObject dropObject;
        [Tooltip("Сила броска по X (влево — отрицательная).")]
        public float dropForceX = 3f;
        [Tooltip("Сила броска по Y.")]
        public float dropForceY = 4f;

        [Header("Какашка кота (опционально)")]
        [Tooltip("Точка спауна какашки. Если null — какашки не будет.")]
        public Transform poopSpawnPoint;
        [Tooltip("Префаб какашки. Если null — какашки не будет.")]
        public GameObject poopPrefab;

        [Header("Имена клипов (как в Cat.fbx)")]
        public string idleClip = "CatArmature|Idle";
        public string walkClip = "CatArmature|Walking";

        Animator _anim;
        public CatState State { get; private set; } = CatState.Idle;

        Vector3 _patrolStart;
        int _dir = 1;

        void Awake()
        {
            _anim = GetComponent<Animator>();
            _patrolStart = transform.position;
        }

        void Update()
        {
            switch (State)
            {
                case CatState.Idle: UpdateIdle(); break;
                case CatState.Scared: break; // управляется корутиной
                case CatState.Frozen: break;
            }
        }

        void UpdateIdle()
        {
            if (patrolEnd == null)
            {
                PlayAnim(idleClip);
                return;
            }

            PlayAnim(walkClip);
            Vector3 target = (_dir > 0) ? patrolEnd.position : _patrolStart;
            float step = patrolSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target, step);

            // Разворот по горизонтали у точки разворота.
            if (Vector3.Distance(transform.position, target) < 0.05f)
            {
                _dir = -_dir;
                transform.localScale = new Vector3(
                    Mathf.Abs(transform.localScale.x) * _dir,
                    transform.localScale.y,
                    transform.localScale.z);
            }
        }

        // --- IBarkReactive ---

        public void OnBark(Transform source)
        {
            if (State != CatState.Idle) return; // повторный лай игнорируем
            StartCoroutine(ScaredRoutine(source));
        }

        IEnumerator ScaredRoutine(Transform source)
        {
            State = CatState.Scared;
            PlayAnim(walkClip);
            _anim.speed = scaredSpeed / patrolSpeed; // быстрее обычного

            // Направление побега — от Корры.
            float escapeDir = (escapeRight ? 1f : -1f);
            // Если Корра справа — убегать влево, и наоборот.
            if (source != null && source.position.x > transform.position.x) escapeDir = -1f;
            else if (source != null && source.position.x < transform.position.x) escapeDir = 1f;

            // Разворачиваем кота.
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x) * (escapeDir > 0 ? 1 : -1),
                transform.localScale.y,
                transform.localScale.z);

            // Роняем предмет сразу при испуге.
            DropItem();

            // Спауним какашку сразу при испуге.
            SpawnPoop();

            // Бежим scaredRunDistance метров.
            float distLeft = scaredRunDistance;
            while (distLeft > 0f)
            {
                float step = scaredSpeed * Time.deltaTime;
                transform.Translate(new Vector3(escapeDir * step, 0f, 0f), Space.World);
                distLeft -= step;
                yield return null;
            }

            State = CatState.Frozen;
            PlayAnim(idleClip);
            _anim.speed = 1f;
        }

        void DropItem()
        {
            if (dropObject == null) return;

            // Отвязываем объект от кота.
            dropObject.transform.SetParent(null);

            var rb = dropObject.GetComponent<Rigidbody>();
            if (rb == null) rb = dropObject.AddComponent<Rigidbody>();

            rb.isKinematic = false;
            rb.AddForce(new Vector3(dropForceX, dropForceY, 0f), ForceMode.Impulse);
            dropObject = null; // больше не трогаем
        }

        void SpawnPoop()
        {
            if (poopPrefab == null || poopSpawnPoint == null) return;
            Instantiate(poopPrefab, poopSpawnPoint.position, Quaternion.identity);
        }

        void PlayAnim(string clip)
        {
            if (_anim == null) return;
            _anim.Play(clip, 0, 0f);
        }

        void OnDrawGizmosSelected()
        {
            if (patrolEnd == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, patrolEnd.position);
            Gizmos.DrawWireSphere(patrolEnd.position, 0.15f);
        }
    }
}
