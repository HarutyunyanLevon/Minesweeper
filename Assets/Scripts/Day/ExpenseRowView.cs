using UnityEngine;
using UnityEngine.UI;

namespace MinefieldClerk.Day
{
    /// <summary>An expense row prefab: price, checkbox and name.</summary>
    public class ExpenseRowView : MonoBehaviour
    {
        [SerializeField] private Text price, label;
        [SerializeField] private Toggle box;
        public Text Price => price;
        public Text Label => label;
        public Toggle Box => box;
    }
}
