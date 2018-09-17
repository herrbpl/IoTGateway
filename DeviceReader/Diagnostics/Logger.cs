// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;

namespace DeviceReader.Diagnostics
{
    public interface ILogger
    {
        LogLevel LogLevel { get; }

        string FormatDate(long time);

        bool DebugIsEnabled { get; }
        bool InfoIsEnabled { get; }

        // The following 4 methods allow to log a message, capturing the context
        // (i.e. the method where the log message is generated)

        void Debug(string message, Action context);
        void Info(string message, Action context);
        void Warn(string message, Action context);
        void Error(string message, Action context);

        // The following 4 methods allow to log a message and some data,
        // capturing the context (i.e. the method where the log message is generated)

        void Debug(string message, Func<object> context);
        void Info(string message, Func<object> context);
        void Warn(string message, Func<object> context);
        void Error(string message, Func<object> context);

        void LogToFile(string filename, string text);
    }

    public class Logger : ILogger
    {
        private readonly string processId;
        private readonly LogLevel logLevel;
        private readonly bool logProcessId;
        private readonly string dateFormat;
        private readonly object fileLock;

        private readonly bool bwEnabled;
        private readonly bool blackListEnabled;
        private readonly bool whiteListEnabled;
        private readonly bool bwPrefixUsed;
        private readonly HashSet<string> blackList;
        private readonly HashSet<string> whiteList;
        private readonly string bwListPrefix;
        private readonly int bwListPrefixLength;

        private readonly Microsoft.Extensions.Logging.ILogger internalLogger;

        // Interesting info here: https://github.com/aspnet/Logging/issues/483
        // Support for Microsoft Logging extensions
        private static ILoggerFactory _Factory = null;

        /// <summary>
        /// Gets  Microsoft.Extensions.Logging.LogLevel from Loglevel
        /// </summary>
        /// <param name="loglevel"></param>
        /// <returns>Microsoft.Extensions.Logging.LogLevel</returns>
        private static Microsoft.Extensions.Logging.LogLevel GetInternalLogLevel(LogLevel loglevel)
        {
            if (loglevel <= LogLevel.Debug) { return Microsoft.Extensions.Logging.LogLevel.Debug; }
            if (loglevel == LogLevel.Info) return Microsoft.Extensions.Logging.LogLevel.Information;
            if (loglevel == LogLevel.Warn) return Microsoft.Extensions.Logging.LogLevel.Warning;
            if (loglevel == LogLevel.Error) return Microsoft.Extensions.Logging.LogLevel.Error;
            return Microsoft.Extensions.Logging.LogLevel.None;

        }

        // Configure default logger factory. By default, add only console and debug.
        public static void ConfigureLogger(ILoggerFactory factory)
        {
            // by default, add debug for anything with current namespace
            factory.AddDebug( 
                (category, loglevel) => {
                    if (category.StartsWith("DeviceReader") && loglevel >= Microsoft.Extensions.Logging.LogLevel.Debug) return true;
                    if (loglevel >= Microsoft.Extensions.Logging.LogLevel.Information) return true;
                    return false;
                });

            // add console
            factory.AddConsole(
               (category, loglevel) => {
                   if (category.StartsWith("DeviceReader") && loglevel >= Microsoft.Extensions.Logging.LogLevel.Debug) return true;
                   if (loglevel >= Microsoft.Extensions.Logging.LogLevel.Information) return true;
                   return false;
               });
            //var db = new 


            // by default, add console with level information.
        }

        // Default logger factory, got idea from https://github.com/Azure/DotNetty/blob/dev/src/DotNetty.Common/Internal/Logging/InternalLoggerFactory.cs
        public static ILoggerFactory DefaultLoggerFactory
        {
            get
            {
                ILoggerFactory factory = Volatile.Read(ref _Factory);
                if (factory == null)
                {
                    factory = new LoggerFactory();
                    ConfigureLogger(factory);
                    ILoggerFactory current = Interlocked.CompareExchange(ref _Factory, factory, null);

                    if (current != null)
                    {
                        return current;
                    }

                    
                }
                return factory;
            }
            set {                
                Contract.Requires(value != null);

                Volatile.Write(ref _Factory, value);
            }
        }

        
        public Logger(string processId) :
            this(processId, new LoggingConfig())
        {
        }

        // how to get correct class name ?
        public Logger(string processId, ILoggingConfig config):
            this(processId, Logger.DefaultLoggerFactory.CreateLogger(typeof(Logger).FullName), config)
        {            
        }

        // Who creates ILogger.. Autofac? D
        public Logger(string processId, Microsoft.Extensions.Logging.ILogger internalLogger, ILoggingConfig config)
        {
            this.processId = processId;
            this.logLevel = config.LogLevel;
            this.logProcessId = config.LogProcessId;
            this.dateFormat = config.DateFormat;

            this.blackList = config.BlackList;
            this.whiteList = config.WhiteList;

            this.blackListEnabled = this.blackList.Count > 0;
            this.whiteListEnabled = this.whiteList.Count > 0;
            this.bwEnabled = this.blackListEnabled || this.whiteListEnabled;

            this.bwPrefixUsed = !string.IsNullOrEmpty(config.BwListPrefix);
            this.bwListPrefix = config.BwListPrefix;
            this.bwListPrefixLength = config.BwListPrefix.Length;

            this.fileLock = new object();

            // for now

            this.internalLogger = internalLogger ?? throw new ArgumentNullException();
        }

        public LogLevel LogLevel => this.logLevel;

        public bool DebugIsEnabled => this.logLevel <= LogLevel.Debug;

        public bool InfoIsEnabled => this.logLevel <= LogLevel.Info;

        public string FormatDate(long time)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(time).ToString(this.dateFormat);
        }

        // The following 4 methods allow to log a message, capturing the context
        // (i.e. the method where the log message is generated)
        public void Debug(string message, Action context)
        {
            if (this.logLevel > LogLevel.Debug) return;
            this.Write(LogLevel.Debug, context.GetMethodInfo(), message);
        }

        public void Info(string message, Action context)
        {
            if (this.logLevel > LogLevel.Info) return;
            this.Write(LogLevel.Info, context.GetMethodInfo(), message);
        }

        public void Warn(string message, Action context)
        {
            if (this.logLevel > LogLevel.Warn) return;
            this.Write(LogLevel.Warn, context.GetMethodInfo(), message);
        }

        public void Error(string message, Action context)
        {
            if (this.logLevel > LogLevel.Error) return;
            this.Write(LogLevel.Error, context.GetMethodInfo(), message);
        }

        // The following 4 methods allow to log a message and some data,
        // capturing the context (i.e. the method where the log message is generated)
        public void Debug(string message, Func<object> context)
        {
            if (this.logLevel > LogLevel.Debug) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(context.Invoke());

            this.Write(LogLevel.Debug, context.GetMethodInfo(), message);
        }

        public void Info(string message, Func<object> context)
        {
            if (this.logLevel > LogLevel.Info) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(context.Invoke());

            this.Write(LogLevel.Info, context.GetMethodInfo(), message);
        }

        public void Warn(string message, Func<object> context)
        {
            if (this.logLevel > LogLevel.Warn) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(context.Invoke());

            this.Write(LogLevel.Warn, context.GetMethodInfo(), message);
        }

        public void Error(string message, Func<object> context)
        {
            if (this.logLevel > LogLevel.Error) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(context.Invoke());

            this.Write(LogLevel.Error, context.GetMethodInfo(), message);
        }

        public void LogToFile(string filename, string text)
        {
            // Without the lock, some logs would be lost due to contentions
            lock (this.fileLock)
            {
                File.AppendAllText(filename, text);
            }
        }

        /// <summary>
        /// Log the message and information about the context, cleaning up
        /// and shortening the class name and method name (e.g. removing
        /// symbols specific to .NET internal implementation)
        /// </summary>
        private void Write(LogLevel level, MethodInfo context, string text)
        {
            // Extract the Class Name from the context
            var classname = "";
            if (context.DeclaringType != null)
            {
                classname = context.DeclaringType.FullName;
            }
            classname = classname.Split(new[] { '+' }, 2).First();
            classname = classname.Split('.').LastOrDefault();

            // Extract the Method Name from the context
            var methodname = context.Name;
            methodname = methodname.Split(new[] { '>' }, 2).First();
            methodname = methodname.Split(new[] { '<' }, 2).Last();

            // Check blacklisted and whitelisted classes and methods
            if (this.bwEnabled)
            {
                var bwClass = classname;
                if (this.bwPrefixUsed && bwClass.StartsWith(this.bwListPrefix))
                {
                    bwClass = bwClass.Substring(this.bwListPrefixLength);
                }

                if (this.blackListEnabled
                    && (this.blackList.Contains(bwClass + "." + methodname)
                        || this.blackList.Contains(bwClass + ".*")))
                {
                    return;
                }

                if (this.whiteListEnabled
                    && !this.whiteList.Contains(bwClass + "." + methodname)
                    && !this.whiteList.Contains(bwClass + ".*"))
                {
                    return;
                }
            }

            var time = DateTimeOffset.UtcNow.ToString(this.dateFormat);

           /* string message = this.logProcessId
                ? $"[{level}][{time}][{this.processId}][{classname}:{methodname}] {text}"
                : $"[{level}][{time}][{classname}:{methodname}] {text}";
            */
            string message = this.logProcessId
                ? $"[{time}][{this.processId}][{classname}:{methodname}] {text}"
                : $"[{time}][{classname}:{methodname}] {text}";

            internalLogger.Log(GetInternalLogLevel(level), message);

            /*
            Console.WriteLine(this.logProcessId
                ? $"[{level}][{time}][{this.processId}][{classname}:{methodname}] {text}"
                : $"[{level}][{time}][{classname}:{methodname}] {text}");
            */
        }
    }
}
