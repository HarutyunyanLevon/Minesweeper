using UnityEngine;
using MinefieldClerk.Rules;

namespace MinefieldClerk.Day
{
    /// <summary>
    /// Placeholder IMGUI papers in the style of the Republic's propaganda: a red banner with gold stars, cream paper,
    /// black ink and a slogan band. Used for the daily directive (briefing) and for one page of the Book of Regulations
    /// with turn buttons. Drawn with flat colours and one procedural star; the real art replaces this at the UI milestone.
    /// </summary>
    public class RulePaper
    {
        private const int BannerFont = 32, SectionFont = 18, HeadingFont = 28, BodyFont = 22, ListBodyFont = 20;
        private const int SmallFont = 18, SloganFont = 20, StampFont = 30;
        private const float Width = 780f, Height = 600f, BorderWidth = 6f;
        private const float BannerHeight = 76f, StripeHeight = 6f, StarSize = 44f, SidePad = 30f, SloganBandHeight = 40f;
        private const float SectionTop = 8f, SectionHeight = 24f;
        private const float TitleTop = 38f, TitleHeight = 40f, RuleLineTop = 80f, RuleLineHeight = 3f, BodyTop = 94f;
        private const float ListTop = 16f, ListHeadingHeight = 34f, ListGap = 18f;
        private const float FooterHeight = 26f, FooterGap = 8f;
        private const float ButtonWidth = 150f, ButtonHeight = 40f, ButtonGap = 10f;
        private const float StampWidth = 190f, StampHeight = 56f, StampLift = 24f, StampAngle = -12f, StampLine = 4f;
        private const int StarTexSize = 64, StarPoints = 5;
        private const float StarInnerRatio = 0.382f;
        private const string NoRules = "No directives today. Carry on, citizen.";
        private const string PreviousLabel = "< Previous";
        private const string NextLabel = "Next >";

        private static readonly Color Red = new Color(0.72f, 0.10f, 0.08f);
        private static readonly Color DarkRed = new Color(0.50f, 0.06f, 0.05f);
        private static readonly Color Cream = new Color(0.93f, 0.87f, 0.70f);
        private static readonly Color Ink = new Color(0.14f, 0.10f, 0.08f);
        private static readonly Color Gold = new Color(0.95f, 0.76f, 0.22f);
        private static readonly Color Black = new Color(0.06f, 0.05f, 0.05f);

        private GUIStyle bannerStyle, sectionStyle, headingStyle, pageBodyStyle, listBodyStyle, smallStyle, sloganStyle, stampStyle;
        private Texture2D star;

        /// <summary>The daily directive: every rule in force today, one after the other.</summary>
        public void Draw(string heading, string footer, string slogan, RuleSet rules)
        {
            EnsureAssets();
            Rect paper = DrawPanel(heading, slogan);

            float textWidth = paper.width - SidePad * 2f;
            float y = paper.y + ListTop;
            if (rules.Count == 0)
            {
                GUI.Label(new Rect(paper.x + SidePad, y, textWidth, ListHeadingHeight), NoRules, headingStyle);
            }
            foreach (RuleEntry entry in rules.Entries)
            {
                GUI.Label(new Rect(paper.x + SidePad, y, textWidth, ListHeadingHeight), entry.Title, headingStyle);
                y += ListHeadingHeight;
                float bodyHeight = listBodyStyle.CalcHeight(new GUIContent(entry.Text), textWidth);
                GUI.Label(new Rect(paper.x + SidePad, y, textWidth, bodyHeight), entry.Text, listBodyStyle);
                y += bodyHeight + ListGap;
            }

            GUI.Label(new Rect(paper.x, paper.yMax - FooterHeight - FooterGap, paper.width, FooterHeight), footer, smallStyle);
        }

        /// <summary>
        /// One page of the book. A non-empty stamp is printed across the page. Returns -1 or +1 when a turn button
        /// was clicked, otherwise 0.
        /// </summary>
        public int DrawPage(string heading, string section, RuleEntry page, int index, int count, string slogan, string stamp, string footer)
        {
            EnsureAssets();
            Rect paper = DrawPanel(heading, slogan);

            float textWidth = paper.width - SidePad * 2f;
            GUI.Label(new Rect(paper.x, paper.y + SectionTop, paper.width, SectionHeight), section, sectionStyle);
            GUI.Label(new Rect(paper.x + SidePad, paper.y + TitleTop, textWidth, TitleHeight), page.Title, headingStyle);
            Fill(new Rect(paper.x + SidePad, paper.y + RuleLineTop, textWidth, RuleLineHeight), Ink);
            float bodyHeight = pageBodyStyle.CalcHeight(new GUIContent(page.Text), textWidth);
            GUI.Label(new Rect(paper.x + SidePad, paper.y + BodyTop, textWidth, bodyHeight), page.Text, pageBodyStyle);

            float footerTop = paper.yMax - FooterHeight - FooterGap;
            float buttonsTop = footerTop - ButtonGap - ButtonHeight;
            if (!string.IsNullOrEmpty(stamp))
            {
                DrawStamp(new Rect(paper.xMax - SidePad - StampWidth, buttonsTop - StampHeight - StampLift, StampWidth, StampHeight), stamp);
            }

            int turn = 0;
            GUI.enabled = index > 0;
            if (GUI.Button(new Rect(paper.x + SidePad, buttonsTop, ButtonWidth, ButtonHeight), PreviousLabel)) turn = -1;
            GUI.enabled = index < count - 1;
            if (GUI.Button(new Rect(paper.xMax - SidePad - ButtonWidth, buttonsTop, ButtonWidth, ButtonHeight), NextLabel)) turn = 1;
            GUI.enabled = true;
            GUI.Label(new Rect(paper.x, buttonsTop, paper.width, ButtonHeight), "Page " + (index + 1) + " / " + count, smallStyle);

            GUI.Label(new Rect(paper.x, footerTop, paper.width, FooterHeight), footer, smallStyle);
            return turn;
        }

        /// <summary>Draws the frame, banner, stars, paper and slogan band; returns the paper area.</summary>
        private Rect DrawPanel(string heading, string slogan)
        {
            var box = new Rect(Screen.width * Num.Half - Width * Num.Half, Screen.height * Num.Half - Height * Num.Half, Width, Height);
            Fill(new Rect(box.x - BorderWidth, box.y - BorderWidth, box.width + BorderWidth * 2f, box.height + BorderWidth * 2f), Black);
            Fill(new Rect(box.x, box.y, box.width, BannerHeight), Red);
            Fill(new Rect(box.x, box.y + BannerHeight, box.width, StripeHeight), Gold);
            var paper = new Rect(box.x, box.y + BannerHeight + StripeHeight, box.width, box.height - BannerHeight - StripeHeight - SloganBandHeight);
            Fill(paper, Cream);
            Fill(new Rect(box.x, paper.yMax, box.width, SloganBandHeight), Black);

            float starY = box.y + (BannerHeight - StarSize) * Num.Half;
            GUI.color = Gold;
            GUI.DrawTexture(new Rect(box.x + SidePad, starY, StarSize, StarSize), star);
            GUI.DrawTexture(new Rect(box.xMax - SidePad - StarSize, starY, StarSize, StarSize), star);
            GUI.color = Color.white;

            GUI.Label(new Rect(box.x, box.y, box.width, BannerHeight), heading, bannerStyle);
            if (!string.IsNullOrEmpty(slogan)) GUI.Label(new Rect(box.x, paper.yMax, box.width, SloganBandHeight), slogan, sloganStyle);
            return paper;
        }

        /// <summary>A red rubber stamp, tilted, over the page.</summary>
        private void DrawStamp(Rect rect, string text)
        {
            Matrix4x4 saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(StampAngle, rect.center);
            Fill(new Rect(rect.x, rect.y, rect.width, StampLine), Red);
            Fill(new Rect(rect.x, rect.yMax - StampLine, rect.width, StampLine), Red);
            Fill(new Rect(rect.x, rect.y, StampLine, rect.height), Red);
            Fill(new Rect(rect.xMax - StampLine, rect.y, StampLine, rect.height), Red);
            GUI.Label(rect, text, stampStyle);
            GUI.matrix = saved;
        }

        private static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void EnsureAssets()
        {
            if (star == null) star = MakeStar();
            if (bannerStyle != null) return;
            bannerStyle = MakeStyle(BannerFont, TextAnchor.MiddleCenter, false, Cream);
            sectionStyle = MakeStyle(SectionFont, TextAnchor.MiddleCenter, false, DarkRed);
            headingStyle = MakeStyle(HeadingFont, TextAnchor.MiddleLeft, false, DarkRed);
            pageBodyStyle = MakeStyle(BodyFont, TextAnchor.UpperLeft, true, Ink);
            listBodyStyle = MakeStyle(ListBodyFont, TextAnchor.UpperLeft, true, Ink);
            smallStyle = MakeStyle(SmallFont, TextAnchor.MiddleCenter, false, Ink);
            sloganStyle = MakeStyle(SloganFont, TextAnchor.MiddleCenter, false, Gold);
            stampStyle = MakeStyle(StampFont, TextAnchor.MiddleCenter, false, Red);
        }

        private static GUIStyle MakeStyle(int size, TextAnchor anchor, bool wrap, Color color)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = FontStyle.Bold, alignment = anchor, wordWrap = wrap };
            style.normal.textColor = color;
            return style;
        }

        /// <summary>A white five-pointed star on a transparent square, tinted at draw time.</summary>
        private static Texture2D MakeStar()
        {
            float centre = (StarTexSize - 1) * Num.Half;
            var outline = new Vector2[StarPoints * 2];
            for (int i = 0; i < outline.Length; i++)
            {
                float radius = (i % 2 == 0 ? 1f : StarInnerRatio) * centre;
                float angle = Num.Tau * i / outline.Length + Mathf.PI * Num.Half;   // first point straight up
                outline[i] = new Vector2(centre + Mathf.Cos(angle) * radius, centre + Mathf.Sin(angle) * radius);
            }

            var pixels = new Color32[StarTexSize * StarTexSize];
            for (int y = 0; y < StarTexSize; y++)
            {
                for (int x = 0; x < StarTexSize; x++)
                {
                    pixels[y * StarTexSize + x] = Inside(outline, new Vector2(x, y)) ? (Color32)Color.white : (Color32)Color.clear;
                }
            }

            var texture = new Texture2D(StarTexSize, StarTexSize, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>Even-odd point-in-polygon test.</summary>
        private static bool Inside(Vector2[] polygon, Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                bool straddles = (polygon[i].y > point.y) != (polygon[j].y > point.y);
                if (straddles && point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }
    }
}
