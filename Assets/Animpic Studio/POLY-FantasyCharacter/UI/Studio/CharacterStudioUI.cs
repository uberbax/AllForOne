using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Animpic.CharacterStudio
{
    [DisallowMultipleComponent]
    public sealed class CharacterStudioUI : MonoBehaviour
    {
        public CharacterCustomizer female;
        public CharacterCustomizer male;
        public Camera viewCamera;
        public Transform stageRoot;
        public CharacterStudioPresentation presentation;
        public Canvas uiCanvas;
        public bool startWithMale;
        private CharacterCustomizer current;
        private Appearance femaleDefault, maleDefault;
        private RectTransform content;
        private Text identity, status, partSummary;
        private Font font;
        private int tab;
        private string paintedPart;
        private bool applying;
        private readonly List<Button> tabButtons = new List<Button>();
        private readonly List<Button> genderButtons = new List<Button>();
        private static readonly Color Background = AnimpicStudioTheme.Panel;
        private static readonly Color Surface = AnimpicStudioTheme.Surface;
        private static readonly Color Soft = AnimpicStudioTheme.Border;
        private static readonly Color Cream = AnimpicStudioTheme.Text;
        private static readonly Color Muted = AnimpicStudioTheme.Muted;
        private static readonly Color Accent = AnimpicStudioTheme.Accent;
        private static readonly Color[] PaletteColors = {
            new Color32(139,91,65,255), new Color32(97,105,87,255), new Color32(113,80,94,255),
            new Color32(79,98,111,255), new Color32(155,132,87,255), new Color32(88,82,100,255)
        };

        public void Configure(CharacterCustomizer femaleCharacter, CharacterCustomizer maleCharacter, Camera camera, Transform root)
        {
            female = femaleCharacter; male = maleCharacter; viewCamera = camera; stageRoot = root;
            if (female && female.Catalog && (femaleDefault == null || femaleDefault.catalogId != female.Catalog.id)) femaleDefault = female.Capture();
            if (male && male.Catalog && (maleDefault == null || maleDefault.catalogId != male.Catalog.id)) maleDefault = male.Capture();
            if (!presentation) presentation = GetComponent<CharacterStudioPresentation>();
            if (!presentation) presentation = gameObject.AddComponent<CharacterStudioPresentation>();
            presentation.Configure(camera, root);
        }
        private void Awake()
        {
            if (female || male) { Configure(female, male, viewCamera, stageRoot); BuildUI(); }
        }
        private void OnDestroy() { if (current) current.Changed -= OnCharacterChanged; }
        public void SetCharacter(bool useMale)
        {
            if (!content) { BuildUI(); if (!content) return; }
            var next = useMale ? male : female;
            if (!next) return;
            if (current) current.Changed -= OnCharacterChanged;
            current = next;
            if (female) female.gameObject.SetActive(next == female);
            if (male) male.gameObject.SetActive(next == male);
            current.Changed += OnCharacterChanged;
            paintedPart = null;
            UpdateIdentity(); RefreshContent();
        }
        public void BuildUI()
        {
            if (!female && !male) return;
            font = AnimpicStudioTheme.BodyFont;
            bool initialMale = current ? current == male : (male && male.gameObject.activeSelf && (!female || !female.gameObject.activeSelf)) || (startWithMale && (!female || !female.gameObject.activeSelf));
            if (female && female.Catalog && (femaleDefault == null || femaleDefault.catalogId != female.Catalog.id)) femaleDefault = female.Capture();
            if (male && male.Catalog && (maleDefault == null || maleDefault.catalogId != male.Catalog.id)) maleDefault = male.Capture();
            var old = transform.Find("Character Studio Canvas"); if (old) Remove(old.gameObject);
            tabButtons.Clear(); genderButtons.Clear();
            var canvasObject = new GameObject("Character Studio Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            uiCanvas = canvasObject.GetComponent<Canvas>(); uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay; uiCanvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = .6f;
            var root = (RectTransform)canvasObject.transform;

            var brand = Box(root, "Brand", Background); TopLeft(brand, 36, 32, 312, 148); Card(brand);
            if (AnimpicStudioTheme.Wordmark)
            {
                var logoObject = new GameObject("Animpic Studio logo", typeof(RectTransform), typeof(Image)); logoObject.transform.SetParent(brand, false);
                var logo = logoObject.GetComponent<Image>(); logo.sprite = AnimpicStudioTheme.Wordmark; logo.preserveAspect = true; logo.raycastTarget = false;
                TopLeft((RectTransform)logo.transform, 10, 2, 286, 95);
            }
            else { var logo = Label(brand, "ANIMPIC", 36, Cream); TopLeft(logo.rectTransform, 24, 24, 264, 56); }
            var brandLine = Box(brand, "Brand line", Accent); TopLeft(brandLine, 24, 98, 30, 2);
            var product = Label(brand, "CHARACTER STUDIO", 14, Muted); product.font = AnimpicStudioTheme.MediumFont; TopLeft(product.rectTransform, 66, 91, 216, 20);
            var brandSub = Label(brand, "POLY FANTASY COLLECTION", 12, Muted); TopLeft(brandSub.rectTransform, 24, 118, 270, 16);

            var left = Box(root, "Character identity", Background); TopLeft(left, 36, 202, 312, 230); Card(left);
            var eyebrow = Label(left, "01  /  YOUR CHARACTER", 13, Accent); eyebrow.font = AnimpicStudioTheme.MediumFont; TopLeft(eyebrow.rectTransform, 24, 21, 264, 18);
            identity = Label(left, "Fantasy Male", 27, Cream); TopLeft(identity.rectTransform, 24, 49, 264, 42);
            var genderRow = Row(left, "Character switch"); TopLeft(genderRow, 24, 110, 264, 44);
            genderButtons.Add(Button(genderRow, "Female", () => SetCharacter(false))); genderButtons.Add(Button(genderRow, "Male", () => SetCharacter(true)));
            var identityLine = Box(left, "Identity divider", AnimpicStudioTheme.Border); TopLeft(identityLine, 24, 176, 264, 1);
            partSummary = Label(left, "", 14, Muted); TopLeft(partSummary.rectTransform, 24, 187, 264, 22);

            var profile = Row(root, "Local profile"); TopLeft(profile, 36, 450, 312, 44);
            Button(profile, "Save look", SaveProfile); Button(profile, "Load look", LoadProfile);
            status = Label(root, "Your changes appear instantly.", 14, Muted); TopLeft(status.rectTransform, 42, 513, 300, 62);

            var panel = Box(root, "Customization panel", Background); Card(panel);
            panel.anchorMin = new Vector2(1, 0); panel.anchorMax = new Vector2(1, 1); panel.pivot = new Vector2(1, .5f);
            panel.offsetMin = new Vector2(-490, 32); panel.offsetMax = new Vector2(-32, -32);
            var heading = Label(panel, "02  /  MAKE IT YOURS", 13, Accent); heading.font = AnimpicStudioTheme.MediumFont; TopLeft(heading.rectTransform, 26, 25, 360, 20);
            var detailTitle = Label(panel, "Character workshop", 27, Cream); TopLeft(detailTitle.rectTransform, 26, 54, 402, 42);
            var tabs = Row(panel, "Categories"); TopStretch(tabs, 24, 24, 116, 44);
            string[] names = { "Appearance", "Outfit", "Colour" };
            for (int i = 0; i < names.Length; i++) { int selected = i; tabButtons.Add(Button(tabs, names[i], () => { tab = selected; UpdateTabs(); RefreshContent(); })); }
            var line = Box(panel, "Divider", AnimpicStudioTheme.Border); TopStretch(line, 24, 24, 181, 1);
            var scrollObject = new GameObject("Settings", typeof(RectTransform), typeof(ScrollRect)); scrollObject.transform.SetParent(panel, false);
            var scrollRect = (RectTransform)scrollObject.transform; Stretch(scrollRect, 0, 198, 0, 110);
            var viewport = Box(scrollRect, "Viewport", Color.clear); Stretch(viewport, 0, 0, 0, 0); viewport.gameObject.AddComponent<RectMask2D>(); viewport.GetComponent<Image>().raycastTarget = true;
            content = Box(viewport, "Content", Color.clear); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1); content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.padding = new RectOffset(24, 24, 4, 28); layout.spacing = 16; layout.childControlWidth = true; layout.childForceExpandWidth = true; layout.childControlHeight = true; layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = scrollObject.GetComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 35; scroll.movementType = ScrollRect.MovementType.Clamped;
            var scrollTrack = Box(scrollRect, "Scroll indicator", AnimpicStudioTheme.Border);
            scrollTrack.anchorMin = new Vector2(1, 0); scrollTrack.anchorMax = Vector2.one; scrollTrack.offsetMin = new Vector2(-9, 4); scrollTrack.offsetMax = new Vector2(-6, -4);
            var scrollbar = scrollTrack.gameObject.AddComponent<Scrollbar>();
            var thumb = Box(scrollTrack, "Scroll thumb", AnimpicStudioTheme.Muted); Stretch(thumb, 0, 0, 0, 0);
            var thumbSkin = thumb.GetComponent<StudioRoundedImage>(); thumbSkin.borderWidth = 0;
            scrollbar.handleRect = thumb; scrollbar.targetGraphic = thumb.GetComponent<Image>(); scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
            scroll.verticalScrollbar = scrollbar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            var footerLine = Box(panel, "Footer divider", AnimpicStudioTheme.Border); footerLine.anchorMin = new Vector2(0, 0); footerLine.anchorMax = new Vector2(1, 0); footerLine.offsetMin = new Vector2(24, 94); footerLine.offsetMax = new Vector2(-24, 95);
            var actions = Row(panel, "Actions"); actions.anchorMin = new Vector2(0, 0); actions.anchorMax = new Vector2(1, 0); actions.pivot = new Vector2(.5f, 0); actions.offsetMin = new Vector2(24, 26); actions.offsetMax = new Vector2(-24, 74);
            Button(actions, "Randomize", Randomize, true); Button(actions, "Reset look", ResetAppearance);

            var hints = Box(root, "Orbit hints", Background); hints.anchorMin = hints.anchorMax = Vector2.zero; hints.pivot = Vector2.zero; hints.anchoredPosition = new Vector2(36, 34); hints.sizeDelta = new Vector2(334, 44);
            var hintText = Label(hints, "DRAG TO ORBIT   /   SCROLL TO ZOOM", 12, Muted); Stretch(hintText.rectTransform, 16, 0, 16, 0); hintText.alignment = TextAnchor.MiddleCenter;
            var view = Row(root, "View controls"); view.anchorMin = view.anchorMax = new Vector2(.5f, 0); view.pivot = new Vector2(.5f, 0); view.anchoredPosition = new Vector2(0, 34); view.sizeDelta = new Vector2(286, 44);
            Button(view, "Reset view", () => { if (presentation) presentation.ResetView(); });
            Button rotate = null; rotate = Button(view, "Auto rotate", () => { if (presentation) { presentation.SetAutoRotate(!presentation.autoRotate); rotate.GetComponentInChildren<Text>().text = presentation.autoRotate ? "Stop rotation" : "Auto rotate"; } });
            if (EventSystem.current == null && GetComponentInChildren<EventSystem>(true) == null)
            {
                var events = new GameObject("Character Studio Events", typeof(EventSystem), typeof(StandaloneInputModule)); events.transform.SetParent(transform, false);
            }
            SetCharacter(initialMale); UpdateTabs();
        }
        private static void Card(RectTransform rect)
        {
            var panel = rect.GetComponent<StudioRoundedImage>(); if (panel) { panel.corners = new Vector4(24, 14, 25, 15); panel.SetVerticesDirty(); }
            if (panel) { panel.drawShadow = true; panel.shadowColor = new Color(0, 0, 0, .16f); panel.shadowOffset = new Vector2(0, -5); panel.SetVerticesDirty(); }
        }

        private void UpdateIdentity()
        {
            if (!current) return;
            if (identity) identity.text = "Fantasy " + current.Catalog.label;
            if (partSummary) partSummary.text = current.GetVisibleParts().Length + " active parts  /  CUSTOM LOOK";
            for (int i = 0; i < genderButtons.Count; i++) SetSelected(genderButtons[i], (i == 0 && current == female) || (i == 1 && current == male));
        }
        private void UpdateTabs() { for (int i = 0; i < tabButtons.Count; i++) SetSelected(tabButtons[i], i == tab); }
        private void OnCharacterChanged() { UpdateIdentity(); if (!applying) RefreshContent(); }
        private void RefreshContent()
        {
            if (!content || !current || !current.Catalog) return;
            for (int child = content.childCount - 1; child >= 0; child--) Remove(content.GetChild(child).gameObject);
            var s = current.Capture(); var c = current.Catalog;
            if (tab == 0)
            {
                Section("FACE & HAIR", "Give your character a distinctive silhouette.");
                Choice("Hairstyle", WithNone(c.hair.Select(o => o.label)), s.hair, v => Change(a => a.hair = v));
                Choice("Eyebrows", WithNone(c.brows.Select(o => o.label)), s.brows, v => Change(a => a.brows = v));
                if (c.beards.Length > 0) Choice("Facial hair", WithNone(c.beards.Select(o => o.label)), s.beard, v => Change(a => a.beard = v));
                if (c.extras.Length > 0)
                {
                    Section("ACCESSORIES", "Optional finishing touches.");
                    foreach (var slot in c.extras)
                    {
                        var extra = (s.extras ?? new SlotChoice[0]).FirstOrDefault(e => e.id == slot.id);
                        string id = slot.id;
                        Choice(slot.label, WithNone(slot.options.Select(o => o.label)), extra == null ? 0 : extra.option, v => Change(a => SetExtra(a, id, v)));
                    }
                }
                Section("PALETTE", "A starting palette for the whole character.");
                Swatches(s.basePalette, i => Change(a => a.basePalette = i));
            }
            else if (tab == 1)
            {
                Section("THE OUTFIT", "Footwear chooses the matching trouser length.");
                Choice("Torso", c.torsos.Select(o => o.label).ToArray(), s.torso, v => Change(a => a.torso = v));
                Choice("Trousers", c.pants.Select(o => o.label).ToArray(), s.pants, v => Change(a => a.pants = v));
                Choice("Footwear", WithNone(c.footwear.Select(o => o.label)), s.footwear, v => Change(a => a.footwear = v));
                foreach (bool left in new[] { true, false })
                {
                    Section(left ? "LEFT ARM" : "RIGHT ARM", "Gloves temporarily replace the forearm and hand.");
                    Choice("Construction", new[] { "One piece", "Two pieces" }, (int)(left ? s.leftArmMode : s.rightArmMode), v => Change(a => { if (left) a.leftArmMode = (ArmMode)v; else a.rightArmMode = (ArmMode)v; }));
                    var forearms = new[] { "Match torso" }.Concat(c.torsos.Select(o => o.label)).ToArray();
                    Choice("Forearm style", forearms, (left ? s.leftForearmStyle : s.rightForearmStyle) + 1, v => Change(a => { if (left) a.leftForearmStyle = v - 1; else a.rightForearmStyle = v - 1; }));
                    Choice("Glove", WithNone(c.gloves.Select(o => o.label)), left ? s.leftGlove : s.rightGlove, v => Change(a => { if (left) a.leftGlove = v; else a.rightGlove = v; }));
                }
            }
            else BuildColour(s);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }
        private void BuildColour(Appearance s)
        {
            Section("COLOUR YOUR CHARACTER", "Apply a palette to all elements, then refine any detail.");
            Swatches(s.basePalette, i => Change(a => a.basePalette = i));
            var parts = current.GetVisibleParts();
            if (parts.Length == 0) return;
            int index = Array.FindIndex(parts, p => p.id == paintedPart); if (index < 0) index = 0;
            paintedPart = parts[index].id;
            Section("INDIVIDUAL ELEMENT", "Choose a visible element to give it its own finish.");
            Choice("Element", parts.Select((p, i) => string.IsNullOrWhiteSpace(p.label) ? "Element " + (i + 1) : p.label).ToArray(), index,
                v => { paintedPart = parts[v].id; RefreshContent(); });
            var paint = (s.paints ?? new PartPaint[0]).FirstOrDefault(p => p.partId == paintedPart);
            int palette = paint == null ? -1 : paint.palette; Color tint = paint == null ? Color.white : paint.tint;
            Swatches(palette, i => Change(a => { var p = EditPaint(a, paintedPart); p.palette = i; }));
            var inherit = Item(38); Button(inherit, "Use character palette", () => Change(a => EditPaint(a, paintedPart).palette = -1));
            Section("TINT", "Mix the selected element's red, green and blue channels.");
            Channel("Red", tint.r, new Color32(192, 119, 114, 255), v => Tint(0, v));
            Channel("Green", tint.g, new Color32(127, 168, 134, 255), v => Tint(1, v));
            Channel("Blue", tint.b, new Color32(125, 151, 191, 255), v => Tint(2, v));
            var reset = Item(40); Button(reset, "Reset this element", () => Change(a => a.paints = (a.paints ?? new PartPaint[0]).Where(p => p.partId != paintedPart).ToArray()));
        }
        private void Tint(int channel, float value)
        {
            if (!current || string.IsNullOrEmpty(paintedPart)) return;
            var paint = (current.Capture().paints ?? new PartPaint[0]).FirstOrDefault(p => p.partId == paintedPart);
            float previous = paint == null ? 1f : paint.tint[channel];
            if (Mathf.Approximately(previous, value)) return;
            Change(a => { var p = EditPaint(a, paintedPart); var color = p.tint; color[channel] = value; color.a = 1; p.tint = color; }, false);
        }
        private void Change(Action<Appearance> edit, bool rebuild = true)
        {
            if (!current) return;
            var next = current.Capture(); edit(next); string error;
            applying = true;
            bool ok;
            try { ok = current.TryApply(next, out error); }
            finally { applying = false; }
            if (!ok) { Message(error); return; }
            UpdateIdentity(); if (rebuild) RefreshContent();
            Message("Look updated.");
        }
        private static PartPaint EditPaint(Appearance a, string id)
        {
            var list = new List<PartPaint>(a.paints ?? new PartPaint[0]); var paint = list.FirstOrDefault(p => p.partId == id);
            if (paint == null) { paint = new PartPaint { partId = id, palette = -1, tint = Color.white }; list.Add(paint); a.paints = list.ToArray(); }
            return paint;
        }
        private static void SetExtra(Appearance a, string id, int option)
        {
            var choices = new List<SlotChoice>(a.extras ?? new SlotChoice[0]); var choice = choices.FirstOrDefault(s => s.id == id);
            if (choice == null) { choice = new SlotChoice { id = id }; choices.Add(choice); }
            choice.option = option; a.extras = choices.ToArray();
        }
        private string SaveKey() { return "Animpic.CharacterStudio.Profile." + current.Catalog.id; }
        public void SaveProfile()
        {
            if (!current) return;
            try { PlayerPrefs.SetString(SaveKey(), current.SaveJson()); PlayerPrefs.Save(); Message("Saved on this device."); }
            catch (Exception e) { Message("Could not save: " + e.Message); }
        }
        public void LoadProfile()
        {
            if (!current) return;
            if (!PlayerPrefs.HasKey(SaveKey())) { Message("Save a look for this character first."); return; }
            string error;
            if (current.TryLoadJson(PlayerPrefs.GetString(SaveKey()), out error)) Message("Your saved look is ready."); else Message(error);
        }
        public void Randomize()
        {
            if (!current) return; string error;
            if (current.Randomize(unchecked(Environment.TickCount ^ Guid.NewGuid().GetHashCode()), out error)) Message("A new look to build on."); else Message(error);
        }
        public void ResetAppearance()
        {
            if (!current) return; var original = current == male ? maleDefault : femaleDefault;
            // Unity hot reload can restore an inline null Appearance as an empty object.
            if (original == null || original.catalogId != current.Catalog.id) { original = current.Capture(); if (current == male) maleDefault = original; else femaleDefault = original; }
            string error; if (current.TryApply(original, out error)) Message("Starting look restored."); else Message(error);
        }
        private void Message(string message) { if (status) status.text = message ?? ""; }
        private static string[] WithNone(IEnumerable<string> options) { return new[] { "None" }.Concat(options).ToArray(); }
        private void Section(string title, string description)
        {
            var item = Item(62); var heading = Label(item, title, 13, Accent); TopStretch(heading.rectTransform, 0, 0, 0, 22);
            var text = Label(item, description, 14, Muted); TopStretch(text.rectTransform, 0, 0, 28, 34); text.alignment = TextAnchor.UpperLeft;
        }
        private void Choice(string title, string[] options, int index, Action<int> changed)
        {
            if (options.Length == 0) return;
            index = Mathf.Clamp(index, 0, options.Length - 1);
            var item = Item(73); var label = Label(item, title, 15, Muted); TopStretch(label.rectTransform, 0, 0, 0, 20);
            var controls = Row(item, title + " controls"); TopStretch(controls, 0, 0, 27, 44);
            controls.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var previous = Button(controls, "‹", () => changed((index + options.Length - 1) % options.Length)); previous.GetComponent<LayoutElement>().flexibleWidth = 0; previous.GetComponent<LayoutElement>().minWidth = 44; previous.GetComponent<LayoutElement>().preferredWidth = 44; previous.GetComponentInChildren<Text>().fontSize = 20;
            var value = Box(controls, "Selected value", Surface); var layout = value.gameObject.AddComponent<LayoutElement>(); layout.minWidth = 0; layout.preferredWidth = 0; layout.flexibleWidth = 1;
            var valueText = Label(value, options[index], 18, Cream); Stretch(valueText.rectTransform, 8, 0, 8, 0); valueText.alignment = TextAnchor.MiddleCenter;
            var next = Button(controls, "›", () => changed((index + 1) % options.Length)); next.GetComponent<LayoutElement>().flexibleWidth = 0; next.GetComponent<LayoutElement>().minWidth = 44; next.GetComponent<LayoutElement>().preferredWidth = 44; next.GetComponentInChildren<Text>().fontSize = 20;
            previous.interactable = next.interactable = options.Length > 1;
        }
        private void Swatches(int selected, Action<int> changed)
        {
            var row = Row(Item(48), "Palette swatches"); Stretch(row, 0, 0, 0, 0); row.GetComponent<HorizontalLayoutGroup>().spacing = 8;
            int count = Mathf.Min(6, current.Catalog.palettes.Length);
            for (int i = 0; i < count; i++)
            {
                int p = i; var button = Button(row, ((char)('A' + i)).ToString(), () => changed(p)); button.image.color = PaletteColors[i];
                var swatch = button.image as StudioRoundedImage; if (swatch) { swatch.borderWidth = selected == i ? 2 : 0; swatch.borderColor = Accent; swatch.SetVerticesDirty(); }
            }
        }
        private void Channel(string name, float value, Color color, Action<float> changed)
        {
            var item = Item(58); var text = Label(item, name, 15, Muted); TopLeft(text.rectTransform, 0, 0, 120, 20);
            var number = Label(item, Mathf.RoundToInt(value * 255).ToString(), 14, Cream); TopStretch(number.rectTransform, 130, 0, 0, 20); number.alignment = TextAnchor.MiddleRight;
            var track = Box(item, name + " slider", Soft); TopStretch(track, 0, 0, 33, 7);
            var slider = track.gameObject.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1;
            var fill = Box(track, "Fill", color); Stretch(fill, 0, 0, 0, 0); slider.fillRect = fill;
            var handleArea = Box(track, "Handle area", Color.clear); Stretch(handleArea, 0, -6, 0, -6);
            var handle = Box(handleArea, "Handle", Cream); handle.sizeDelta = new Vector2(15, 19); slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>(); slider.direction = Slider.Direction.LeftToRight;
            slider.SetValueWithoutNotify(value);
            // Editor layout rebuilds can invoke onValueChanged without user input.
            float lastValue = value;
            slider.onValueChanged.AddListener(v => {
                if (!slider || !slider.isActiveAndEnabled || Mathf.Approximately(v, lastValue)) return;
                lastValue = v;
                number.text = Mathf.RoundToInt(v * 255).ToString(); changed(v);
            });
        }
        private RectTransform Item(float height)
        {
            var item = Box(content, "Setting", Color.clear); var layout = item.gameObject.AddComponent<LayoutElement>(); layout.preferredHeight = height; layout.flexibleHeight = 0; return item;
        }
        private RectTransform Row(Transform parent, string name)
        {
            var row = Box(parent, name, Color.clear); var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 8; layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = true; return row;
        }
        private Button Button(Transform parent, string text, Action click, bool accent = false)
        {
            var rect = Box(parent, text, accent ? Accent : Surface); var button = rect.gameObject.AddComponent<Button>(); if (parent.GetComponent<LayoutGroup>() == null) Stretch(rect, 0, 0, 0, 0);
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1.16f, 1.14f, 1.12f); colors.pressedColor = new Color(.82f, .82f, .82f); colors.selectedColor = Color.white; colors.disabledColor = new Color(.55f, .55f, .55f); button.colors = colors;
            button.targetGraphic = rect.GetComponent<Image>(); button.navigation = new Navigation { mode = Navigation.Mode.None };
            var element = rect.gameObject.AddComponent<LayoutElement>(); element.flexibleWidth = 1; element.minHeight = 36;
            var label = Label(rect, text, 16, Color.white); Stretch(label.rectTransform, 5, 0, 5, 0); label.alignment = TextAnchor.MiddleCenter;
            label.font = AnimpicStudioTheme.MediumFont;
            var skin = rect.GetComponent<StudioRoundedImage>(); if (skin && accent) { skin.accentGradient = true; skin.color = Color.white; skin.borderWidth = 0; skin.SetVerticesDirty(); }
            button.onClick.AddListener(() => click()); return button;
        }
        private static void SetSelected(Button button, bool selected)
        { var skin = button.image as StudioRoundedImage; if (skin) { skin.accentGradient = selected; skin.borderColor = selected ? Accent : AnimpicStudioTheme.Border; skin.color = selected ? Color.white : Surface; skin.SetVerticesDirty(); } else button.image.color = selected ? Accent : Surface; button.GetComponentInChildren<Text>().color = Color.white; }
        private Text Label(Transform parent, string text, int size, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false); var label = go.GetComponent<Text>();
            label.font = size >= 24 ? AnimpicStudioTheme.HeadingFont : font; label.text = text; label.fontSize = size; label.color = color; label.alignment = TextAnchor.MiddleLeft; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate; return label;
        }
        private static RectTransform Box(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), color.a > 0 ? typeof(StudioRoundedImage) : typeof(Image)); go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = color.a > 0;
            var skin = image as StudioRoundedImage;
            if (skin && (name.IndexOf("divider", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("line", StringComparison.OrdinalIgnoreCase) >= 0 || name == "Fill" || name == "Handle")) { skin.borderWidth = 0; skin.corners = Vector4.one * 6; }
            return (RectTransform)go.transform;
        }
        private static void TopLeft(RectTransform rect, float x, float y, float width, float height)
        { rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); }
        private static void TopStretch(RectTransform rect, float left, float right, float top, float height)
        { rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(.5f, 1); rect.offsetMin = new Vector2(left, -top - height); rect.offsetMax = new Vector2(-right, -top); }
        private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top); }
        private static void Remove(GameObject go) { go.SetActive(false); if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
    }
}
