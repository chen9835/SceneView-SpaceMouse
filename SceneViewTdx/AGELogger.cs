
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System.Reflection;

namespace RuntimeSDKTest
{
    internal class AGELogger
    {
        ILog _logger = null;
        Level _level = Level.Info;

        private AGELogger()
        {
            ApplyLogSettings();
            GetLogger();
        }

        private static AGELogger s_inst = new AGELogger();
        public static AGELogger GetInst()
        {
            return s_inst;
        }

        /// <summary>
        /// Logs a message object with the Error level.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        public void Error(string message)
        {
            if (_level <= Level.Error)
            {
                _logger?.Error(CleanMessage(message));
            }
        }
        /// <summary>
        /// Log a message object with the Fatal level.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        public void Fatal(string message)
        {
            if (_level <= Level.Fatal)
            {
                _logger?.Fatal(CleanMessage(message));
            }
        }
        /// <summary>
        /// Logs a message object with the Info level.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        public void Info(string message)
        {
            if (_level <= Level.Info)
            {
                _logger?.Info(CleanMessage(message));
            }
        }
        /// <summary>
        /// Log a message object with the Debug level.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        public void Debug(string message)
        {
            if (_level <= Level.Debug)
            {
                _logger?.Debug(CleanMessage(message));
            }
        }
        /// <summary>
        /// Log a message object with the Warning level.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        public void Warn(string message)
        {
            if (_level <= Level.Warn)
            {
                _logger?.Warn(CleanMessage(message));
            }
        }

        private static string CleanMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }

            string clean = message.Replace('\n', '_').Replace('\r', '_');

            string encode = System.Web.HttpUtility.HtmlEncode(clean);
            if (!clean.Equals(encode))
            {
                encode += " (Encoded)";
            }

            return encode;
        }

        public void ApplyLogSettings()
        {
            try
            {
                var hierarchy = LogManager.GetRepository(Assembly.GetEntryAssembly()) as Hierarchy;

                PatternLayout patternLayout = new PatternLayout();
                patternLayout.ConversionPattern = "%property{log4net:HostName} %username %date{yyyy-MM-dd HH:mm:ss} %level  %message%newline%newline";
                patternLayout.ActivateOptions();

                RollingFileAppender roller = new RollingFileAppender();
                roller.AppendToFile = true;
                roller.File = System.Environment.CurrentDirectory + @"\logs\runtimesdk.log";
                roller.Layout = patternLayout;
                roller.MaxSizeRollBackups = 1000;
                roller.MaximumFileSize = 10000 + "KB";
                roller.RollingStyle = RollingFileAppender.RollingMode.Size;
                roller.StaticLogFileName = false;
                roller.ActivateOptions();
                hierarchy.Root.AddAppender(roller);

                MemoryAppender memory = new MemoryAppender();
                memory.ActivateOptions();
                hierarchy.Root.AddAppender(memory);

                hierarchy.Root.Level = Level.Info;
                _level = hierarchy.Root.Level;
                hierarchy.Configured = true;
            }
            catch
            {

            }
        }

        private void GetLogger()
        {
            try
            {
                _logger = LogManager.GetLogger(typeof(RollingFileAppender));
                _logger?.Debug("Logger was created successfully.");
            }
            catch
            {

            }
        }
    }
}
