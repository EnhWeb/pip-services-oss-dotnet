using PipServices.Commons.Log;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PipServices.Oss.ElasticSearch;
using Xunit;

namespace PipServices.Oss.Fixtures
{
    public sealed class ElasticSearchLoggerFixture
    {
        private readonly TestElasticSearchLogger Logger;

        static readonly List<string> MessageLevelStrings = new List<string> { "FATAL", "ERROR", "WARN", "INFO", "DEBUG", "TRACE" };

        public ElasticSearchLoggerFixture(TestElasticSearchLogger logger)
        {
            Logger = logger;
        }

        public void TestLogLevel()
        {
            Assert.True(Logger.Level >= LogLevel.None);
            Assert.True(Logger.Level <= LogLevel.Trace);
        }

        public void TestSimpleLogging()
        {
            const string correlationId = "abc123";

            Logger.RemoveAllSavedOutput();
            Logger.Level = LogLevel.Trace;

            Logger.Fatal(correlationId, "Fatal error message");
            Logger.Error(correlationId, "Error message");
            Logger.Warn(correlationId, "Warning message");

            Logger.Dump();

            Logger.Info(correlationId, "Information message");
            Logger.Debug(correlationId, "Debug message");
            Logger.Trace(correlationId, "Trace message");

            Logger.Dump();

            var totalLogCount = 0;
            var batches = 0;
            while (Logger.SavedMessages.TryDequeue(out var messages))
            {
                for(var m = 0; m < messages.Count; ++m)
                {
                    var message = messages[m];
                    
                    Assert.Equal(MessageLevelStrings[totalLogCount + m], message.Level);
                    var firstWord = message.Message.Substring(0, message.Message.IndexOf(' '));

                    if (Enum.TryParse(typeof(LogLevel), firstWord, out var level))
                    {
                        Assert.Equal( message.Level, level.ToString().ToUpper());
                    }

                    Assert.Equal(correlationId, message.CorrelationId);
                    if (message.Error != null)
                    {
                        Assert.Equal(correlationId, message.Error.CorrelationId);
                    }
                }

                totalLogCount += messages.Count;
                Assert.True(messages.Count <= 6);
                ++batches;
            }
            Assert.True(batches >= 2);
        }

        public void TestErrorLogging()
        {
            Logger.RemoveAllSavedOutput();
            const string correlationId = "123abc";
            try
            {
                // Raise an exception
                throw new Exception();
            }
            catch (Exception ex)
            {
                Logger.Fatal(correlationId, ex, "Fatal error");
                Logger.Error(correlationId, ex, "Recoverable error");
            }

            Logger.Dump();

            var batchCount = 1;

            while (Logger.SavedMessages.TryDequeue(out var messages))
            {
                Assert.True(batchCount <= 1);

                Assert.Equal(2, messages.Count);
                
                Assert.Equal("FATAL", messages[0].Level);
                Assert.Equal("ERROR", messages[1].Level);

                Assert.Equal(correlationId, messages[0].CorrelationId);
                Assert.Equal(correlationId, messages[1].CorrelationId);

                Assert.Equal(correlationId, messages[0].Error.CorrelationId);
                Assert.Equal(correlationId, messages[1].Error.CorrelationId);

                ++batchCount;
            }
        }
    }
}