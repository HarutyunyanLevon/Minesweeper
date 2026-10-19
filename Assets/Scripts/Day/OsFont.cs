using UnityEngine;
using UnityEngine.UI;

namespace MinefieldClerk.Day
{
    /// <summary>Gives a Text a font installed on the player's system (first name in the list that exists), e.g. a handwriting face.</summary>
    [RequireComponent(typeof(Text))]
    public class OsFont : MonoBehaviour
    {
        [Tooltip("Comma separated font names, tried in order; if none exists the Text keeps its own font")]
        [SerializeField] private string fontNames = "Segoe Script,Ink Free,Bradley Hand ITC,Segoe Print,Comic Sans MS";

        private void Awake()
        {
            var text = GetComponent<Text>();
            string[] names = fontNames.Split(',');
            for (int i = 0; i < names.Length; i++) names[i] = names[i].Trim();
            Font font = Font.CreateDynamicFontFromOSFont(names, text.fontSize);
            if (font != null) text.font = font;
        }
    }
}
