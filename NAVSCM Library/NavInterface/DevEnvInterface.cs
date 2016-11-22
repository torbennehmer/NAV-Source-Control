using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
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
