using log4net;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;

namespace OLS.Casy.Core.Logging.SQLite.EF
{
    /// <summary>
    /// Implementation of <see cref="ILogger"/>.
    /// Uses log4net to provie logging information on rolling file appender.
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ILogger))]
    public class Logger : ILogger, IPartImportsSatisfiedNotification, IDisposable
    {
        private const string ANALYZED_LOGGER_PATTERN = "'{0}' {2} [{1}]";
        private const string THREAD_ID_NAME_PATTERN = "({0}) {1}";

        private readonly ILog _appLogger;
        private readonly IEnvironmentService _environmentService;
        private readonly LogContext _logContext;

        public int GetSystemLogEntryCount(DateTime? fromDate, DateTime? toDate, IEnumerable<int> categories)
        {
            var logEntries = _logContext.Logs.Where(l => l.Level != "DEBUG").Select(x => new { x.Date, x.Category }).ToList();

            return logEntries.Count(l =>
            {
                bool result = true;
                if(fromDate.HasValue)
                {
                    result &= l.Date.Date >= fromDate.Value.Date;
                    if (!result) return false;
                }
                if(toDate.HasValue)
                {
                    result &= l.Date.Date <= toDate.Value.Date.AddDays(1);
                    if (!result) return false;
                }

                if(categories.Any())
                {
                    return categories.Contains(l.Category);
                }
                return true;
            });
        }

        public IList<SystemLogEntry> GetSystemLogEntries(DateTime? fromDate, DateTime? toDate, IEnumerable<int> categories, int startIndex, int count)
        {
            List<SystemLogEntry> result = new List<SystemLogEntry>();

            var logEntries = _logContext.Logs.Where(l => l.Level != "DEBUG").OrderByDescending(log => log.Date).ToList().AsQueryable();

            if(fromDate.HasValue)
            {
                logEntries = logEntries.Where(l => l.Date.Date >= fromDate.Value.Date);
            }
            if(toDate.HasValue)
            {
                logEntries = logEntries.Where(l => l.Date.Date <= toDate.Value.Date.AddDays(1));
            }
            if(categories.Any())
            {
                logEntries = logEntries.Where(l => categories.Contains(l.Category));
            }

            var logEntriesList = logEntries.Skip(startIndex)
                .Take(count).ToList();

            foreach (var logEntry in logEntriesList)
            {
                result.Add(new SystemLogEntry()
                {
                    Date = logEntry.Date,
                    Level = logEntry.Level,
                    Message = logEntry.Message,
                    User = logEntry.User,
                    Category = (LogCategory)logEntry.Category
                });
            }
            return result;
        }

        public void CleanupSystemLogEntries(int daysOlderThan)
        {
            var compareData = DateTime.UtcNow.Date.AddDays(-1 * daysOlderThan);
            var toDelete = _logContext.Logs.ToList();
            toDelete = toDelete.Where(x => x.Date.Date < compareData).ToList();

            foreach(var entry in toDelete)
            {
                _logContext.Logs.Remove(entry);
            }
            _logContext.SaveChanges();
        }

        static Logger()
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            log4net.Config.XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            //var assembly = Assembly.GetAssembly(typeof(Logger));
            // Get all embedded resources 
            //string[] embeddedResourceNames = assembly.GetManifestResourceNames();

            //foreach (string resourceName in embeddedResourceNames)
            //{
                //if (resourceName.Contains("log4net.config"))
                //{
                    //log4net.Config.XmlConfigurator.Configure(assembly.GetManifestResourceStream(resourceName));
                    //break;
                //}
            //}

        }

        /// <summary>
        /// MEF importing constructor
        /// </summary>
        /// <param name="authenticationService">Implementation of <see cref="IAuthenticationService"/> </param>
        [ImportingConstructor]
        public Logger(IEnvironmentService environmentService)
            : this(LogManager.GetLogger(typeof(Logger)))
        {
            _logContext = new LogContext();

            this._environmentService = environmentService;
        }

        internal Logger(ILog appLogger)
        {
            this._appLogger = appLogger;
        }

        /// <summary>
        /// Logs a message with DEBUG-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        public void Debug<T>(LogCategory logCategory, string message, Expression<Func<T>> expression)
        {
            this.Debug(logCategory, message, expression == null ? null : expression.Body);
        }

        /// <summary>
        /// Logs a message with DEBUG-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        public void Debug(LogCategory logCategory, string message, Expression<Action> expression)
        {
            this.Debug(logCategory, message, expression == null ? null : expression.Body);
        }

        /// <summary>
        /// Logs a message with ERROR-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        public void Error<T>(LogCategory logCategory, string message, Expression<Func<T>> expression)
        {
            this.Error(logCategory, message, expression == null ? null : expression.Body, null);
        }

        /// <summary>
        /// Logs a message and exception with ERROR-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        /// <param name="exception">Exception to be logged</param>
        public void Error(LogCategory logCategory, string message, Expression<Action> expression, Exception exception)
        {
            this.Error(logCategory, message, expression == null ? null : expression.Body, exception);
        }

        /// <summary>
        /// Logs a message with ERROR-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        public void Error(LogCategory logCategory, string message, Expression<Action> expression)
        {
            this.Error(logCategory, message, expression == null ? null : expression.Body, null);
        }

        /// <summary>
        /// Logs a message with FATAL-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        public void Fatal<T>(LogCategory logCategory, string message, Expression<Func<T>> expression)
        {
            this.Fatal(logCategory, message, expression == null ? null : expression.Body, null);
        }

        /// <summary>
        /// Logs a message with FATAL-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        public void Fatal(LogCategory logCategory, string message, Expression<Action> expression)
        {
            this.Fatal(logCategory, message, expression == null ? null : expression.Body, null);
        }

        /// <summary>
        /// Logs a message and exception with FATAL-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        /// <param name="exception">Exception to be logged</param>
        public void Fatal(LogCategory logCategory, string message, Expression<Action> expression, Exception exception)
        {
            this.Fatal(logCategory, message, expression == null ? null : expression.Body, exception);
        }

        /// <summary>
        /// Logs a message with INFO-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        public void Info(LogCategory logCategory, string message)
        {
            if (this._appLogger.IsInfoEnabled)
            {
                SetCategory(logCategory);
                SetUserName();
                this._appLogger.Info(message);
            }
        }

        public void Error(LogCategory logCategory, string message)
        {
            if (this._appLogger.IsErrorEnabled)
            {
                SetCategory(logCategory);
                SetUserName();
                this._appLogger.Error(message);
            }
        }

        /// <summary>
        /// Logs a message with WARN-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        public void Warn<T>(LogCategory logCategory, string message, Expression<Func<T>> expression)
        {
            this.Warn(logCategory, message, expression == null ? null : expression.Body);
        }

        /// <summary>
        /// Logs a message with WARN-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        public void Warn(LogCategory logCategory, string message, Expression<Action> expression)
        {
            this.Warn(logCategory, message, expression == null ? null : expression.Body);
        }

        /// <summary>
        /// Logs a message with DEBUG-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        private void Debug(LogCategory logCategory, string message, Expression expression)
        {
            if (this._appLogger.IsDebugEnabled)
            {
                SetCategory(logCategory);
                SetUserName();
                var msg = CreateLogMessage(message, expression);
                this._appLogger.Debug(msg);
            }
        }

        private void Error(LogCategory logCategory, string message, Expression expression, Exception exception)
        {
            if (this._appLogger.IsErrorEnabled)
            {
                SetCategory(logCategory);
                SetUserName();
                if (null == exception)
                    this._appLogger.Error(CreateLogMessage(message, expression));
                else
                {
                    this._appLogger.Error(CreateLogMessage(message, expression), exception);
                    LogUnhandledException(exception);
                }
            }
        }

        private void Fatal(LogCategory logCategory, string message, Expression expression, Exception exception)
        {
            if (this._appLogger.IsFatalEnabled)
            {
                SetCategory(logCategory);
                SetUserName();
                if (null == exception)
                    this._appLogger.Fatal(CreateLogMessage(message, expression));
                else
                {
                    this._appLogger.Fatal(CreateLogMessage(message, expression), exception);
                    LogUnhandledException(exception);
                }
            }
        }

        private void Warn(LogCategory logCategory, string message, Expression expression)
        {
            if (this._appLogger.IsWarnEnabled)
            {
                SetCategory(logCategory);
                SetUserName();
                this._appLogger.Warn(CreateLogMessage(message, expression));
            }
        }

        private string AnalyzeExpression(Expression expression)
        {
            var result = new object[3];

            // Thread Id
            result[0] = string.Format(THREAD_ID_NAME_PATTERN, Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.Name);

            var memExpr = expression as MemberExpression;
            if (memExpr != null)
            {
                result[1] = memExpr.Member.Name;
                result[2] = memExpr.Expression.Type.FullName;
                return string.Format(ANALYZED_LOGGER_PATTERN, result);
            }

            var methCallExpr = expression as MethodCallExpression;
            if (methCallExpr != null)
            {
                result[1] = methCallExpr.Method.Name;
                result[2] = methCallExpr.Object == null ? methCallExpr.Method.DeclaringType.Name : methCallExpr.Object.Type.FullName;
                return string.Format(ANALYZED_LOGGER_PATTERN, result);
            }

            var unaryExpr = expression as UnaryExpression;
            if (unaryExpr != null)
            {
                methCallExpr = unaryExpr.Operand as MethodCallExpression;
                if (methCallExpr != null)
                {
                    result[1] = methCallExpr.Method.Name;
                    result[2] = methCallExpr.Object == null
                                    ? methCallExpr.Method.DeclaringType.Name
                                    : methCallExpr.Object.Type.FullName;
                    return string.Format(ANALYZED_LOGGER_PATTERN, result);
                }

                memExpr = unaryExpr.Operand as MemberExpression;
                if (memExpr != null)
                {
                    result[1] = memExpr.Member.Name;
                    result[2] = memExpr.Expression.Type.FullName;
                    return string.Format(ANALYZED_LOGGER_PATTERN, result);
                }
            }

            var newExpression = expression as NewExpression;
            if (newExpression != null)
            {
                result[1] = newExpression.Constructor.Name;
                result[2] = newExpression.Constructor.DeclaringType.FullName;
                return string.Format(ANALYZED_LOGGER_PATTERN, result);
            }

            return null;
        }

        private string CreateLogMessage(string message, Expression expression)
        {
            
            string callerInfo = this.AnalyzeExpression(expression);
            var user = _environmentService.GetEnvironmentInfo("LoggedInUserName");
            var curUser = user == null ? "Not logged in" : user;
            return !string.IsNullOrEmpty(callerInfo) ? string.Format("{0} ({1}) '{2}'", callerInfo, curUser, message) : string.Format("'{0}'", message);
        }

        public void OnImportsSatisfied()
        {
            if (Migrations.CheckForMigration(_logContext))
            {
                if (File.Exists("log.db.bak"))
                {
                    File.Delete("log.db.bak");
                }
                CreateBackupFile("log.db.bak");
                Migrations.DoMigration(_logContext);
            }

            SetUserName();
        }

        public void CreateBackupFile(string fileName)
        {
            SQLiteConnection destination = new SQLiteConnection("Data Source=" + fileName);
            destination.Open();

            //var connection = _logContext.Database.Connection as SQLiteConnection;
            //if(connection != null)
            //{
                //connection.BackupDatabase();BackupDatabase(destination);
            //}

            SQLiteConnection source = this._logContext.Database.Connection as SQLiteConnection;
            source.BackupDatabase(destination, "main", "main", -1, null, 0);

            destination.Close();
        }

        private void SetUserName()
        {
            GlobalContext.Properties["CasyUser"] = _environmentService.GetEnvironmentInfo("LoggedInUserName");
        }

        private void SetCategory(LogCategory logCategory)
        {
            GlobalContext.Properties["Category"] = (int)logCategory;
        }

        private static void LogUnhandledException(Exception exception)
        {
            StringBuilder unhandeledExceptionText = new StringBuilder();

            //var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                unhandeledExceptionText.AppendLine(string.Format("Program catched an exception and handled it. {0}; {1};", exception.Message, exception.StackTrace));

                if (exception is ReflectionTypeLoadException)
                {
                    ReflectionTypeLoadException le = exception as ReflectionTypeLoadException;

                    foreach (var e in le.LoaderExceptions)
                    {
                        unhandeledExceptionText.AppendLine(string.Format("Program catched an exception and handled it. {0}; {1};", e.Message, e.StackTrace));
                    }
                }

                exception = exception.InnerException;
                while (exception != null)
                {
                    //this.Logger.Fatal(String.Format("Runtime Service has unhandled exception catched. {0}; {1};", exception.Message, exception.StackTrace),
                    //                () => this.OnUnhandledException(sender, e), exception);
                    unhandeledExceptionText.AppendLine(string.Format("Program catched an exception and handled it. {0}; {1};", exception.Message, exception.StackTrace));
                    exception = exception.InnerException;

                    if (exception is ReflectionTypeLoadException)
                    {
                        ReflectionTypeLoadException le = exception as ReflectionTypeLoadException;

                        foreach (var e in le.LoaderExceptions)
                        {
                            unhandeledExceptionText.AppendLine(string.Format("Program catched an exception and handled it. {0}; {1};", e.Message, e.StackTrace));
                        }
                    }
                }




                //todo Error handing implementieren 
                //todo MessageBox mit Hilfe des UI-Services verwenden
                //MessageBox.Show(exception.Message, "Uncaught thread exception");
            }
            else
            {
                //this.Logger.Fatal(String.Format("AppService has unhandled exception catched."),
                //                    () => this.OnUnhandledException(sender, e));

                //MessageBox.Show(exception.Message, "AppService has unhandled exception catched.");

                unhandeledExceptionText.AppendLine("AppService has unhandled exception catched.");
            }

            string path = @"UndhandeledException.txt";
            if (!File.Exists(path))
            {
                File.WriteAllText(path, unhandeledExceptionText.ToString() + Environment.NewLine);
            }
            else
            {
                File.AppendAllText(path, unhandeledExceptionText.ToString() + Environment.NewLine);
            }
            //this.Logger.Dispose();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _logContext.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        ~Logger() {
           // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
           Dispose(false);
         }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        
        #endregion
    }
}
