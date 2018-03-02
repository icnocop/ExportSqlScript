// <copyright file="Exporter.cs" company="Ivan Hamilton, Rami Abughazaleh, and contributors">
//   Copyright (c) Ivan Hamilton, Rami Abughazaleh, and contributors. All rights reserved.
// </copyright>

namespace ExportSqlScript
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.SqlServer.Management.Sdk.Sfc;
    using Microsoft.SqlServer.Management.Smo;
    using Microsoft.SqlServer.Management.Smo.Broker;

    /// <summary>
    /// Exporter.
    /// </summary>
    public class Exporter
    {
        private readonly ILog logger;

        /// <summary>
        /// The configuration
        /// </summary>
        private readonly Config config;

        /// <summary>Methods for custom scripted types</summary>
        private readonly Dictionary<Type, ScriptObjectDelegate> customScriptMethods = new Dictionary<Type, ScriptObjectDelegate>();

        /// <summary>Order of object creation</summary>
        private readonly StringCollection fileBuildOrder = new StringCollection();

        /// <summary>Urns already scripted</summary>
        private readonly StringCollection scriptedUrns = new StringCollection();

        private List<SqlSmoObject> independentObjectsLast = new List<SqlSmoObject>();

        private List<DependencyTreeNode> resolvingDependenciesOn = new List<DependencyTreeNode>();

        /// <summary>Server SMO object for scripting.</summary>
        private Server server;

        /// <summary>
        /// Initializes a new instance of the <see cref="Exporter"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="config">The configuration.</param>
        public Exporter(ILog logger, Config config)
        {
            this.logger = logger;
            this.config = config;

            this.customScriptMethods.Add(typeof(Database), this.ScriptDatabase);
            this.customScriptMethods.Add(typeof(Table), this.ScriptTable);
            this.customScriptMethods.Add(typeof(View), this.ScriptView);
        }

        /// <summary>
        /// Delegate for custom scripting methods for SmoObjects
        /// </summary>
        internal delegate StringCollection ScriptObjectDelegate(SqlSmoObject sqlSmoObject, Urn urn, ScriptingOptions scriptingOptions);

        /// <summary>
        /// Runs the export.
        /// </summary>
        public void Run()
        {
            // Create server and database connection
            this.server = new Server(this.config.server);
            Database db = this.server.Databases[this.config.database];

            // Collections for objects with and without dependancies
            List<Urn> dependentObjects = new List<Urn>();
            List<SqlSmoObject> independentObjectsFirst = new List<SqlSmoObject>();

            List<DataTable> dtList = new List<DataTable>();
            string[] names = Enum.GetNames(typeof(DatabaseObjectTypes));
            DatabaseObjectTypes[] values = (DatabaseObjectTypes[])Enum.GetValues(typeof(DatabaseObjectTypes));
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                if (name != "All" && Array.IndexOf(this.config.excludeTypes.ToUpper().Split(','), name.ToUpper()) == -1)
                {
                    this.logger.Debug("Getting objects: " + name);
                    dtList.Add(db.EnumObjects(values[i]));
                }
            }

            foreach (DataTable dt in dtList)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    // Select all objects, or only the named object
                    if (this.config.objectName == null || dr["Name"].Equals(this.config.objectName))
                    {
                        // Ignore excluded schemas
                        if (Array.IndexOf(Config.aExcludedSchemas, dr["Schema"]) == -1)
                        {
                            Urn urn = new Urn(dr["Urn"].ToString());

                            // Ignore excluded types
                            if (Array.IndexOf(this.config.excludeTypes.ToUpper().Split(','), urn.Type.ToUpper()) == -1)
                            {
                                // Split into dependant / independant types
                                if (Array.IndexOf(Config.aDependentTypeOrder, dr["DatabaseObjectTypes"].ToString()) != -1)
                                {
                                    // Add the URN to the dependent object list
                                    dependentObjects.Add(urn);
                                }
                                else
                                {
                                    // Add the sqlSmoObject to the independent object list
                                    SqlSmoObject sqlSmoObject = this.server.GetSmoObject(urn);
                                    if (sqlSmoObject is ServiceBroker)
                                    {
                                        // For ServiceBroker object, add child BrokerServices and not self
                                        foreach (BrokerService brokerService in (sqlSmoObject as ServiceBroker).Services)
                                        {
                                            this.independentObjectsLast.Add(brokerService);
                                        }
                                    }
                                    else
                                    {
                                        // Other objects
                                        independentObjectsFirst.Add(sqlSmoObject);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Export database creation
            if (this.config.scriptDatabase)
            {
                this.ScriptObject(db);
            }

            // Sort independent objects
            SMOObjectComparer smoObjectComparer = new SMOObjectComparer();
            independentObjectsFirst.Sort(smoObjectComparer);
            this.independentObjectsLast.Sort(smoObjectComparer);

            // Export starting independent objects
            foreach (SqlSmoObject sqlSmoObject in independentObjectsFirst)
            {
                this.ScriptObject(sqlSmoObject);
            }

            // Get dependancy information, sort and export dependent objects
            if (dependentObjects.Count > 0)
            {
                // Sort and export dependent objects types
                DependencyWalker dependencyWalker = new DependencyWalker(this.server);
                DependencyTree dependencyTree =
                    dependencyWalker.DiscoverDependencies(dependentObjects.ToArray(), DependencyType.Parents);
                this.ScriptDependencyTreeNode(dependencyTree);
            }

            // Export ending independent objects
            foreach (SqlSmoObject sqlSmoObject in this.independentObjectsLast)
            {
                this.ScriptObject(sqlSmoObject);
            }

            // Output file order information
            if (this.fileBuildOrder.Count > 0)
            {
                // Get filename
                string fileName = this.config.orderFilename;
                this.logger.Debug("Creating Order file: " + fileName);
                if (this.config.outputDirectory != null)
                {
                    fileName = Path.Combine(this.config.outputDirectory, fileName);
                }

                // Create output directory if needed
                string g = Path.GetDirectoryName(fileName);
                if (g.Length != 0)
                {
                    Directory.CreateDirectory(g);
                }

                StreamWriter sw = new StreamWriter(fileName);
                foreach (string s in this.fileBuildOrder)
                {
                    sw.WriteLine(s);
                }

                sw.Close();
            }
        }

        public string NameFromUrn(Urn urn)
        {
            return "[" + urn.XPathExpression.GetAttribute("Schema", urn.Type) + "].[" + urn.XPathExpression.GetAttribute("Name", urn.Type) + "]";
        }

        /// <summary>
        /// Determines whether the specified SqlSmoObject is excluded from scripting.
        /// </summary>
        /// <param name="sqlSmoObject">The SqlSmoObject.</param>
        /// <returns>
        ///     <c>true</c> if the SqlSmoObject is system or default and shoudl be excludeed; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsExcludedObject(SqlSmoObject sqlSmoObject)
        {
            // Exclude "IsSystemObject" objects
            try
            {
                bool isSystemObject = (bool)sqlSmoObject.GetType().InvokeMember(
                    "IsSystemObject",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty,
                    null,
                    sqlSmoObject,
                    null);
                if (isSystemObject)
                {
                    return true;
                }
            }
            catch (MissingMemberException)
            {
            }

            // Exclude default system MessageTypes
            if (sqlSmoObject is MessageType)
            {
                if (((MessageType)sqlSmoObject).ID <= 65535)
                {
                    return true;
                }
            }

            // Exclude default system ServiceContractS
            if (sqlSmoObject is ServiceContract)
            {
                if (((ServiceContract)sqlSmoObject).ID <= 65535)
                {
                    return true;
                }
            }

            // Exclude default system ServiceQueues (Bit 7 128?)
            if (sqlSmoObject is ServiceQueue)
            {
                if (Array.IndexOf(Config.aDefaultServiceQueues, sqlSmoObject.ToString()) != -1)
                {
                    return true;
                }
            }

            // Exclude default system ServiceRoutes
            // A single default route [AutoCreatedLocal] ID:65536 exists
            // 65537 is user created
            if (sqlSmoObject is ServiceRoute)
            {
                if (((ServiceRoute)sqlSmoObject).ID <= 65536)
                {
                    return true;
                }
            }

            // Exclude default system BrokerService
            if (sqlSmoObject is BrokerService)
            {
                if (((BrokerService)sqlSmoObject).ID <= 65535)
                {
                    return true;
                }
            }

            // Exclude "microsoft_database_tools_support" objects
            try
            {
                ExtendedPropertyCollection epc =
                    (ExtendedPropertyCollection)sqlSmoObject.GetType().InvokeMember(
                        "ExtendedProperties",
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty,
                        null,
                        sqlSmoObject,
                        null);
                if (epc.Contains("microsoft_database_tools_support"))
                {
                    return true;
                }
            }
            catch (MissingMemberException)
            {
            }

            return false;
        }

        /// <summary>
        /// Converts a set of SQL statements to suitable string.
        /// </summary>
        /// <param name="objectStatements">The SQL statements.</param>
        /// <returns>Formatted script</returns>
        private static string CleanStatements(StringCollection objectStatements)
        {
            StringBuilder objectScript = new StringBuilder();

            // Write statements to script
            if (objectStatements != null)
            {
                foreach (string statement in objectStatements)
                {
                    char[] charsToTrim = { ' ', (char)9, (char)13, (char)10 };
                    objectScript.AppendLine(statement.Trim(charsToTrim));
                    objectScript.AppendLine("GO");
                }
            }

            return objectScript.ToString();
        }

        /// <summary>
        /// Escapes SQL text literals for use in sql queries.
        /// </summary>
        /// <param name="s">The string to escape.</param>
        /// <returns>The escaped SQL text.</returns>
        private static string EscapeSQLText(string s)
        {
            return s.Replace("'", "''");
        }

        /// <summary>
        /// Neatens up a table creation script.
        /// Collapses redundant ALTER TABLE (CHECK|NOCHECK) CONSTRAINTS.
        /// </summary>
        /// <param name="statements">The table creation statement collection.</param>
        private static void CleanupTableCreation(StringCollection statements)
        {
            /*SET ANSI_NULLS ON
            GO
            SET QUOTED_IDENTIFIER ON
            GO
            SET ANSI_PADDING ON*/

            StringCollection necessaryStatements = new StringCollection();
            StringCollection redundantStatements = new StringCollection();

            Regex modConstraintRegEx =
                new Regex(
                    @"ALTER\sTABLE\s+(?<table>[^\s]+)\s+(?<type>CHECK|NOCHECK)\sCONSTRAINT\s+(?<constraint>\[[^\]]+\])");
            Regex addConstraintRegEx =
                new Regex(
                    @"ALTER\sTABLE\s+(?<table>[^\s]+)\s+WITH\s+(?<type>CHECK|NOCHECK)\s+ADD\s+CONSTRAINT\s+(?<constraint>\[[^\]]+\])");

            // Walk thru statements checking for "ADD CONSTRAINT
            foreach (string statement in statements)
            {
                Match addMatch = addConstraintRegEx.Match(statement);
                if (addMatch.Success)
                {
                    // This is an ALTER TABLE ADD CONSTRAINT
                    // Find any ALTER TABLE WITH CHECK/NOCHECK that change it
                    string outType = addMatch.Groups["type"].Value;
                    foreach (string modStatement in statements)
                    {
                        Match modMatch = modConstraintRegEx.Match(modStatement);
                        if (modMatch.Groups["table"].Value.Equals(addMatch.Groups["table"].Value)
                            && modMatch.Groups["constraint"].Value.Equals(addMatch.Groups["constraint"].Value))
                        {
                            outType = modMatch.Groups["type"].Value;
                            redundantStatements.Add(modStatement);
                        }
                    }

                    // Cut out old state and add in latest one
                    string newstatement =
                        statement.Remove(addMatch.Groups["type"].Index, addMatch.Groups["type"].Length).Insert(
                            addMatch.Groups["type"].Index, outType);
                    necessaryStatements.Add(newstatement);
                }
                else if (redundantStatements.Contains(statement))
                {
                    // Ignore redundant statements
                }
                else
                {
                    necessaryStatements.Add(statement);
                }
            }

            statements.Clear();
            foreach (string s in necessaryStatements)
            {
                statements.Add(s);
            }
        }

        /// <summary>
        /// Clean up the filename.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>Cleaned file name</returns>
        private static string CleanFilename(string fileName)
        {
            // Convert strange characters for filenames
            // fileName = Regex.Replace(fileName, @"[<>;:*/#\|\?\\]", "_");
            // Other characters not good for filenames
            // fileName = Regex.Replace(fileName, @"[@!%',{}=\[\]\$\(\)\^]", "-");
            // And a couple more
            // fileName = Regex.Replace(fileName, @"[~+\.]", "-");

            // Replace " []" with "."
            fileName = Regex.Replace(fileName, @"[ \[\]]", ".");

            // Replace invalid filename characters with "."
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '.');
            }

            // Replace ".." with "."
            while (fileName.Contains(".."))
            {
                fileName = fileName.Replace("..", ".");
            }

            // Remove starting & trailing "."
            fileName = fileName.Trim('.');

            return fileName;
        }

        private void ScriptDependencyTreeNode(DependencyTreeNode dependencyTreeNode)
        {
            // Check exported list for current item, exit if already resolved
            if ((dependencyTreeNode.Urn != null) && this.scriptedUrns.Contains(dependencyTreeNode.Urn.ToString()))
            {
                return;
            }

            // Check if we're trying to scripting an object we're already attempting to resolve.
            if (this.resolvingDependenciesOn.Exists(i => i.Urn == dependencyTreeNode.Urn))
            {
                if (!this.config.scriptForeignKeysSeparately)
                {
                    string circle = string.Empty;
                    foreach (var item in this.resolvingDependenciesOn)
                    {
                        if (item.Urn != null)
                        {
                            circle = circle + this.NameFromUrn(item.Urn) + " > ";
                        }
                    }

                    this.logger.Warning("WARNING: Circular Reference (consider \"/sfks\"): " + circle + this.NameFromUrn(dependencyTreeNode.Urn));
                }

                return;
            }

            // Export dependancies first
            if (dependencyTreeNode.HasChildNodes)
            {
                // Load dependencies into sortable list
                List<DtnSmo> children = new List<DtnSmo>();
                {
                    DependencyTreeNode dependencyTreeNodeChild = dependencyTreeNode.FirstChild;
                    while (dependencyTreeNodeChild != null)
                    {
                        DtnSmo sortUnit = default(DtnSmo);
                        sortUnit.dependencyTreeNode = dependencyTreeNodeChild;
                        sortUnit.namedSmoObject = (NamedSmoObject)this.server.GetSmoObject(dependencyTreeNodeChild.Urn);
                        children.Add(sortUnit);
                        dependencyTreeNodeChild = dependencyTreeNodeChild.NextSibling;
                    }
                }

                // Sort dependencies
                children.Sort(new DependencyComparer());

                // Add current object to "resolving dependencies" list
                this.resolvingDependenciesOn.Add(dependencyTreeNode);

                // Recursively call self to script dependencies
                foreach (DtnSmo dependencyTreeNodeChild1 in children)
                {
                    string objServer = string.Empty;
                    string objDb = string.Empty;
                    string depServer = string.Empty;
                    string depDb = string.Empty;

                    // Don't script cross database dependancies
                    if (dependencyTreeNode.Urn != null)
                    {
                        objServer = dependencyTreeNode.Urn.XPathExpression.GetAttribute("Name", "Server");
                        objDb = dependencyTreeNode.Urn.XPathExpression.GetAttribute("Name", "Database");
                        depServer = dependencyTreeNodeChild1.dependencyTreeNode.Urn.XPathExpression.GetAttribute("Name", "Server");
                        depDb = dependencyTreeNodeChild1.dependencyTreeNode.Urn.XPathExpression.GetAttribute("Name", "Database");
                    }

                    var depSchema = dependencyTreeNodeChild1.dependencyTreeNode.Urn.XPathExpression.GetAttribute("Schema", dependencyTreeNodeChild1.dependencyTreeNode.Urn.Type);
                    var depName = dependencyTreeNodeChild1.dependencyTreeNode.Urn.XPathExpression.GetAttribute("Name", dependencyTreeNodeChild1.dependencyTreeNode.Urn.Type);
                    if (objServer == depServer && objDb == depDb)
                    {
                        this.ScriptDependencyTreeNode(dependencyTreeNodeChild1.dependencyTreeNode);
                    }
                    else
                    {
                        this.logger.Debug(string.Format("Skipping external dependancy: [{1}].[{2}].[{3}]", depServer, depDb, depSchema, depName));
                    }
                }

                // Remove current object from "resolving dependencies" list
                this.resolvingDependenciesOn.Remove(dependencyTreeNode);
            }

            // Script the object we were originally called for
            if (dependencyTreeNode.Urn != null)
            {
                // Script object
                this.ScriptObject(this.server.GetSmoObject(dependencyTreeNode.Urn));

                // Add this object to the exported list
                this.scriptedUrns.Add(dependencyTreeNode.Urn.ToString());
            }
        }

        /// <summary>
        /// Scripts the SqlSmoObject using defined handler, or generic Script method
        /// </summary>
        /// <param name="sqlSmoObject">The SqlSmoObject to script.</param>
        private void ScriptObject(SqlSmoObject sqlSmoObject)
        {
            // Don't script excluded objects
            if (IsExcludedObject(sqlSmoObject))
            {
                return;
            }

            // Urn urn = new Urn(sqlSmoObject.Urn);
            Urn urn = sqlSmoObject.Urn;

            // Setup scripting options
            ScriptingOptions scriptingOptions = new ScriptingOptions();
            this.ConfigureScriptingOptions(scriptingOptions, urn);
            StringCollection objectStatements;
            if (this.customScriptMethods.ContainsKey(sqlSmoObject.GetType()))
            {
                objectStatements = this.customScriptMethods[sqlSmoObject.GetType()](sqlSmoObject, urn, scriptingOptions);
            }
            else
            {
                try
                {
                    // Call the "Script" method of sqlSmoObject (which may or may not exist)
                    object[] invokeMemberArgs = { scriptingOptions };
                    objectStatements =
                        sqlSmoObject.GetType().InvokeMember(
                            "Script",
                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod,
                            null,
                            sqlSmoObject,
                            invokeMemberArgs)
                        as
                        StringCollection;
                }
                catch (MissingMethodException)
                {
                    this.logger.Debug("Object doesn't provide Script method:" + urn.Type + ", " + sqlSmoObject);
                    return;
                }
            }

            // Clean if foreign key
            CleanupTableCreation(objectStatements);

            // The output script
            string objectScript = CleanStatements(objectStatements);

            // Get object Name (Remove SchemaQualify if needed)
            string name = this.ObjectName(sqlSmoObject);

            // Write script to output
            if (objectScript.Length != 0)
            {
                this.WriteScript(urn.Type, name, objectScript);
            }
        }

        private string ObjectName(SqlSmoObject sqlSmoObject)
        {
            string name;
            if (sqlSmoObject is ForeignKey)
            {
                name = this.ObjectName((sqlSmoObject as ForeignKey).Parent) + "." + (sqlSmoObject as ForeignKey).ToString();
            }
            else if (!this.config.scriptSchemaQualify && sqlSmoObject is ScriptSchemaObjectBase)
            {
                name = "[" + (sqlSmoObject as ScriptSchemaObjectBase).Name + "]";
            }
            else
            {
                name = sqlSmoObject.ToString();
            }

            return name;
        }

        /// <summary>
        /// Custom Script method for database objects.
        /// </summary>
        /// <param name="sqlSmoObject">The database.</param>
        /// <param name="urn">The urn.</param>
        /// <param name="scriptingOptions">The scripting options.</param>
        /// <returns>Empty string collection (no residual statements)</returns>
        private StringCollection ScriptDatabase(SqlSmoObject sqlSmoObject, Urn urn, ScriptingOptions scriptingOptions)
        {
            Database database = sqlSmoObject as Database;
            StringCollection databaseStatements = database.Script(scriptingOptions);
            this.WriteScript(urn.Type, sqlSmoObject.ToString(), databaseStatements.ToString());

            foreach (DatabaseDdlTrigger databaseDdlTrigger in database.Triggers)
            {
                StringCollection databaseDdlTriggerStatements = databaseDdlTrigger.Script(scriptingOptions);
                this.WriteScript(databaseDdlTrigger.Urn.Type, databaseDdlTrigger.ToString(), CleanStatements(databaseDdlTriggerStatements));
            }

            // Return empty collection
            return new StringCollection();
        }

        /// <summary>
        /// Custom Script method for table objects.
        /// </summary>
        /// <param name="sqlSmoObject">The table.</param>
        /// <param name="urn">The urn.</param>
        /// <param name="scriptingOptions">The scripting options.</param>
        /// <returns>Table creation statements</returns>
        private StringCollection ScriptTable(SqlSmoObject sqlSmoObject, Urn urn, ScriptingOptions scriptingOptions)
        {
            Table table = sqlSmoObject as Table;
            if (this.config.scriptForeignKeysSeparately)
            {
                // Don't script foreign keys
                scriptingOptions.DriAll = false;
                scriptingOptions.DriAllConstraints = false;
                scriptingOptions.DriAllKeys = false;
                scriptingOptions.DriChecks = true;
                scriptingOptions.DriClustered = true;
                scriptingOptions.DriDefaults = true;
                scriptingOptions.DriForeignKeys = false;
                scriptingOptions.DriIncludeSystemNames = true;
                scriptingOptions.DriIndexes = true;
                scriptingOptions.DriNonClustered = true;
                scriptingOptions.DriPrimaryKey = true;
                scriptingOptions.DriUniqueKeys = true;

                // scriptingOptions.DriWithNoCheck = true;

                // Queue foreign keys for later scripting
                foreach (ForeignKey fk in table.ForeignKeys)
                {
                    this.independentObjectsLast.Add(fk);
                }
            }

            StringCollection tableStatements = table.Script(scriptingOptions);
            CleanupTableCreation(tableStatements);

            // WORKAROUND: Script extended properties for primary key index (missed by SMO - Index.ExtendedProperties doesn't include the INDEX, only the CONSTRAINT extended properties)
            if (this.config.scriptExtendedProperties)
            {
                foreach (Index index in table.Indexes)
                {
                    if (index.IndexKeyType == IndexKeyType.DriPrimaryKey)
                    {
                        DataSet ds = table.Parent.ExecuteWithResults(string.Format("SELECT name, value FROM fn_listextendedproperty(NULL, 'schema', '{0}', 'table', '{1}', 'index', '{2}')", table.Schema, table.Name, index.Name));
                        if (ds.Tables[0].Rows.Count > 0)
                        {
                            foreach (DataRow row in ds.Tables[0].Rows)
                            {
                                tableStatements.Add(string.Format("EXEC sys.sp_addextendedproperty @name=N'{3}', @value=N'{4}' , @level0type=N'SCHEMA',@level0name=N'{0}', @level1type=N'TABLE',@level1name=N'{1}', @level2type=N'INDEX',@level2name=N'{2}'", EscapeSQLText(table.Schema), EscapeSQLText(table.Name), EscapeSQLText(index.Name), EscapeSQLText(row["Name"].ToString()), EscapeSQLText(row["Value"].ToString())));
                            }
                        }
                    }
                }
            }

            return tableStatements;
        }

        private StringCollection ScriptView(SqlSmoObject sqlSmoObject, Urn urn, ScriptingOptions scriptingOptions)
        {
            StringCollection tableStatements = (sqlSmoObject as View).Script(scriptingOptions);

            // Workaround - even with SchemaQualify=false, the schema is still included
            if (!scriptingOptions.SchemaQualify)
            {
                // View dec
                var createViewRegEx = new Regex(@"(?<action>[^ ]+) VIEW \[(?<schema>[^\]]+)\]\.\[(?<name>[^\]]+)\]");
                const string CreateViewSubst = "${action} VIEW [${name}]";

                var result = new StringCollection();
                foreach (var tableStatement in tableStatements)
                {
                    result.Add(createViewRegEx.Replace(tableStatement, CreateViewSubst));
                }

                tableStatements = result;
            }

            return tableStatements;
        }

        /// <summary>
        /// Configures the default scripting options
        /// </summary>
        /// <param name="scriptingOptions">The scripting options.</param>
        /// <param name="urn">The object urn.</param>
        private void ConfigureScriptingOptions(ScriptingOptions scriptingOptions, Urn urn)
        {
            // Defaults
            // scriptingOptions.AgentAlertJob = false;
            // scriptingOptions.AgentJobId = true;
            // scriptingOptions.AgentNotify = false;
            // scriptingOptions.AllowSystemObjects = true;
            // scriptingOptions.AnsiFile = false;
            // scriptingOptions.AnsiPadding = false;
            // scriptingOptions.AppendToFile = false;
            // scriptingOptions.BatchSize = 1;
            // scriptingOptions.Bindings = false;
            // scriptingOptions.ChangeTracking = false;
            // scriptingOptions.ClusteredIndexes = false;
            // scriptingOptions.ContinueScriptingOnError = false;
            // scriptingOptions.ConvertUserDefinedDataTypesToBaseType = false;
            // scriptingOptions.DdlBodyOnly = false;
            // scriptingOptions.DdlHeaderOnly = false;
            // scriptingOptions.Default = true;
            // scriptingOptions.DriAll = false;
            // scriptingOptions.DriAllConstraints = false;
            // scriptingOptions.DriAllKeys = false;
            // scriptingOptions.DriChecks = false;
            // scriptingOptions.DriClustered = false;
            // scriptingOptions.DriDefaults = false;
            // scriptingOptions.DriForeignKeys = false;
            // scriptingOptions.DriIncludeSystemNames = false;
            // scriptingOptions.DriIndexes = false;
            // scriptingOptions.DriNonClustered = false;
            // scriptingOptions.DriPrimaryKey = false;
            // scriptingOptions.DriUniqueKeys = false;
            // scriptingOptions.DriWithNoCheck = false;
            // scriptingOptions.Encoding = System.Text.UnicodeEncoding.Default;
            // scriptingOptions.EnforceScriptingOptions = false;
            // scriptingOptions.ExtendedProperties = false;
            // scriptingOptions.FileName = null;
            // scriptingOptions.FullTextCatalogs = false;
            // scriptingOptions.FullTextIndexes = false;
            // scriptingOptions.FullTextStopLists = false;
            // scriptingOptions.IncludeDatabaseContext = false;
            // scriptingOptions.IncludeDatabaseRoleMemberships = false;
            // scriptingOptions.IncludeFullTextCatalogRootPath = false;
            // scriptingOptions.IncludeHeaders = false;
            // scriptingOptions.IncludeIfNotExists = false;
            // scriptingOptions.Indexes = false;
            // scriptingOptions.LoginSid = false;
            // scriptingOptions.NoAssemblies = false;
            // scriptingOptions.NoCollation = false;
            // scriptingOptions.NoCommandTerminator = false;
            // scriptingOptions.NoExecuteAs = false;
            // scriptingOptions.NoFileGroup = false;
            // scriptingOptions.NoFileStream = false;
            // scriptingOptions.NoFileStreamColumn = false;
            // scriptingOptions.NoIdentities = false;
            // scriptingOptions.NoIndexPartitioningSchemes = false;
            // scriptingOptions.NoMailProfileAccounts = false;
            // scriptingOptions.NoMailProfilePrincipals = false;
            // scriptingOptions.NonClusteredIndexes = false;
            // scriptingOptions.NoTablePartitioningSchemes = false;
            // scriptingOptions.NoVardecimal = true;
            // scriptingOptions.NoViewColumns = false;
            // scriptingOptions.NoXmlNamespaces = false;
            // scriptingOptions.OptimizerData = false;
            // scriptingOptions.Permissions = false;
            // scriptingOptions.PrimaryObject = true;
            // scriptingOptions.SchemaQualify = true;
            // scriptingOptions.SchemaQualifyForeignKeysReferences = false;
            // scriptingOptions.ScriptBatchTerminator = false;
            // scriptingOptions.ScriptData = false;
            // scriptingOptions.ScriptDataCompression = true;
            // scriptingOptions.ScriptDrops = false;
            // scriptingOptions.ScriptOwner = false;
            // scriptingOptions.ScriptSchema = true;
            // scriptingOptions.Statistics = true;
            // scriptingOptions.TargetDatabaseEngineType = Microsoft.SqlServer.Management.Common.DatabaseEngineType.Standalone;
            // scriptingOptions.TargetServerVersion = SqlServerVersion.Version80;
            // scriptingOptions.TimestampToBinary = false;
            // scriptingOptions.ToFileOnly = false;
            // scriptingOptions.Triggers = false;
            // scriptingOptions.WithDependencies = false;
            // scriptingOptions.XmlIndexes = false;
            //

            // Overrides
            scriptingOptions.AgentAlertJob = true;
            scriptingOptions.AgentNotify = true;
            scriptingOptions.AllowSystemObjects = true;
            scriptingOptions.AnsiPadding = true;
            scriptingOptions.Bindings = true;
            scriptingOptions.ClusteredIndexes = true;
            scriptingOptions.DriAll = true;
            scriptingOptions.ExtendedProperties = this.config.scriptExtendedProperties;
            scriptingOptions.FullTextCatalogs = true;
            scriptingOptions.FullTextIndexes = true;
            scriptingOptions.IncludeIfNotExists = Array.IndexOf(Config.aIfExistsObjectTypes, urn.Type) != -1;
            scriptingOptions.Indexes = true;
            scriptingOptions.LoginSid = true;
            scriptingOptions.NoCollation = !this.config.scriptCollation; // Use collations
            scriptingOptions.NoFileGroup = !this.config.scriptFileGroup;
            scriptingOptions.NonClusteredIndexes = true;
            scriptingOptions.Permissions = true;
            scriptingOptions.SchemaQualify = this.config.scriptSchemaQualify;

            // ALTER TABLE [tProfile]  WITH NOCHECK  or ALTER TABLE [dbo].[tProfile]  WITH NOCHECK
            scriptingOptions.SchemaQualifyForeignKeysReferences = this.config.scriptSchemaQualify;

            // REFERENCES [tBranch] ([BranchId]) or REFERENCES [dbo].[tBranch] ([BranchId])
            scriptingOptions.Triggers = true;
        }

        /// <summary>
        /// Writes the script to desired output
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="objectName">Name of the object.</param>
        /// <param name="objectScript">The object script.</param>
        private void WriteScript(string objectType, string objectName, string objectScript)
        {
            this.logger.Debug(objectType + ": " + objectName);

            if (this.config.outputType == Config.OutputType.None)
            {
                this.logger.Information(objectScript);
                return;
            }

            string fileName;

            if (this.config.outputType == Config.OutputType.SingleFile)
            {
                fileName = CleanFilename(this.config.objectName ?? this.config.database) + ".sql";
            }
            else
            {
                // Filename is objectname.sql
                fileName = CleanFilename(objectName) + ".sql";
            }

            // Add prefix directory or type name
            switch (this.config.outputType)
            {
                case Config.OutputType.Tree:
                    if (objectType.Equals("Database"))
                    {
                        fileName = "Database.sql";
                    }
                    else
                    {
                        fileName = Path.Combine(objectType, fileName);
                    }

                    break;
                case Config.OutputType.Files:
                    fileName = objectType + "." + fileName;
                    break;
            }

            if (this.config.outputType != Config.OutputType.SingleFile)
            {
                // Add file to build order
                this.fileBuildOrder.Add(fileName);
            }

            // Add the output directory to filename
            if (string.IsNullOrEmpty(this.config.outputDirectory))
            {
                this.config.outputDirectory = Directory.GetCurrentDirectory();
            }

            fileName = Path.Combine(this.config.outputDirectory, fileName);

            // Make sure the directory exists (create it if necessary)
            string dirPath = Path.GetDirectoryName(fileName);

            // Create file and write script
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            // Create file and write script
            if (!File.Exists(fileName))
            {
                this.logger.Debug("Creating file: " + fileName);
            }

            StreamWriter sw = new StreamWriter(fileName, this.config.outputType == Config.OutputType.SingleFile);
            sw.Write(objectScript);
            sw.Close();
        }
    }
}
