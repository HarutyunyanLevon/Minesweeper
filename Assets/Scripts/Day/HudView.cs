using UnityEngine;
using UnityEngine.UI;

namespace MinefieldClerk.Day
{
    /// <summary>What the HUD shows this frame.</summary>
    public struct HudState
    {
        public int Day;
        public string Difficulty;
        public int Money;
        public bool CollectMode;
        public int Collected, Flags, Quota;
        public float Battery01;
        public bool BatteryEmpty;
        public bool HasProbeLimit;
        public int Probes, ProbeCap;
        public bool HasKits;
        public int KitsLeft, KitsTotal;
        public string KeyHint, WorkHint;
    }

    /// <summary>
    /// The in-shift HUD, a prefab you edit in Unity (Resources/UI/Hud/HudWindow.prefab): three plates (day, money, quota),
    /// the detector battery, key hints, the hotbar, the toast line and the dusk overlay that darkens as the battery drains.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        [Header("Screen")]
        [SerializeField] private Image dusk;
        [SerializeField] private Color duskColor = new Color(0.12f, 0.05f, 0.2f);
        [SerializeField, Range(0f, 1f)] private float duskMax = 0.35f;   // darkest overlay when the battery is empty
        [SerializeField] private GameObject content;                     // everything except the dusk and the toast

        [Header("Plates")]
        [SerializeField] private Text dayText, moneyText, quotaText;

        [Header("Battery")]
        [SerializeField] private RectTransform fill;                     // stretches inside its parent; the script sets its right edge
        [SerializeField] private float fillMinWidth = 14f;               // narrowest fill, so the rounded ends never overlap
        [SerializeField] private Text deadText;
        [SerializeField] private Color barFull = new Color(0.45f, 0.78f, 0.2f), barMid = new Color(0.95f, 0.75f, 0.18f), barLow = new Color(0.86f, 0.2f, 0.12f);

        [Header("Hints and hotbar")]
        [SerializeField] private Text keyHint, workHint;
        [SerializeField] private HotbarSlot flagSlot, detectorSlot, kitSlot;
        [SerializeField] private Color warnColor = new Color(0.95f, 0.35f, 0.25f);
        [SerializeField] private string probeLimitFormat = "Probes {0}/{1}", detectorLabel = "Detector", flagsLabel = "Flags", kitsFormat = "Kits {0}/{1}";
        [SerializeField] private string dayFormat = "Day {0} ({1})", moneyFormat = "Money {0}", flagQuotaFormat = "Flags {0} / {1}", collectQuotaFormat = "Collected {0} / {1}";

        [Header("Toast")]
        [SerializeField] private Text toast;
        [SerializeField] private float toastSeconds = 2.5f;

        private float toastUntil;

        private void Awake()
        {
            toast.gameObject.SetActive(false);
            deadText.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (toast.gameObject.activeSelf && Time.time >= toastUntil) toast.gameObject.SetActive(false);
        }

        public void ShowToast(string text)
        {
            toast.text = text;
            toast.gameObject.SetActive(true);
            toastUntil = Time.time + toastSeconds;
        }

        /// <summary>Shows or hides the plates, battery, hints and hotbar (the dusk and the toast stay).</summary>
        public void SetContentVisible(bool visible)
        {
            if (content.activeSelf != visible) content.SetActive(visible);
        }

        public void SetDusk(float battery01)
        {
            Color c = duskColor;
            c.a = (1f - Mathf.Clamp01(battery01)) * duskMax;
            dusk.color = c;
        }

        public void Apply(HudState s)
        {
            SetDusk(s.Battery01);
            if (!content.activeSelf) return;

            dayText.text = string.Format(dayFormat, s.Day, s.Difficulty);
            moneyText.text = string.Format(moneyFormat, s.Money);
            quotaText.text = string.Format(s.CollectMode ? collectQuotaFormat : flagQuotaFormat, s.CollectMode ? s.Collected : s.Flags, s.Quota);
            ApplyBattery(s);

            keyHint.text = s.KeyHint;
            workHint.text = s.WorkHint;
            workHint.gameObject.SetActive(s.HasKits);

            flagSlot.Set(s.Flags.ToString(), flagsLabel, Color.white);
            bool overLimit = s.HasProbeLimit && s.Probes > s.ProbeCap;
            detectorSlot.Set(s.Probes.ToString(), s.HasProbeLimit ? string.Format(probeLimitFormat, s.Probes, s.ProbeCap) : detectorLabel, overLimit ? warnColor : Color.white);
            kitSlot.gameObject.SetActive(s.HasKits);
            if (s.HasKits) kitSlot.Set(s.KitsLeft.ToString(), string.Format(kitsFormat, s.KitsLeft, s.KitsTotal), s.KitsLeft == 0 ? warnColor : Color.white);
        }

        private void ApplyBattery(HudState s)
        {
            float charge = Mathf.Clamp01(s.Battery01);
            fill.gameObject.SetActive(charge > 0f);
            if (charge > 0f)
            {
                float area = ((RectTransform)fill.parent).rect.width;
                float share = area > 0f ? Mathf.Max(charge, fillMinWidth / area) : charge;
                fill.anchorMax = new Vector2(Mathf.Min(1f, share), 1f);
                fill.GetComponent<Image>().color = BatteryColor(charge);
            }
            deadText.gameObject.SetActive(s.BatteryEmpty);
        }

        private Color BatteryColor(float charge)
        {
            const float half = 0.5f;
            return charge >= half ? Color.Lerp(barMid, barFull, (charge - half) / half) : Color.Lerp(barLow, barMid, charge / half);
        }
    }
}
