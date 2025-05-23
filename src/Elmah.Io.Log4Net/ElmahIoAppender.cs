﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
#if NETSTANDARD
using System.Reflection;
#endif
using Elmah.Io.Client;
using log4net.Appender;
using log4net.Core;
using log4net.Util;
using AssemblyInfo = Elmah.Io.Client.AssemblyInfo;

namespace Elmah.Io.Log4Net
{
    /// <summary>
    /// Appender for storing log4net messages to elmah.io.
    /// </summary>
    public class ElmahIoAppender : AppenderSkeleton
    {
#if NETSTANDARD
        private static readonly string _assemblyVersion = typeof(ElmahIoAppender).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        private static readonly string _elmahIoClientVersion = typeof(IElmahioAPI).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        private static readonly string _log4netAssemblyVersion = typeof(AppenderSkeleton).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
#else
        private static readonly string _assemblyVersion = typeof(ElmahIoAppender).Assembly.GetName().Version.ToString();
        private static readonly string _elmahIoClientVersion = typeof(IElmahioAPI).Assembly.GetName().Version.ToString();
        private static readonly string _log4netAssemblyVersion = typeof(AppenderSkeleton).Assembly.GetName().Version.ToString();
#endif

        private IElmahioAPI _client;
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
        private const string CorrelationIdKey = "correlationid";
        private const string CategoryKey = "category";
        private readonly string[] knownKeys = [HostnameKey, QueryStringKey, FormKey, CookiesKey, ServerVariablesKey, StatusCodeKey, UrlKey, VersionKey, MethodKey, SourceKey, UserKey, ApplicationKey, TypeKey, CorrelationIdKey, CategoryKey];

        /// <summary>
        /// The configured IElmahioAPI client to use for communicating with the elmah.io API. The appender create the client
        /// manually but you can get it to set up OnMessage actions etc.
        /// </summary>
        public IElmahioAPI Client
        {
            get
            {
                EnsureClient();
                return _client;
            }
            set
            {
                _client = value;
            }
        }

        /// <summary>
        /// The ID of the log to store log messages in.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2376:Write-only properties should not be used", Justification = "A property setter is required by NLog")]
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

        /// <summary>
        /// The API key to use when calling the elmah.io API. The API key must have the Messages | Write permission enabled.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2376:Write-only properties should not be used", Justification = "A property setter is required by NLog")]
        public string ApiKey
        {
            set { _apiKey = value; }
        }

        /// <summary>
        /// Set an application name on all log messages.
        /// </summary>
        public string Application { get; set; }

        ///<inheritdoc/>
        public override void ActivateOptions()
        {
            CreateInstallation();
        }

        /// <summary>
        /// Store a log message to elmah.io.
        /// </summary>
        protected override void Append(LoggingEvent loggingEvent)
        {
            EnsureClient();

            var properties = loggingEvent.GetProperties();
            var message = new CreateMessage
            {
                Title = loggingEvent.RenderedMessage,
                Severity = LevelToSeverity(loggingEvent.Level).ToString(),
                DateTime = DateTimeToOffset(loggingEvent.TimeStampUtc),
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
                CorrelationId = CorrelationId(properties),
                Category = Category(loggingEvent, properties),
                ServerVariables = ServerVariables(loggingEvent),
                Cookies = Cookies(loggingEvent),
                Form = Form(loggingEvent),
                QueryString = QueryString(loggingEvent),
            };
            _client.Messages.CreateAndNotify(_logId, message);
        }

        private static DateTimeOffset? DateTimeToOffset(DateTime timeStampUtc)
        {
            return timeStampUtc == DateTime.MinValue ? null : (DateTimeOffset?)timeStampUtc;
        }

        private static IList<Item> QueryString(LoggingEvent loggingEvent)
        {
            return Items(loggingEvent, QueryStringKey);
        }

        private static IList<Item> Form(LoggingEvent loggingEvent)
        {
            return Items(loggingEvent, FormKey);
        }

        private static IList<Item> Cookies(LoggingEvent loggingEvent)
        {
            return Items(loggingEvent, CookiesKey);
        }

        private static IList<Item> ServerVariables(LoggingEvent loggingEvent)
        {
            return Items(loggingEvent, ServerVariablesKey);
        }

        private static IList<Item> Items(LoggingEvent loggingEvent, string key)
        {
            var properties = loggingEvent.GetProperties();
            if (properties == null) return [];
            foreach (var property in properties.GetKeys().Where(property => property.ToLower().Equals(key)))
            {
                var value = properties[property];
                if (value is Dictionary<string, string> values)
                {
                    return [.. values.Select(v => new Item(v.Key, v.Value))];
                }
            }

            return [];
        }

        private static string CorrelationId(PropertiesDictionary properties)
        {
            return String(properties, CorrelationIdKey);
        }

        private static int? StatusCode(PropertiesDictionary properties)
        {
            var statusCode = String(properties, StatusCodeKey);
            if (string.IsNullOrWhiteSpace(statusCode)) return null;
            if (!int.TryParse(statusCode, out int code)) return null;
            return code;
        }

        private static string Url(PropertiesDictionary properties)
        {
            return String(properties, UrlKey);
        }

        private static string Version(PropertiesDictionary properties)
        {
            return String(properties, VersionKey);
        }

        private static string Method(PropertiesDictionary properties)
        {
            return String(properties, MethodKey);
        }

        private static string Source(LoggingEvent loggingEvent, PropertiesDictionary properties)
        {
            var source = String(properties, SourceKey);
            if (!string.IsNullOrWhiteSpace(source)) return source;
            return loggingEvent.ExceptionObject?.GetBaseException().Source;
        }

        private static string Category(LoggingEvent loggingEvent, PropertiesDictionary properties)
        {
            var category = String(properties, CategoryKey);
            if (!string.IsNullOrWhiteSpace(category)) return category;
            return loggingEvent.LoggerName;
        }

        private static string User(LoggingEvent loggingEvent, PropertiesDictionary properties)
        {
            var user = String(properties, UserKey);
            if (!string.IsNullOrWhiteSpace(user)) return user;
            var userName = loggingEvent.UserName;
            if (!string.IsNullOrWhiteSpace(userName) && !userName.Equals("NOT AVAILABLE")) return userName;
            return null;
        }

        private string ResolveApplication(LoggingEvent loggingEvent, PropertiesDictionary properties)
        {
            var application = String(properties, ApplicationKey);
            if (!string.IsNullOrWhiteSpace(application)) return application;
            if (!string.IsNullOrWhiteSpace(Application)) return Application;
            var domain = loggingEvent.Domain;
            if (!string.IsNullOrWhiteSpace(domain) && !domain.Equals("NOT AVAILABLE")) return domain;
            return null;
        }

        private static string Type(LoggingEvent loggingEvent, PropertiesDictionary properties)
        {
            var type = String(properties, TypeKey);
            if (!string.IsNullOrWhiteSpace(type)) return type;
            return loggingEvent.ExceptionObject?.GetBaseException().GetType().FullName;
        }

        private static string Hostname(PropertiesDictionary properties)
        {
            var hostname = String(properties, HostnameKey);
            if (!string.IsNullOrWhiteSpace(hostname)) return hostname;
            var log4netHostname = "log4net:HostName";
            if (properties != null && properties.Count > 0 && properties.Contains(log4netHostname)) return properties[log4netHostname].ToString();
#if !NETSTANDARD1_3
            var machineName = Environment.MachineName;
            if (!string.IsNullOrWhiteSpace(machineName)) return machineName;
#endif
            var computerName = Environment.GetEnvironmentVariable("COMPUTERNAME");
            if (!string.IsNullOrWhiteSpace(computerName)) return computerName;
            return null;
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

        private static Severity? LevelToSeverity(Level level)
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

        private static string String(PropertiesDictionary properties, string name)
        {
            if (properties == null || properties.Count == 0) return null;
            if (!properties.GetKeys().Any(key => key.Equals(name, StringComparison.OrdinalIgnoreCase))) return null;

            var property = properties[name.ToLower()];
            return property?.ToString();
        }

        private void EnsureClient()
        {
            if (_client == null)
            {
                var api = ElmahioAPI.Create(_apiKey, new ElmahIoOptions
                {
                    Timeout = new TimeSpan(0, 0, 5),
                    UserAgent = UserAgent(),
                });
                api.Messages.OnMessageFail += (sender, args) =>
                {
                    LogLog.Error(GetType(), args.Error.Message, args.Error);
                };
                _client = api;
            }
        }

        private static string UserAgent()
        {
            return new StringBuilder()
                .Append(new ProductInfoHeaderValue(new ProductHeaderValue("Elmah.Io.Log4Net", _assemblyVersion)).ToString())
                .Append(" ")
                .Append(new ProductInfoHeaderValue(new ProductHeaderValue("log4net", _log4netAssemblyVersion)).ToString())
                .ToString();
        }

        private void CreateInstallation()
        {
            EnsureClient();

            try
            {
                var logger = new LoggerInfo
                {
                    Type = "Elmah.Io.Log4Net",
                    Properties =
                    [
                        new Item("Layout", Layout?.ToString()),
                        new Item("Name", Name),
                        new Item("Threshold", Threshold?.ToString()),
                    ],
                    Assemblies =
                    [
                        new AssemblyInfo { Name = "Elmah.Io.Log4Net", Version = _assemblyVersion, },
                        new AssemblyInfo { Name = "Elmah.Io.Client", Version = _elmahIoClientVersion, },
                        new AssemblyInfo { Name = "log4net", Version = _log4netAssemblyVersion, }
                    ],
                    ConfigFiles = [],
                    EnvironmentVariables = [],
                };

                var installation = new CreateInstallation
                {
                    Type = ApplicationInfoHelper.GetApplicationType(),
                    Name = Application,
                    Loggers = [logger]
                };

#if NETSTANDARD
                var location = typeof(ElmahIoAppender).GetTypeInfo().Assembly.ToString();
#else
                var location = typeof(ElmahIoAppender).Assembly.Location;
#endif
                var configFilePath = Path.Combine(Path.GetDirectoryName(location), "log4net.config");
                if (File.Exists(configFilePath))
                {
                    logger.ConfigFiles.Add(new ConfigFile
                    {
                        Name = Path.GetFileName(configFilePath),
                        Content = File.ReadAllText(configFilePath),
                        ContentType = "application/xml",
                    });
                }

                // Include environment variables from all possible sources since we don't know in which context log4net is being executed.
                EnvironmentVariablesHelper.GetElmahIoAppSettingsEnvironmentVariables().ForEach(v => logger.EnvironmentVariables.Add(v));
                EnvironmentVariablesHelper.GetAspNetCoreEnvironmentVariables().ForEach(v => logger.EnvironmentVariables.Add(v));
                EnvironmentVariablesHelper.GetDotNetEnvironmentVariables().ForEach(v => logger.EnvironmentVariables.Add(v));
                EnvironmentVariablesHelper.GetAzureEnvironmentVariables().ForEach(v => logger.EnvironmentVariables.Add(v));
                EnvironmentVariablesHelper.GetAzureFunctionsEnvironmentVariables().ForEach(v => logger.EnvironmentVariables.Add(v));

                _client.Installations.Create(_logId.ToString(), installation);
            }
            catch (Exception ex)
            {
                LogLog.Error(GetType(), ex.Message, ex);
            }
        }
    }
}