using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace NAVSCM
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo("Config Files\\log4net.config"));

            log.Info("Starting up...");

            log.Debug("Debug");
            log.Warn("Warn");
            log.Error("Error");
            log.Fatal("Fatal");

            log.Info("Shutting down...");

            Console.ReadLine();
        }
    }
}
