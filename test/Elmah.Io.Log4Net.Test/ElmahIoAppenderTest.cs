using System;
using System.Collections.Generic;
using System.Linq;
using Elmah.Io.Client;
using Elmah.Io.Client.Models;
using log4net.Core;
using log4net.Util;
using NSubstitute;
using NUnit.Framework;

namespace Elmah.Io.Log4Net.Test
{
    public class ElmahIoAppenderTest
    {
        IElmahioAPI _clientMock;
        IMessages _messagesMock;
        ElmahIoAppender _sut;

        [SetUp]
        public void SetUp()
        {
            _clientMock = Substitute.For<IElmahioAPI>();
            _messagesMock = Substitute.For<IMessages>();
            _clientMock.Messages.Returns(_messagesMock);
            _sut = new ElmahIoAppender
            {
                Client = _clientMock
            };
        }

        [Test]
        public void CanLogWellKnownProperties()
        {
            // Arrange
            CreateMessage message = null;
            _messagesMock
                .When(x => x.CreateAndNotify(Arg.Any<Guid>(), Arg.Any<CreateMessage>()))
                .Do(x => message = x.Arg<CreateMessage>());

            var now = DateTime.UtcNow;
            var hostname = Guid.NewGuid().ToString();
            var type = Guid.NewGuid().ToString();
            var application = Guid.NewGuid().ToString();
            var user = Guid.NewGuid().ToString();
            var source = Guid.NewGuid().ToString();
            var method = Guid.NewGuid().ToString();
            var version = Guid.NewGuid().ToString();
            var url = Guid.NewGuid().ToString();
            var statuscode = 404;

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
            _messagesMock.Received().CreateAndNotify(Arg.Any<Guid>(), Arg.Any<CreateMessage>());
        }

        [Test]
        public void CanLogMessage()
        {
            // Arrange
            CreateMessage message = null;
            _messagesMock
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
            Assert.That(message.DateTime, Is.EqualTo(now));
            Assert.That(message.Hostname, Is.EqualTo(hostname));
            Assert.That(message.Data, Is.Not.Null);
            Assert.That(message.Data.Count, Is.EqualTo(1));
            Assert.That(message.Data[0].Key, Is.EqualTo("log4net:HostName"));
            Assert.That(message.Data[0].Value, Is.EqualTo(hostname));
            Assert.That(message.Application, Is.EqualTo(data.Domain));
            Assert.That(message.Source, Is.EqualTo(data.LoggerName));
            Assert.That(message.User, Is.EqualTo(data.UserName));
            Assert.That(message.Title, Is.EqualTo(data.Message));
        }

        private PropertiesDictionary Properties(string hostname)
        {
            var properties = new PropertiesDictionary();
            properties["log4net:HostName"] = hostname;
            return properties;
        }

        private LoggingEventData LoggingEventData(DateTime now, PropertiesDictionary properties)
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
    }
}