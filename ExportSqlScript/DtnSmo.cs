using Microsoft.SqlServer.Management.Smo;

namespace ExportSQLScript
{
    /// <summary>
    /// Holds a DependencyTreeNode & NamedSmoObject association.
    /// </summary>
    public struct DtnSmo
    {
        /// <summary>
        /// The DependencyTreeNode
        /// </summary>
        internal DependencyTreeNode dependencyTreeNode;

        /// <summary>
        /// The NamedSmoObject
        /// </summary>
        internal NamedSmoObject namedSmoObject;
    }
}