using System;
using Elmah.Io.Client;
using log4net.Core;
using log4net.Util;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;
using ILogger = Elmah.Io.Client.ILogger;

namespace Elmah.Io.Log4Net.Test
{
    public class ElmahIoAppenderTest
    {
        Fixture _fixture;
        Mock<ILogger> _logger;
        ElmahIoAppender _sut;

        [SetUp]
        public void SetUp()
        {
            _fixture = new Fixture();
            _logger = new Mock<ILogger>();
            _sut = new ElmahIoAppender
            {
                Logger = _logger.Object
            };
        }

        [Test]
        public void CanLogMinimumMessage()
        {
            // Arrange

            // Act
            _sut.DoAppend(new LoggingEvent(new LoggingEventData()));

            // Assert
            _logger.Verify(x => x.Log(It.IsAny<Message>()));
        }

        [Test]
        public void CanLogMessage()
        {
            // Arrange
            Message message = null;
            _logger.Setup(x => x.Log(It.IsAny<Message>())).Callback<Message>(msg => message = msg);

            var now = DateTime.Now;
            var hostname = _fixture.Create<string>();

            var properties = Properties(hostname);
            var data = LoggingEventData(now, properties);

            // Act
            _sut.DoAppend(new LoggingEvent(data));

            // Assert
            Assert.That(message, Is.Not.Null);
            Assert.That(message.Severity, Is.EqualTo(Severity.Error));
            Assert.That(message.DateTime, Is.EqualTo(now.ToUniversalTime()));
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
                TimeStamp = now,
                Properties = properties,
                Domain = _fixture.Create<string>(),
                LoggerName = _fixture.Create<string>(),
                UserName = _fixture.Create<string>(),
                Message = _fixture.Create<string>(),
            };
        }
    }
}