using System;
using Elmah.Io.Client;
using Elmah.Io.Client.Models;
using log4net.Core;
using log4net.Util;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;

namespace Elmah.Io.Log4Net.Test
{
    public class ElmahIoAppenderTest
    {
        Fixture _fixture;
        Mock<IElmahioAPI> _clientMock;
        Mock<IMessages> _messagesMock;
        ElmahIoAppender _sut;

        [SetUp]
        public void SetUp()
        {
            _fixture = new Fixture();
            _clientMock = new Mock<IElmahioAPI>();
            _messagesMock = new Mock<IMessages>();
            _clientMock.Setup(x => x.Messages).Returns(_messagesMock.Object);
            _sut = new ElmahIoAppender
            {
                Client = _clientMock.Object
            };
        }

        [Test]
        public void CanLogMinimumMessage()
        {
            // Arrange

            // Act
            _sut.DoAppend(new LoggingEvent(new LoggingEventData()));

            // Assert
            _messagesMock.Verify(x => x.CreateAndNotify(It.IsAny<Guid>(), It.IsAny<CreateMessage>()));
        }

        [Test]
        public void CanLogMessage()
        {
            // Arrange
            CreateMessage message = null;
            _messagesMock
                .Setup(x => x.CreateAndNotify(It.IsAny<Guid>(), It.IsAny<CreateMessage>()))
                .Callback<Guid, CreateMessage>((logId, msg) =>
                {
                    message = msg;
                });

            var now = DateTime.UtcNow;
            var hostname = _fixture.Create<string>();

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
                Domain = _fixture.Create<string>(),
                LoggerName = _fixture.Create<string>(),
                UserName = _fixture.Create<string>(),
                Message = _fixture.Create<string>(),
            };
        }
    }
}