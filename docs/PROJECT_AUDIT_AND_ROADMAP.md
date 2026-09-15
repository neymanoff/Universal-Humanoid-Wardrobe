# Universal Humanoid Wardrobe — Аудит проекта и Архитектурный План

> **Дата аудита**: Сентябрь 2026  
> **Версия Unity**: Unity 6 (6000.5.10f1)  
> **Цель**: Создание модульного, расширяемого и производительного пакета гардероба для Unity Asset Store (бесплатный релиз для портфолио).

---

## 1. Экспресс-анализ: почему после клона с GitHub «слетели вещи»

В ходе расследования структуры репозитория и Git-истории были выявлены три взаимосвязанные проблемы:

### 1.1. Ошибка Git LFS (404 Object does not exist on the server)
* В файле `.gitattributes` все 3D-модели (`*.fbx`) и текстуры (`*.png`, `*.ttf`) настроены на отслеживание через **Git LFS**.
* При отправке коммитов с предыдущего компьютера в репозиторий GitHub попали текстовые указатели LFS (LFS pointers размером ~130 байт, например: `oid sha256:157891c0...`), но сами бинарные файлы на сервер GitHub LFS загружены **не были** (либо превышена квота бесплатного тарифа GitHub LFS в 1 ГБ, либо пуш был выполнен обычной командой `git push` без завершения загрузки LFS-хранилища).
* При клонировании на новом компьютере Git попытался скачать файлы через фильтр `smudge` и получил от GitHub ошибку:
  ```text
  Error downloading object: [404] Object does not exist on the server
  ```

### 1.2. Автоматическое удаление `.meta` файлов редактором Unity
* Когда на диске отсутствовали бинарные файлы FBX/PNG (или они были повреждены), редактор Unity при первом запуске обнаружил «осиротевшие» `.meta` файлы и **автоматически удалил их** (стандартное поведение AssetDatabase).
* Из-за удаления `.meta` файлов потерялись GUID всех моделей и текстур. Префабы, материалы и ScriptableObject'ы потеряли ссылки на меши и текстуры («слетели вещи»).
* **Текущий статус исправления**: Мы полностью восстановили все `.meta` файлы из истории Git командой `git checkout -- "*.meta"`. Все GUID, настройки импорта Humanoid-ригов и привязки материалов восстановлены и ожидают копирования исходных бинарных FBX/PNG файлов.

### 1.3. Случайное индексирование удаления всех файлов (Staged Deletions)
* В индексе Git все 401 файл проекта были помечены как `deleted` (вероятно, следствие команды `git rm -r --cached .`, запущенной при попытке удалить `.gemini` из репозитория).
* **Текущий статус исправления**: Мы выполнили `git reset HEAD`, вернув индекс в корректное состояние без потери рабочих файлов.

---

## 2. Анализ текущего кода и найденные баги

### 2.1. `SkinnedMeshRemapper.cs`
* **Как работает сейчас**:
  * Сканирует скелет целевого персонажа, строит `Dictionary<string, Transform>` по именам костей.
  * Проходит по костям `clothingRenderer.bones` и сопоставляет их по точному строковому совпадению `currentBones[i].name`.
  * Удаляет дублирующую иерархию костей префаба (`CleanupDuplicateSkeleton`).
* **Критические недочёты и баги**:
  1. **Слёт Bounds / Frustum Culling**: В строке `clothingRenderer.rootBone = targetSkeletonRoot;` корень костей устанавливается на базовый `GameObject` персонажа вместо корневой кости скелета (`Hips` / `spine`). Из-за этого Unity рассчитывает bounding box со смещением, что приводит к внезапному исчезновению одежды при повороте камеры (frustum culling).
  2. **Жесткая привязка к именам костей**: Поиск ведется строго по имени. Если меш одежды сделан в Rigify (`spine.001`, `thigh.L`), а модель персонажа из Mixamo (`mixamorig:Hips`, `mixamorig:LeftUpLeg`), строковое совпадение выдает предупреждение, и кость не привязывается.
  3. **Неиспользуемый компонент**: В методе `Remap` объявляется переменная `Animator animator = targetSkeletonRoot.GetComponentInChildren<Animator>();`, которая затем нигде не используется.

### 2.2. `WardrobeManagerEditor.cs`
* **Баг двойного инстанцирования в Editor Preview**:
  ```csharp
  GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(item.prefab);
  _manager.Equip(item.slot, instance);
  ```
  Внутри `_manager.Equip` вызывается `EquipInternal`, который **повторно** вызывает `Instantiate(prefab, transform, false);`.
  В результате первый экземпляр `instance` остается болтаться в корне сцены как неудаляемая «утечка», а на персонажа надевается второй экземпляр.

### 2.3. `Neymanoffunity.HumanoidWardrobe.Tests.asmdef`
* В сборке тестов `Runtime` была допущена опечатка в ссылке: `"Neymanoff.HumanoidWardrobeardrobe"`.
* В `Editor.Tests` были указаны несуществующие сборки с суффиксом `unity`.
* **Текущий статус**: Опечатки исправлены, asmdef компилируются корректно.

---

## 3. Архитектура: разделение игры, гардероба и инвентаря

Главное требование: **модуль должен быть полностью независимым от любой конкретной игры, но легко встраиваться в неё**.

```
 ┌────────────────────────────────────────────────────────┐
 │                    Game Layer                          │
 │  (Game Inventory, Save/Load System, Scene Transition)  │
 └──────────────────────────┬─────────────────────────────┘
                            │ (Events / Loadout DTO)
                            ▼
 ┌────────────────────────────────────────────────────────┐
 │            Universal Humanoid Wardrobe Core             │
 │                                                        │
 │  ┌───────────────────────┐   ┌──────────────────────┐  │
 │  │    WardrobeManager    │   │   WardrobeLoadout    │  │
 │  │  (Central Controller) │   │  (Serializable Data) │  │
 │  └───────────┬───────────┘   └──────────────────────┘  │
 │              │                                         │
 │       ┌──────┴──────────────┐                          │
 │       ▼                     ▼                          │
 │ ┌──────────────────┐  ┌───────────────────────────┐    │
 │ │SkinnedMeshRemapper│  │  HumanoidAttachmentPoint  │    │
 │ │(Deforming clothes│  │  (Rigid weapons, helmets, │    │
 │ │  & armor meshes) │  │   shields & accessories)  │    │
 │ └──────────────────┘  └───────────────────────────┘    │
 └────────────────────────────────────────────────────────┘
                            ▲
                            │ (Optional UI Integration)
 ┌──────────────────────────┴─────────────────────────────┐
 │                Wardrobe Demo / UI Layer                │
 │    (DemoInventoryUI, EquipmentSlotUI, Turntable)       │
 └────────────────────────────────────────────────────────┘
```

### 3.1. Как одежда сохраняется между сценами и во время игры
Существует 2 основных сценария в реальных играх:

#### Сценарий А: Персонаж переносится между сценами (`DontDestroyOnLoad`)
* Персонаж настраивается в сцене гардероба/создания персонажа.
* На объект персонажа вешается `DontDestroyOnLoad(gameObject)`.
* При загрузке игрового уровня надетые объекты префабов (дочерние объекты костей и ремапнутые меши) **автоматически сохраняются** на персонаже, анимации продолжают работать, ничего не слетает.

#### Сценарий Б: Персонаж спавнится из префаба на уровне, гардероб восстанавливается из сохранения (Рекомендуемый стандарт)
* Модуль должен иметь класс данных `WardrobeLoadout`, поддерживающий сериализацию в JSON:
  ```csharp
  [System.Serializable]
  public class WardrobeLoadout
  {
      public List<EquippedItemEntry> items = new();
  }
  
  [System.Serializable]
  public struct EquippedItemEntry
  {
      public EquipmentSlot slot;
      public string itemId; // Идентификатор WardrobeItemSO
  }
  ```
* В `WardrobeManager` добавляются методы:
  * `WardrobeLoadout GetCurrentLoadout()` — возвращает текущее состояние надетых предметов.
  * `void ApplyLoadout(WardrobeLoadout loadout)` — очищает текущий сет и надевает все предметы из снаряжения.
  * `string ToJson()` / `void FromJson(string json)` — для легкой интеграции с любой системой сохранений (PlayerPrefs, EasySave, файл сохранения).

### 3.2. Как гардероб взаимодействует с инвентарем игры
Гардероб **не должен диктовать**, как устроен инвентарь (весовой, сеточный, RPG-слотовый). Он должен предоставлять контракт (API и интерфейс):

1. **Интерфейс поставщика предметов `IWardrobeInventoryProvider`**:
   ```csharp
   public interface IWardrobeInventoryProvider
   {
       IReadOnlyList<WardrobeItemSO> GetAvailableItems();
       bool CanEquipItem(WardrobeItemSO item, EquipmentSlot slot);
   }
   ```
2. **Событийная модель (Observer Pattern)**:
   Разработчик игры просто подписывается на события `WardrobeManager`:
   * `event Action<EquipmentSlot, WardrobeItemSO, GameObject> OnItemEquipped;`
   * `event Action<EquipmentSlot, WardrobeItemSO> OnItemUnequipped;`
   * `event Action<WardrobeLoadout> OnLoadoutChanged;`
   *Если игрок надел меч в гардеробе, игровая логика получает событие и добавляет персонажу +10 к урону; если снял — отнимает.*

### 3.3. Как сцена гардероба получает персонажа для переодевания
* **Случай 1 (Одиночный персонаж / текущий игрок)**:
  `WardrobeManager` на объекте персонажа в сцене либо автоматически регистрируется в `WardrobeManager.CurrentInstance`, либо передается ссылкой в инспекторе.
* **Случай 2 (Выбор персонажа / Подиум / Кастомизация разных героев)**:
  Компонент `WardrobePreviewStage` позволяет менять целевого персонажа:
  ```csharp
  wardrobeUI.SetTargetCharacter(newCharacterWardrobeManager);
  ```
  UI автоматически считывает текущий лодаут нового персонажа и обновляет иконки в слотах.

---

## 4. План доработок для релиза на Asset Store

1. **Исправление бинарных ассетов (совместно с пользователем)**:
   * Загрузка/копирование реальных моделей (`Dummy.fbx`, `SM_Slavic_Helmet.fbx`, `SM_Slavic_Chest_Armor.fbx`) и текстур без конфликтов LFS.
2. **Доработка `SkinnedMeshRemapper`**:
   * Привязка `rootBone` к `Hips` скелета вместо корня GameObject.
   * Универсальное сопоставление костей через `Humanoid Avatar` (`Animator.GetBoneTransform`) как запасной вариант к именам костей.
   * Автоматическое копирование `localBounds` тела для исключения исчезновения одежды при отсечении камерой.
3. **Исправление `WardrobeManagerEditor`**:
   * Устранение утечки префабов при нажатии *Preview Default Loadout*.
4. **Добавление сериализации и сохранения**:
   * Создание `WardrobeLoadout` и методов `GetCurrentLoadout()` / `ApplyLoadout()`.
5. **Создание полноценной документации и Samples**:
   * Готовый `README.md` для корня проекта и для пакета.
   * Полное руководство `Documentation/humanoid-wardrobe.md`.
   * Вынесение демо-сцены и UI в официальный формат `Samples~/DemoScene`.
