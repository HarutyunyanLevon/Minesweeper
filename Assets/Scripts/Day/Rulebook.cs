using System.Collections.Generic;
using UnityEngine;
using MinefieldClerk.Rules;

namespace MinefieldClerk.Day
{
    /// <summary>
    /// The pages of the Book of Regulations for one shift, in the order of the day's rule list (Resources/Days/days.json):
    /// the basic decrees wherever "basic" stands, the other directives where their ids stand. The book shows two pages at once (a spread: even page on
    /// the left, the next on the right). Keeps the spread position; drawing is RulePaper's job.
    /// The book opens on the spread holding the newest directive that is new or changed since the previous day (or the
    /// newest directive if none changed, or the first spread if there are no directives). After that it stays on the
    /// spread the player left it on. Rebuild, called at the start of every shift, resets all of this.
    /// </summary>
    public class Rulebook
    {
        private const int PagesPerSpread = 2;

        private readonly List<RuleEntry> pages = new List<RuleEntry>();
        private readonly HashSet<string> changedIds = new HashSet<string>();
        private Dictionary<string, string> yesterday = new Dictionary<string, string>();
        private Dictionary<string, string> today = new Dictionary<string, string>();
        private int snapshotDay = -1;
        private int leftIndex;

        public int PageCount => pages.Count;
        public int SpreadCount => Mathf.Max(1, (pages.Count + PagesPerSpread - 1) / PagesPerSpread);
        /// <summary>Zero-based number of the spread on screen.</summary>
        public int SpreadIndex => leftIndex / PagesPerSpread;
        /// <summary>Index of the left page of the spread on screen.</summary>
        public int LeftIndex => leftIndex;
        public string Slogan => BasicRules.SloganFor(SpreadIndex);

        public bool HasPage(int index) => index >= 0 && index < pages.Count;
        public RuleEntry Page(int index) => pages[index];
        /// <summary>True when the page is one of today's directives and not a basic decree.</summary>
        public bool IsSpecialPage(int index) => HasPage(index) && pages[index].Id != RuleIds.Basic;
        /// <summary>True when the page is a directive that is new or has different numbers than the previous day.</summary>
        public bool IsChangedPage(int index) => HasPage(index) && IsSpecialPage(index) && changedIds.Contains(pages[index].Id);

        /// <summary>Builds the pages for a new shift and puts the book on its opening spread.</summary>
        public void Rebuild(int day, RuleSet rules)
        {
            pages.Clear();
            pages.AddRange(rules.Entries);

            // "Changed" is measured against the previous day; a re-roll of the same day keeps that comparison.
            if (day != snapshotDay)
            {
                yesterday = today;
                snapshotDay = day;
            }
            today = new Dictionary<string, string>();
            changedIds.Clear();
            foreach (RuleEntry entry in rules.Entries)
            {
                if (entry.Id == RuleIds.Basic) continue;
                today[entry.Id] = entry.Text;
                if (!yesterday.TryGetValue(entry.Id, out string before) || before != entry.Text) changedIds.Add(entry.Id);
            }

            leftIndex = SpreadStart(OpeningPage());
        }

        /// <summary>Turns the book by a number of spreads (negative = back), staying inside the book.</summary>
        public void Turn(int step)
        {
            int spread = Mathf.Clamp(SpreadIndex + step, 0, SpreadCount - 1);
            leftIndex = spread * PagesPerSpread;
        }

        private static int SpreadStart(int pageIndex)
        {
            return pageIndex / PagesPerSpread * PagesPerSpread;
        }

        private int OpeningPage()
        {
            int newest = -1;
            for (int i = pages.Count - 1; i >= 0; i--)
            {
                if (!IsSpecialPage(i)) continue;
                if (newest < 0) newest = i;   // no directive changed: open on the last directive
                if (changedIds.Contains(pages[i].Id)) return i;
            }
            return Mathf.Max(0, newest);      // no directives today: open on the first page
        }
    }
}
