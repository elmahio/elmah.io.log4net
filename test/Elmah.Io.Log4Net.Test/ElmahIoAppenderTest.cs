using System;
using System.Collections.Generic;
using System.Linq;
using Elmah.Io.Client;
using log4net.Core;
using log4net.Util;
using NSubstitute;
using NUnit.Framework;

namespace Elmah.Io.Log4Net.Test
{
    public class ElmahIoAppenderTest
    {
        IElmahioAPI _clientMock;
        IMessagesClient _messagesClientMock;
        ElmahIoAppender _sut;

        [SetUp]
        public void SetUp()
        {
            _clientMock = Substitute.For<IElmahioAPI>();
            _messagesClientMock = Substitute.For<IMessagesClient>();
            _clientMock.Messages.Returns(_messagesClientMock);
            _sut = new ElmahIoAppender
            {
                Client = _clientMock,
                Name = "TestLogger",
            };
        }

        [Test]
        public void CanLogWellKnownProperties()
        {
            // Arrange
            CreateMessage message = null;
            _messagesClientMock
                .When(x => x.CreateAndNotify(Arg.Any<Guid>(), Arg.Any<CreateMessage>()))
                .Do(x => message = x.Arg<CreateMessage>());

            var now = DateTime.UtcNow;
            var hostname = RandString();
            var type = RandString();
            var application = RandString();
            var user = RandString();
            var source = RandString();
            var method = RandString();
            var version = RandString();
            var url = RandString();
            var statuscode = 404;
            var correlationId = RandString();
            var category = RandString();

            var properties = new PropertiesDictionary();
            properties["hostname"] = hostname;
            properties["type"] = type;
            properties["application"] = application;
            properties["user"] = user;
            properties["source"] = source;
            properties["method"] = method;
            properties["version"] = version;
            properties["url"] = url;
            properties["statuscode"] = statuscode;
            properties["correlationid"] = correlationId;
            properties["category"] = category;
            properties["servervariables"] = new Dictionary<string, string> { { "serverVariableKey", "serverVariableValue" } };
            properties["cookies"] = new Dictionary<string, string> { { "cookiesKey", "cookiesValue" } };
            properties["form"] = new Dictionary<string, string> { { "formKey", "formValue" } };
            properties["querystring"] = new Dictionary<string, string> { { "queryStringKey", "queryStringValue" } };
            var data = LoggingEventData(now, properties);

            // Act
            _sut.DoAppend(new LoggingEvent(data));

            // Assert
            Assert.That(message, Is.Not.Null);
            Assert.That(message.Hostname, Is.EqualTo(hostname));
            Assert.That(message.Type, Is.EqualTo(type));
            Assert.That(message.Application, Is.EqualTo(application));
            Assert.That(message.User, Is.EqualTo(user));
            Assert.That(message.Source, Is.EqualTo(source));
            Assert.That(message.Method, Is.EqualTo(method));
            Assert.That(message.Version, Is.EqualTo(version));
            Assert.That(message.Url, Is.EqualTo(url));
            Assert.That(message.StatusCode, Is.EqualTo(statuscode));
            Assert.That(message.CorrelationId, Is.EqualTo(correlationId));
            Assert.That(message.Category, Is.EqualTo(category));
            Assert.That(message.ServerVariables.Any(sv => sv.Key == "serverVariableKey" && sv.Value == "serverVariableValue"));
            Assert.That(message.Cookies.Any(sv => sv.Key == "cookiesKey" && sv.Value == "cookiesValue"));
            Assert.That(message.Form.Any(sv => sv.Key == "formKey" && sv.Value == "formValue"));
            Assert.That(message.QueryString.Any(sv => sv.Key == "queryStringKey" && sv.Value == "queryStringValue"));
        }

        [Test]
        public void CanLogMinimumMessage()
        {
            // Arrange

            // Act
            _sut.DoAppend(new LoggingEvent(new LoggingEventData()));

            // Assert
            _messagesClientMock.Received().CreateAndNotify(Arg.Any<Guid>(), Arg.Any<CreateMessage>());
        }

        [Test]
        public void CanLogMessage()
        {
            // Arrange
            CreateMessage message = null;
            _messagesClientMock
                .When(x => x.CreateAndNotify(Arg.Any<Guid>(), Arg.Any<CreateMessage>()))
                .Do(x => message = x.Arg<CreateMessage>());

            var now = DateTime.UtcNow;
            var hostname = Guid.NewGuid().ToString();

            var properties = Properties(hostname);
            var data = LoggingEventData(now, properties);

            // Act
            _sut.DoAppend(new LoggingEvent(data));

            // Assert
            Assert.That(message, Is.Not.Null);
            Assert.That(message.Severity, Is.EqualTo(Severity.Error.ToString()));
            Assert.That(message.DateTime.Value.DateTime, Is.EqualTo(now));
            Assert.That(message.Hostname, Is.EqualTo(hostname));
            Assert.That(message.Data, Is.Not.Null);
            Assert.That(message.Data.Count, Is.EqualTo(1));
            Assert.That(message.Data.First().Key, Is.EqualTo("log4net:HostName"));
            Assert.That(message.Data.First().Value, Is.EqualTo(hostname));
            Assert.That(message.Application, Is.EqualTo(data.Domain));
            Assert.That(message.Category, Is.EqualTo(data.LoggerName));
            Assert.That(message.User, Is.EqualTo(data.UserName));
            Assert.That(message.Title, Is.EqualTo(data.Message));
        }

        [Test]
        public void CanLogMessageWithException()
        {
            // Arrange
            CreateMessage message = null;
            _messagesClientMock
                .When(x => x.CreateAndNotify(Arg.Any<Guid>(), Arg.Any<CreateMessage>()))
                .Do(x => message = x.Arg<CreateMessage>());

            var loggingEvent = new LoggingEvent(null, null, null, Level.Error, "A message", new ArgumentException("Oh no"));

            // Act
            _sut.DoAppend(loggingEvent);

            // Assert
            Assert.That(message, Is.Not.Null);
            Assert.That(message.Severity, Is.EqualTo(Severity.Error.ToString()));
            Assert.That(message.Data, Is.Not.Null);
            Assert.That(message.Data.Any(d => d.Key == "X-ELMAHIO-EXCEPTIONINSPECTOR"));
            Assert.That(message.Type, Is.EqualTo("System.ArgumentException"));
            Assert.That(message.Title, Is.EqualTo("A message"));
        }

        private static PropertiesDictionary Properties(string hostname)
        {
            var properties = new PropertiesDictionary();
            properties["log4net:HostName"] = hostname;
            return properties;
        }

        private static LoggingEventData LoggingEventData(DateTime now, PropertiesDictionary properties)
        {
            return new LoggingEventData
            {
                Level = Level.Error,
                TimeStampUtc = now,
                Properties = properties,
                Domain = Guid.NewGuid().ToString(),
                LoggerName = Guid.NewGuid().ToString(),
                UserName = Guid.NewGuid().ToString(),
                Message = Guid.NewGuid().ToString(),
            };
        }

        private static string RandString()
        {
            return Guid.NewGuid().ToString();
        }
    }
}