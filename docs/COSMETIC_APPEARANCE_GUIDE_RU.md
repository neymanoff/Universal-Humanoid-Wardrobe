# Пошаговое руководство для начинающих: Косметическая внешность (Причёска, Борода и Цвет)

Это руководство написано простым языком для разработчиков, которые только начинают работать с Unity и пакетом `Universal-Humanoid-Wardrobe`. Здесь разобрано всё устройство новой необязательной подсистемы косметической внешности (`CharacterAppearance`).

---

## 1. Зачем сделана эта система и как она работает простыми словами

В RPG-играх у персонажа, помимо экипировки (шлема, брони, оружия в руках), есть **собственная внешность**: причёска, борода и их цвет.

Почему нельзя было просто надевать причёску в слот головы (`EquipmentSlot.Head`), а бороду — в слот маски (`EquipmentSlot.FaceMask`)?
1. Если положить причёску в слот `Head`, то при надевании короны, обруча или шапки причёска будет сниматься и удаляться как обычный предмет экипировки.
2. Персонажу нужно помнить свою выбранную причёску, бороду и их цвет даже тогда, когда он надел глухой закрытый шлем или классовый капюшон: волосы временно прячутся под шлемом, а как только шлем снимается — выбранная причёска с тем же самым цветом сразу возвращается на место.
3. Не всем персонажам в игре нужна кастомизация волос и бороды (например, скелетам, монстрам или простым манекенам). Поэтому система сделана **отдельным необязательным компонентом** `CharacterAppearance`: обычный `WardrobeManager` работает сам по себе и не требует подключать косметику.

**Как это работает внутри:**
* На персонажа по желанию добавляется компонент `CharacterAppearance`.
* В него назначается каталог (`CosmeticCatalogSO`), в котором лежат карточки причёсок и бород (`CosmeticOptionSO`).
* Когда выбирается причёска или борода, `CharacterAppearance` создаёт её 3D-префаб и привязывает к голове персонажа (через уже знакомые компоненты `HumanoidAttachmentPoint` или `SkinnedMeshRemapper`).
* Цвет волос и бороды накладывается через `MaterialPropertyBlock` индивидуально на конкретного персонажа, поэтому **исходный материал в проекте не портится, а другие персонажи на сцене не перекрашиваются**.

---

## 2. Какие новые файлы, компоненты и настройки появились

Все новые скрипты находятся внутри пакета `Packages/com.neymanoff_unity.humanoid-wardrobe/`:

| Файл | За что отвечает |
| :--- | :--- |
| `Runtime/CosmeticTypes.cs` | Перечисления `CosmeticCategory` (`Hair`, `Beard`), битовая маска скрытия `CosmeticVisibilityMask` (`None`, `Hair`, `Beard`, `All`), структура цветового пресета `CosmeticColorPreset` и результат загрузки `AppearanceApplyResult`. |
| `Runtime/CosmeticOptionSO.cs` | ScriptableObject-карточка одной конкретной причёски или бороды: хранит стабильный `optionId`, название, категорию (`Hair` / `Beard`), 3D-префаб, кость крепления (`Head`), настройки материалов и шейдерных свойств цвета. |
| `Runtime/CosmeticCatalogSO.cs` | ScriptableObject-каталог, объединяющий список доступных причёсок (`hairOptions`), список бород (`beardOptions`) и готовую палитру цветов (`colorPresets`). |
| `Runtime/CharacterAppearanceData.cs` | Класс данных (DTO) для сохранения и загрузки внешности в JSON (`hairOptionId`, `beardOptionId`, `hairColor`, `beardColor`, `linkColors`). |
| `Runtime/ICharacterAppearance.cs` | Небольшой публичный интерфейс (контракт), через который внешний адаптер игры управляет внешностью и ограничениями видимости. |
| `Runtime/CharacterAppearance.cs` | Необязательный `MonoBehaviour`-компонент, который вешается на персонажа. Управляет спавном причёски и бороды, покраской и скрытием под шлемом/комплектом. |
| `Editor/CharacterAppearanceEditor.cs` | Кастомный инспектор для `CharacterAppearance`: добавляет удобные кнопки быстрого переключения причёсок, бород и цветов прямо в окне Inspector. |

Также в `WardrobeItemSO.cs` появилось новое поле:
* **`Hidden Cosmetics` (`CosmeticVisibilityMask`)** — флажки (`Hair`, `Beard`), указывающие, должен ли этот предмет экипировки (например, закрытый шлем) временно скрывать волосы и/или бороду, пока он надет.

Готовые примеры и заглушки в папке `Assets/`:
* Префабы-заглушки: `Assets/Prefabs/Cosmetics/` (`Hair_Short_Stub.prefab`, `Hair_TopKnot_Stub.prefab`, `Beard_Full_Stub.prefab`, `Beard_Goatee_Stub.prefab`).
* Карточки и каталог: `Assets/ScriptableObjects/Cosmetics/` (`HairShortOptionSO.asset`, `HairTopKnotOptionSO.asset`, `BeardFullOptionSO.asset`, `BeardGoateeOptionSO.asset`, `DefaultCosmeticCatalogSO.asset`).

---

## 3. Как пошагово добавить новую причёску или бороду

### Требования к 3D-модели:
1. **Жёсткая (статичная) модель причёски или бороды**:
   * Повесьте на корневой объект префаба компонент `HumanoidAttachmentPoint`.
   * Включите `Use Custom Bone` и выберите `Target Bone = Head`.
   * Подгоните положение (`Local Position`, `Local Rotation`, `Local Scale`) относительно кости головы (или выставьте объект на голове в сцене и нажмите ПКМ по компоненту -> `Capture Transform as Default Offsets`).
2. **Скиннинговая (гнущаяся по костям) модель**:
   * Если борода или длинные волосы привязаны к скелету (`SkinnedMeshRenderer`), повесьте на префаб компонент `SkinnedMeshRemapper` — точно так же, как на броню.

### Пошаговое создание карточки в Unity:
1. В окне **Project** нажмите ПКМ по папке `Assets/ScriptableObjects/Cosmetics/` -> **Create -> Humanoid Wardrobe -> Cosmetic Option**.
2. Назовите ассет, например `HairLongBraidSO`.
3. В **Inspector** заполните поля:
   * **Option Id**: уникальный стабильный идентификатор латиницей без пробелов (например, `hair_long_braid`). Именно по нему игра будет сохранять выбор игрока.
   * **Display Name**: отображаемое имя (например, `Long Braid`).
   * **Category**: выберите `Hair` (для причёски) или `Beard` (для бороды).
   * **Prefab**: перетащите ваш 3D-префаб причёски или бороды.
   * **Default Attachment Bone**: оставьте `Head`.
4. Откройте ваш каталог `DefaultCosmeticCatalogSO` и добавьте созданную карточку в список **Hair Options** (или **Beard Options**).

---

## 4. Как настроить цвета и материалы

Чтобы волосы и борода красиво перекрашивались в любой выбранный цвет:
1. **Текстура материала**: базовая текстура (`Base Map` / `Albedo`) материала волос должна быть светлой (белой или светло-серой с нарисованными прядями), а базовый цвет материала в ассете — белым (`#FFFFFF`). Тогда при умножении на выбранный цвет в шейдере получится чистый оттенок (блонд, рыжий, чёрный, седой).
2. **Настройки в `CosmeticOptionSO`**:
   * **Supports Color Tinting**: включите галочку (`true`).
   * **Color Shader Properties**: список имён цветовых свойств шейдера. По умолчанию там уже указаны `_BaseColor` (стандарт для URP Lit) и `_Color` (стандарт для Built-in / Legacy шейдеров). Если у вас кастомный шейдер волос (например, с параметром `_HairTint`), просто добавьте его имя в этот список.
   * **Target Material Indices**: если на модели причёски есть металлические заколки или ленты (например, материал `0` — волосы, а материал `1` — золотое кольцо в бороде), укажите в `Target Material Indices` только индекс `0`. Тогда перекрашиваться будут только волосы, а заколка останется золотой! Если список пустой — перекрашиваются все материалы на префабе.
   * **Target Renderer Names**: если внутри префаба несколько дочерних объектов `MeshRenderer` / `SkinnedMeshRenderer` и красить нужно только конкретные, укажите их имена. Если список пустой — красятся все рендереры префаба.
3. **Одинаковый или разный цвет волос и бороды (`Link Hair And Beard Color`)**:
   * На компоненте `CharacterAppearance` есть галочка **Link Hair And Beard Color**.
   * Если она включена (`true`), смена цвета волос через `SetHairColor(color)` автоматически красит и бороду в тот же цвет.
   * Если её выключить (`false`) или вызвать `SetColors(hairColor, beardColor, linkColors: false)`, волосы и борода могут иметь совершенно разные цвета.

---

## 5. Как указать, что головной убор или внешний комплект скрывает волосы/бороду

В системе предусмотрено **два способа** временного скрытия внешности, которые работают одновременно и безопасно складываются друг с другом:

### Способ А: Через предмет экипировки (`WardrobeItemSO`)
1. Выделите ассет шлема или маски (например, `Assets/ScriptableObjects/Head/SlavicHelmetSO.asset`).
2. В разделе **Cosmetic Compatibility** найдите поле **Hidden Cosmetics**:
   * Выберите `Hair`, если шлем открытый снизу (скрывает только причёску, а борода видна).
   * Выберите `Beard`, если это плотная маска на нижнюю часть лица.
   * Выберите `Everything` (`Hair | Beard`), если это глухой рыцарский шлем или капюшон с забралом.
3. Когда персонаж надевает этот предмет через `WardrobeManager`, `CharacterAppearance` автоматически скрывает указанные части. При снятии предмета они автоматически возвращаются.

### Способ Б: Через внешний визуальный комплект или адаптер (из кода)
Если в игре внешний вид брони задаётся целиком классовым комплектом (без надевания отдельного предмета в `WardrobeManager`), адаптер может наложить ограничение видимости по строковому ключу:

```csharp
// Скрыть причёску от имени классового комплекта "Heir_HeavyHelm":
appearance.SetVisibilityRestriction("Heir_HeavyHelm", CosmeticVisibilityMask.Hair);

// Когда классовый шлем скрыт или сменился на открытый комплект — снимаем ограничение:
appearance.RemoveVisibilityRestriction("Heir_HeavyHelm");
```

**Важное правило нескольких источников:**
Если на персонаже одновременно надет шлем (источник 1, скрывает `Hair`) И включён внешний капюшон (источник 2, скрывает `Hair`), то при снятии только шлема волосы **останутся скрытыми**, пока не будет снят и капюшон. При этом выбранный `optionId` причёски и её цвет ни в какой момент не теряются!

---

## 6. Как протестировать всё в демо-сцене и через тесты

### В демо-сцене (`Assets/Scenes/WardrobeDemoScene.unity`):
1. Откройте сцену `Assets/Scenes/WardrobeDemoScene.unity` и нажмите **Play**.
2. Слева на экране расположены кнопки:
   * **Hairstyle** — переключает причёски по кругу (`Short Crop` -> `Warrior Top-Knot` -> `None`).
   * **Beard** — переключает бороды по кругу (`Full Beard` -> `Goatee` -> `None`).
   * **Hair Color** — переключает цветовые пресеты из каталога (`Dark Brown`, `Golden Blonde`, `Copper Red`, `Silver Grey`, `Raven Black`). Для простых заглушек в `CosmeticOptionSO` включена галочка `Clear Base Texture On Tint`, чтобы тёмная текстура общего материала не затемняла выбранный цвет.
   * **Hide Helmet / Show Helmet** — включает и выключает отображение надетого шлема (`HideHelmetVisual`). Когда отображение шлема скрыто, сам шлем **остаётся надетым в ячейке `Head`** (и даёт свои игровые бонусы), но его 3D-модель прячется, а голова, причёска и борода персонажа остаются открытыми!
3. Наденьте шлем **Slavic Helmet** из инвентаря справа (кликом или перетаскиванием в слот головы):
   * По умолчанию вы увидите, что причёска автоматически скрылась под шлемом, а борода осталась на месте.
   * Нажмите кнопку **Hide Helmet** — шлем останется в слоте экипировки, но визуально скроется с головы, а выбранная причёска и голова снова появятся.
   * Нажмите **Show Helmet** — шлем снова появится на голове и скроет причёску.
4. Вы также можете выделить объект `Dummy` в Hierarchy и прямо в окне **Inspector** на компоненте `CharacterAppearance` нажимать кнопки быстрого предпросмотра (`Short Crop`, `Warrior Top-Knot`, `Full Beard`, `Goatee`, `None`, цветовые пресеты).

### Через автоматические тесты (Unity Test Runner):
1. В меню Unity откройте **Window -> General -> Test Runner**.
2. Перейдите на вкладку **PlayMode** (или **EditMode**).
3. Найдите набор тестов `CharacterAppearanceTests` и нажмите **Run All**.
4. Проверяются: независимый выбор и очистка, сосуществование со слотами `Head` и `FaceMask`, изоляция цветов между двумя персонажами (`CharacterA` и `CharacterB`), наложение нескольких источников скрытия и сохранение/восстановление JSON (включая обработку отсутствующего ID без ошибок).

---

## 7. Как мастер будет подключать это в основную игру через адаптер

В основной игре (*Legends: Legacy of the Lost*) у Наследника (или других героев) внешний вид обмундирования зависит от класса, а оружие отображается в руках. Пакет `Universal-Humanoid-Wardrobe` ничего не знает про Наследника или классы — вся связь делается в тонком классе-адаптере на стороне игры через интерфейс `ICharacterAppearance`:

```csharp
using Neymanoff.HumanoidWardrobe;
using UnityEngine;

public class HeirAppearanceAdapter : MonoBehaviour
{
    [SerializeField] private CharacterAppearance characterAppearance;

    // 1. Применение выбора игрока из экрана кастомизации:
    public void ApplyPlayerCustomization(string hairId, string beardId, Color hairCol, Color beardCol, bool sameColor)
    {
        characterAppearance.SetColors(hairCol, beardCol, sameColor);
        characterAppearance.SetHair(hairId);
        characterAppearance.SetBeard(beardId);
    }

    // 2. Когда класс Наследника меняет визуальный комплект брони:
    public void OnClassVisualOutfitApplied(bool outfitHasClosedHelmet, bool outfitCoversChin)
    {
        CosmeticVisibilityMask mask = CosmeticVisibilityMask.None;
        if (outfitHasClosedHelmet) mask |= CosmeticVisibilityMask.Hair;
        if (outfitCoversChin) mask |= CosmeticVisibilityMask.Beard;

        characterAppearance.SetVisibilityRestriction("ClassVisualOutfit", mask);
    }

    // 3. Сохранение в сейв-файл игры:
    public string ExportSaveJson()
    {
        CharacterAppearanceData dto = characterAppearance.GetAppearanceData();
        return dto.ToJson();
    }

    // 4. Загрузка из сейв-файла игры:
    public void ImportSaveJson(string json)
    {
        CharacterAppearanceData dto = CharacterAppearanceData.FromJson(json);
        AppearanceApplyResult result = characterAppearance.ApplyAppearanceData(dto);

        if (!result.IsFullyResolved)
        {
            Debug.LogWarning($"Некоторые старые ID причёски/бороды не найдены: Hair={result.MissingHairId}, Beard={result.MissingBeardId}");
        }
    }
}
```

---

## 8. Типичные ошибки и как их быстро исправить

| Проблема | Причина | Как быстро исправить |
| :--- | :--- | :--- |
| При вызове `SetHair("...")` в консоли предупреждение `Catalog is not assigned` или `was not found in catalog`. | На компоненте `CharacterAppearance` не назначен `Catalog`, либо новая карточка `CosmeticOptionSO` не добавлена в список `Hair Options` / `Beard Options` внутри каталога. | Выделите персонажа, проверьте поле `Catalog` на `CharacterAppearance` и убедитесь, что карточка добавлена в нужный список в `DefaultCosmeticCatalogSO`, а её `Category` соответствует списку (`Hair` или `Beard`). |
| Причёска или борода появляется в ногах персонажа (в точке `0,0,0`). | На префабе причёски/бороды нет `HumanoidAttachmentPoint` (или `SkinnedMeshRemapper`), либо на персонаже `Animator` не настроен как `Humanoid`. | Добавьте на корень префаба причёски `HumanoidAttachmentPoint`, включите `Use Custom Bone` и выберите `Target Bone = Head`. Проверьте, что у персонажа в `Animator` назначен Humanoid Avatar. |
| Цвет выбирается в коде/инспекторе, но модель в сцене не меняет оттенок. | Шейдер материала использует нестандартное имя свойства цвета, либо базовый цвет материала в самом `.mat` файле чёрный (`0,0,0`). | Откройте материал волос и выставьте его `Base Map` / `Base Color` в белый (`#FFFFFF`). Проверьте имя свойства цвета в шейдере (например, `_BaseColor`) и убедитесь, что оно есть в списке `Color Shader Properties` на `CosmeticOptionSO`. |
| Сняли шлем, но волосы всё равно не появились. | Остался активным ещё один источник скрытия (например, надет предмет в другом слоте с флагом `Hidden Cosmetics = Hair` или не снято внешнее ограничение `SetVisibilityRestriction`). | Посмотрите в Inspector на компонент `CharacterAppearance` во время PlayMode: в информационном блоке `Restrictions` видно текущую маску ограничений. Проверьте `Hidden Cosmetics` на остальных надетых предметах или вызовите `ClearExternalVisibilityRestrictions()`. |
