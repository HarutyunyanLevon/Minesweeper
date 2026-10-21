using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using MinefieldClerk.Day;

namespace MinefieldClerk.EditorTools
{
    /// <summary>
    /// Creates every UI prefab: windows (briefing book, debrief report, expenses) in Assets/Resources/UI/Windows, HUD and work bar in Assets/Resources/UI/Hud. Run it once, then edit the prefabs by hand
    /// (open them in Prefab Mode). Running it again rebuilds them from scratch and overwrites your edits.
    /// Menu: Minefield/UI/Build Windows
    /// </summary>
    public static class UiWindowBuilder
    {
        private const string WindowsFolder = "Assets/Resources/UI/Windows", HudFolder = "Assets/Resources/UI/Hud";
        private const string DebriefPrefab = "DebriefWindow", ExpensesPrefab = "ExpensesWindow", RulebookPrefab = "RulebookWindow", HudPrefab = "HudWindow", WorkBarPrefab = "WorkBar";

        // ---- Shared look
        private static readonly Color Ink = new Color(0.32f, 0.18f, 0.09f), ReportInk = new Color(0.30f, 0.07f, 0.05f);
        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.7f), Paper = new Color(0.96f, 0.94f, 0.86f);
        private static readonly Color Tick = new Color(0.15f, 0.45f, 0.18f), BodyInk = new Color(0.14f, 0.10f, 0.08f);
        private static readonly Color DarkRed = new Color(0.50f, 0.06f, 0.05f), StampRed = new Color(0.72f, 0.10f, 0.08f);
        private static readonly Color Cream = new Color(0.93f, 0.87f, 0.70f), Gold = new Color(0.95f, 0.76f, 0.22f);
        private const float Half = 0.5f;
        // ---- HUD (designed for the 1920 x 1080 HUD canvas)
        private const float HudMargin = 18f, GapY = 6f, PlateW = 250f, PlateAspect = 368f / 86f, PlateBorderScale = 0.68f;
        private const float PlateIconH = 30f, PlateIconX = 40f, PlateTextX = 82f, PlateTextRight = 26f;
        private const float BarH = 40f, BarFrameArtH = 46f, BarFrameLeft = 16f, BarFrameRight = 21f, BarFrameTop = 12f, BarIconW = 32f, BarIconH = 38f;
        private const float HintH = 22f, SlotH = 66f, SlotGap = 40f, SlotBottom = 52f, CaptionH = 20f, CaptionGap = 2f;
        private const float CounterLeft = 0.70f, CounterTop = 0.66f, CounterRight = 0.96f, CounterBottom = 0.94f;   // the dark square on a slot
        private const int FontPlate = 19, FontHint = 15, FontCaption = 14, FontCounter = 14, FontBar = 15, ToastFont = 26;
        private const float ToastW = 960f, ToastH = 44f, ToastBottom = 40f;
        private static readonly Color Light = new Color(1f, 0.94f, 0.78f), Warn = new Color(0.95f, 0.35f, 0.25f), BarFull = new Color(0.45f, 0.78f, 0.2f);
        // ---- Work bar
        private const float WorkW = 140f, WorkH = 14f, WorkInset = 2f, WorkLabelH = 24f, WorkLabelGap = 4f;
        private const int WorkFont = 18;
        private static readonly Color WorkBack = new Color(0f, 0f, 0f, 0.7f), WorkFill = new Color(0.95f, 0.8f, 0.3f);
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

        [MenuItem("Minefield/UI/Build Prefabs (overwrites edits)")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(WindowsFolder);
            Directory.CreateDirectory(HudFolder);
            BuildDebrief();
            BuildExpenses();
            BuildRulebook();
            BuildHud();
            BuildWorkBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UI] Prefabs built in " + WindowsFolder + " and " + HudFolder);
        }

        // ================= Debrief report =================

        private static void BuildDebrief()
        {
            GameObject root = Node("DebriefWindow", null);
            Stretch(Rt(root));
            Pic(Fill("Dim", root.transform), null, DimColor);
            RectTransform frame = Center("Frame", root.transform, DebriefW, DebriefH);

            Pic(Box("Background", frame, 0, 0, DebriefW, DebriefH), Spr("Debrief/report_form", FilterMode.Point), Color.white);
            Text title = Label(Box("Title", frame, 0, TitleY, DebriefW, TitleH), "STATE REPORT - DAY 1", TitleSize, ReportInk, TextAnchor.MiddleCenter);
            RectTransform rows = Box("Rows", frame, RowsX, FirstLine - RowPitch, RowsW, RowPitch * RowCount);
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
            Button next = MakeButton("NextButton", frame, NextX, NextY, NextW, NextH, null, new Color(0, 0, 0, 0), "Next (Enter)", SmallSize, ReportInk, out nextLabel);

            var win = root.AddComponent<DebriefWindow>();
            Set(win, "titleText", title); Set(win, "rowsRoot", rows); Set(win, "rowTemplate", rowView);
            Set(win, "next", next); Set(win, "nextLabel", nextLabel);
            Save(root, WindowsFolder, DebriefPrefab);
        }

        // ================= Expenses (family page) =================

        private static void BuildExpenses()
        {
            GameObject root = Node("ExpensesWindow", null);
            Stretch(Rt(root));
            Pic(Fill("Dim", root.transform), null, DimColor);
            RectTransform family = Center("Frame", root.transform, DebriefW, DebriefH);

            Pic(Box("Background", family, 0, 0, DebriefW, DebriefH), Spr("Debrief/family_table", FilterMode.Point), Color.white);
            float lh = LocketW * LocketAspect;
            Pic(Box("Locket", family, LocketX, LocketY, LocketW, lh), Spr("Debrief/locket_open", FilterMode.Point), Color.white);
            RectTransform note = Box("Note", family, LocketX + LocketW * NoteLeft, LocketY + lh * NoteTop, LocketW * NoteW, lh * NoteH);
            note.localRotation = Quaternion.Euler(0f, 0f, NoteTilt);
            Label(note, "Come\nhome", NoteSize, new Color(0.17f, 0.23f, 0.40f), TextAnchor.MiddleCenter);
            note.gameObject.AddComponent<OsFont>();
            var shadow = note.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
            shadow.effectDistance = NoteShadow;

            Pic(Box("Notebook", family, PadX, PadY, PadW, PadH), Spr("Debrief/notebook", FilterMode.Point), Color.white);
            Text header = Label(Box("Header", family, PadX, PadY + HeaderY, PadW, HeaderH), "For the family", HeaderSize, Ink, TextAnchor.MiddleCenter);
            header.gameObject.AddComponent<OsFont>();
            Text inHand = Label(Box("InHand", family, PadX, PadY + InHandY, PadW, InHandH), "In hand: 0", InHandSize, Ink, TextAnchor.MiddleCenter);
            inHand.gameObject.AddComponent<OsFont>();

            RectTransform list = Box("Expenses", family, PadX + ListX, PadY + ListY, PadW - ListX * 2f, LineH * ListRows);
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
            Pic(Fill("Inner", boxRt, BoxInset), null, Paper);
            Image check = Pic(Fill("Check", boxRt, CheckInset), null, Tick);
            var toggle = boxRt.gameObject.AddComponent<Toggle>();
            toggle.transition = Selectable.Transition.None;
            toggle.targetGraphic = boxBg;
            toggle.graphic = check;
            toggle.isOn = true;
            toggle.navigation = new Navigation { mode = Navigation.Mode.None };
            Text exLabel = Label(Box("Label", exT, LabelX, 0, rowW - LabelX, LineH), "Pay the bills", ExpenseSize, Ink, TextAnchor.LowerLeft);
            exLabel.gameObject.AddComponent<OsFont>();
            Set(exView, "price", price); Set(exView, "label", exLabel); Set(exView, "box", toggle);

            Text famLabel;
            Button famNext = MakeButton("NextDayButton", family, PadX + (PadW - FamilyNextW) * Half, PadY + FamilyNextY, FamilyNextW, FamilyNextH, null, Ink, "Next day (Enter)", SmallSize + 2, Ink, out famLabel);
            Image inlay = Pic(Fill("Inlay", famNext.transform, ButtonBorder), null, Paper);   // a paper inlay so the button reads as an outlined stamp
            inlay.transform.SetSiblingIndex(0);
            famLabel.transform.SetAsLastSibling();

            var win = root.AddComponent<ExpensesWindow>();
            Set(win, "inHandText", inHand); Set(win, "expenseRoot", list); Set(win, "expenseTemplate", exView); Set(win, "next", famNext);
            Save(root, WindowsFolder, ExpensesPrefab);
        }

        // ================= HUD =================

        private static void BuildHud()
        {
            GameObject root = Node("HudWindow", null);
            Stretch(Rt(root));
            Image dusk = Pic(Fill("Dusk", root.transform), null, new Color(0.12f, 0.05f, 0.2f, 0f));
            RectTransform content = Fill("Content", root.transform);

            // top-right column: plates, battery, hints
            RectTransform col = Node("Column", content).GetComponent<RectTransform>();
            col.anchorMin = col.anchorMax = col.pivot = Vector2.one;
            col.anchoredPosition = new Vector2(-HudMargin, -HudMargin);
            col.sizeDelta = new Vector2(PlateW, 0f);
            var vg = col.gameObject.AddComponent<VerticalLayoutGroup>();
            vg.spacing = GapY;
            vg.childControlWidth = vg.childControlHeight = true;
            vg.childForceExpandWidth = true;
            vg.childForceExpandHeight = false;
            col.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            float plateH = PlateW / PlateAspect;
            Text day = Plate("DayPlate", col, "icon_coins", "Day 1 (Medium)", plateH);   // the kit extraction swapped these two names:
            Text money = Plate("MoneyPlate", col, "icon_calendar", "Money 0", plateH);   // the file icon_coins holds the calendar art
            Text quota = Plate("QuotaPlate", col, "icon_star", "Flags 0 / 12", plateH);

            RectTransform battery = LayoutBox("Battery", col, BarH);
            Sprite bolt = Spr("UI/icon_lightning", FilterMode.Point);
            float boltH = BarIconH;
            Pic(Box("Icon", battery, 0, (BarH - boltH) * Half, boltH * Aspect(bolt), boltH), bolt, Color.white);
            float art = BarH / BarFrameArtH;
            RectTransform frame = Box("Frame", battery, BarIconW, 0, PlateW - BarIconW, BarH);
            Sliced(Pic(frame, Spr("UI/bar_battery_frame", FilterMode.Point), Color.white), art);
            RectTransform area = Fill("FillArea", frame);
            area.offsetMin = new Vector2(BarFrameLeft * art, BarFrameTop * art);
            area.offsetMax = new Vector2(-BarFrameRight * art, -BarFrameTop * art);
            RectTransform fill = Fill("Fill", area);
            Sliced(Pic(fill, Spr("UI/bar_fill_solid", FilterMode.Point), BarFull), art);
            Text dead = Label(Fill("DeadText", frame), "DETECTOR DEAD", FontBar, Warn, TextAnchor.MiddleCenter);
            Outline(dead);

            Text keyHint = Hint("KeyHint", col);
            Text workHint = Hint("WorkHint", col);

            // bottom centre: hotbar
            RectTransform hotbar = Node("Hotbar", content).GetComponent<RectTransform>();
            hotbar.anchorMin = hotbar.anchorMax = new Vector2(Half, 0f);
            hotbar.pivot = new Vector2(Half, 0f);
            hotbar.anchoredPosition = new Vector2(0f, SlotBottom);
            var hg = hotbar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hg.spacing = SlotGap;
            hg.childAlignment = TextAnchor.LowerCenter;
            hg.childControlWidth = hg.childControlHeight = true;
            hg.childForceExpandWidth = hg.childForceExpandHeight = false;
            var fitter = hotbar.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            HotbarSlot flag = Slot("FlagSlot", hotbar, "slot_flag", "Flags");
            HotbarSlot detector = Slot("DetectorSlot", hotbar, "slot_detector", "Detector");
            HotbarSlot kit = Slot("KitSlot", hotbar, "slot_defuse_kit", "Kits");

            // toast line (also over the windows' dim, below the windows)
            RectTransform toastRt = Box("Toast", root.transform, 0, 0, ToastW, ToastH);
            toastRt.anchorMin = toastRt.anchorMax = new Vector2(Half, 0f);
            toastRt.pivot = new Vector2(Half, 0f);
            toastRt.anchoredPosition = new Vector2(0f, ToastBottom);
            Text toast = Label(toastRt, "Toast message", ToastFont, Light, TextAnchor.MiddleCenter);
            Outline(toast);

            var view = root.AddComponent<HudView>();
            Set(view, "dusk", dusk); Set(view, "content", content.gameObject);
            Set(view, "dayText", day); Set(view, "moneyText", money); Set(view, "quotaText", quota);
            Set(view, "fill", fill); Set(view, "deadText", dead);
            Set(view, "keyHint", keyHint); Set(view, "workHint", workHint);
            Set(view, "flagSlot", flag); Set(view, "detectorSlot", detector); Set(view, "kitSlot", kit);
            Set(view, "toast", toast);
            Save(root, HudFolder, HudPrefab);
        }

        private static Text Plate(string name, Transform parent, string icon, string text, float h)
        {
            RectTransform plate = LayoutBox(name, parent, h);
            Sliced(Pic(plate, Spr("UI/panel_mid", FilterMode.Point), Color.white), PlateBorderScale);
            Sprite sprite = Spr("UI/" + icon, FilterMode.Point);
            float iw = PlateIconH * Aspect(sprite);
            Pic(Box("Icon", plate, PlateIconX - iw * Half, (h - PlateIconH) * Half, iw, PlateIconH), sprite, Color.white);
            return Label(Box("Text", plate, PlateTextX, 0, PlateW - PlateTextX - PlateTextRight, h), text, FontPlate, Ink, TextAnchor.MiddleLeft);
        }

        private static Text Hint(string name, Transform parent)
        {
            RectTransform rt = LayoutBox(name, parent, HintH);
            Text t = Label(rt, "Hint", FontHint, Light, TextAnchor.MiddleRight);
            Outline(t);
            return t;
        }

        private static HotbarSlot Slot(string name, Transform parent, string sprite, string caption)
        {
            Sprite sp = Spr("UI/" + sprite, FilterMode.Point);
            RectTransform rt = Rt(Node(name, parent));
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = SlotH;
            le.preferredWidth = SlotH * Aspect(sp);
            Pic(Fill("Art", rt), sp, Color.white);
            RectTransform counter = Fill("Counter", rt);
            Frac(counter, CounterLeft, CounterTop, CounterRight, CounterBottom);
            Text count = Label(counter, "0", FontCounter, Color.white, TextAnchor.MiddleCenter);
            Outline(count);
            RectTransform cap = Rt(Node("Caption", rt));
            cap.anchorMin = cap.anchorMax = new Vector2(Half, 0f);
            cap.pivot = new Vector2(Half, 1f);
            cap.anchoredPosition = new Vector2(0f, -CaptionGap);
            cap.sizeDelta = new Vector2(SlotH * Aspect(sp) * 2f, CaptionH);
            Text label = Label(cap, caption, FontCaption, Color.white, TextAnchor.UpperCenter);
            Outline(label);
            var slot = rt.gameObject.AddComponent<HotbarSlot>();
            Set(slot, "count", count); Set(slot, "caption", label);
            return slot;
        }

        // ================= Work bar =================

        private static void BuildWorkBar()
        {
            GameObject root = Node("WorkBar", null);
            RectTransform rt = Rt(root);
            rt.anchorMin = rt.anchorMax = new Vector2(Half, Half);
            rt.pivot = new Vector2(Half, 1f);
            rt.sizeDelta = new Vector2(WorkW, WorkH);
            Pic(Fill("Back", root.transform), null, WorkBack);
            RectTransform fill = Fill("Fill", root.transform, WorkInset);
            Pic(fill, null, WorkFill);
            RectTransform cap = Rt(Node("Label", root.transform));
            cap.anchorMin = cap.anchorMax = new Vector2(Half, 1f);
            cap.pivot = new Vector2(Half, 0f);
            cap.anchoredPosition = new Vector2(0f, WorkLabelGap);
            cap.sizeDelta = new Vector2(WorkW, WorkLabelH);
            Text label = Label(cap, "Defusing", WorkFont, Light, TextAnchor.MiddleCenter);
            Outline(label);
            var view = root.AddComponent<WorkBarView>();
            Set(view, "fill", fill); Set(view, "label", label);
            Save(root, HudFolder, WorkBarPrefab);
        }

        // ================= Rulebook =================

        private static void BuildRulebook()
        {
            GameObject root = Node("RulebookWindow", null);
            Stretch(Rt(root));
            Pic(Fill("Dim", root.transform), null, DimColor);
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
            Save(root, WindowsFolder, RulebookPrefab);
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

        /// <summary>A child that sits in a layout group with a fixed height.</summary>
        private static RectTransform LayoutBox(string name, Transform parent, float height)
        {
            RectTransform rt = Rt(Node(name, parent));
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return rt;
        }

        /// <summary>Makes an Image draw its nine-slice borders at <paramref name="borderScale"/> x their art size.</summary>
        private static void Sliced(Image img, float borderScale)
        {
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f / borderScale;
        }

        private static void Outline(Text t)
        {
            var shadow = t.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
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

        [System.Serializable] private class KitSprite { public string name; public int left, top, right, bottom; }
        [System.Serializable] private class KitFile { public KitSprite[] sprites; }
        private static Dictionary<string, KitSprite> kit;

        /// <summary>Nine-slice borders of the UI kit sprites (Resources/UI/ui_kit.json), by sprite name.</summary>
        private static KitSprite KitInfo(string name)
        {
            if (kit == null)
            {
                kit = new Dictionary<string, KitSprite>();
                string file = "Assets/Resources/UI/ui_kit.json";
                if (File.Exists(file)) foreach (KitSprite k in JsonUtility.FromJson<KitFile>(File.ReadAllText(file)).sprites) kit[k.name] = k;
            }
            kit.TryGetValue(name, out KitSprite info);
            return info;
        }

        /// <summary>Makes sure the png in Resources is imported as a Sprite (with its kit nine-slice borders) and returns it.</summary>
        private static Sprite Spr(string resourcePath, FilterMode filter)
        {
            string path = "Assets/Resources/" + resourcePath + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogError("[UI] Missing texture " + path); return null; }

            KitSprite info = KitInfo(Path.GetFileNameWithoutExtension(path));
            Vector4 border = info == null ? Vector4.zero : new Vector4(info.left, info.bottom, info.right, info.top);
            bool dirty = importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single || importer.spriteBorder != border;
            if (dirty)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = border;
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

        private static void Save(GameObject root, string folder, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, folder + "/" + name + ".prefab");
            Object.DestroyImmediate(root);
        }
    }
}
