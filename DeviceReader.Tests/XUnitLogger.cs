using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;

namespace DeviceReader.Tests
{
    public class XUnitLoggerProvider : ILoggerProvider
    {
        private ITestOutputHelper _output;
        public XUnitLoggerProvider(ITestOutputHelper output)
        {
            _output = output;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(_output);
        }

        public void Dispose()
        {
            return;
        }
    }

    public class XUnitLogger : ILogger,IDisposable
    {
        private ITestOutputHelper _output;
        public XUnitLogger(ITestOutputHelper output)
        {
            _output = output;
        }



        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public void Dispose()
        {            
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _output.WriteLine(formatter(state, exception));
        }
    }
}
