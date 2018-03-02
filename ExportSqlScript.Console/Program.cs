/* Copyright 2007 Ivan Hamilton.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.IO;
using NOption;

namespace ExportSqlScript.Console
{
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
        private static void Run(Logger logger)
        {
            //Parse command line
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
