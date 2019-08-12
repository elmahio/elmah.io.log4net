using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using Elmah.Io.Client;
using Elmah.Io.Client.Models;
using log4net.Appender;
using log4net.Core;
using log4net.Util;

namespace Elmah.Io.Log4Net
{
    public class ElmahIoAppender : AppenderSkeleton
    {
#if NETSTANDARD
        internal static string _assemblyVersion = typeof(ElmahIoAppender).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
#else
        internal static string _assemblyVersion = typeof(ElmahIoAppender).Assembly.GetName().Version.ToString();
#endif

        public IElmahioAPI Client;
        private Guid _logId;
        private string _apiKey;

        public string LogId
        {
            set
            {
                if (!Guid.TryParse(value, out _logId))
                {
                    throw new ArgumentException("LogId is not a GUID");
                }
            }
        }

        public string ApiKey
        {
            set { _apiKey = value; }
        }

        public string Application { get; set; }

        public override void ActivateOptions()
        {
            EnsureClient();
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            EnsureClient();

            var message = new CreateMessage
            {
                Title = loggingEvent.RenderedMessage,
                Severity = LevelToSeverity(loggingEvent.Level).ToString(),
                DateTime = loggingEvent.TimeStampUtc,
                Detail = loggingEvent.ExceptionObject?.ToString(),
                Data = PropertiesToData(loggingEvent.GetProperties()),
                Application = ResolveApplication(loggingEvent),
                Source = Source(loggingEvent),
                User = User(loggingEvent),
                Hostname = Hostname(loggingEvent),
                Type = Type(loggingEvent),
                Method = Method(loggingEvent),
                Version = Version(loggingEvent),
                Url = Url(loggingEvent),
                StatusCode = StatusCode(loggingEvent),
            };

            Client.Messages.CreateAndNotify(_logId, message);
        }

        private int? StatusCode(LoggingEvent loggingEvent)
        {
            var statusCode = String(loggingEvent, "statuscode");
            if (string.IsNullOrWhiteSpace(statusCode)) return null;
            if (!int.TryParse(statusCode, out int code)) return null;
            return code;
        }

        private string Url(LoggingEvent loggingEvent)
        {
            return String(loggingEvent, "url");
        }

        private string Version(LoggingEvent loggingEvent)
        {
            return String(loggingEvent, "version");
        }

        private string Method(LoggingEvent loggingEvent)
        {
            return String(loggingEvent, "method");
        }

        private string Source(LoggingEvent loggingEvent)
        {
            var source = String(loggingEvent, "source");
            if (!string.IsNullOrWhiteSpace(source)) return source;
            return loggingEvent.LoggerName;
        }

        private string User(LoggingEvent loggingEvent)
        {
            var user = String(loggingEvent, "user");
            if (!string.IsNullOrWhiteSpace(user)) return user;
            return loggingEvent.UserName;
        }

        private string ResolveApplication(LoggingEvent loggingEvent)
        {
            var application = String(loggingEvent, "application");
            if (!string.IsNullOrWhiteSpace(application)) return application;
            return Application ?? loggingEvent.Domain;
        }

        private string Type(LoggingEvent loggingEvent)
        {
            var type = String(loggingEvent, "type");
            if (!string.IsNullOrWhiteSpace(type)) return type;
            return loggingEvent.ExceptionObject?.GetType().FullName;
        }

        private string Hostname(LoggingEvent loggingEvent)
        {
            var hostname = String(loggingEvent, "hostname");
            if (!string.IsNullOrWhiteSpace(hostname)) return hostname;
            var log4netHostname = "log4net:HostName";
            var properties = loggingEvent.GetProperties();
            if (properties == null || properties.Count == 0 || !properties.Contains(log4netHostname)) return null;
            return properties[log4netHostname].ToString();
        }

        private List<Item> PropertiesToData(PropertiesDictionary properties)
        {
            return properties.GetKeys().Select(key => new Item {Key = key, Value = properties[key].ToString()}).ToList();
        }

        private Severity? LevelToSeverity(Level level)
        {
            if (level == Level.Emergency) return Severity.Fatal;
            if (level == Level.Fatal) return Severity.Fatal;
            if (level == Level.Alert) return Severity.Fatal;
            if (level == Level.Critical) return Severity.Fatal;
            if (level == Level.Severe) return Severity.Fatal;
            if (level == Level.Error) return Severity.Error;
            if (level == Level.Warn) return Severity.Warning;
            if (level == Level.Notice) return Severity.Information;
            if (level == Level.Info) return Severity.Information;
            if (level == Level.Debug) return Severity.Debug;
            if (level == Level.Fine) return Severity.Verbose;
            if (level == Level.Trace) return Severity.Verbose;
            if (level == Level.Finer) return Severity.Verbose;
            if (level == Level.Verbose) return Severity.Verbose;
            if (level == Level.Finest) return Severity.Verbose;

            return Severity.Information;
        }

        static string String(LoggingEvent loggingEvent, string name)
        {
            if (loggingEvent == null || loggingEvent.Properties == null || loggingEvent.Properties.Count == 0) return null;
            if (!loggingEvent.Properties.GetKeys().Any(key => key.ToLower().Equals(name.ToLower()))) return null;

            var property = loggingEvent.Properties[name.ToLower()];
            return property?.ToString();
        }

        private void EnsureClient()
        {
            if (Client == null)
            {
                ElmahioAPI api = new ElmahioAPI(new ApiKeyCredentials(_apiKey), HttpClientHandlerFactory.GetHttpClientHandler(new ElmahIoOptions()));
                api.HttpClient.Timeout = new TimeSpan(0, 0, 5);
                api.HttpClient.DefaultRequestHeaders.UserAgent.Clear();
                api.HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("Elmah.Io.Log4Net", _assemblyVersion)));
                Client = api;
            }
        }
    }
}