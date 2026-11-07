using UnityEngine;

namespace DonutQuest.Gameplay
{
    /// <summary>
    /// Лай Корры на кнопку X (Steam Deck buttonWest / клавиша E).
    ///
    /// При нажатии X:
    ///   1. Проверяет cooldown (0.5 с по умолчанию).
    ///   2. Запускает анимацию через KorraAnimator (barkClip).
    ///   3. Physics.OverlapSphere радиусом barkRadius — ищет IBarkReactive.
    ///   4. Вызывает OnBark() на каждом найденном.
    ///   5. Спаунит визуальную «волну» (опционально).
    ///
    /// Удержание X у куста — это СОХРАНЕНИЕ (SaveZone), а не лай.
    /// KorraAnimator следит за BarkHeld + зоной сам; здесь мы работаем с BarkPressed.
    /// </summary>
    [RequireComponent(typeof(KorraInput))]
    public class KorraBark : MonoBehaviour
    {
        [Header("Радиус и перезарядка")]
        [Tooltip("Радиус поиска цели в метрах. SPEC: ~4 м.")]
        public float barkRadius = 4f;
        [Tooltip("Перезарядка между лаями в секундах. SPEC: 0.5 с.")]
        public float cooldown = 0.5f;

        [Header("Маска слоёв")]
        [Tooltip("На каких слоях ищем IBarkReactive. Default — всё.")]
        public LayerMask targetLayers = ~0;

        [Header("Визуальная волна (опционально)")]
        [Tooltip("Префаб мультяшной волны-«гав». Если null — волны не будет.")]
        public GameObject barkWavePrefab;
        [Tooltip("Живёт секунд")]
        public float waveDuration = 0.6f;

        KorraInput _input;
        KorraAnimator _anim;
        float _cooldownLeft;

        void Awake()
        {
            _input = GetComponent<KorraInput>();
            _anim = GetComponentInChildren<KorraAnimator>();
        }

        void Update()
        {
            if (_cooldownLeft > 0f) _cooldownLeft -= Time.deltaTime;

            if (_input.BarkPressed && _cooldownLeft <= 0f)
            {
                // Лай не работает во время удержания (сохранение).
                // BarkHeld может быть true, если игрок только нажал — проверяем.
                if (!_input.BarkHeld || _cooldownLeft <= 0f)
                    Bark();
            }
        }

        void Bark()
        {
            _cooldownLeft = cooldown;

            // Анимация — в KorraAnimator; он сам переключит на barkClip.
            // (KorraAnimator уже слушает BarkPressed — здесь ничего делать не нужно.)

            // Волна
            if (barkWavePrefab != null)
            {
                var wave = Instantiate(barkWavePrefab, transform.position, Quaternion.identity);
                Destroy(wave, waveDuration);
            }

            // Поиск целей
            var hits = Physics.OverlapSphere(transform.position, barkRadius, targetLayers);
            foreach (var hit in hits)
            {
                // Один объект может иметь несколько компонентов-реакций —
                // GetComponents обходит все; обычно реакция одна, но при наследовании ок.
                var reactives = hit.GetComponents<IBarkReactive>();
                foreach (var r in reactives)
                    r.OnBark(transform);
            }
        }

        // Gizmo: показывает радиус в сцене — удобно при расстановке кота.
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.25f);
            Gizmos.DrawSphere(transform.position, barkRadius);
            Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, barkRadius);
        }
    }
}
