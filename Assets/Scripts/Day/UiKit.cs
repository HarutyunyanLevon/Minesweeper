using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MinefieldClerk.Day
{
    [Serializable]
    public class UiSpriteInfo
    {
        public string name;
        public int w, h;
        /// <summary>Nine-slice borders in texture pixels (0 = fixed-size sprite).</summary>
        public int left, top, right, bottom;
    }

    [Serializable]
    public class UiKitFile
    {
        public int scaleDown;
        public UiSpriteInfo[] sprites;
    }

    /// <summary>
    /// The SpriteCook UI kit (Resources/UI/*.png plus ui_kit.json with the nine-slice borders), drawn with IMGUI:
    /// plain textures, nine-slice panels and click buttons. Textures use point filtering (set by UiTextureImporter).
    /// </summary>
    public static class UiKit
    {
        private const string Folder = "UI/";
        private const string Manifest = "UI/ui_kit";
        private const int SliceColumns = 3;

        private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private static Dictionary<string, UiSpriteInfo> infos;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCaches()
        {
            textures.Clear();
            infos = null;
        }

        /// <summary>A kit sprite by name (Resources/UI/&lt;name&gt;).</summary>
        public static Texture2D Tex(string name)
        {
            return Load(Folder + name);
        }

        /// <summary>Any texture under Resources by path without extension, cached, with the given filter (point by default). Null (and an error log) if missing.</summary>
        public static Texture2D Load(string resourcePath, FilterMode filter = FilterMode.Point)
        {
            if (!textures.TryGetValue(resourcePath, out Texture2D tex))
            {
                tex = Resources.Load<Texture2D>(resourcePath);
                if (tex != null) tex.filterMode = filter;
                else Debug.LogError("[UiKit] Missing texture Resources/" + resourcePath);
                textures[resourcePath] = tex;
            }
            return tex;
        }

        /// <summary>Width / height of a sprite.</summary>
        public static float Aspect(string name)
        {
            Texture2D tex = Tex(name);
            return tex == null ? 1f : (float)tex.width / tex.height;
        }

        public static void Draw(string name, Rect rect)
        {
            Texture2D tex = Tex(name);
            if (tex != null) GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, true);
        }

        /// <summary>Draws a sprite with the given height, width following its aspect, at the position.</summary>
        public static Rect DrawFitHeight(string name, float x, float y, float height)
        {
            var rect = new Rect(x, y, height * Aspect(name), height);
            Draw(name, rect);
            return rect;
        }

        /// <summary>Draws a scalable sprite stretched to the rect with its corners kept at <paramref name="borderScale"/> x their size.</summary>
        public static void NineSlice(string name, Rect rect, float borderScale)
        {
            Texture2D tex = Tex(name);
            UiSpriteInfo info = Info(name);
            if (tex == null || info == null) return;

            float l = info.left * borderScale, r = info.right * borderScale, t = info.top * borderScale, b = info.bottom * borderScale;
            float[] xs = { rect.x, rect.x + l, rect.xMax - r, rect.xMax };
            float[] ys = { rect.y, rect.y + t, rect.yMax - b, rect.yMax };
            float[] us = { 0f, (float)info.left / tex.width, 1f - (float)info.right / tex.width, 1f };
            // Texture v runs bottom to top, rows here run top to bottom.
            float[] vs = { 1f, 1f - (float)info.top / tex.height, (float)info.bottom / tex.height, 0f };

            for (int row = 0; row < SliceColumns; row++)
            {
                for (int col = 0; col < SliceColumns; col++)
                {
                    var dest = new Rect(xs[col], ys[row], xs[col + 1] - xs[col], ys[row + 1] - ys[row]);
                    var uv = new Rect(us[col], vs[row + 1], us[col + 1] - us[col], vs[row] - vs[row + 1]);
                    if (dest.width > 0f && dest.height > 0f) GUI.DrawTextureWithTexCoords(dest, tex, uv, true);
                }
            }
        }

        /// <summary>A texture button: draws the pressed sprite while the mouse is held over it. Returns true on click.</summary>
        public static bool Button(string normal, string pressed, Rect rect)
        {
            bool down = Event.current.type != EventType.Layout && rect.Contains(Event.current.mousePosition) && Mouse.current != null && Mouse.current.leftButton.isPressed;
            Draw(down ? pressed : normal, rect);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private static UiSpriteInfo Info(string name)
        {
            if (infos == null)
            {
                infos = new Dictionary<string, UiSpriteInfo>();
                var file = Resources.Load<TextAsset>(Manifest);
                if (file != null)
                {
                    foreach (UiSpriteInfo info in JsonUtility.FromJson<UiKitFile>(file.text).sprites) infos[info.name] = info;
                }
            }
            infos.TryGetValue(name, out UiSpriteInfo found);
            return found;
        }
    }
}
