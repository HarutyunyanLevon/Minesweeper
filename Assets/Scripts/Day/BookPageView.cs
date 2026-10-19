using UnityEngine;
using UnityEngine.UI;
using MinefieldClerk.Rules;

namespace MinefieldClerk.Day
{
    /// <summary>One page of the open book (a prefab child). Fills itself from a rule entry.</summary>
    public class BookPageView : MonoBehaviour
    {
        private const string PosterFolder = "Rulebook/Posters/";

        [Header("Rule with a poster")]
        [SerializeField] private GameObject posterGroup;
        [SerializeField] private RawImage poster;
        [SerializeField, Range(0f, 1f)] private float posterFocus = 0.7f;   // 0 = crop the poster from the top, 1 = from the bottom
        [SerializeField] private GameObject stamp;
        [SerializeField] private Text stampText;
        [SerializeField] private Text ribbonTitle;
        [SerializeField] private Text bodyWithPoster;

        [Header("Rule without a poster")]
        [SerializeField] private Text plainTitle;
        [SerializeField] private Text bodyPlain;

        [Header("Page without a rule")]
        [SerializeField] private GameObject bareGroup;
        [SerializeField] private Text bareSlogan;

        [Header("All pages")]
        [SerializeField] private Text number;

        public void Fill(Rulebook book, int index, string stampWord)
        {
            number.text = "- " + (index + 1) + " -";
            bool has = book.HasPage(index);
            bareGroup.SetActive(!has);
            if (!has)
            {
                posterGroup.SetActive(false);
                plainTitle.gameObject.SetActive(false);
                bodyWithPoster.gameObject.SetActive(false);
                bodyPlain.gameObject.SetActive(false);
                bareSlogan.text = BasicRules.SloganFor(index + book.SpreadCount);
                return;
            }

            RuleEntry page = book.Page(index);
            Texture2D tex = string.IsNullOrEmpty(page.Poster) ? null : Resources.Load<Texture2D>(PosterFolder + page.Poster);
            bool withPoster = tex != null;
            posterGroup.SetActive(withPoster);
            plainTitle.gameObject.SetActive(!withPoster);
            bodyWithPoster.gameObject.SetActive(withPoster);
            bodyPlain.gameObject.SetActive(!withPoster);
            if (withPoster)
            {
                poster.texture = tex;
                Rect size = poster.rectTransform.rect;
                float shown = Mathf.Min(1f, tex.width * size.height / (size.width * tex.height));   // fraction of the poster height that fits the frame
                poster.uvRect = new Rect(0f, (1f - shown) * posterFocus, 1f, shown);
                ribbonTitle.text = page.Title;
                bodyWithPoster.text = page.Text;
                bool changed = book.IsChangedPage(index) && !string.IsNullOrEmpty(stampWord);
                stamp.SetActive(changed);
                stampText.text = stampWord;
            }
            else
            {
                plainTitle.text = page.Title;
                bodyPlain.text = page.Text;
            }
        }
    }
}
