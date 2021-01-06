using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace OLS.Casy.Core.Logging.Api
{
    /// <summary>
    /// Interface for a logger implementation
    /// </summary>
    public interface ILogger : IDisposable
    {
        //int GetSystemLogEntryCount();
        int GetSystemLogEntryCount(DateTime? fromDate, DateTime? toDate, IEnumerable<int> categories);
        //IList<SystemLogEntry> GetSystemLogEntries(int startIndex, int count);
        IList<SystemLogEntry> GetSystemLogEntries(DateTime? fromDate, DateTime? toDate, IEnumerable<int> categories, int startIndex, int count);
        void CleanupSystemLogEntries(int daysOlderThan);

        /// <summary>
        /// Logs a message with DEBUG-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        void Debug(LogCategory logCategory, string message, Expression<Action> expression);

        /// <summary>
        /// Logs a message with DEBUG-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        void Debug<T>(LogCategory logCategory, string message, Expression<Func<T>> expression);

        /// <summary>
        /// Logs a message with INFO-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        void Info(LogCategory logCategory, string message);

        /// <summary>
        /// Logs a message with WARN-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        void Warn(LogCategory logCategory, string message, Expression<Action> expression);

        /// <summary>
        /// Logs a message with WARN-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        void Warn<T>(LogCategory logCategory, string message, Expression<Func<T>> expression);

        void Error(LogCategory logCategory, string message);

        /// <summary>
        /// Logs a message with ERROR-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        void Error(LogCategory logCategory, string message, Expression<Action> expression);

        /// <summary>
        /// Logs a message and exception with ERROR-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        /// <param name="exception">Exception to be logged</param>
        void Error(LogCategory logCategory, string message, Expression<Action> expression, Exception exception);

        /// <summary>
        /// Logs a message with ERROR-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        void Error<T>(LogCategory logCategory, string message, Expression<Func<T>> expression);

        /// <summary>
        /// Logs a message with FATAL-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        void Fatal(LogCategory logCategory, string message, Expression<Action> expression);

        /// <summary>
        /// Logs a message and exception with FATAL-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        /// <param name="exception">Exception to be logged</param>
        void Fatal(LogCategory logCategory, string message, Expression<Action> expression, Exception exception);

        /// <summary>
        /// Logs a message with FATAL-priority
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="expression">Expression indicating the origin of the log message</param>
        void Fatal<T>(LogCategory logCategory, string message, Expression<Func<T>> expression);

        void CreateBackupFile(string fileName);
    }
}
