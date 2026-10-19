using System;
using System.Reflection;
using UnityEngine;

namespace MinefieldClerk.CameraRig
{
    /// <summary>
    /// Removes the black bars: the URP Pixel Perfect Camera crops the view to its reference aspect by default.
    /// Runs once after the scene loads, switches the crop frame off and lets the camera fill the whole window.
    /// Done by name so this file does not depend on the URP 2D assembly.
    /// </summary>
    public static class CameraBootstrap
    {
        private const string PixelPerfectTypeName = "PixelPerfectCamera";
        private const string CropFrameProperty = "cropFrame";
        private const int CropFrameNone = 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Apply()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            cam.rect = new Rect(0f, 0f, 1f, 1f);

            foreach (Component c in cam.GetComponents<Component>())
            {
                if (c == null || c.GetType().Name != PixelPerfectTypeName) continue;
                SetEnum(c, CropFrameProperty, CropFrameNone);
            }
        }

        private static void SetEnum(Component c, string name, int value)
        {
            PropertyInfo p = c.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (p != null && p.CanWrite) p.SetValue(c, Enum.ToObject(p.PropertyType, value));
        }
    }
}
