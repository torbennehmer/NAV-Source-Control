using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

namespace NavScm.NavInterface
{
    public class NavFileObject : NavBaseObject, IEquatable<NavFileObject>, IComparable<NavFileObject>
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(NavFileObject));
        private Dictionary<string, string> ObjectProperties = new Dictionary<string, string>();

        public string FileName { get; private set; }

        public NavFileObject(string fileNameToLoad)
        {
            FileName = fileNameToLoad;
            ParseFile();
        }

        /// <summary>
        /// Parses the complete file header (OBJECT heading line and the following OBJECT-PROPERTIES.
        /// </summary>
        private void ParseFile()
        {
            try
            {
                // TODO: Ignores Encoding at this point, should user CP 437, if I remember right (ouch).
                using (TextReader reader = File.OpenText(FileName))
                {
                    // 1st Line identifies object
                    string tmp = reader.ReadLine();
                    log.DebugFormat("Line read: {0}", tmp);
                    ParseHeader(tmp);

                    // 2nd Line opening braces
                    // 3rd Line opening Properties
                    // 4th Line opening braces
                    tmp = reader.ReadLine();
                    log.DebugFormat("Line read: {0}", tmp);
                    if (tmp != "{")
                        throw new InvalidDataException("Line 2 did not match '{'.");

                    tmp = reader.ReadLine();
                    log.DebugFormat("Line read: {0}", tmp);
                    if (tmp != "  OBJECT-PROPERTIES")
                        throw new InvalidDataException("Line 3 did not match '  OBJECT-PROPERTIES'.");

                    tmp = reader.ReadLine();
                    log.DebugFormat("Line read: {0}", tmp);
                    if (tmp != "  {")
                        throw new InvalidDataException("Line 4 did not match '  {'.");

                    // Now we'll get a buch of key=value lines like "Date=28.09.15", we pass them all into a generic function
                    // which creates a dictionary for us we can process easier. must finish with closing braces.
                    bool finished = false;
                    while (reader.Peek() >= 0)
                    {
                        tmp = reader.ReadLine();
                        log.DebugFormat("Line read: {0}", tmp);

                        // Check against block ending
                        if (tmp == "  }")
                        {
                            log.Debug("Block finished, breaking.");
                            finished = true;
                            break;
                        }
                        
                        // Parse the line, rinse and repeat.
                        ParseObjectPropertyLine(tmp);
                    }
                    if (!finished)
                        throw new InvalidDataException("The file ended prematurely inside the OBJECT-PROPERTIES block.");
                    ParseObjectProperties();
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Failed to parse {0}: {1}", FileName, ex.Message);
                log.Debug("Parser Exception:", ex);
                throw;
            }
        }

        /// <summary>
        /// Parsed the first line of the file, identifying object Type, its ID and the name.
        /// <code>
        /// OBJECT Codeunit 99000854 Inventory Profile Offsetting
        /// </code>
        /// </summary>
        /// <param name="header">the complete line read.</param>
        private void ParseHeader(string header)
        {
            Match m = Regex.Match(header, @"^OBJECT ([a-zA-Z]+) (\d+) (.*)$");
            if (! m.Success || m.Groups.Count != 4)
                throw new InvalidDataException($"File {FileName} is not a valid NAV text file, Header line is invalid.");

            switch (m.Groups[1].Value.ToLower())
            {
                case "codeunit": NavType = NavObjectType.Codeunit; break;
                case "menusuite": NavType = NavObjectType.MenuSuite; break;
                case "page": NavType = NavObjectType.Page; break;
                case "query": NavType = NavObjectType.Query; break;
                case "report": NavType = NavObjectType.Report; break;
                case "table": NavType = NavObjectType.Table; break;
                case "xmlport": NavType = NavObjectType.XmlPort; break;
                default:
                    throw new InvalidDataException($"File {FileName} contains the unsupported object type {m.Groups[1].Value}.");
            }

            if (!Int32.TryParse(m.Groups[2].Value, out int id))
                throw new InvalidDataException($"File {FileName} contains an invalid ID {m.Groups[2].Value}.");
            ID = id;

            Name = m.Groups[3].Value;
        }

        /// <summary>
        /// Parses object propery block lines, puts them into a dictionary for further processing.
        /// <code>
        ///   OBJECT-PROPERTIES
        ///   {
        ///     Date=28.09.15;
        ///     Time=12:00:00;
        ///     Version List=CMNM6.03;
        ///   }
        /// </code>
        /// </summary>
        /// <param name="tmp">A line taking the form (whitespace)(propertyname)=(propertyvalue)</param>
        private void ParseObjectPropertyLine(string line)
        {
            Match m = Regex.Match(line, @"^    ([^=]+)=(.*);$");
            if (!m.Success || m.Groups.Count != 3)
                throw new InvalidDataException($"File {FileName} is not a valid NAV text file, a property line is invalid.");

            string key = m.Groups[1].Value.ToLower();
            string value = m.Groups[2].Value;

            ObjectProperties[key] = value;
            log.DebugFormat("Added Property {0} => {1}", key, value);
        }

        /// <summary>
        /// Parses the properties read earlier and validates them as good as possible.
        /// </summary>
        private void ParseObjectProperties()
        {
            log.DebugFormat("Found {0} Properties:", ObjectProperties.Count);

            // TODO: Parsing of file Timestamps is currently using system culture, needs to be checked if this is correct
            // No idea, what NAV does at this point, files seem to use local time formats (ouch).

            if (!ObjectProperties.ContainsKey("date"))
                throw new InvalidDataException("Key 'date' not found in Object Properties.");

            // TODO: No idea, how this behaves in an international environment. Can't be the solution to hardcode this.
            if (!DateTime.TryParseExact(ObjectProperties["date"], "dd.MM.yy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime tmpDate))
                throw new InvalidDataException($"Could not parse value '{ObjectProperties["date"]}' of key 'date' into a DateTime.");
            ModifiedDate = tmpDate;

            if (!ObjectProperties.ContainsKey("time"))
                throw new InvalidDataException("Key 'time' not found in Object Properties.");
            if (!DateTime.TryParseExact(ObjectProperties["time"], "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out tmpDate))
                throw new InvalidDataException($"Could not parse value '{ObjectProperties["time"]}' of key 'time' into a DateTime.");
            ModifiedTime = tmpDate;

            if (!ObjectProperties.ContainsKey("version list"))
                throw new InvalidDataException("Key 'version list' not found in Object Properties.");
            VersionList = ObjectProperties["version list"];
        }

        public bool Equals(NavFileObject other)
        {
            return base.Equals(other);
        }

        public int CompareTo(NavFileObject other)
        {
            return base.CompareTo(other);
        }

    }
}
