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
    class DevEnvInterface
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(DevEnvInterface));

        /// <summary>
        /// Helper structure to capture the full execution result of finsql.exe.
        /// </summary>
        public struct CommandResult
        {
            /// <summary>
            /// The exit code of finsql.exe. So far this is always 0, tests pending.
            /// </summary>
            public int ExitCode;

            /// <summary>
            /// True, if the command executed successfully.
            /// </summary>
            public bool Success;

            /// <summary>
            /// The output produced by the command, as written to navcommandresult.txt by finsql.exe.
            /// </summary>
            public string CommandOutput;

            /// <summary>
            /// The errormessage if any, as written to the log file passed to finsql.exe. Empty, if
            /// the call was successful.
            /// </summary>
            public string ErrorMessage;

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
            Contract.Ensures(Directory.Exists(Contract.Result<string>()));
            Contract.Ensures(! Directory.EnumerateFileSystemEntries(Contract.Result<string>()).Any());

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
            Contract.Ensures(!Directory.Exists(tempPath));

            string errorLog = $"{tempPath}\\error.log";
            string commandOutput = $"{tempPath}\\navcommandresult.txt";
            string fullArguments = $"{command},LogFile=\"{errorLog}\",ServerName=\"{DatabaseServer}\",Database=\"{DatabaseName}\"";

            log.DebugFormat("ExecuteCommand: Working in {0}", tempPath);
            log.InfoFormat("ExecuteCommand: Executing: {0} {1}", DevEnvPath, fullArguments);

            // Execute finsql.exe and wait for its exit...
            Process process = new Process();
            Contract.Ensures(process.HasExited);

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

            // Parse and store finsql.exe result information
            CommandResult result;
            if (File.Exists(errorLog))
            {
                result = new CommandResult(process.ExitCode, false, File.ReadAllText(commandOutput), File.ReadAllText(errorLog));
                log.ErrorFormat("ExecuteCommand: Command failed with: {0}", result.ErrorMessage);
                log.DebugFormat("ExecuteCommand: Command result: {0}", result.CommandOutput);
                log.DebugFormat("ExecuteCommand: finsql.exe finished with exit code {0}", process.ExitCode);
            }
            else
            {
                result = new CommandResult(process.ExitCode, true, File.ReadAllText(commandOutput), "");
                log.Info("ExecuteCommand: Command executed successfully");
                log.DebugFormat("ExecuteCommand: Command result: {0}", result.CommandOutput);
            }

            // Do some cleanup and be done with it.
            foreach (var entry in Directory.EnumerateFileSystemEntries(tempPath))
            {
                log.DebugFormat("ExecuteCommand: Deleting File {0}", entry);
                File.Delete(entry);
            }
            Directory.Delete(tempPath);

            return result;
        }

        /// <summary>
        /// Exports a given NAV object to disk.
        /// </summary>
        /// <param name="obj">The NAV object as taken from the SQL database or from the cache (doesn't matter).</param>
        /// <param name="destinationFileName">The name of the destination file. The system ensures, that the file
        /// ends with .txt, as finsql.exe deduces the export format from the destiation files extension (crap).</param>
        public void Export(NavObject obj, string destinationFileName)
        {
            Contract.Requires(obj != null);
            Contract.Requires(destinationFileName != "");
        }

    }
}
