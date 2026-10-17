using System.Collections.Generic;
using UnityEngine;
using MinefieldClerk.Rules;

namespace MinefieldClerk.Day
{
    /// <summary>
    /// The pages of the Book of Regulations for one shift: the basic decrees first (Resources/Rulebook/basic_rules.json),
    /// then the directives in force today, oldest rule first. Keeps the page position; drawing is RulePaper's job.
    /// The book opens on the newest directive that is new or changed since the previous day (or the newest directive
    /// if none changed, or the first page if there are no directives). After that it stays on the page the player left it on.
    /// Rebuild, called at the start of every shift, resets all of this.
    /// </summary>
    public class Rulebook
    {
        private readonly List<RuleEntry> pages = new List<RuleEntry>();
        private readonly HashSet<string> changedIds = new HashSet<string>();
        private Dictionary<string, string> yesterday = new Dictionary<string, string>();
        private Dictionary<string, string> today = new Dictionary<string, string>();
        private int snapshotDay = -1;
        private int basicCount;
        private int index;

        public int PageCount => pages.Count;
        public int Index => index;
        public RuleEntry Current => pages.Count > 0 ? pages[index] : new RuleEntry("", "", "");
        /// <summary>True while the page shown is one of today's directives (they come after the basic decrees).</summary>
        public bool IsSpecialPage => index >= basicCount;
        /// <summary>True when the page shown is a directive that is new or has different numbers than the previous day.</summary>
        public bool IsChangedPage => IsSpecialPage && changedIds.Contains(Current.Id);
        public string Slogan => BasicRules.SloganFor(index);

        /// <summary>Builds the pages for a new shift and puts the book on its opening page.</summary>
        public void Rebuild(int day, RuleSet rules, DifficultySettings settings)
        {
            pages.Clear();
            BasicRules.AddPages(pages, settings);
            basicCount = pages.Count;
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
                today[entry.Id] = entry.Text;
                if (!yesterday.TryGetValue(entry.Id, out string before) || before != entry.Text) changedIds.Add(entry.Id);
            }

            index = OpeningPage();
        }

        /// <summary>Turns the page by a step (negative = back), staying inside the book.</summary>
        public void Turn(int step)
        {
            index = Mathf.Clamp(index + step, 0, Mathf.Max(0, pages.Count - 1));
        }

        private int OpeningPage()
        {
            if (pages.Count == basicCount) return 0;   // no directives today: open on the first decree
            for (int i = pages.Count - 1; i >= basicCount; i--)
            {
                if (changedIds.Contains(pages[i].Id)) return i;
            }
            return pages.Count - 1;
        }
    }
}
