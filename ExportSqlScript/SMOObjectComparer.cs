using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;

namespace ExportSQLScript
{
    /// <summary>
    /// Compares SqlSmoObject objects for creation order.
    /// Sorts by object type in Config.aIndependentTypeOrder, type name, unnamed objects first, by caseInsensitive name, .
    /// </summary>
    public class SMOObjectComparer : IComparer<SqlSmoObject>
    {
        private static readonly CaseInsensitiveComparer caseInsensitiveComparer = new CaseInsensitiveComparer();

        /// <summary>
        /// Compares two SqlSmoObjects and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// Sorts by object type in Config.aIndependentTypeOrder, type name, unnamed objects first, by caseInsensitive name, .
        /// </summary>
        /// <param name="x">The first SqlSmoObject to compare.</param>
        /// <param name="y">The second SqlSmoObject to compare.</param>
        /// <returns>
        /// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
        /// </returns>
        /// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
        int IComparer<SqlSmoObject>.Compare(SqlSmoObject x, SqlSmoObject y)
        {
            //Sort by aIndependentTypeOrder (recognised objects first)
            string xtype = x.Urn.Type;
            string ytype = y.Urn.Type;
            int xtypei = Array.IndexOf(Config.aIndependentTypeOrder, xtype);
            int ytypei = Array.IndexOf(Config.aIndependentTypeOrder, ytype);
            if (xtypei == -1) xtypei = 1000;
            if (ytypei == -1) ytypei = 1000;
            int i = Comparer.DefaultInvariant.Compare(xtypei, ytypei);
            if (i != 0) return i;
            //Sort by type name
            i = Comparer.DefaultInvariant.Compare(xtype, ytype);
            if (i != 0) return i;
            //Sort by unnamable objects first
            if (!(x is NamedSmoObject)) i--;
            if (!(y is NamedSmoObject)) i++;
            if (i != 0) return i;
            //If both unnameable use URN string
            if (!(x is NamedSmoObject))
                return Comparer.DefaultInvariant.Compare(x.Urn.ToString(), y.Urn.ToString());
            //Otherwise use SMO name
            return caseInsensitiveComparer.Compare(((NamedSmoObject) x).Name, ((NamedSmoObject) y).Name);
        }
    }
}