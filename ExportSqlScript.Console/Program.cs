// <copyright file="Program.cs" company="Ivan Hamilton, Rami Abughazaleh, and contributors">
//   Copyright (c) Ivan Hamilton, Rami Abughazaleh, and contributors. All rights reserved.
// </copyright>

namespace ExportSqlScript.Console
{
    using System;
    using System.IO;
    using NOption;

    /// <summary>
    /// Main program class
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Static Main method for program execution.
        /// </summary>
        private static void Main()
        {
            Logger logger = new Logger();

            try
            {
                Run(logger);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                Environment.ExitCode = 1;
            }
        }

        /// <summary>
        /// Instance main execution method
        /// </summary>
        /// <param name="logger">The logger.</param>
        private static void Run(Logger logger)
        {
            // Parse command line
            OptionParser op = new OptionParser();
            ParseForTypeResults parseResults = op.ParseForType(typeof(Config));
            if (parseResults == null)
            {
                DisplayUsage(logger);
                return;
            }

            Config config = new Config();
            parseResults.Apply(config);

            Exporter scripter = new Exporter(logger, config);
            scripter.Run();
        }

        /// <summary>
        /// Displays the usage help text.
        /// </summary>
        /// <param name="logger">The logger.</param>
        private static void DisplayUsage(Logger logger)
        {
            logger.Information(
                @"Generates script(s) for SQL Server 2008 R2, 2008, 2005 & 2000 database objects

" +
                Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]).ToUpper() +
                @" Server Database [Object] [/od:outdir]

  Server Database [Object]
                  Specifies server, database and database object to script.

Output format:
  /od:outdir      The output directory for generated files
  /ot:outType     Arrangement of output from scripting. 
                    File    A single file
                    Files   One file per object (filename prefixed by type)
                    Tree    One directory per object type (one file per object)
  /of:ordFile     The dependency order filename

Script Generation:
  /sdb            Script database creation
  /sc             Script collations
  /sfg            Script file groups
  /ssq            Script with schema qualifiers
  /sep            Script extended properties
  /sfks           Script foreign keys separate to table (resolves circular reference issue)

Object Selection:
  /xt:type[,type] Object types not to export
");
        }
    }
}
