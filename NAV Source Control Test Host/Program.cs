using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using System.Runtime.Serialization;
using System.Xml;
using System.Data.Linq;
using NavScm;
using NavScm.NavInterface;
using System.IO;

namespace NavScm.TestHost
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo("Config Files\\log4net.config"));

            log.Info("Starting up...");

            var context = new NavSQLDataContext("Data Source=tbrt-sql-erp-01;Initial Catalog=\"TERRABIT 2015 DEV\";Integrated Security=True");
            var NavSqlObjects = context.NavDBObject;

            log.InfoFormat("{0} total entries in NavSqlObjects", NavSqlObjects.Count());

            var query = from sql in NavSqlObjects
                        where sql.Modified == 1 && sql.Type > 0
                        select sql;

            int count = query.Count();

            log.InfoFormat("{0} modified objects detected, reading them into the cache...", count);

            var foundObjects = new Dictionary<string, NavDBObject>(count);

            // int i = 1;
            foreach (NavDBObject o in query)
            {
                //log.DebugFormat("Row {6}/{7}: Type {0}, ID {1}, Name {2}, Modified {3} {4}, Version {5}",
                //    o.Type, o.ID, o.Name, o.Date.ToShortDateString(), o.Time.ToShortTimeString(), o.Version_List, i++, count);

                foundObjects.Add(o.CacheKey, o);
            }

            log.InfoFormat("Collection has {0} entries, writing to cache.xml", foundObjects.Count);

            FileStream writer = new FileStream("cache.xml", FileMode.Create);
            DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, NavDBObject>));
            serializer.WriteObject(writer, foundObjects);
            writer.Close();

            log.Info("Reloading these entries");
            FileStream reader = new FileStream("cache.xml", FileMode.Open);
            XmlDictionaryReader xmlReader = XmlDictionaryReader.CreateTextReader(reader, new XmlDictionaryReaderQuotas());
            var loadedObjects = (Dictionary<string, NavDBObject>)serializer.ReadObject(xmlReader, true);

            DevEnvInterface devenv = new DevEnvInterface("C:\\Program Files (x86)\\Microsoft Dynamics NAV\\tbrt-nav-erp-02\\RoleTailored Client\\finsql.exe",
                "tbrt-sql-erp-01", "TERRABIT 2015 DEV");

            log.Info($"Exporting {foundObjects.Count} Objects...");
            foreach (var entry in loadedObjects)
            {
                string dirname = entry.Value.NavType.ToString();
                if (!Directory.Exists(dirname))
                {
                    log.Info($"Directory {dirname} does not exist, creating...");
                    Directory.CreateDirectory(dirname);
                }
                log.Info($"Exporting {entry.Value.ToString()} to {entry.Value.FileName}");
                devenv.Export(entry.Value, $"{Directory.GetCurrentDirectory()}\\Working Copy\\{entry.Value.FileName}");
            }

            // 5.99997 => CU TN_Test

            //log.Debug("Dumping sample object descriptor for 5.99997 ");

            //NavObject o2 = loadedObjects["5.99997"];
            //log.DebugFormat("Type {0}, ID {1}, Name {2}, Modified {3} {4}, Version {5}, FileName {6}",
            //    o2.Type, o2.ID, o2.Name, o2.Date.ToShortDateString(), o2.Time.ToShortTimeString(), o2.Version_List, o2.FileName);

            //log.Debug("=== Exporting sample objects ===");

            //devenv.Export(loadedObjects["5.80"], $"{Directory.GetCurrentDirectory()}\\CU80.txt");
            //devenv.Export(loadedObjects["5.99996"], $"{Directory.GetCurrentDirectory()}\\CU99996.txt");
            //devenv.Export(loadedObjects["5.99997"], $"{Directory.GetCurrentDirectory()}\\CU99997.txt");
            //devenv.Export(loadedObjects["5.99998"], $"{Directory.GetCurrentDirectory()}\\CU99998.txt");
            //devenv.Export(loadedObjects["1.13"], $"{Directory.GetCurrentDirectory()}\\TAB13.txt");

            ////log.Debug("=== Importing TN_WORK ===");

            //o2 = devenv.Import(loadedObjects["5.99997"], $"{Directory.GetCurrentDirectory()}\\CU99997.txt");
            //log.DebugFormat("Object after import: Type {0}, ID {1}, Name {2}, Modified {3} {4}, Version {5}",
            //    o2.Type, o2.ID, o2.Name, o2.Date.ToShortDateString(), o2.Time.ToShortTimeString(), o2.Version_List);

            //log.Debug("=== Compiling TN_WORK ===");

            //o2 = devenv.Compile(loadedObjects["5.99997"]);
            //log.DebugFormat("Object after compilation: Type {0}, ID {1}, Name {2}, Modified {3} {4}, Version {5}",
            //    o2.Type, o2.ID, o2.Name, o2.Date.ToShortDateString(), o2.Time.ToShortTimeString(), o2.Version_List);

            //NavTextObject textObj = new NavTextObject($"{Directory.GetCurrentDirectory()}\\CU80.txt");
            //log.DebugFormat("Loaded CU80.txt:{0}", textObj.ToString());

            log.Info("Shutting down...");

            Console.ReadLine();
        }
    }
}
