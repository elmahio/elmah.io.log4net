using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4netassembly = log4net;

namespace elmah.io.log4net.console
{
    class Program
    {
        static void Main(string[] args)
        {
            log4netassembly.Config.XmlConfigurator.Configure();
            var log = log4netassembly.LogManager.GetLogger(typeof(Program));
            log.Debug("This is a debug message");
            log.Error("This is an error message", new Exception());
            log.Fatal("This is a fatal message");
            log.Info("This is an information message");
            log.Warn("This is a warning message");
            Console.ReadLine();
        }
    }
}
