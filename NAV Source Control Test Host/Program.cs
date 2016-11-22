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
            var NavSqlObjects = context.NavObject;

            log.InfoFormat("{0} total entries in NavSqlObjects", NavSqlObjects.Count());

            var query = from sql in NavSqlObjects
                        where sql.Modified == 1
                        select sql;

            int count = query.Count();

            log.InfoFormat("{0} modified objects detected, reading them...", count);

            var foundObjects = new Dictionary<string, NavObject>(count);

            int i = 1;
            foreach (NavObject o in query)
            {
                log.DebugFormat("Row {6}/{7}: Type {0}, ID {1}, Name {2}, Modified {3} {4}, Version {5}",
                    o.Type, o.ID, o.Name, o.Date.ToShortDateString(), o.Time.ToShortTimeString(), o.Version_List, i++, count);

                foundObjects.Add(o.CacheKey, o);
            }

            log.InfoFormat("Collection has {0} entries, writing to cache.xml", foundObjects.Count);

            FileStream writer = new FileStream("cache.xml", FileMode.Create);
            DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, NavObject>));
            serializer.WriteObject(writer, foundObjects);
            writer.Close();

            log.Info("Reloading these entries");
            FileStream reader = new FileStream("cache.xml", FileMode.Open);
            XmlDictionaryReader xmlReader = XmlDictionaryReader.CreateTextReader(reader, new XmlDictionaryReaderQuotas());
            var loadedObjects = (Dictionary<string, NavObject>)serializer.ReadObject(xmlReader, true);

            log.Debug("Dumping sample object 1.82004");

            NavObject o2 = loadedObjects["1.82004"];
            log.DebugFormat("Type {0}, ID {1}, Name {2}, Modified {3} {4}, Version {5}",
                o2.Type, o2.ID, o2.Name, o2.Date.ToShortDateString(), o2.Time.ToShortTimeString(), o2.Version_List);

            log.Info("Shutting down...");

            Console.ReadLine();
        }
    }
}
