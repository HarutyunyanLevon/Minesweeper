using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MinefieldClerk.Sound;

namespace MinefieldClerk.Day
{
    /// <summary>One line of the formal report.</summary>
    public struct ReportRow
    {
        public string Label, Value;
        public bool Bad;
    }

    /// <summary>
    /// The formal state report at the end of the day, a prefab you edit in Unity (Resources/UI/Windows/DebriefWindow.prefab).
    /// The row template inside the prefab is copied once per line at runtime and hides itself.
    /// </summary>
    public class DebriefWindow : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private RectTransform rowsRoot;
        [SerializeField] private ReportRowView rowTemplate;
        [SerializeField] private Button next;
        [SerializeField] private Text nextLabel;
        [SerializeField] private string nextText = "Next (Enter)";
        [SerializeField] private string lastText = "Next day (Enter)";
        [SerializeField] private Color badColor = new Color(0.70f, 0.10f, 0.08f);

        private readonly List<GameObject> spawned = new List<GameObject>();

        /// <summary>The player finished reading (button or Enter).</summary>
        public event Action Finished;

        public bool Visible => gameObject.activeSelf;

        private void Awake()
        {
            rowTemplate.gameObject.SetActive(false);
            next.onClick.AddListener(Confirm);
        }

        public void Hide() { gameObject.SetActive(false); }

        /// <summary>Shows the report; <paramref name="last"/> = no expenses page follows.</summary>
        public void Open(string reportTitle, List<ReportRow> rows, bool last)
        {
            foreach (GameObject go in spawned) Destroy(go);
            spawned.Clear();
            titleText.text = reportTitle;
            nextLabel.text = last ? lastText : nextText;

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
            gameObject.SetActive(true);
            UiCanvas.ClearSelection();
        }

        public void Confirm()
        {
            UiCanvas.ClearSelection();
            Sfx.Play(SfxEvent.UiClick);
            Finished?.Invoke();
        }
    }
}
