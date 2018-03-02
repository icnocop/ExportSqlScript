using System;
using System.Collections;
using System.Collections.Generic;

namespace ExportSQLScript
{
    /// <summary>
    /// Compares DtnSmo objects for creation order.
    /// Sorts by object type in Config.aDependentTypeOrder, by caseInsensitive name.
    /// </summary>
    public class DependencyComparer : IComparer<DtnSmo>
    {
        private static readonly CaseInsensitiveComparer caseInsensitiveComparer = new CaseInsensitiveComparer();

        /// <summary>
        /// Compares two DTN_SMOs and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// Sorts by object type in Config.aDependentTypeOrder, by caseInsensitive name.
        /// </summary>
        /// <param name="x">The first DtnSmo to compare.</param>
        /// <param name="y">The second DtnSmo to compare.</param>
        /// <returns>
        /// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
        /// </returns>
        int IComparer<DtnSmo>.Compare(DtnSmo x, DtnSmo y)
        {
            DtnSmo dtnSmoX = x;
            DtnSmo dtnSmoY = y;
            //Sort by aDependentTypeOrder
            string xType = dtnSmoX.namedSmoObject.Urn.Type;
            string yType = dtnSmoY.namedSmoObject.Urn.Type;
            int xTypeSortIndex = Array.IndexOf(Config.aDependentTypeOrder, xType);
            int yTypeSortIndex = Array.IndexOf(Config.aDependentTypeOrder, yType);
            int i = Comparer.DefaultInvariant.Compare(xTypeSortIndex, yTypeSortIndex);
            if (i != 0)
                return i;
            //Sort by Name
            return caseInsensitiveComparer.Compare(dtnSmoX.namedSmoObject.Name, dtnSmoY.namedSmoObject.Name);
        }
    }
}