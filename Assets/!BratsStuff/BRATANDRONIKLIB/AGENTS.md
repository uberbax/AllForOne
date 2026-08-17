BRATANDRONIKLIB — СТАНДАРТ

BratAndronikLIB — внутренняя папка проекта, компактный локальный view-код и assets для визуального polish без вмешательства в модель и архитектуру игры.
1. БАЗОВЫЕ ПРИНЦИПЫ
· Один компонент — одно локальное визуальное поведение.
· Только view-логика: отображение, движение, анимация и локальные визуальные эффекты.
· Внешние ссылки — Inspector
· Компоненты того же GameObject — берутся в Awake.
· Рабочие defaults: после добавления компонента базовый эффект уже настроен.
· Компонент изменяет только свой объект, его children и явно переданные ссылки.
· После отключения компонент не оставляет объект в промежуточном визуальном состоянии.
· В Update и каждом тике coroutine не создавать новые объекты, коллекции и строки без необходимости.
· Все кешируется - нет повторных Find*/GetComponent 
2. СТРУКТУРА ПАПКИ
Assets/BratAndronikLIB/
  Scripts/
  Images/
  Modules/
    <Name>/
      Scripts/
      Prefabs/
      Animations/
      Shaders/
      Materials/
      Textures/
      Images/
      VFX/
  Shared/
  AGENTS.MD

Папки модуля
· Scripts — одиночные view-компоненты без дополнительных assets.
· Images — одиночные спрайты и спрайт-атласы для использования вне.
· Modules/<Name> — компонент или эффект, которому нужны несколько связанных файлов.
· Shared — общие assets нескольких модулей, например один noise, mask или material.
· AGENTS.MD - файл с локальными системными инструкциями для Codex 
Подпапки модуля
· Scripts — MonoBehaviour модуля.
· Prefabs — готовые визуальные объекты.
· Animations — AnimationClip и Animator Controller.
· Shaders — шейдеры Surface для Built-In 
· Materials — материалы модуля.
· Textures — технические текстуры: masks, noise, gradients, normal maps.
· Images — sprites, icons и UI images.
· VFX — ParticleSystem и связанные assets.

Правило размещения
· Один самостоятельный скрипт → Scripts
· Скрипт + связанные assets → один Modules/<Name>
· Внутри модуля создаются только реально нужные подпапки.
3. ИМЕНОВАНИЕ
Тип поведения в имени скрипта
· View — специальное отображение или поведение объекта на сцене.
· Motion — локальное движение, вращение или scale.
· GUI — специальное поведение гуи элемента.
· Effect — запускаемый визуальный эффект VFX или SpriterAnim или Animator.
· Render — управление локальными shader/material-параметрами.

Имена
· Scripts: BRAT<Type><Name>. Например: BRATViewFloatingNumber, BRATGUIMapShow, BRATViewCoinDrop, BRATRenderWater.
· Файл и C#-класс имеют одинаковое имя: BRATEffectPortal.cs → BRATEffectPortal.
· Assets: BRAT<AssetType><Name>. Например: BRATPrefabCoinDrop, BRATMaterialWater, BRATShaderWater, BRATTextureWaterNoise, BRATImageCoin.
· Название описывает конкретное поведение или asset.
4. СТАНДАРТ MONOBEHAVIOUR
Inspector
· Настраиваемые ссылки и параметры — public с рабочими defaults. Runtime-state и внутренние кэши — private.
· [RequireComponent] — если локальный компонент на том же GameObject обязателен для работы.
· [Min]/[Range] — когда числовая настройка имеет реальный диапазон.
· [Header]/[Tooltip] — когда поле или группа полей большие или без них неочевидны.
Публичные методы
· Public — только действия, которые реально вызываются извне: Play(), Stop(), SetValue(...), SetVisible(...).
Lifecycle
· Awake — кэш локальных ссылок и исходного визуального состояния.
· OnEnable/OnDisable — только если эффект нужно запускать, останавливать или восстанавливать при активации объекта.
· Update/LateUpdate/FixedUpdate — только для поведения, которое действительно требует постоянного обновления.
· Неиспользуемые lifecycle-методы не объявляются.

5. PREFAB И ANIMATION
Prefab
· Внутренние ссылки Prefab назначены; внешние scene-зависимости, если нужны, передаются или находятся при инициализации.
· Если Prefab управляется одним view-компонентом, он находится на root; визуальные offsets/scale/rotation при необходимости остаются на child.
Animation
· AnimationClip/Animator — для визуальных анимаций, которые удобнее настраивать curves и timing
· Простое procedural motion остаётся в MonoBehaviour.
6. SHADERS И MATERIALS
Shaders
· Стандарт — Built-in Render Pipeline
· В Inspector выводятся только параметры, которые реально нужно настраивать.
Materials
· Один общий вид — один reusable material asset.
· Локальные параметры конкретного Renderer — через MaterialPropertyBlock.
· Для доступа к материалу использовать Renderer.sharedMaterial. 
7. ОБНОВЛЕНИЯ И СОВМЕСТИМОСТЬ
· .meta обновляются вместе с assets и не пересоздаются.
· При переименовании serialized field использовать [FormerlySerializedAs("oldName")].
· Заменяемый public API сначала помечать [Obsolete("Use X instead.", false)].
· Старый компонент/API удалять только после миграции всех использований.
8. ПРИМЕР VIEW-КОМПОНЕНТА
Короткий локальный эффект масштаба: рабочие defaults, внешний Play(), cleanup на disable, без Update.

using System.Collections;
using UnityEngine;

public sealed class BRATMotionPunchScale : MonoBehaviour
{
    [Min(0.01f)] public float duration = 0.2f;
    [Min(1f)] public float scale = 1.15f;

    private Vector3 _baseScale;
    private Coroutine _routine;

    public void Play()
    {
        Stop();
        _routine = StartCoroutine(PlayRoutine());
    }

    private void Stop()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;
        transform.localScale = _baseScale;
    }

    private void Awake() => _baseScale = transform.localScale;
    private void OnDisable() => Stop();

    private IEnumerator PlayRoutine()
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float progress = t / duration;
            float punch = Mathf.Sin(progress * Mathf.PI);
            transform.localScale = _baseScale * Mathf.Lerp(1f, scale, punch);
            yield return null;
        }

        transform.localScale = _baseScale;
        _routine = null;
    }
}
