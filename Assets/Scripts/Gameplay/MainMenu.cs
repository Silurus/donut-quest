using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DonutQuest.Gameplay
{
    /// <summary>
    /// Главное меню. Рисуется через IMGUI (OnGUI) — работает везде без Canvas и префабов,
    /// поэтому в git попадает одним .cs-файлом, а не бинарной сценой.
    ///
    /// Управление сделано на собственных InputAction, а не через EventSystem:
    /// InputSystemUIInputModule навигирует только uGUI (Canvas), а IMGUI ему не подчиняется.
    /// Иначе на Steam Deck меню не реагировало бы на геймпад.
    ///
    /// Пункты: «Продолжить» (только при наличии сейва), «Новая игра», «Выход».
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Header("Куда идти после выбора")]
        [Tooltip("Имя сцены уровня. Должна быть в Build Settings.")]
        public string levelSceneName = "01_Lawn";

        [Header("Внешний вид")]
        public int buttonWidth = 420;
        public int buttonHeight = 74;
        public int titleFontSize = 56;
        public int buttonFontSize = 30;

        InputAction _navUp, _navDown, _submit;
        GUIStyle _titleStyle, _buttonStyle;
        int _selected;
        bool _busy;

        /// <summary>Пункты меню. Строится в Start — зависит от наличия сейва.</summary>
        enum Item { Continue, NewGame, Quit }
        Item[] _items;

        void Awake()
        {
            _navUp = Button("MenuUp",
                "<Gamepad>/dpad/up", "<Gamepad>/leftStick/up",
                "<Keyboard>/upArrow", "<Keyboard>/w");
            _navDown = Button("MenuDown",
                "<Gamepad>/dpad/down", "<Gamepad>/leftStick/down",
                "<Keyboard>/downArrow", "<Keyboard>/s");
            _submit = Button("MenuSubmit",
                "<Gamepad>/buttonSouth", "<Keyboard>/enter", "<Keyboard>/space");
        }

        static InputAction Button(string name, params string[] paths)
        {
            var a = new InputAction(name, InputActionType.Button);
            foreach (var p in paths) a.AddBinding(p);
            return a;
        }

        void OnEnable()
        {
            _navUp.Enable(); _navDown.Enable(); _submit.Enable();
        }

        void OnDisable()
        {
            _navUp.Disable(); _navDown.Disable(); _submit.Disable();
        }

        void Start()
        {
            // Меню должно реагировать, даже если вышли из паузы или кат-сцены,
            // которые могли оставить timeScale = 0.
            Time.timeScale = 1f;

            // Курсор в меню виден — на Steam Deck мышь может быть не подключена,
            // но при отладке на клавиатуре он нужен.
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            BuildItems();
            _selected = 0;
        }

        void BuildItems()
        {
            // «Продолжить» только при наличии сейва — иначе пункт-обманка.
            _items = SaveSystem.HasSave
                ? new[] { Item.Continue, Item.NewGame, Item.Quit }
                : new[] { Item.NewGame, Item.Quit };
        }

        void Update()
        {
            if (_busy) return;

            if (_navDown.WasPressedThisFrame()) Move(1);
            if (_navUp.WasPressedThisFrame()) Move(-1);
            if (_submit.WasPressedThisFrame()) Activate(_selected);
        }

        void Move(int delta)
        {
            // Заворачиваем по кругу: с последнего пункта вниз — на первый.
            _selected = (_selected + delta + _items.Length) % _items.Length;
        }

        void Activate(int index)
        {
            // Защита от рассинхрона: список мог измениться между кадрами.
            if (index < 0 || index >= _items.Length) return;

            _busy = true; // блокируем повторный ввод, пока идёт загрузка сцены
            switch (_items[index])
            {
                case Item.Continue: ContinueGame(); break;
                case Item.NewGame: StartNewGame(); break;
                case Item.Quit: QuitGame(); break;
            }
        }

        void ContinueGame()
        {
            SaveData data = SaveSystem.Load();
            if (data == null)
            {
                // Файл есть, но битый — не падаем, начинаем новую игру.
                Debug.LogWarning("[MainMenu] Сейв не читается — начинаем новую игру");
                StartNewGame();
                return;
            }

            SaveLoadBridge.Put(data);
            Debug.Log($"[MainMenu] Продолжаем: позиция {data.Position}, " +
                      $"огурчики {data.cucumbers}, сыр {data.cheese}, сохранён {data.savedAtUtc}");
            LoadLevel();
        }

        void StartNewGame()
        {
            // Сбрасываем возможный «висящий» сейв от прошлого нажатия «Продолжить»,
            // иначе новая игра стартовала бы с чужой позиции.
            SaveLoadBridge.Discard();

            // Сейв не удаляем: он один на игру, и перезапишется при первом сохранении.
            // Так игрок может передумать и вернуться к старому прогрессу, не выходя в меню.
            Debug.Log("[MainMenu] Новая игра");
            LoadLevel();
        }

        void LoadLevel()
        {
            if (string.IsNullOrEmpty(levelSceneName))
            {
                Debug.LogError("[MainMenu] Не задано имя сцены уровня (levelSceneName)");
                _busy = false;
                return;
            }
            SceneManager.LoadScene(levelSceneName);
        }

        void QuitGame()
        {
            Debug.Log("[MainMenu] Выход");
            Application.Quit();
#if UNITY_EDITOR
            // Application.Quit() в редакторе ничего не делает — останавливаем Play сами.
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        void EnsureStyles()
        {
            if (_buttonStyle != null) return;

            // LegacyRuntime.ttf — единственный встроенный шрифт в Unity 6.
            // Просто "Arial.ttf" тут молча вернёт null, и текст не нарисуется.
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) Debug.LogError("[MainMenu] Не найден LegacyRuntime.ttf — текста не будет");

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = titleFontSize,
                alignment = TextAnchor.MiddleCenter,
                font = font,
            };
            _titleStyle.normal.textColor = new Color(1f, 0.87f, 0.55f);

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = buttonFontSize,
                alignment = TextAnchor.MiddleCenter,
                font = font,
            };
        }

        void OnGUI()
        {
            EnsureStyles();

            // Полупрозрачная подложка, чтобы меню читалось на любом фоне.
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Вертикальный центр, считаем от числа пунктов, а не от константы.
            float blockHeight = 110f + _items.Length * (buttonHeight + 16f);
            float y = (Screen.height - blockHeight) * 0.5f;
            float x = (Screen.width - buttonWidth) * 0.5f;

            var titleRect = new Rect(x, y, buttonWidth, 90f);
            GUI.Label(titleRect, "Donut Quest", _titleStyle);

            y += 110f;
            for (int i = 0; i < _items.Length; i++)
            {
                var rect = new Rect(x, y, buttonWidth, buttonHeight);

                if (i == _selected)
                {
                    // Подсветка выбранного пункта — иначе непонятно, куда нажмётся A.
                    GUI.backgroundColor = new Color(1f, 0.75f, 0.35f);
                }
                else
                {
                    GUI.backgroundColor = Color.white;
                }

                if (GUI.Button(rect, Label(_items[i]), _buttonStyle) && !_busy)
                {
                    // Мышь тоже работает: клик сразу активирует пункт.
                    _selected = i;
                    Activate(i);
                    break;
                }

                y += buttonHeight + 16f;
            }

            GUI.backgroundColor = Color.white;

            // Подсказка по управлению — геймпад и клавиатура.
            var hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                font = _buttonStyle.font,
            };
            hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.65f);
            GUI.Label(new Rect(x, y + 8f, buttonWidth, 30f),
                "Стик / ↑↓ — выбор, A / Enter — подтвердить", hintStyle);
        }

        static string Label(Item item)
        {
            switch (item)
            {
                case Item.Continue: return "Продолжить";
                case Item.NewGame: return "Новая игра";
                default: return "Выход";
            }
        }
    }
}
