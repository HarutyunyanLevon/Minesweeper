using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using MinefieldClerk.Day;

namespace MinefieldClerk.EditorTools
{
    /// <summary>
    /// Creates the UI window prefabs (briefing book, debrief) in Assets/Resources/UI/Windows. Run it once, then edit the prefabs by hand
    /// (open them in Prefab Mode). Running it again rebuilds them from scratch and overwrites your edits.
    /// Menu: Minefield/UI/Build Windows
    /// </summary>
    public static class UiWindowBuilder
    {
        private const string Folder = "Assets/Resources/UI/Windows";
        private const string DebriefPrefab = "DebriefWindow", RulebookPrefab = "RulebookWindow";

        // ---- Shared look
        private static readonly Color Ink = new Color(0.32f, 0.18f, 0.09f), ReportInk = new Color(0.30f, 0.07f, 0.05f);
        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.7f), Paper = new Color(0.96f, 0.94f, 0.86f);
        private static readonly Color Tick = new Color(0.15f, 0.45f, 0.18f), BodyInk = new Color(0.14f, 0.10f, 0.08f);
        private static readonly Color DarkRed = new Color(0.50f, 0.06f, 0.05f), StampRed = new Color(0.72f, 0.10f, 0.08f);
        private static readonly Color Cream = new Color(0.93f, 0.87f, 0.70f), Gold = new Color(0.95f, 0.76f, 0.22f);
        private const float Half = 0.5f;
        private const float DebriefW = 1024f, DebriefH = 576f, BookW = 1024f, BookH = 660f;

        // ---- Debrief: report page (canvas pixels)
        private const float TitleY = 60f, TitleH = 70f, RowsX = 100f, RowsW = 824f, FirstLine = 207f, RowPitch = 33.7f, RowCount = 8f;
        private const float NextX = 740f, NextY = 462f, NextW = 188f, NextH = 48f;
        private const int TitleSize = 40, RowSize = 22, SmallSize = 18;

        // ---- Debrief: family page
        private const float LocketX = 66f, LocketY = 130f, LocketW = 320f, LocketAspect = 311f / 420f;
        private const float NoteLeft = 0.555f, NoteTop = 0.27f, NoteW = 0.30f, NoteH = 0.40f, NoteTilt = -9f;
        private const int NoteSize = 21;
        private const float PadX = 400f, PadY = 110f, PadW = 560f, PadH = 346f;
        private const float HeaderY = 46f, HeaderH = 34f, InHandY = 76f, InHandH = 24f;
        private const int HeaderSize = 26, InHandSize = 17, ExpenseSize = 22;
        private const float ListX = 68f, ListY = 98.5f, LineH = 39.5f, PriceW = 64f, BoxSize = 24f, BoxInset = 3f, CheckInset = 7f, LabelX = 100f, ListRows = 4f;
        private const float FamilyNextW = 220f, FamilyNextH = 34f, FamilyNextY = 282f, ButtonBorder = 2f;
        private static readonly Vector2 NoteShadow = new Vector2(1.5f, -1.5f);

        // ---- Rulebook (art pixels of the 1024 x 580 spread)
        private const float SpreadH = 580f, LeftCenterX = 262f, PageW = 356f, PosterW = 330f, PosterH = 236f, PosterTop = 40f, PosterBorder = 3f;
        private const float RibbonW = 392f, RibbonAspect = 133f / 720f, RibbonOverlap = 34f, TextGap = 10f, TextBottom = 468f, TextSide = 16f;
        private const float RibbonL = 0.15f, RibbonT = 0.10f, RibbonR = 0.85f, RibbonB = 0.72f, StampW = 96f, StampMargin = 2f, StampTilt = -14f;
        private const float PlainTitleY = 70f, PlainTitleH = 40f, PlainTextY = 120f, NumberY = 470f, NumberH = 18f;
        private const float StarY = 110f, StarH = 100f, SloganY = 232f, SloganH = 60f, RuleY = 310f, RuleH = 2f, RuleInset = 50f, MinistryY = 330f, MinistryH = 24f, NoteY = 362f;
        private const float NavGap = 8f, NavH = 44f, CloseH = 34f, FooterH = 22f, NavLeft = 0.25f, NavRight = 0.75f;
        private const int BookTitleSize = 24, BookBodyMin = 8, BookBodyMax = 30, NumberSize = 10, SloganSize = 20, FooterSize = 11, StampSize = 14, PlainTitleSize = 22;
        private const int BareSloganSize = 20, MinistrySize = 11, BareNoteMax = 14;
        private const string MinistryText = "MINISTRY OF LAND RECLAMATION";
        private const string BareNote = "New directives are posted each morning. Read them before you take the field.";
        private const string SampleBody = "The text of the directive appears here and shrinks to fit the page.";

        [MenuItem("Minefield/UI/Build Windows (overwrites prefabs)")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(Folder);
            BuildDebrief();
            BuildRulebook();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UI] Windows built in " + Folder);
        }

        [MenuItem("Minefield/UI/Delete Obsolete IMGUI Scripts")]
        public static void DeleteObsolete()
        {
            string[] files = { "Assets/Scripts/Day/RulePaper.cs", "Assets/Scripts/Day/DebriefScreen.cs", "Assets/Scripts/Day/DebriefLayout.cs",
                               "Assets/Resources/Debrief/layout.json", "Assets/Resources/Debrief/notepad.png", "Assets/Resources/Rulebook/poster_holder.png" };
            foreach (string f in files) if (AssetDatabase.DeleteAsset(f)) Debug.Log("[UI] Deleted " + f);
        }

        // ================= Debrief =================

        private static void BuildDebrief()
        {
            GameObject root = Node("DebriefWindow", null);
            Stretch(Rt(root));
            Image dim = Pic(Box("Dim", root.transform, 0, 0, 0, 0), null, DimColor);
            Stretch(dim.rectTransform);
            RectTransform frame = Center("Frame", root.transform, DebriefW, DebriefH);

            // ---- Page 1: state report
            RectTransform report = Box("ReportPage", frame, 0, 0, DebriefW, DebriefH);
            Pic(Box("Background", report, 0, 0, DebriefW, DebriefH), Spr("Debrief/report_form", FilterMode.Point), Color.white);
            Text title = Label(Box("Title", report, 0, TitleY, DebriefW, TitleH), "STATE REPORT - DAY 1", TitleSize, ReportInk, TextAnchor.MiddleCenter);
            RectTransform rows = Box("Rows", report, RowsX, FirstLine - RowPitch, RowsW, RowPitch * RowCount);
            var group = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childAlignment = TextAnchor.UpperLeft;
            group.childControlWidth = group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            RectTransform rowT = Box("RowTemplate", rows, 0, 0, RowsW, RowPitch);
            rowT.gameObject.AddComponent<LayoutElement>().preferredHeight = RowPitch;
            var rowView = rowT.gameObject.AddComponent<ReportRowView>();
            Text rowLabel = Label(Fill("Label", rowT), "Mines located  40 x 10", RowSize, ReportInk, TextAnchor.LowerLeft);
            Text rowValue = Label(Fill("Value", rowT), "+400", RowSize, ReportInk, TextAnchor.LowerRight);
            Set(rowView, "label", rowLabel); Set(rowView, "value", rowValue);
            Text nextLabel;
            Button reportNext = MakeButton("NextButton", report, NextX, NextY, NextW, NextH, null, new Color(0, 0, 0, 0), "Next (Enter)", SmallSize, ReportInk, out nextLabel);

            // ---- Page 2: family
            RectTransform family = Box("FamilyPage", frame, 0, 0, DebriefW, DebriefH);
            Pic(Box("Background", family, 0, 0, DebriefW, DebriefH), Spr("Debrief/family_table", FilterMode.Point), Color.white);
            float lh = LocketW * LocketAspect;
            Pic(Box("Locket", family, LocketX, LocketY, LocketW, lh), Spr("Debrief/locket_open", FilterMode.Point), Color.white);
            RectTransform note = Box("Note", family, LocketX + LocketW * NoteLeft, LocketY + lh * NoteTop, LocketW * NoteW, lh * NoteH);
            note.localRotation = Quaternion.Euler(0f, 0f, NoteTilt);
            Text noteText = Label(note, "Come\nhome", NoteSize, new Color(0.17f, 0.23f, 0.40f), TextAnchor.MiddleCenter);
            note.gameObject.AddComponent<OsFont>();
            var shadow = note.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
            shadow.effectDistance = NoteShadow;

            Pic(Box("Notebook", family, PadX, PadY, PadW, PadH), Spr("Debrief/notebook", FilterMode.Point), Color.white);
            Text header = Label(Box("Header", family, PadX, PadY + HeaderY, PadW, HeaderH), "For the family", HeaderSize, Ink, TextAnchor.MiddleCenter);
            header.gameObject.AddComponent<OsFont>();
            Text inHand = Label(Box("InHand", family, PadX, PadY + InHandY, PadW, InHandH), "In hand: 0", InHandSize, Ink, TextAnchor.MiddleCenter);
            inHand.gameObject.AddComponent<OsFont>();

            RectTransform list = Box("Expenses", family, PadX + ListX, PadY + ListY, PadW - ListX - ListX * Half, LineH * ListRows);
            var vg = list.gameObject.AddComponent<VerticalLayoutGroup>();
            vg.childAlignment = TextAnchor.UpperLeft;
            vg.childControlWidth = vg.childControlHeight = true;
            vg.childForceExpandWidth = true;
            vg.childForceExpandHeight = false;
            float rowW = list.sizeDelta.x;
            RectTransform exT = Box("ExpenseTemplate", list, 0, 0, rowW, LineH);
            exT.gameObject.AddComponent<LayoutElement>().preferredHeight = LineH;
            var exView = exT.gameObject.AddComponent<ExpenseRowView>();
            Text price = Label(Box("Price", exT, 0, 0, PriceW, LineH), "40", ExpenseSize, Ink, TextAnchor.LowerLeft);
            price.gameObject.AddComponent<OsFont>();
            RectTransform boxRt = Box("Checkbox", exT, PriceW, LineH - BoxSize - BoxInset, BoxSize, BoxSize);
            Image boxBg = Pic(boxRt, null, Ink);
            Image inner = Pic(Fill("Inner", boxRt, BoxInset), null, Paper);
            Image check = Pic(Fill("Check", boxRt, CheckInset), null, Tick);
            var toggle = boxRt.gameObject.AddComponent<Toggle>();
            toggle.transition = Selectable.Transition.None;
            toggle.targetGraphic = boxBg;
            toggle.graphic = check;
            toggle.isOn = true;
            Navigation none = new Navigation { mode = Navigation.Mode.None };
            toggle.navigation = none;
            Text exLabel = Label(Box("Label", exT, LabelX, 0, rowW - LabelX, LineH), "Pay the bills", ExpenseSize, Ink, TextAnchor.LowerLeft);
            exLabel.gameObject.AddComponent<OsFont>();
            Set(exView, "price", price); Set(exView, "label", exLabel); Set(exView, "box", toggle);

            Text famLabel;
            Button famNext = MakeButton("NextDayButton", family, PadX + (PadW - FamilyNextW) * Half, PadY + FamilyNextY, FamilyNextW, FamilyNextH, null, Ink, "Next day (Enter)", SmallSize + 2, Ink, out famLabel);
            // a paper inlay so the button reads as an outlined stamp
            Image inlay = Pic(Fill("Inlay", famNext.transform, ButtonBorder), null, Paper);
            inlay.transform.SetSiblingIndex(0);
            inlay.raycastTarget = false;
            famLabel.transform.SetAsLastSibling();

            var win = root.AddComponent<DebriefWindow>();
            Set(win, "reportPage", report.gameObject); Set(win, "titleText", title); Set(win, "rowsRoot", rows); Set(win, "rowTemplate", rowView);
            Set(win, "reportNext", reportNext); Set(win, "reportNextLabel", nextLabel);
            Set(win, "familyPage", family.gameObject); Set(win, "inHandText", inHand); Set(win, "expenseRoot", list); Set(win, "expenseTemplate", exView);
            Set(win, "familyNext", famNext);
            family.gameObject.SetActive(false);   // pages overlap in the prefab: switch it on in the Inspector to edit it
            Save(root, DebriefPrefab);
        }

        // ================= Rulebook =================

        private static void BuildRulebook()
        {
            GameObject root = Node("RulebookWindow", null);
            Stretch(Rt(root));
            Image dim = Pic(Box("Dim", root.transform, 0, 0, 0, 0), null, DimColor);
            Stretch(dim.rectTransform);
            RectTransform book = Center("Book", root.transform, BookW, BookH);
            Pic(Box("Spread", book, 0, 0, BookW, SpreadH), Spr("Rulebook/book_spread", FilterMode.Bilinear), Color.white);

            BookPageView left = BuildPage("LeftPage", book, LeftCenterX);
            BookPageView right = BuildPage("RightPage", book, BookW - LeftCenterX);

            Sprite prevSp = Spr("UI/btn_prev_normal", FilterMode.Bilinear), prevPr = Spr("UI/btn_prev_pressed", FilterMode.Bilinear);
            Sprite nextSp = Spr("UI/btn_next_normal", FilterMode.Bilinear), nextPr = Spr("UI/btn_next_pressed", FilterMode.Bilinear);
            Sprite closeSp = Spr("UI/btn_close", FilterMode.Bilinear);
            float rowTop = SpreadH + NavGap;
            float navW = NavH * Aspect(prevSp);
            Button prev = IconButton("PrevButton", book, BookW * NavLeft - navW * Half, rowTop, navW, NavH, prevSp, prevPr);
            Button next = IconButton("NextButton", book, BookW * NavRight - navW * Half, rowTop, navW, NavH, nextSp, nextPr);
            Text slogan = Label(Box("Slogan", book, 0, rowTop, BookW, NavH), "THE REPUBLIC IS COUNTING ON YOU", SloganSize, Gold, TextAnchor.MiddleCenter);
            slogan.raycastTarget = false;
            float closeW = CloseH * Aspect(closeSp);
            Button close = IconButton("CloseButton", book, BookW - closeW, rowTop + (NavH - CloseH) * Half, closeW, CloseH, closeSp, closeSp);
            Text footer = Label(Box("Footer", book, 0, rowTop + NavH, BookW, FooterH), "A / D: turn the page      Enter or B: begin the shift", FooterSize, Cream, TextAnchor.MiddleCenter);

            var win = root.AddComponent<RulebookWindow>();
            Set(win, "leftPage", left); Set(win, "rightPage", right);
            Set(win, "prev", prev); Set(win, "next", next); Set(win, "close", close);
            Set(win, "slogan", slogan); Set(win, "footer", footer);
            Save(root, RulebookPrefab);
        }

        private static BookPageView BuildPage(string name, Transform book, float centerX)
        {
            RectTransform page = Box(name, book, centerX - PageW * Half, 0, PageW, SpreadH);
            var view = page.gameObject.AddComponent<BookPageView>();

            // rule with a poster: framed poster, a ribbon with the title, the stamp, then the text
            RectTransform pg = Box("PosterGroup", page, 0, 0, PageW, SpreadH);
            float px = (PageW - PosterW) * Half;
            Pic(Box("PosterFrame", pg, px - PosterBorder, PosterTop - PosterBorder, PosterW + PosterBorder * 2f, PosterH + PosterBorder * 2f), null, BodyInk);
            RectTransform posterRt = Box("Poster", pg, px, PosterTop, PosterW, PosterH);
            var raw = posterRt.gameObject.AddComponent<RawImage>();
            raw.texture = Resources.Load<Texture2D>("Rulebook/Posters/poster_land");
            raw.raycastTarget = false;
            Sprite stampSp = Spr("UI/stamp_blank", FilterMode.Bilinear);
            float stampH = StampW / Aspect(stampSp);
            RectTransform stamp = Box("Stamp", pg, px + PosterW - StampW - StampMargin, PosterTop + StampMargin, StampW, stampH);
            Pic(stamp, stampSp, Color.white).raycastTarget = false;
            stamp.localRotation = Quaternion.Euler(0f, 0f, StampTilt);
            Text stampText = Label(Fill("StampText", stamp), "AMENDED", StampSize, StampRed, TextAnchor.MiddleCenter);
            float ribbonH = RibbonW * RibbonAspect;
            RectTransform ribbon = Box("Ribbon", pg, (PageW - RibbonW) * Half, PosterTop + PosterH - RibbonOverlap, RibbonW, ribbonH);
            Pic(ribbon, Spr("Rulebook/poster_ribbon", FilterMode.Bilinear), Color.white).raycastTarget = false;
            RectTransform band = Box("RibbonTitle", ribbon, 0, 0, 0, 0);
            Frac(band, RibbonL, RibbonT, RibbonR, RibbonB);
            Text ribbonTitle = Label(band, "DIRECTIVE TITLE", BookTitleSize, Cream, TextAnchor.MiddleCenter);
            BestFit(ribbonTitle, 6, BookTitleSize);
            float bodyY = PosterTop + PosterH - RibbonOverlap + ribbonH + TextGap;
            Text bodyPoster = Label(Box("BodyWithPoster", page, TextSide, bodyY, PageW - TextSide * 2f, TextBottom - bodyY), SampleBody, BookBodyMax, BodyInk, TextAnchor.UpperLeft);
            BestFit(bodyPoster, BookBodyMin, BookBodyMax);

            // rule without a poster
            Text plainTitle = Label(Box("PlainTitle", page, 0, PlainTitleY, PageW, PlainTitleH), "DIRECTIVE TITLE", PlainTitleSize, DarkRed, TextAnchor.MiddleCenter);
            Text bodyPlain = Label(Box("BodyPlain", page, TextSide, PlainTextY, PageW - TextSide * 2f, TextBottom - PlainTextY), SampleBody, BookBodyMax, BodyInk, TextAnchor.UpperLeft);
            BestFit(bodyPlain, BookBodyMin, BookBodyMax);

            // page with no rule: star, slogan, line, ministry, note
            RectTransform bare = Box("BareGroup", page, 0, 0, PageW, SpreadH);
            Sprite star = Spr("UI/icon_star", FilterMode.Bilinear);
            float starW = StarH * Aspect(star);
            Pic(Box("Star", bare, (PageW - starW) * Half, StarY, starW, StarH), star, Color.white).raycastTarget = false;
            Text bareSlogan = Label(Box("Slogan", bare, TextSide, SloganY, PageW - TextSide * 2f, SloganH), "SLOGAN", BareSloganSize, DarkRed, TextAnchor.MiddleCenter);
            BestFit(bareSlogan, BookBodyMin, BareSloganSize);
            Pic(Box("Rule", bare, RuleInset, RuleY, PageW - RuleInset * 2f, RuleH), null, DarkRed);
            Label(Box("Ministry", bare, 0, MinistryY, PageW, MinistryH), MinistryText, MinistrySize, DarkRed, TextAnchor.MiddleCenter);
            Text bareNote = Label(Box("Note", bare, TextSide, NoteY, PageW - TextSide * 2f, TextBottom - NoteY), BareNote, BareNoteMax, BodyInk, TextAnchor.UpperLeft);
            BestFit(bareNote, BookBodyMin, BareNoteMax);

            Text number = Label(Box("Number", page, 0, NumberY, PageW, NumberH), "- 1 -", NumberSize, BodyInk, TextAnchor.MiddleCenter);

            Set(view, "posterGroup", pg.gameObject); Set(view, "poster", raw); Set(view, "stamp", stamp.gameObject); Set(view, "stampText", stampText);
            Set(view, "ribbonTitle", ribbonTitle); Set(view, "bodyWithPoster", bodyPoster);
            Set(view, "plainTitle", plainTitle); Set(view, "bodyPlain", bodyPlain);
            Set(view, "bareGroup", bare.gameObject); Set(view, "bareSlogan", bareSlogan); Set(view, "number", number);
            bare.gameObject.SetActive(false);        // overlapping layouts in the prefab: toggle these in the Inspector to edit them
            plainTitle.gameObject.SetActive(false);
            bodyPlain.gameObject.SetActive(false);
            return view;
        }

        // ================= Helpers =================

        private static GameObject Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform Rt(GameObject go) { return (RectTransform)go.transform; }

        /// <summary>A child placed by its top-left corner (x right, y down) inside the parent's top-left corner.</summary>
        private static RectTransform Box(string name, Transform parent, float x, float y, float w, float h)
        {
            RectTransform rt = Rt(Node(name, parent));
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        private static RectTransform Center(string name, Transform parent, float w, float h)
        {
            RectTransform rt = Rt(Node(name, parent));
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(Half, Half);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        private static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static RectTransform Fill(string name, Transform parent, float inset = 0f)
        {
            RectTransform rt = Rt(Node(name, parent));
            Stretch(rt, inset);
            return rt;
        }

        /// <summary>Anchors a rect to a part of its parent given as fractions from the top-left.</summary>
        private static void Frac(RectTransform rt, float left, float top, float right, float bottom)
        {
            rt.anchorMin = new Vector2(left, 1f - bottom);
            rt.anchorMax = new Vector2(right, 1f - top);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static Image Pic(RectTransform rt, Sprite sprite, Color color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static Text Label(RectTransform rt, string text, int size, Color color, TextAnchor anchor)
        {
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.raycastTarget = false;
            return t;
        }

        private static void BestFit(Text t, int min, int max)
        {
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = min;
            t.resizeTextMaxSize = max;
        }

        private static Button MakeButton(string name, Transform parent, float x, float y, float w, float h, Sprite sprite, Color color, string text, int size, Color textColor, out Text label)
        {
            RectTransform rt = Box(name, parent, x, y, w, h);
            Image img = Pic(rt, sprite, color);
            img.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            label = Label(Fill("Label", rt), text, size, textColor, TextAnchor.MiddleCenter);
            return button;
        }

        private static Button IconButton(string name, Transform parent, float x, float y, float w, float h, Sprite normal, Sprite pressed)
        {
            RectTransform rt = Box(name, parent, x, y, w, h);
            Image img = Pic(rt, normal, Color.white);
            img.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { pressedSprite = pressed, highlightedSprite = normal, selectedSprite = normal };
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        private static float Aspect(Sprite s) { return s.rect.width / s.rect.height; }

        /// <summary>Makes sure the png in Resources is imported as a Sprite and returns it.</summary>
        private static Sprite Spr(string resourcePath, FilterMode filter)
        {
            string path = "Assets/Resources/" + resourcePath + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogError("[UI] Missing texture " + path); return null; }
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = filter;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path)) if (o is Sprite sp) { sprite = sp; break; }
            if (sprite == null) Debug.LogError("[UI] No sprite at " + path);
            return sprite;
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError("[UI] No field " + field + " on " + target.GetType().Name); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Save(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "/" + name + ".prefab");
            Object.DestroyImmediate(root);
        }
    }
}
