using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MinefieldClerk.Family;
using MinefieldClerk.Sound;

namespace MinefieldClerk.Day
{
    /// <summary>
    /// The family page: a locket, a note and a notebook with the optional expenses of the evening. A prefab you edit in Unity
    /// (Resources/UI/Windows/ExpensesWindow.prefab). The expense template inside it is copied once per line and hides itself.
    /// </summary>
    public class ExpensesWindow : MonoBehaviour
    {
        [SerializeField] private Text inHandText;
        [SerializeField] private string inHandPrefix = "In hand: ";
        [SerializeField] private RectTransform expenseRoot;
        [SerializeField] private ExpenseRowView expenseTemplate;
        [SerializeField] private Button next;
        [SerializeField] private Color faintColor = new Color(0.32f, 0.18f, 0.09f, 0.35f);

        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly List<ExpenseRowView> views = new List<ExpenseRowView>();
        private readonly List<Color> colors = new List<Color>();
        private List<ExpenseLine> lines = new List<ExpenseLine>();
        private int money;

        /// <summary>The player is done with the evening (Next day button or Enter).</summary>
        public event Action Finished;

        public bool Visible => gameObject.activeSelf;

        private void Awake()
        {
            expenseTemplate.gameObject.SetActive(false);
            next.onClick.AddListener(Confirm);
        }

        public void Hide() { gameObject.SetActive(false); }

        /// <summary>Lists the expenses; the ticks are written back into the lines.</summary>
        public void Open(List<ExpenseLine> expenseLines, int cash)
        {
            lines = expenseLines;
            money = cash;
            foreach (GameObject go in spawned) Destroy(go);
            spawned.Clear();
            views.Clear();
            colors.Clear();

            foreach (ExpenseLine line in lines)
            {
                ExpenseRowView view = Instantiate(expenseTemplate, expenseRoot);
                view.gameObject.SetActive(true);
                view.Price.text = line.Cost.ToString();
                view.Label.text = line.Def.label;
                view.Box.SetIsOnWithoutNotify(line.Checked);
                ExpenseLine captured = line;
                view.Box.onValueChanged.AddListener(on => { captured.Checked = on; UiCanvas.ClearSelection(); Sfx.Play(SfxEvent.UiClick); Refresh(); });
                views.Add(view);
                colors.Add(view.Label.color);
                spawned.Add(view.gameObject);
            }
            Refresh();
            gameObject.SetActive(true);
            UiCanvas.ClearSelection();
        }

        /// <summary>Money left, and which lines can still be ticked.</summary>
        private void Refresh()
        {
            int total = 0;
            foreach (ExpenseLine l in lines) if (l.Checked) total += l.Cost;
            int left = money - total;
            inHandText.text = inHandPrefix + left;
            for (int i = 0; i < views.Count; i++)
            {
                bool can = lines[i].Checked || lines[i].Cost <= left;
                views[i].Box.interactable = can;
                views[i].Label.color = views[i].Price.color = can ? colors[i] : faintColor;
            }
        }

        public void Confirm()
        {
            UiCanvas.ClearSelection();
            Sfx.Play(SfxEvent.UiClick);
            Finished?.Invoke();
        }
    }
}
