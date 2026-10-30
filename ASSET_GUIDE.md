# Гайд по 3D-моделям для Donut Quest

## Как я ищу модели

1. **Критерии поиска:**
   - Лицензия: CC0, CC-BY (для личного пользования)
   - Формат: **FBX** (лучше всего для Unity) или **GLB/glTF**
   - Качество: low-poly (до 50k треугольников для Steam Deck), риггированы, с анимациями
   - Платформы: Sketchfab, OpenGameArt, itch.io, Quaternius, Poly.pizza

2. **Почему FBX?**
   - Формат Autodesk, стандарт для игровых движков
   - Хранит: геометрию, материалы, риг (кости), анимации в одном файле
   - Unity импортирует напрямую, создаёт Material и Animator автоматически

3. **Почему GLB/glTF вторичный:**
   - GLB — бинарный контейнер glTF (текстуры + модель в одном файле)
   - Меньше поддержки анимаций в некоторых редакторах
   - Но подходит, если FBX недоступен

## Как подключить модель в Unity

### Этап 1: Скачать и положить в проект

```bash
# Структура:
Assets/Models/
├── Corga/
│   ├── Corga.fbx
│   ├── Corga.fbx.meta
│   └── Textures/
│       └── corga_diffuse.png
└── Cat/
    ├── Cat.fbx
    └── Cat.fbx.meta
```

### Этап 2: Импортные настройки в Unity Editor

После перетаскивания FBX в папку Assets:
1. Выбрать файл в Project
2. Инспектор (Inspector) → Import Settings:
   - **Rig:** выбрать "Humanoid" или "Generic" (зависит от модели)
   - **Animation:** галка "Import Animation" должна быть ON
   - **Materials:** Import Materials = ON
   - **Textures:** Embedded или External (зависит от файла)
3. Apply

### Этап 3: Использовать в коде (C#)

```csharp
using UnityEngine;

public class PlayerController : MonoBehaviour {
    private Animator animator;
    private Rigidbody rb;
    
    void Start() {
        // Получить Animator из дочернего GameObject'а модели
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        
        if (animator == null) Debug.LogError("Animator not found!");
    }
    
    void Update() {
        float moveInput = Input.GetAxis("Horizontal");
        
        // Перемещение
        rb.velocity = new Vector3(moveInput * 5f, rb.velocity.y, 0);
        
        // Анимация
        if (moveInput != 0) {
            animator.SetBool("IsMoving", true);
            animator.SetFloat("Speed", Mathf.Abs(moveInput));
        } else {
            animator.SetBool("IsMoving", false);
        }
    }
    
    public void Jump() {
        animator.SetTrigger("Jump");
        rb.velocity = new Vector3(rb.velocity.x, 5f, 0);
    }
    
    public void Bark() {
        animator.SetTrigger("Attack"); // или своя анимация для лая
    }
}
```

## Что внутри FBX-файла

Когда ты скачиваешь `Corga.fbx`:

```
FBX Binary Format
├── Mesh (геометрия)
│   └── Vertices, Faces, Normals, UVs
├── Armature (риг/скелет)
│   ├── Root bone
│   ├── Spine
│   ├── Head
│   ├── Front Left Leg
│   ├── Front Right Leg
│   ├── Back Left Leg
│   └── Back Right Leg
├── AnimStack "Idle"
│   └── Keyframes для каждой кости (позиция, ротация)
├── AnimStack "Walk"
│   └── Keyframes (шаг корги)
├── AnimStack "Run"
│   └── Keyframes (бег)
├── Material "Corga_Body"
│   └── Diffuse (цвет), Normal map, Specular
└── Texture references
    ├── corga_diffuse.png (цвет)
    ├── corga_normal.png (выпуклость)
    └── corga_spec.png (блеск)
```

Unity при импорте:
- Создаёт **Prefab** (шаблон объекта)
- Парсит анимации в **Animation Clips** (Idle, Walk, Run)
- Генерирует **Avatar** (описание скелета для Animator)
- Ищет текстуры автоматически

## Для Корры (главный персонаж)

Нужно:
- Скачать FBX с риггингом
- Импортировать в Assets/Models/Characters/
- Создать Prefab: Assets/Prefabs/Corga.prefab
- Добавить скрипт PlayerController.cs
- В сцене: перетащить prefab, выставить физику (Rigidbody + BoxCollider)

Анимации, которые нужны для спеки:
- Idle (стойка)
- Walk/Run (движение)
- Jump (прыжок)
- Bark (лай)
- Poop (какать для сохранения)
- Eat (еда — огурчик, сыр)

Если в модели нет анимации "Poop" или "Eat", придётся либо взять другую модель, либо создать свою.

## Для Кота (враг)

- Скачать отдельный FBX
- Импортировать рядом
- Создать префаб Assets/Prefabs/Enemy_Cat.prefab
- Добавить скрипт EnemyAI.cs (логика: следить за Коррой, бежать, прятаться на ветке)
- Использовать анимации: Idle, Walk, Run, (опционально Fear/Escape)

## Почему именно этот процесс?

1. **FBX-файл — это текст в бинарном формате**: не портится при распаковке, поддерживается везде
2. **Unity автоматизирует 80%**: импорт, риг, анимации
3. **Prefab — шаблон**: если понадобится 5 котов, создашь из одного prefab'а
4. **Animator Controller — граф переходов**: Idle → Walk → Run (логика переключения анимаций)

## Если модель не подходит

- Слишком полигонов? Используй Blender (бесплатно) для LOD (Level of Detail)
- Нет нужной анимации? Возьми другую модель или генерируй AI-анимацию (Mixamo — бесплатно)
- Неправильный риг? Переримируй в Blender (1–2 часа)
