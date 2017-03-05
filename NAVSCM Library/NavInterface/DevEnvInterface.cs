using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace NavScm.NavInterface
{
    /// <summary>
    /// Interfaces to the NAV IDE command line interface to export / import objects.
    /// </summary>
    /// <remarks>
    /// <para>The interface is based on the powershell snippets delivered with NAV. 
    /// Note, that the devenv does not give any return values. Instead, errors can only
    /// be detected by the existance of the log file, which is created only upon errors.</para>
    /// <para>Currently, the interface expects to be able to access the database using
    /// NTLM Single Sign on. SQL user/pass authentication is not supported.</para>
    /// </remarks>
    public class DevEnvInterface
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(DevEnvInterface));

        /// <summary>
        /// The Context to use for DB operations.
        /// </summary>
        private NavSQLDataContext navSqlContext;

        /// <summary>
        /// Helper structure to capture the full execution result of finsql.exe.
        /// </summary>
        public struct CommandResult
        {
            /// <summary>
            /// The exit code of finsql.exe. So far this is always 0, tests pending.
            /// </summary>
            public readonly int ExitCode;

            /// <summary>
            /// True, if the command executed successfully.
            /// </summary>
            public readonly bool Success;

            /// <summary>
            /// The output produced by the command, as written to navcommandresult.txt by finsql.exe.
            /// </summary>
            public readonly string CommandOutput;

            /// <summary>
            /// The errormessage if any, as written to the log file passed to finsql.exe. Empty, if
            /// the call was successful.
            /// </summary>
            public readonly string ErrorMessage;

            /// <summary>
            /// Constructs the whole object.
            /// </summary>
            /// <param name="exitCode">The exit code of finsql.exe.</param>
            /// <param name="success">Flag indicating success.</param>
            /// <param name="output">The output produced by the command, as written to navcommandresult.txt by finsql.exe.</param>
            /// <param name="errorMessage">The errormessage if any, as written to the log file passed to finsql.exe. Empty, if
            /// the call was successful.</param>
            public CommandResult(int exitCode, bool success, string commandOutput, string errorMessage)
            {
                this.ExitCode = exitCode;
                this.Success = success;
                this.CommandOutput = commandOutput;
                this.ErrorMessage = errorMessage;
            }
        }

        /// <summary>
        /// Path to the dev env executable.
        /// </summary>
        protected string DevEnvPath { get; private set; }

        /// <summary>
        /// Hostname of the database server.
        /// </summary>
        protected string DatabaseServer { get; private set; }

        /// <summary>
        /// Name of the database itself.
        /// </summary>
        protected string DatabaseName { get; private set; }

        /// <summary>
        /// Create an interface class to a given NAV developer environment.
        /// </summary>
        /// <param name="devEnvPath">Full path and name to finsql.exe (or however you call it).</param>
        public DevEnvInterface(string devEnvPath, string databaseServer, string databaseName)
        {
            Contract.Requires(devEnvPath != "");
            Contract.Requires(databaseServer != "");
            Contract.Requires(databaseName != "");
            Contract.Ensures(File.Exists(DevEnvPath));
            Contract.Ensures(databaseName == DatabaseName);
            Contract.Ensures(databaseServer == DatabaseServer);
            Contract.Ensures(devEnvPath == DevEnvPath);
            
            DevEnvPath = devEnvPath;
            if (!File.Exists(DevEnvPath))
                throw new InvalidOperationException($"The file {DevEnvPath} was not found.");

            DatabaseServer = databaseServer;
            DatabaseName = databaseName;

            navSqlContext = new NavSQLDataContext($"Data Source=\"{DatabaseServer}\";Initial Catalog=\"{DatabaseName}\";Integrated Security=True");

            if (log.IsDebugEnabled)
            {
                log.Debug($"Constructed and attached to DevEnv {DevEnvPath}");
                log.Debug($"Using database [{DatabaseName}] on server {DatabaseServer}");
            }
        }

        /// <summary>
        /// Creates a new temporary directory for finsql.exe to store its stuff into it. Creates an
        /// unique temporary directory using Path.GetTempPath and Path.GetRandomFileName, which should
        /// work easily out of the box. Retries 3 times in case of unexpected errors and aborts afterwards.
        /// </summary>
        /// <returns>The full path to the created directory, which is empty.</returns>
        protected string GetNewTempDirectory()
        {
            // Try a few times. Uniqueness should not be a problem, as GetRandomFileName is cryptographically strong,
            // but beware of other errors e.g. during director creation.
            // Note, that in theory an race condition is possible here in case GetRandomFileName doesn't behave.
            for (int i = 0; i < 3; i++)
            {
                string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                log.DebugFormat("GetNewTempDirectoy: Checking against tempPath {0}", tempPath);
                if (Directory.Exists(tempPath))
                {
                    log.ErrorFormat("GetNewTempDirectory: The directory {0} did already exist, this is highly unusual, trying again nevertheless...", tempPath);
                    continue;
                }

                DirectoryInfo dirInfo;
                try
                {
                    dirInfo = Directory.CreateDirectory(tempPath);
                }
                catch (Exception ex)
                {
                    log.ErrorFormat("GetNewTempDirectory: The directory {0} did not exist but could be created: {1}", tempPath, ex.Message);
                    log.Debug("GetNewTempDirectory: Exception Details:", ex);
                    log.Error("GetNewTempDirectory: Trying again nevertheless...");
                    continue;
                }

                Contract.Assert(!Directory.EnumerateFileSystemEntries(tempPath).Any());

                return tempPath;
            }

            log.Fatal("GetNewTempDirectory: Could not generate a new temporary directory after three attempts, this is fatal.");
            throw new InvalidOperationException("Could not create a new temp directory, this is fatal.");
        }

        /// <summary>
        /// Executes the given command with finsql.exe. Database access parameters and log file 
        /// parameters are added automatically and must not be included in the given command. 
        /// finsql.exe is run synchronously, so this call blocks until whatever you requested
        /// from it is done.
        /// </summary>
        /// <remarks>
        /// <para>Uses GetNewTempDirectory to create a directory to work in. Be aware, that all
        /// files stored in it will be deleted unconditionally after execution completes. 
        /// The directory itself is deleted as well.</para>
        /// <para>Appends LogFile, ServerName and Database arguments to the given command.</para>
        /// </remarks>
        /// <param name="command">The command to execute, excluding any database login information
        /// and log file specification. Do not add a trailing comma as well.</param>
        /// <returns>Full command execution result, see the CommandResult structure for details.</returns>
        protected CommandResult ExecuteCommand(string command)
        {
            Contract.Requires(command != "");

            string tempPath = GetNewTempDirectory();

            string errorLog = $"{tempPath}\\error.log";
            string commandOutput = $"{tempPath}\\navcommandresult.txt";
            string fullArguments = $"{command},LogFile=\"{errorLog}\",ServerName=\"{DatabaseServer}\",Database=\"{DatabaseName}\"";

            log.DebugFormat("ExecuteCommand: Working in {0}", tempPath);
            log.InfoFormat("ExecuteCommand: Executing: {0} {1}", DevEnvPath, fullArguments);

            // Execute finsql.exe and wait for its exit...
            Process process = new Process();

            process.StartInfo.FileName = DevEnvPath;
            process.StartInfo.Arguments = fullArguments;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.WorkingDirectory = tempPath;
            process.StartInfo.CreateNoWindow = true;

            Stopwatch sw = new Stopwatch();
            if (log.IsDebugEnabled)
            {
                sw.Start();
            }

            process.Start();
            process.WaitForExit();
            
            if (log.IsDebugEnabled)
            {
                sw.Stop();
                log.Debug($"ExecuteCommand: Execution took: {sw.Elapsed}");
            }

            // Parse and store finsql.exe result information, be aware, that we are not getting
            // Unicode back from finsql.exe, this is CP850 instead. Interesting, all this in the 
            // current millenia, as CP850 was introduced 1987 with DOS 3.3... Not to mention that
            // the current windows ANSI Encoding is ignored as well. (Checked up to NAV 2015).
            var finsqlEncoding = Encoding.GetEncoding(850);
            CommandResult result;
            if (File.Exists(errorLog))
            {
                result = new CommandResult(process.ExitCode, false, File.ReadAllText(commandOutput, finsqlEncoding), File.ReadAllText(errorLog, finsqlEncoding));
                log.ErrorFormat("ExecuteCommand: Command failed with: {0}", result.ErrorMessage);
                log.DebugFormat("ExecuteCommand: Command result: {0}", result.CommandOutput);
                log.DebugFormat("ExecuteCommand: finsql.exe finished with exit code {0}", process.ExitCode);
            }
            else
            {
                result = new CommandResult(process.ExitCode, true, File.ReadAllText(commandOutput, finsqlEncoding), "");
                log.DebugFormat("ExecuteCommand successful: Command result: {0}", result.CommandOutput);
            }

            // Do some cleanup and be done with it.
            foreach (var entry in Directory.EnumerateFileSystemEntries(tempPath))
            {
                log.DebugFormat("ExecuteCommand: Deleting File {0}", entry);
                File.Delete(entry);
            }
            Directory.Delete(tempPath);

            Contract.Assert(!Directory.Exists(tempPath));
            Contract.Assume(process.HasExited);

            return result;
        }

        /// <summary>
        /// Exports a given NAV object to disk.
        /// </summary>
        /// <remarks>
        /// <para>The file name must end with .txt, as finsql.exe deduces the export format from the destiation files 
        /// extension (crap). We have no other option here as to play by these rules.</para>
        /// <para>Be aware, that NAV uses some strange mix of CP850 and CP1252 to encode the text files, 
        /// this is mean stuff here. The call does not try to convert this into something more sensible
        /// at this point, especially since the IDE won't be able to handle this properly if you have to
        /// work with the files manually. Checked with NAV 2015, YMMV.</para></remarks>
        /// <para>Check http://forum.mibuso.com/discussion/37078/encoding-of-exported-navision-objects-txt-files 
        /// for further details about this.</para>
        /// <param name="obj">The NAV object as taken from the SQL database or from the cache (doesn't matter).</param>
        /// <param name="destinationFileName">The name of the destination file. The file name must end with .txt.</param>
        public void Export(NavDBObject obj, string destinationFileName)
        {
            Contract.Requires(obj != null);
            Contract.Requires(destinationFileName != "");
            Contract.Requires(Path.GetExtension(destinationFileName) == ".txt");

            // TODO: Skip Unlicensed objects?
            string command = $"Command=ExportObjects,File=\"{destinationFileName}\",Filter=\"{obj.GetFilter()}\"";
            log.DebugFormat("Export: Built command string: {0}", command);
            var result = ExecuteCommand(command);
            if (! result.Success)
            {
                throw new ArgumentException($"Cannot export object {obj.NavType} ID {obj.ID}: {result.ErrorMessage}");
            }
        }

        /// <summary>
        /// Imports a given NAV object into the database from the file given. The existing file is overwritten,
        /// schema changes are executed forcibly, so beware of possible data loss.
        /// </summary>
        /// <remarks>
        /// <para>The file name must end with .txt, as finsql.exe deduces the export format from the destiation files 
        /// extension (crap). We have no other option here as to play by these rules.</para>
        /// <para>Be aware, that NAV uses some strange mix of CP850 and CP1252 to encode the text files, 
        /// this is mean stuff here. The call does not try to convert this into something more sensible
        /// at this point, especially since the IDE won't be able to handle this properly if you have to
        /// work with the files manually. Checked with NAV 2015, YMMV.</para></remarks>
        /// <para>Check http://forum.mibuso.com/discussion/37078/encoding-of-exported-navision-objects-txt-files 
        /// for further details about this.</para>
        /// <param name="obj">The NAV object as taken from the SQL database or from the cache (doesn't matter).</param>
        /// <param name="sourceFileName">The name of the source file. The file name must end with .txt.</param>
        /// <returns>A new NavObject representing the imported object.</returns>
        public NavDBObject Import(NavDBObject obj, string sourceFileName)
        {
            Contract.Requires(obj != null);
            Contract.Requires(sourceFileName != "");
            Contract.Requires(Path.GetExtension(sourceFileName) == ".txt");

            // TODO: Skip Unlicensed objects?
            string command = $"Command=ImportObjects,File=\"{sourceFileName}\",ImportAction=overwrite,SynchronizeSchemaChanges=force";
            log.DebugFormat("Import: Built command string: {0}", command);
            var result = ExecuteCommand(command);
            if (!result.Success)
            {
                throw new ArgumentException($"Cannot import object {obj.NavType} ID {obj.ID} from file {sourceFileName}: {result.ErrorMessage}");
            }

            return navSqlContext.NavDBObject.Where(o => o.Type == obj.Type && o.ID == obj.ID).First();
        }


        /// <summary>
        /// Compiles the NavObject given and reloads its object descriptor from the database. Compilation is done with
        /// forced schema changes, so beware of possible data loss.
        /// </summary>
        /// <param name="obj">The NAV object as taken from the SQL database or from the cache (doesn't matter).</param>
        /// <returns>A new NavObject representing the imported object.</returns>
        public NavDBObject Compile(NavDBObject obj)
        {
            Contract.Requires(obj != null);

            // TODO: Skip Unlicensed objects?
            string command = $"Command=CompileObjects,Filter=\"{obj.GetFilter()}\",SynchronizeSchemaChanges=force";
            log.DebugFormat("Compile: Built command string: {0}", command);
            var result = ExecuteCommand(command);
            if (!result.Success)
            {
                throw new ArgumentException($"Cannot compile object {obj.NavType} ID {obj.ID}: {result.ErrorMessage}");
            }

            return navSqlContext.NavDBObject.Where(o => o.Type == obj.Type && o.ID == obj.ID).First();
        }
    }
}
