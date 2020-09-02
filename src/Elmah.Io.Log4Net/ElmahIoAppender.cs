using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
#if NETSTANDARD
using System.Reflection;
#endif
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

        private const string HostnameKey = "hostname";
        private const string QueryStringKey = "querystring";
        private const string FormKey = "form";
        private const string CookiesKey = "cookies";
        private const string ServerVariablesKey = "servervariables";
        private const string StatusCodeKey = "statuscode";
        private const string UrlKey = "url";
        private const string VersionKey = "version";
        private const string MethodKey = "method";
        private const string SourceKey = "source";
        private const string UserKey = "user";
        private const string ApplicationKey = "application";
        private const string TypeKey = "type";
        private readonly string[] knownKeys = new[] { HostnameKey, QueryStringKey, FormKey, CookiesKey, ServerVariablesKey, StatusCodeKey, UrlKey, VersionKey, MethodKey, SourceKey, UserKey, ApplicationKey, TypeKey };

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

            var properties = loggingEvent.GetProperties();
            var message = new CreateMessage
            {
                Title = loggingEvent.RenderedMessage,
                Severity = LevelToSeverity(loggingEvent.Level).ToString(),
                DateTime = loggingEvent.TimeStampUtc,
                Detail = loggingEvent.ExceptionObject?.ToString(),
                Data = PropertiesToData(properties, loggingEvent.ExceptionObject),
                Application = ResolveApplication(loggingEvent, properties),
                Source = Source(loggingEvent, properties),
                User = User(loggingEvent, properties),
                Hostname = Hostname(properties),
                Type = Type(loggingEvent, properties),
                Method = Method(properties),
                Version = Version(properties),
                Url = Url(properties),
                StatusCode = StatusCode(properties),
                ServerVariables = ServerVariables(loggingEvent),
                Cookies = Cookies(loggingEvent),
                Form = Form(loggingEvent),
                QueryString = QueryString(loggingEvent),
            };

            Client.Messages.CreateAndNotify(_logId, message);
        }

        private IList<Item> QueryString(LoggingEvent loggingEvent)
        {
            return Items(loggingEvent, QueryStringKey);
        }

        private IList<Item> Form(LoggingEvent loggingEvent)
        {
            return Items(loggingEvent, FormKey);
        }

        private IList<Item> Cookies(LoggingEvent loggingEvent)
        {
            return Items(loggingEvent, CookiesKey);
        }

        private IList<Item> ServerVariables(LoggingEvent loggingEvent)
        {
            return Items(loggingEvent, ServerVariablesKey);
        }

        private IList<Item> Items(LoggingEvent loggingEvent, string key)
        {
            var properties = loggingEvent.GetProperties();
            if (properties == null) return null;
            foreach (var property in properties.GetKeys())
            {
                if (property.ToLower().Equals(key))
                {
                    var value = properties[property];
                    if (value is Dictionary<string, string> values)
                    {
                        return values.Select(v => new Item(v.Key, v.Value)).ToList();
                    }
                }
            }

            return null;
        }

        private int? StatusCode(PropertiesDictionary properties)
        {
            var statusCode = String(properties, StatusCodeKey);
            if (string.IsNullOrWhiteSpace(statusCode)) return null;
            if (!int.TryParse(statusCode, out int code)) return null;
            return code;
        }

        private string Url(PropertiesDictionary properties)
        {
            return String(properties, UrlKey);
        }

        private string Version(PropertiesDictionary properties)
        {
            return String(properties, VersionKey);
        }

        private string Method(PropertiesDictionary properties)
        {
            return String(properties, MethodKey);
        }

        private string Source(LoggingEvent loggingEvent, PropertiesDictionary properties)
        {
            var source = String(properties, SourceKey);
            if (!string.IsNullOrWhiteSpace(source)) return source;
            if (loggingEvent.ExceptionObject == null) return loggingEvent.LoggerName;
            return loggingEvent.ExceptionObject.GetBaseException().Source;
        }

        private string User(LoggingEvent loggingEvent, PropertiesDictionary properties)
        {
            var user = String(properties, UserKey);
            if (!string.IsNullOrWhiteSpace(user)) return user;
            return loggingEvent.UserName;
        }

        private string ResolveApplication(LoggingEvent loggingEvent, PropertiesDictionary properties)
        {
            var application = String(properties, ApplicationKey);
            if (!string.IsNullOrWhiteSpace(application)) return application;
            return Application ?? loggingEvent.Domain;
        }

        private string Type(LoggingEvent loggingEvent, PropertiesDictionary properties)
        {
            var type = String(properties, TypeKey);
            if (!string.IsNullOrWhiteSpace(type)) return type;
            return loggingEvent.ExceptionObject?.GetBaseException().GetType().FullName;
        }

        private string Hostname(PropertiesDictionary properties)
        {
            var hostname = String(properties, HostnameKey);
            if (!string.IsNullOrWhiteSpace(hostname)) return hostname;
            var log4netHostname = "log4net:HostName";
            if (properties == null || properties.Count == 0 || !properties.Contains(log4netHostname)) return null;
            return properties[log4netHostname].ToString();
        }

        private List<Item> PropertiesToData(PropertiesDictionary properties, Exception exception)
        {
            var items = new List<Item>();
            foreach (var key in properties.GetKeys().Where(key => !knownKeys.Contains(key.ToLower())))
            {
                var value = properties[key];
                if (value != null) items.Add(new Item(key, properties[key].ToString()));
            }

            if (exception != null)
            {
                items.AddRange(exception.ToDataList());
            }

            return items;
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

        static string String(PropertiesDictionary properties, string name)
        {
            if (properties == null || properties.Count == 0) return null;
            if (!properties.GetKeys().Any(key => key.ToLower().Equals(name.ToLower()))) return null;

            var property = properties[name.ToLower()];
            return property?.ToString();
        }

        private void EnsureClient()
        {
            if (Client == null)
            {
                var api = (ElmahioAPI)ElmahioAPI.Create(_apiKey);
                api.HttpClient.Timeout = new TimeSpan(0, 0, 5);
                api.HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("Elmah.Io.Log4Net", _assemblyVersion)));
                Client = api;
            }
        }
    }
}