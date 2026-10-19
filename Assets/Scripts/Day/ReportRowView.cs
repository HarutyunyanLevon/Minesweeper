using UnityEngine;
using UnityEngine.UI;

namespace MinefieldClerk.Day
{
    /// <summary>A report row prefab: a label on the left and a value on the right.</summary>
    public class ReportRowView : MonoBehaviour
    {
        [SerializeField] private Text label, value;
        public Text Label => label;
        public Text Value => value;
    }
}
