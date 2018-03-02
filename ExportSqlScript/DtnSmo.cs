using Microsoft.SqlServer.Management.Smo;

namespace ExportSqlScript
{
    /// <summary>
    /// Holds a DependencyTreeNode & NamedSmoObject association.
    /// </summary>
    public struct DtnSmo
    {
        /// <summary>
        /// The DependencyTreeNode
        /// </summary>
        public DependencyTreeNode dependencyTreeNode;

        /// <summary>
        /// The NamedSmoObject
        /// </summary>
        public NamedSmoObject namedSmoObject;
    }
}