using System;
using System.Collections.Generic;
using UnityEngine;

namespace MinefieldClerk.Family
{
    [Serializable]
    public class ExpenseDef
    {
        public string id;
        public string label;
        public int cost;            // 0 with kind "medicine": the difficulty's medicine cost is used
        public string kind;
        public bool placeholder;    // family needs: real system comes later
    }

    [Serializable]
    public class ExpenseFile { public ExpenseDef[] items; }

    /// <summary>Optional daily expenses from Resources/Family/expenses.json. None is required to carry on.</summary>
    public static class ExpenseTable
    {
        public const string KindMedicine = "medicine";
        private const string Path = "Family/expenses";

        public static ExpenseDef[] Load()
        {
            TextAsset asset = Resources.Load<TextAsset>(Path);
            if (asset == null) return new ExpenseDef[0];
            ExpenseFile file = JsonUtility.FromJson<ExpenseFile>(asset.text);
            return file != null && file.items != null ? file.items : new ExpenseDef[0];
        }
    }

    /// <summary>One line of the family checklist for today.</summary>
    public class ExpenseLine
    {
        public ExpenseDef Def;
        public int Cost;
        public bool Checked;
    }
}
