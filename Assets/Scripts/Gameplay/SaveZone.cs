using UnityEngine;

namespace DonutQuest.Gameplay
{
    /// <summary>
    /// Зона сохранения у куста. Игрок заходит в триггер, видит подсказку,
    /// удерживает X (Bark) holdSaveSeconds — игра сохраняется.
    /// Отпустил раньше — прогресс удержания сбрасывается.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class SaveZone : MonoBehaviour
    {
        [Header("Удержание")]
        [Tooltip("Сколько секунд держать X")]
        public float holdSaveSeconds = 1f;

        [Header("Ссылки")]
        public KorraInput korraInput;
        public KorraController korraController;

        [Header("Состояние игры, которое пишем в сейв")]
        public int cucumbers;
        public int cheese;
        public float playTimeSeconds;

        /// <summary>0..1 — прогресс удержания, для кругового индикатора в HUD.</summary>
        public float HoldProgress => _hold / holdSaveSeconds;
        public bool PlayerInside { get; private set; }

        float _hold;
        bool _saved;

        void Reset()
        {
            var box = GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2f, 2f, 2f);
        }

        void Awake()
        {
            var box = GetComponent<BoxCollider>();
            if (box) box.isTrigger = true;
        }

        void Update()
        {
            if (!PlayerInside || korraInput == null)
            {
                _hold = 0f;
                return;
            }

            // Держим X — копим прогресс, Корра стоит на месте.
            if (korraInput.BarkHeld)
            {
                if (korraController != null) korraController.Locked = true;
                _hold += Time.deltaTime;
                if (_hold >= holdSaveSeconds && !_saved)
                {
                    _saved = true;
                    _hold = 0f;
                    DoSave();
                }
            }
            else
            {
                if (korraController != null) korraController.Locked = false;
                _hold = 0f;      // отпустил раньше — начинай заново
                _saved = false;  // можно сохраниться ещё раз, не выходя из зоны
            }
        }

        void DoSave()
        {
            var data = new SaveData
            {
                posX = transform.position.x,
                posY = transform.position.y,
                posZ = 0f,
                cucumbers = cucumbers,
                cheese = cheese,
                playTimeSeconds = playTimeSeconds,
            };

            if (SaveSystem.Save(data, out string error))
            {
                Debug.Log($"[SaveZone] Сохранено в {SaveSystem.Path}");
                OnSaved?.Invoke(data);
            }
            else
            {
                _saved = false; // разрешаем повторить попытку
            }
        }

        /// <summary>Событие для HUD и звука. Подписывайся, а не правь этот класс.</summary>
        public event System.Action<SaveData> OnSaved;

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            PlayerInside = true;
            if (korraInput == null) korraInput = other.GetComponent<KorraInput>();
            if (korraController == null) korraController = other.GetComponent<KorraController>();
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            PlayerInside = false;
            _hold = 0f;
            _saved = false;
            if (korraController != null) korraController.Locked = false;
        }

        void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            if (!box) return;
            Gizmos.color = new Color(0.4f, 1f, 0.4f, 0.25f);
            Gizmos.DrawCube(transform.position + box.center, box.size);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + box.center, box.size);
        }
    }
}
