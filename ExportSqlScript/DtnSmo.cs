// <copyright file="DtnSmo.cs" company="Ivan Hamilton, Rami Abughazaleh, and contributors">
//   Copyright (c) Ivan Hamilton, Rami Abughazaleh, and contributors. All rights reserved.
// </copyright>

namespace ExportSqlScript
{
    using Microsoft.SqlServer.Management.Smo;

    /// <summary>
    /// Holds a DependencyTreeNode and NamedSmoObject association.
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