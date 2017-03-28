using System;
using System.Linq;
using System.Threading;
using Elmah.Io.Log4Net;
using log4net.Repository.Hierarchy;
using log4netassembly = log4net;

namespace elmah.io.log4net.console
{
    class Program
    {
        static void Main(string[] args)
        {
            log4netassembly.Config.XmlConfigurator.Configure();
            var log = log4netassembly.LogManager.GetLogger(typeof(Program));

            // Set custom data
            //SetCustomData();

            log4netassembly.GlobalContext.Properties["ApplicationIdentifier"] = "MyCoolApp";
            log4netassembly.ThreadContext.Properties["ThreadId"] = Thread.CurrentThread.ManagedThreadId;

            log.Debug("This is a debug message");
            log.Error("This is an error message", new Exception());
            log.Fatal("This is a fatal message");
            log.Info("This is an information message");
            log.Warn("This is a warning message");

            Console.ReadLine();
        }

        /// <summary>
        /// This method set a custom version number on all messages sent to elmah.io through log4net.
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        private static void SetCustomData()
        {
            Hierarchy hier = log4netassembly.LogManager.GetRepository() as Hierarchy;

            // Get ADONetAppender
            var elmahIoAppender = 
                (ElmahIoAppender)(hier?.GetAppenders())
                .FirstOrDefault(appender => appender.Name.Equals("ElmahIoAppender", StringComparison.InvariantCultureIgnoreCase));

            if (elmahIoAppender == null) return;

            elmahIoAppender.Client.Messages.OnMessage += (sender, args) =>
            {
                args.Message.Version = "1.0.0";
            };
        }
    }
}
