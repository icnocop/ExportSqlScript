using System;
using NOption;

namespace ExportSQLScript
{
    /// <summary>
    /// Configuration class holding application config
    /// </summary>
    internal class Config
    {
        /// <summary>
        /// Order to script out objects with dependancies.
        /// </summary>
        protected internal static string[] aDependentTypeOrder = {
            "UserDefinedFunction",
            "Table",
            "View",
            "StoredProcedure",
            "Default",
            "Rule",
            "Trigger",
            "UserDefinedAggregate",
            "Synonym",
            "UserDefinedDataType",
            "XmlSchemaCollection",
            "UserDefinedType",
            "PartitionScheme",
            "PartitionFunction",
            "SqlAssembly"
        };

        /// <summary>
        /// Order to script out objects with out dependancies.
        /// </summary>
        protected internal static string[] aIndependentTypeOrder = {"Role", "User", "Schema"};

        /// <summary>
        /// Objects to include "If Not Exists" in scripts.
        /// </summary>
        protected internal static string[] aIfExistsObjectTypes = {
            "Role", "MessageType", "Schema", "ServiceContract", "ServiceQueue", "ServiceRoute", "User"
        };

        /// <summary>
        /// Schemas to exclude from scripting.
        /// </summary>
        protected internal static string[] aExcludedSchemas = {"sys", "INFORMATION_SCHEMA"};

        /// <summary>
        /// Objects that purport no dependancies, but actually do. So are exported last.
        /// </summary>
        //static internal protected string[] aDependentlessObjectsLast ={ "Microsoft.SqlServer.Management.Smo.Broker.BrokerService" };
        /// <summary>
        /// SQL's own default Service Queues. No need to script them.
        /// </summary>
        protected internal static string[] aDefaultServiceQueues = {
            "[dbo].[EventNotificationErrorsQueue]",
            "[dbo].[QueryNotificationErrorsQueue]",
            "[dbo].[ServiceBrokerQueue]"
        };

        /// <summary>
        /// SQL's own default Service Brokers. No need to script them.
        /// </summary>
        protected internal static string[] aDefaultBrokerServices = {
            "[http://schemas.microsoft.com/SQL/Notifications/EventNotificationService]"
            ,
            "[http://schemas.microsoft.com/SQL/Notifications/QueryNotificationService]"
            ,
            "[http://schemas.microsoft.com/SQL/ServiceBroker/ServiceBroker]"
        };

        /// <summary>
        /// Scripting file layout type
        /// </summary>
        public enum OutputType
        {
            /// <summary>Single file for all objects</summary>
            StdOut,
            /// <summary>One file per object, name prefixed by type</summary>
            Files,
            /// <summary>One file per object, split into directories by type</summary>
            Tree
        }

        /// <summary>Output file layout</summary>
        [KeyValueCommandLineOption("ot")] protected internal OutputType outputType = OutputType.StdOut;

        [KeyValueCommandLineOption("od")] protected internal string outputDirectory = null;
        [KeyValueCommandLineOption("of")] protected internal string orderFilename = "fileOrder.txt";

        //Output script 
        [FlagCommandLineOption("sdb")] protected internal bool scriptDatabase = false;
        [FlagCommandLineOption("sc")] protected internal bool scriptCollation = false;
        [FlagCommandLineOption("sfg")] protected internal bool scriptFileGroup = false;
        [FlagCommandLineOption("ssq")] protected internal bool scriptSchemaQualify = false;
        [FlagCommandLineOption("sep")] protected internal bool scriptExtendedProperties = false;
        [FlagCommandLineOption("sfks")] protected internal bool scriptForeignKeysSeparately = false;

        //Object selection
        [ArgumentCommandLineOption(1), RequiredCommandLineOption] protected internal String server = null;
        [ArgumentCommandLineOption(2), RequiredCommandLineOption] protected internal String database = null;
        [ArgumentCommandLineOption(3)] protected internal String objectName = null;

        [KeyValueCommandLineOption("xt")] protected internal String excludeTypes = "";

        //Connection info
        [KeyValueCommandLineOption("U")] protected internal String userName = null;
        [KeyValueCommandLineOption("P")] protected internal String password = null;
    }
}