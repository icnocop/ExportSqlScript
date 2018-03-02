# Export SQL Script

Command line driven utility to export MS SQL objects to script files suitable for database creation and revision control.
Uses 2008R2 Server Management Objects (SMO) which are compatible with SQL Server 2000, SQL Server 2005, SQL Server 2008 and SQL Server 2008 R2.

## Quick Start
### Running the tool
In a nutshell
{{
EXPORTSQLSCRIPT Server Database
}}
That'll script to StdOut by default. 

Chances are, you'll want the file based tree: "/ot:Tree", dependency order info: "/of:_file_", a particular output directory: "/od:_outdir_" & schema info "/ssq"
{{
ExportSQLScript.exe localhost AdventureWorks /ot:Tree /of:CreationOrder.txt /od:"outdir" /ssq
}}

### Recreating the DB
This tool only provides output scripts. But something simple like CMD.EXE can achieve this:
{{
C:\>sqlcmd -E -S localhost -Q "CREATE DATABASE MYDB"

C:\>for /F "" %i in (CreationOrder.txt) do sqlcmd -E -S localhost -d MYDB -f 65001 -i %i
}}

You'll need to change "%i" to "%%i" for use inside a batch file.
"-f 65001" tells SQLCMD to use UTF-8, otherwise extended characters may incorrectly translated.

You may need the Microsoft® SQL Server® 2008 R2 Shared Management Objects:
[ http://www.microsoft.com/downloads/en/details.aspx?displaylang=en&FamilyID=ceb4346f-657f-4d28-83f5-aae0c5c83d52]( http://www.microsoft.com/downloads/en/details.aspx?displaylang=en&FamilyID=ceb4346f-657f-4d28-83f5-aae0c5c83d52)

## Why?
I had two needs:
# Turn a database into a set of scripts able to create the database
# Turn a database into a set of scripts suitable to version control

**The dependency problem**
If you create all objects within a single file:
* You can include them in dependency order.
* If you modify a dependency the whole file changes drastically, confusing diff tools
If you create all objects in separate files:
* You can include them in dependency file in order.
* If you modify a dependency a small section of the dependency file changes, NOT confusing diff tools

**SQL Server Management Studio**
SQL Server Management Studio's "Generate SQL Server Scripts Wizard" appeared to be the right tool for the job but:
* After fulfilling dependencies, objects were created in no particular order (could change between DB instances)
* Some export options didn't function at all
* Doesn't lend itself to automation

## An Answer
A command line tool for exporting schema objects from SQL databases.
Creates one file in creation order, or mutiple files including a creation order file.
A work in progress, but useful enough to share.

## Credits

Ivan Hamilton's original source code from [codeplex](https://archive.codeplex.com/?p=exportsqlscript)