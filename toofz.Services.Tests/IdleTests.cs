﻿using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace toofz.Services.Tests
{
    class IdleTests
    {
        [TestClass]
        public class StartNew
        {
            [TestMethod]
            public void ReturnsInstance()
            {
                // Arrange
                TimeSpan updateInterval = TimeSpan.Zero;

                // Act
                var idle = Idle.StartNew(updateInterval);

                // Assert
                Assert.IsInstanceOfType(idle, typeof(Idle));
            }
        }

        [TestClass]
        public class WriteTimeRemaining
        {
            [TestMethod]
            public void TimeRemaining_WritesTimeRemaining()
            {
                // Arrange
                TimeSpan updateInterval = TimeSpan.FromSeconds(75);
                DateTime startTime = new DateTime(2017, 8, 27, 12, 51, 1);
                var mockLog = new Mock<ILog>();
                var log = mockLog.Object;
                var idle = new Idle(updateInterval, startTime, log);
                var from = startTime + TimeSpan.FromSeconds(60);

                // Act
                idle.WriteTimeRemaining(from);

                // Assert
                mockLog.Verify(l => l.Info("Next run takes place in 15 seconds..."));
            }

            [TestMethod]
            public void NoTimeRemaining_WritesStartingImmediately()
            {
                // Arrange
                TimeSpan updateInterval = TimeSpan.FromSeconds(75);
                DateTime startTime = new DateTime(2017, 8, 27, 12, 51, 1);
                var mockLog = new Mock<ILog>();
                var log = mockLog.Object;
                var idle = new Idle(updateInterval, startTime, log);
                var from = startTime + TimeSpan.FromSeconds(90);

                // Act
                idle.WriteTimeRemaining(from);

                // Assert
                mockLog.Verify(l => l.Info("Next run starting immediately..."));
            }
        }

        [TestClass]
        public class GetTimeRemaining
        {
            [TestMethod]
            public void ReturnsTimeRemaining()
            {
                // Arrange
                TimeSpan updateInterval = TimeSpan.FromSeconds(75);
                DateTime startTime = new DateTime(2017, 8, 27, 12, 51, 1);
                var mockLog = new Mock<ILog>();
                var log = mockLog.Object;
                var idle = new Idle(updateInterval, startTime, log);
                var from = startTime + TimeSpan.FromSeconds(60);

                // Act
                var remaining = idle.GetTimeRemaining(from);

                // Assert
                Assert.AreEqual(TimeSpan.FromSeconds(15), remaining);
            }
        }

        [TestClass]
        public class DelayAsync
        {
            [TestMethod]
            public async Task TimeRemaining_DelaysForTimeRemaining()
            {
                // Arrange
                TimeSpan updateInterval = TimeSpan.FromSeconds(75);
                DateTime startTime = new DateTime(2017, 8, 27, 12, 51, 1);
                var mockLog = new Mock<ILog>();
                var log = mockLog.Object;
                var idle = new Idle(updateInterval, startTime, log);
                var from = startTime + TimeSpan.FromSeconds(60);
                var mockTask = new Mock<ITask>();
                var task = mockTask.Object;
                var cancellationToken = CancellationToken.None;

                // Act
                await idle.DelayAsync(from, task, cancellationToken);

                // Assert
                mockTask.Verify(t => t.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
            }

            [TestMethod]
            public async Task NoTimeRemaining_DoesNotDelay()
            {
                // Arrange
                TimeSpan updateInterval = TimeSpan.FromSeconds(75);
                DateTime startTime = new DateTime(2017, 8, 27, 12, 51, 1);
                var mockLog = new Mock<ILog>();
                var log = mockLog.Object;
                var idle = new Idle(updateInterval, startTime, log);
                var from = startTime + TimeSpan.FromSeconds(90);
                var mockTask = new Mock<ITask>();
                var task = mockTask.Object;
                var cancellationToken = CancellationToken.None;

                // Act
                await idle.DelayAsync(from, task, cancellationToken);

                // Assert
                mockTask.Verify(t => t.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
            }
        }
    }
}
