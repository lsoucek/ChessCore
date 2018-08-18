using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Linq;

namespace ChessCoreEngine.Utils
{
    public class Logger
    {
        public enum LogLevels { UNKNOWN, FATAL, ERROR, WARN, INFO, DEBUG, TRACE }

        string ClassName { get; }

        public LogLevels LogLevel { get; }

        public bool IsTraceLevelLog => LogLevel >= LogLevels.TRACE;
        public bool IsDebugLevelLog => LogLevel >= LogLevels.DEBUG;
        public bool IsInfoLevelLog => LogLevel >= LogLevels.INFO;
        public bool IsWarnLevelLog => LogLevel >= LogLevels.WARN;
        public bool IsErrorLevelLog => LogLevel >= LogLevels.ERROR;
        public bool IsFatalLevelLog => LogLevel >= LogLevels.FATAL;

        //16-07 19:09:38.093 [1] INFO KRM.Infra.ServiceBase.ServiceHost - Program started

        public Logger(string clsName, LogLevels logLevel = LogLevels.TRACE) { ClassName = clsName; LogLevel = logLevel; }

        public void Trace(string msg) { if (IsTraceLevelLog) LogImpl(LogLevels.TRACE, msg); }
        public void Debug(string msg) { if (IsDebugLevelLog) LogImpl(LogLevels.DEBUG, msg); }
        public void Info(string msg) { if (IsInfoLevelLog) LogImpl(LogLevels.INFO, msg); }
        public void Warn(string msg) { if (IsWarnLevelLog) LogImpl(LogLevels.WARN, msg); }
        public void Error(string msg) { if (IsErrorLevelLog) LogImpl(LogLevels.ERROR, msg); }
        public void Fatal(string msg) { if (IsFatalLevelLog) LogImpl(LogLevels.FATAL, msg); }

        void LogImpl(LogLevels level, string msg) => LogManager.LogData(level, Thread.CurrentThread, ClassName, msg);
    }

    public static class LogManager
    {
        public static List<LogTarget> LogTargets = new List<LogTarget>();

        public static Logger.LogLevels LogLevel { get; set; }

        public static Logger GetLogger(Type t) => new Logger(t.Name);

        internal static void LogData(Logger.LogLevels level, Thread currentExecThread, string className, string msg)
        {
            if (LogLevel >= level)
            {
                DateTime time = DateTime.Now;
                LogTargets.ForEach(item => item.Write(time, level, currentExecThread, className, msg));
            }
        }
    }

    public abstract class LogTarget
    {
        internal abstract void Write(DateTime timeStamp, Logger.LogLevels lvl, Thread currentExecThread, string className, string msg);

        protected string FormatMessage(DateTime timeStamp, Logger.LogLevels lvl, string threadName, string className, string msg) => $"{timeStamp.ToString("dd-MM HH:mm:ss.fff")} [{threadName}] {lvl.ToString()} {className} - {msg}{Environment.NewLine}";
    }

    public class ConsoleLogTarget : LogTarget
    {
        readonly ConsoleColor stdBgColor = Console.BackgroundColor;
        readonly ConsoleColor stdFgColor = Console.ForegroundColor;

        internal override void Write(DateTime timeStamp, Logger.LogLevels lvl, Thread currentExecThread, string className, string msg)
        {
            string threadName = currentExecThread.Name ?? currentExecThread.ManagedThreadId.ToString();

            switch (lvl)
            {
                case Logger.LogLevels.FATAL: HighLight("FATAL", ConsoleColor.White, ConsoleColor.Red); break;
                case Logger.LogLevels.ERROR: HighLight("ERROR", ConsoleColor.Red, ConsoleColor.Black); break;
                case Logger.LogLevels.WARN: HighLight("WARN", ConsoleColor.Yellow, ConsoleColor.Black); break;
                case Logger.LogLevels.DEBUG: HighLight("DEBUG", ConsoleColor.White, ConsoleColor.DarkBlue); break;
                case Logger.LogLevels.TRACE: HighLight("TRACE", ConsoleColor.Cyan, ConsoleColor.Black); break;
                default:
                    Console.Write(FormatMessage(timeStamp, lvl, threadName, className, msg));
                    break;
            }

            void HighLight(string pattern, ConsoleColor fontColor, ConsoleColor backgroundColor)
            {
                int idx = msg.IndexOf($"] {pattern} ");

                Console.Write($"{DateTime.Now.ToString("dd-MM HH:mm:ss.fff")} [{threadName}] ");

                Console.BackgroundColor = backgroundColor;
                Console.ForegroundColor = fontColor;
                Console.Write($"{lvl.ToString()}");

                Console.BackgroundColor = stdBgColor;
                Console.ForegroundColor = stdFgColor;
                Console.Write($" {className} - {msg}{Environment.NewLine}");
            }            
        }
    }

    public class FileLogTarget : LogTarget
    {
        readonly string fileName;
        readonly Logger.LogLevels outLevel;
        
        public FileLogTarget(string logFileName, Logger.LogLevels allowedLevel = Logger.LogLevels.TRACE, bool createEmptyLogFile = false)
        {
            fileName = logFileName;
            if (createEmptyLogFile && File.Exists(fileName)) File.Delete(fileName);
            outLevel = allowedLevel;
        }

        internal override void Write(DateTime timeStamp, Logger.LogLevels lvl, Thread currentExecThread, string className, string msg)
        {
            if (lvl <= outLevel)
                File.AppendAllText(fileName, FormatMessage(timeStamp, lvl, currentExecThread.Name ?? currentExecThread.ManagedThreadId.ToString(), className, msg));
        }
    }
}
