using System;
using UnityEngine;
using UnityEngine.UI;
using MinefieldClerk.Rules;

namespace MinefieldClerk.Day
{

    /// <summary>
    /// The Book of Regulations (daily briefing), a prefab you edit in Unity (Resources/UI/Windows/RulebookWindow.prefab).
    /// Two pages, turn buttons, slogan, close button and a footer line.
    /// </summary>
    public class RulebookWindow : MonoBehaviour
    {
        [SerializeField] private BookPageView leftPage, rightPage;
        [SerializeField] private Button prev, next, close;
        [SerializeField] private Text slogan, footer;
        [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.35f;

        private Rulebook book;
        private string stampWord = "";

        /// <summary>The close button was pressed.</summary>
        public event Action Closed;

        public bool Visible => gameObject.activeSelf;

        private void Awake()
        {
            prev.onClick.AddListener(() => Turn(-1));
            next.onClick.AddListener(() => Turn(1));
            close.onClick.AddListener(() => { UiCanvas.ClearSelection(); Closed?.Invoke(); });
        }

        public void Show(Rulebook rulebook, string stamp, string footerText)
        {
            book = rulebook;
            stampWord = stamp;
            footer.text = footerText;
            gameObject.SetActive(true);
            UiCanvas.ClearSelection();
            Refresh();
        }

        public void Hide() { gameObject.SetActive(false); }

        private void Turn(int step)
        {
            UiCanvas.ClearSelection();
            book.Turn(step);
            Refresh();
        }

        /// <summary>Redraws both pages (after the book was turned or rebuilt).</summary>
        public void Refresh()
        {
            if (book == null) return;
            leftPage.Fill(book, book.LeftIndex, stampWord);
            rightPage.Fill(book, book.LeftIndex + 1, stampWord);
            slogan.text = book.Slogan;
            SetEnabled(prev, book.SpreadIndex > 0);
            SetEnabled(next, book.SpreadIndex < book.SpreadCount - 1);
        }

        private void SetEnabled(Button button, bool on)
        {
            button.interactable = on;
            Color c = button.image.color;
            c.a = on ? 1f : disabledAlpha;
            button.image.color = c;
        }
    }
}
