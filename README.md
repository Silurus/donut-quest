# Donut Quest

2.5D платформер про корги Корру. Unity 6.3 LTS, C#. Основная платформа — Steam Deck, сборка под Windows тоже.

## Что где лежит

| Путь | Что это |
|---|---|
| `SPEC.md` | Спецификация игры: сюжет, зоны, механики, управление, движок |
| `docs/UNITY_DEV_GUIDE.md` | Гайд по проекту: что где в Unity, как собрать префаб, как я проверяю код |
| `Assets/Models/Corra/corgi.glb` | Модель Корры, CC BY 4.0, автор zinaida |
| `Assets/Models/Cat/Cat.fbx` | Кот, Quaternius CC0 |
| `Assets/Scripts/Gameplay/` | Ввод, движение, анимации персонажа |
| `Assets/Scripts/Editor/` | Сборщик префаба, работает только в редакторе |
| `tools/compilecheck/` | Проверка компиляции без Unity |

## Быстрый старт

```bash
# проверить, что код компилируется (Unity не нужен)
bash tools/compilecheck/check.sh
```

В Unity: открыть проект → проверить, что у `corgi.glb` есть три клипа → меню **Donut Quest → Собрать префаб Корры** → перетащить `Assets/Prefabs/Corra.prefab` в сцену → Play.

## Состояние

- [x] Спецификация v0.4
- [x] Модель Корры в проекте, разобрана и проверена
- [x] Движение, ввод, аниматор, сборщик префаба
- [x] Проверка компиляции локально и в GitHub Actions

### Задачи на завтра (31 октября)

- [ ] **Переименовать в коде `Corra` → `Korra`.** По-русски героиню зовут Корра, а в латинице это Korra, не Corra. Переименование сквозное, чтобы не осталось смеси из двух написаний:
  - классы: `CorraInput` → `KorraInput`, `CorraController` → `KorraController`, `CorraAnimator` → `KorraAnimator`, `CorraPrefabBuilder` → `KorraPrefabBuilder`;
  - файлы в `Assets/Scripts/Gameplay/` и `Assets/Scripts/Editor/`;
  - папка модели `Assets/Models/Corra/` → `Assets/Models/Korra/`;
  - префаб `Assets/Prefabs/Corra.prefab` → `Korra.prefab` и объект в сцене;
  - Animator Controller `CorraAnimator.controller` → `KorraAnimator.controller`;
  - пункт меню **Donut Quest → Собрать префаб Корры** остаётся, он по-русски;
  - константы путей внутри `KorraPrefabBuilder` (ModelPath, ControllerPath, PrefabPath);
  - упоминания в `README.md`, `docs/UNITY_DEV_GUIDE.md`, `ASSET_GUIDE.md`.
  После переименования прогнать `bash tools/compilecheck/check.sh` и дождаться зелёного CI.
- [ ] Первая сцена с землёй
- [ ] Зона сохранения у куста
- [ ] Лай и реакции кота
- [ ] Огурчики и сыр
- [ ] Уровень целиком

## Лицензии

Модель Корры — CC BY 4.0, автор zinaida. Текст атрибуции в `Assets/Models/Corra/ATTRIBUTION.md`, его нужно указать при публикации игры.
