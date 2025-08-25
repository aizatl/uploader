using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Terminaldiagnostic
{
    public class Logger
    {
        public enum Loglevel
        {
            Error,
            Warning,
            Info,
            Verbose
        }

        private enum LogMode
        {
            TraceLog,
            TraceOnly,
            LogOnly
        }

        const string traceCategory = "Logger";
        TraceSwitch traceSwitch = new TraceSwitch("SwcTraceLevel", "Trace Switch Level");

        private string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private int logKeptDuration = 0;
        private Timer tmrAutoPurge;
        private TextWriter w;
        private DateTime logDT = DateTime.MinValue;
        private readonly object syncObject;
        private LogMode logMode;

        public Logger(string _logPath, string _logMode, int _logKeptDuration)
        {
            if (!string.IsNullOrEmpty(_logPath))
                logPath = Path.Combine(_logPath, "Logs");

            logKeptDuration = _logKeptDuration;

            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);

            if (logKeptDuration > 0)
            {
                // run every 12 hours
                tmrAutoPurge = new Timer(new TimerCallback(PurgeLogFile), null, 1000, 43200000);
            }

            if (_logMode == "1")
                logMode = LogMode.TraceLog;
            else if (_logMode == "2")
                logMode = LogMode.LogOnly;
            else
                logMode = LogMode.TraceLog;

            syncObject = new object();
        }

        ~Logger()
        {
            if (tmrAutoPurge != null)
            {
                tmrAutoPurge.Change(Timeout.Infinite, Timeout.Infinite);
                tmrAutoPurge.Dispose();
            }
        }

        private void PurgeLogFile(object state)
        {
            try
            {
                DirectoryInfo logDir = new DirectoryInfo(logPath);
                foreach (FileInfo file in logDir.GetFiles("*.log", SearchOption.TopDirectoryOnly))
                {
                    DateTime fileDate;
                    if (DateTime.TryParseExact(file.Name.Split('_')[1].Replace(".log", ""), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fileDate))
                    {
                        if (fileDate < DateTime.Now.AddDays(-logKeptDuration))
                            file.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLineIf(traceSwitch.TraceError, string.Format("[Error] PurgeLogFile: {0}", ex.ToString()), traceCategory);
            }
        }

        public void WriteLog(Loglevel logLevel, string module, string deviceNo, string deviceIP, string message, Exception exception = null)
        {
            try
            {
                if (!((logLevel == Loglevel.Error && traceSwitch.TraceError) ||
                    (logLevel == Loglevel.Warning && traceSwitch.TraceWarning) ||
                    (logLevel == Loglevel.Info && traceSwitch.TraceInfo) ||
                    (logLevel == Loglevel.Verbose && traceSwitch.TraceVerbose)))
                    return;

                if (exception != null)
                {
                    Exception baseException = exception.GetBaseException();
                    message = message ?? baseException.Message;
                }

                string log = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    logLevel.ToString(),
                    string.IsNullOrEmpty(deviceNo) ? "" : deviceNo,
                    string.IsNullOrEmpty(deviceIP) ? "" : deviceIP,
                    string.IsNullOrEmpty(module) ? "" : module,
                    string.IsNullOrEmpty(message) ? "" : message.Replace("\r\n", "#"),
                    exception != null ? string.Format("<Exception> {0}", exception.StackTrace.Replace("\r\n", "#")) : "");

                if (logMode == LogMode.TraceLog || logMode == LogMode.TraceOnly)
                {
                    if (logLevel == Loglevel.Info)
                        Trace.WriteLineIf(traceSwitch.TraceInfo, log, traceCategory);
                    else if (logLevel == Loglevel.Warning)
                        Trace.WriteLineIf(traceSwitch.TraceWarning, log, traceCategory);
                    else if (logLevel == Loglevel.Error)
                        Trace.WriteLineIf(traceSwitch.TraceError, log, traceCategory);
                    else
                        Trace.WriteLineIf(traceSwitch.TraceVerbose, log, traceCategory);
                }

                if (logMode == LogMode.TraceLog || logMode == LogMode.LogOnly)
                    WriteLogFile(log);
            }
            catch (Exception ex)
            {
                Trace.WriteLineIf(traceSwitch.TraceError, string.Format("[Error] WriteLogFile: {0}", ex.ToString()), traceCategory);
            }
        }

        public string ConstructLog(Loglevel logLevel, string module, string deviceNo, string deviceIP, string message, Exception exception = null, bool writeLog = true)
        {
            try
            {
                if (exception != null)
                {
                    Exception baseException = exception.GetBaseException();
                    message = message ?? baseException.Message;
                }

                string log = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    logLevel.ToString(),
                    string.IsNullOrEmpty(deviceNo) ? "" : deviceNo,
                    string.IsNullOrEmpty(deviceIP) ? "" : deviceIP,
                    string.IsNullOrEmpty(module) ? "" : module,
                    string.IsNullOrEmpty(message) ? "" : message.Replace("\r\n", "#"),
                    exception != null ? string.Format("<Exception> {0}", exception.ToString().Replace("\r\n", "#")) : "");

                if (writeLog)
                    WriteLog(logLevel, module, deviceNo, deviceIP, message, exception);

                return log;
            }
            catch (Exception ex)
            {
                Trace.WriteLineIf(traceSwitch.TraceError, string.Format("[Error] ConstructLog: {0}", ex.ToString()), traceCategory);
                return "";
            }
        }

        private void WriteLogFile(string log)
        {
            lock (syncObject)
            {
                DateTime dt = DateTime.Now;

                if (logDT == DateTime.MinValue || logDT.Date != dt.Date)
                {
                    logDT = dt;
                    string logFile = Path.Combine(logPath, string.Format("Log_{0}.log", dt.ToString("yyyy-MM-dd")));
                    w = TextWriter.Synchronized(File.AppendText(logFile));
                }

                w.WriteLine(log);
                w.Flush();
            }
        }
    }
}
