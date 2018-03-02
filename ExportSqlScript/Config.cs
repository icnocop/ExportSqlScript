using System;
using NOption;

namespace ExportSqlScript
{
    /// <summary>
    /// Configuration class holding application config
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Order to script out objects with dependancies.
        /// </summary>
        public static string[] aDependentTypeOrder = {
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
        public static string[] aIfExistsObjectTypes = {
            "Role", "MessageType", "Schema", "ServiceContract", "ServiceQueue", "ServiceRoute", "User"
        };

        /// <summary>
        /// Schemas to exclude from scripting.
        /// </summary>
        public static string[] aExcludedSchemas = {"sys", "INFORMATION_SCHEMA"};

        /// <summary>
        /// Objects that purport no dependancies, but actually do. So are exported last.
        /// </summary>
        //static internal protected string[] aDependentlessObjectsLast ={ "Microsoft.SqlServer.Management.Smo.Broker.BrokerService" };
        /// <summary>
        /// SQL's own default Service Queues. No need to script them.
        /// </summary>
        public static string[] aDefaultServiceQueues = {
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
            /// <summary>Do not output to a file</summary>
            None,

            /// <summary>Single file for all objects</summary>
            SingleFile,

            /// <summary>One file per object, name prefixed by type</summary>
            Files,

            /// <summary>One file per object, split into directories by type</summary>
            Tree
        }

        /// <summary>Output file layout</summary>
        [KeyValueCommandLineOption("ot")]
        public OutputType outputType = OutputType.None;

        [KeyValueCommandLineOption("od")]
        public string outputDirectory = null;

        [KeyValueCommandLineOption("of")]
        public string orderFilename = "fileOrder.txt";

        //Output script 
        [FlagCommandLineOption("sdb")]
        public bool scriptDatabase = false;

        [FlagCommandLineOption("sc")]
        public bool scriptCollation = false;

        [FlagCommandLineOption("sfg")]
        public bool scriptFileGroup = false;

        [FlagCommandLineOption("ssq")]
        public bool scriptSchemaQualify = false;

        [FlagCommandLineOption("sep")]
        public bool scriptExtendedProperties = false;

        [FlagCommandLineOption("sfks")]
        public bool scriptForeignKeysSeparately = false;

        //Object selection
        [ArgumentCommandLineOption(1), RequiredCommandLineOption]
        public String server = null;

        [ArgumentCommandLineOption(2), RequiredCommandLineOption]
        public String database = null;

        [ArgumentCommandLineOption(3)]
        public String objectName = null;

        [KeyValueCommandLineOption("xt")]
        public String excludeTypes = "";

        //Connection info
        [KeyValueCommandLineOption("U")]
        protected internal String userName = null;

        [KeyValueCommandLineOption("P")]
        protected internal String password = null;
    }
}