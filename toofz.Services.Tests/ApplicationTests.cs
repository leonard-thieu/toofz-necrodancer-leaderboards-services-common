﻿using System;
using System.IO;
using System.Threading.Tasks;
using log4net;
using Microsoft.ApplicationInsights.Extensibility;
using Moq;
using toofz.Services.Tests.Properties;
using Xunit;

namespace toofz.Services.Tests
{
    public class ApplicationTests
    {
        private readonly ApplicationAdapter app = new ApplicationAdapter();

        public class RunAsyncMethod : ApplicationTests
        {
            public RunAsyncMethod()
            {
                mockSettings.SetupAllProperties();
                mockSettings.SetupProperty(s => s.KeyDerivationIterations, 1);
                settings = mockSettings.Object;
                log = mockLog.Object;
            }

            private readonly Mock<ISettings> mockSettings = new Mock<ISettings>();
            private ISettings settings;
            private readonly Mock<ILog> mockLog = new Mock<ILog>();
            private ILog log;
            private readonly TelemetryConfiguration telemetryConfiguration = new TelemetryConfiguration();

            [Fact]
            public async Task LogIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                string[] args = { };
                log = null;

                // Act -> Assert
                await Assert.ThrowsAsync<ArgumentNullException>(() =>
                {
                    return app.RunAsync(args, settings, log, telemetryConfiguration);
                });
            }

            [Fact]
            public async Task InitializesLogging()
            {
                // Arrange
                string[] args = { };

                // Act
                await app.RunAsync(args, settings, log, telemetryConfiguration);

                // Assert
                mockLog.Verify(l => l.Debug("Initialized logging."));
            }

            [Fact]
            public async Task ArgsIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                string[] args = null;

                // Act -> Assert
                await Assert.ThrowsAsync<ArgumentNullException>(() =>
                {
                    return app.RunAsync(args, settings, log, telemetryConfiguration);
                });
            }

            [Fact]
            public async Task SettingsIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                string[] args = { };
                settings = null;

                // Act -> Assert
                await Assert.ThrowsAsync<ArgumentNullException>(() =>
                {
                    return app.RunAsync(args, settings, log, telemetryConfiguration);
                });
            }

            [Fact]
            public async Task ReloadsSettings()
            {
                // Arrange
                string[] args = { };

                // Act
                await app.RunAsync(args, settings, log, telemetryConfiguration);

                // Assert
                mockSettings.Verify(s => s.Reload(), Times.Once);
            }

            [Fact]
            public async Task InstrumentationKeyIsNotSet_LogsWarning()
            {
                // Arrange
                string[] args = { };
                mockSettings.SetupProperty(s => s.InstrumentationKey, null);

                // Act
                await app.RunAsync(args, settings, log, telemetryConfiguration);

                // Assert
                mockLog.Verify(l => l.Warn("An Application Insights instrumentation key is not set. Telemetry will not be reported to Application Insights."));
            }

            [Fact]
            public async Task InstrumentationKeyIsSet_SetsInstrumentationKeyForTelemetry()
            {
                // Arrange
                string[] args = { };
                mockSettings.SetupProperty(s => s.InstrumentationKey, "myInstrumentationKey");

                // Act
                await app.RunAsync(args, settings, log, telemetryConfiguration);

                // Assert
                Assert.Equal("myInstrumentationKey", telemetryConfiguration.InstrumentationKey);
            }

            [Fact]
            public async Task Returns0()
            {
                // Arrange
                string[] args = { };

                // Act
                var ret = await app.RunAsync(args, settings, log, telemetryConfiguration);

                // Assert
                Assert.Equal(0, ret);
            }

            [Trait("Category", "Uses file system")]
            public class IntegrationTests : SettingsTestsBase<TestSettings>
            {
                public IntegrationTests() : base(TestSettings.Default)
                {
                    originalCurrentDirectory = Directory.GetCurrentDirectory();
                    // Services start with their current directory set to the system directory.
                    Directory.SetCurrentDirectory(Environment.SystemDirectory);
                }

                private readonly string originalCurrentDirectory;

                protected override void Dispose(bool disposing)
                {
                    if (disposing)
                    {
                        Directory.SetCurrentDirectory(originalCurrentDirectory);
                    }

                    base.Dispose(disposing);
                }

                [Fact]
                public async Task LoadsSettingsBeforeAccessingThem()
                {
                    // Arrange
                    // Create a settings file that has the instrumentation key set
                    Directory.SetCurrentDirectory(AppContext.BaseDirectory);
                    settings.LeaderboardsConnectionString = new EncryptedSecret("myConnectionString", 1);
                    settings.Save();

                    // Reset environment
                    settings.LeaderboardsConnectionString = null;
                    Directory.SetCurrentDirectory(Environment.SystemDirectory);

                    var app = new ApplicationAdapter();
                    string[] args = { };
                    var log = Mock.Of<ILog>();
                    var telemetryConfiguration = TelemetryConfiguration.Active;

                    // Act
                    await app.RunAsync(args, settings, log, telemetryConfiguration);

                    // Assert
                    Assert.Equal("myConnectionString", settings.LeaderboardsConnectionString.Decrypt());
                }
            }
        }

        private class ApplicationAdapter : Application<ISettings>
        {
            internal override Task<int> RunAsyncOverride(string[] args, ISettings settings) => Task.FromResult(0);
        }
    }
}
