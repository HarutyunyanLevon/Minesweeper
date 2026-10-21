using UnityEngine;
using UnityEngine.UI;

namespace MinefieldClerk.Day
{
    /// <summary>One slot of the hotbar (prefab child): a counter on the dark square and a caption underneath.</summary>
    public class HotbarSlot : MonoBehaviour
    {
        [SerializeField] private Text count, caption;

        public void Set(string counter, string label, Color tint)
        {
            count.text = counter;
            caption.text = label;
            count.color = caption.color = tint;
        }
    }
}
