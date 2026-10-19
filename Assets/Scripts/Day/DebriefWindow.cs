using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MinefieldClerk.Family;

namespace MinefieldClerk.Day
{
    /// <summary>One line of the formal report.</summary>
    public struct ReportRow
    {
        public string Label, Value;
        public bool Bad;
    }

    /// <summary>
    /// End-of-day window, a prefab you edit in Unity (Resources/UI/Windows/DebriefWindow.prefab). Page 1 is the formal state report
    /// (goals and fines), page 2 the family page with the optional expenses. The row and expense templates inside the prefab are
    /// copied once per line at runtime and hidden themselves.
    /// </summary>
    public class DebriefWindow : MonoBehaviour
    {
        [Header("Report page")]
        [SerializeField] private GameObject reportPage;
        [SerializeField] private Text titleText;
        [SerializeField] private RectTransform rowsRoot;
        [SerializeField] private ReportRowView rowTemplate;
        [SerializeField] private Button reportNext;
        [SerializeField] private Text reportNextLabel;
        [SerializeField] private string nextText = "Next (Enter)";
        [SerializeField] private string lastText = "Next day (Enter)";
        [SerializeField] private Color badColor = new Color(0.70f, 0.10f, 0.08f);

        [Header("Family page")]
        [SerializeField] private GameObject familyPage;
        [SerializeField] private Text inHandText;
        [SerializeField] private string inHandPrefix = "In hand: ";
        [SerializeField] private RectTransform expenseRoot;
        [SerializeField] private ExpenseRowView expenseTemplate;
        [SerializeField] private Button familyNext;
        [SerializeField] private Color faintColor = new Color(0.32f, 0.18f, 0.09f, 0.35f);

        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly List<ExpenseRowView> expenseViews = new List<ExpenseRowView>();
        private readonly List<Color> expenseColors = new List<Color>();
        private List<ExpenseLine> lines = new List<ExpenseLine>();
        private int money;

        /// <summary>The last page was finished (Next day).</summary>
        public event Action Finished;

        public bool Visible => gameObject.activeSelf;
        public int Page { get; private set; }
        public List<ExpenseLine> Lines => lines;
        public int Total { get { int t = 0; foreach (ExpenseLine l in lines) if (l.Checked) t += l.Cost; return t; } }

        private void Awake()
        {
            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
            if (expenseTemplate != null) expenseTemplate.gameObject.SetActive(false);
            reportNext.onClick.AddListener(OnReportNext);
            familyNext.onClick.AddListener(OnFamilyNext);
        }

        public void Hide() { gameObject.SetActive(false); }

        public void Open(string reportTitle, List<ReportRow> rows, List<ExpenseLine> expenseLines, int cash)
        {
            lines = expenseLines;
            money = cash;
            Clear();
            titleText.text = reportTitle;

            Color normal = rowTemplate.Label.color;
            foreach (ReportRow row in rows)
            {
                ReportRowView view = Instantiate(rowTemplate, rowsRoot);
                view.gameObject.SetActive(true);
                view.Label.text = row.Label;
                view.Value.text = row.Value;
                view.Label.color = view.Value.color = row.Bad ? badColor : normal;
                spawned.Add(view.gameObject);
            }

            foreach (ExpenseLine line in lines)
            {
                ExpenseRowView view = Instantiate(expenseTemplate, expenseRoot);
                view.gameObject.SetActive(true);
                view.Price.text = line.Cost.ToString();
                view.Label.text = line.Def.label;
                view.Box.SetIsOnWithoutNotify(line.Checked);
                ExpenseLine captured = line;
                view.Box.onValueChanged.AddListener(on => { captured.Checked = on; UiCanvas.ClearSelection(); RefreshExpenses(); });
                expenseViews.Add(view);
                expenseColors.Add(view.Label.color);
                spawned.Add(view.gameObject);
            }

            ShowPage(0);
            gameObject.SetActive(true);
            UiCanvas.ClearSelection();
        }

        private void Clear()
        {
            foreach (GameObject go in spawned) Destroy(go);
            spawned.Clear();
            expenseViews.Clear();
            expenseColors.Clear();
        }

        private void ShowPage(int page)
        {
            Page = page;
            reportPage.SetActive(page == 0);
            familyPage.SetActive(page == 1);
            reportNextLabel.text = lines.Count > 0 ? nextText : lastText;
            RefreshExpenses();
        }

        /// <summary>Money left, and which lines can still be ticked.</summary>
        private void RefreshExpenses()
        {
            int left = money - Total;
            inHandText.text = inHandPrefix + left;
            for (int i = 0; i < expenseViews.Count; i++)
            {
                ExpenseLine line = lines[i];
                bool can = line.Checked || line.Cost <= left;
                ExpenseRowView view = expenseViews[i];
                view.Box.interactable = can;
                Color c = can ? expenseColors[i] : faintColor;
                view.Label.color = view.Price.color = c;
            }
        }

        /// <summary>Next page, or true when the last page is done.</summary>
        public bool Advance()
        {
            if (Page == 0 && lines.Count > 0) { ShowPage(1); return false; }
            return true;
        }

        private void OnReportNext()
        {
            UiCanvas.ClearSelection();
            if (Advance()) Finished?.Invoke();
        }

        private void OnFamilyNext()
        {
            UiCanvas.ClearSelection();
            Finished?.Invoke();
        }
    }
}
