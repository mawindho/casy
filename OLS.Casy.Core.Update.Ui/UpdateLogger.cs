using System;
using System.IO;
using System.Linq.Expressions;

namespace OLS.Casy.Core.Update.Ui
{
    public class UpdateLogger
    {
        private const string ANALYZED_LOGGER_PATTERN = "\"{0}\"\t{2}\t[{1}]";
        private string _fileName;
        //private readonly ILog _updateLogger;

        public UpdateLogger(string version)
            //: this(LogManager.GetLogger(typeof(UpdateLogger)))
        {
            _fileName = $"UpdateLog_{version.Replace(".", "_")}.log";
            if (!File.Exists(_fileName))
            {
                var file = File.Create(_fileName);
                file.Close();
            }
        }

        //internal UpdateLogger(ILog updateLogger)
        //{
            //this._updateLogger = updateLogger;
        //}

        public void Debug<T>(string message, Expression<Func<T>> expression)
        {
            this.Debug(message, expression == null ? null : expression.Body);
        }

        public void Debug(string message, Expression<Action> expression)
        {
            this.Debug(message, expression == null ? null : expression.Body);
        }

        public void Error<T>(string message, Expression<Func<T>> expression)
        {
            this.Error(message, expression == null ? null : expression.Body, null);
        }

        public void Error(string message, Expression<Action> expression, Exception exception)
        {
            this.Error(message, expression == null ? null : expression.Body, exception);
        }

        public void Error(string message, Expression<Action> expression)
        {
            this.Error(message, expression == null ? null : expression.Body, null);
        }

        public void Fatal<T>(string message, Expression<Func<T>> expression)
        {
            this.Fatal(message, expression == null ? null : expression.Body, null);
        }

        public void Fatal(string message, Expression<Action> expression)
        {
            this.Fatal(message, expression == null ? null : expression.Body, null);
        }

        public void Fatal(string message, Expression<Action> expression, Exception exception)
        {
            this.Fatal(message, expression == null ? null : expression.Body, exception);
        }

        public void Info<T>(string message, Expression<Func<T>> expression)
        {
            this.Info(message, expression == null ? null : expression.Body);
        }

        public void Info(string message, Expression<Action> expression)
        {
            this.Info(message, expression == null ? null : expression.Body);
        }

        public void Info(string message)
        {
            this.Info(message, null);
        }

        public void Warn<T>(string message, Expression<Func<T>> expression)
        {
            this.Warn(message, expression == null ? null : expression.Body);
        }

        public void Warn(string message, Expression<Action> expression)
        {
            this.Warn(message, expression == null ? null : expression.Body);
        }

        private void Debug(string message, Expression expression)
        {
            //if (this._updateLogger.IsDebugEnabled)
            //{
                var msg = CreateLogMessage(message, expression);
            //this._updateLogger.Debug(msg);

                File.AppendAllText(_fileName, $"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}] DEBUG: {msg}\n");
            //}
        }

        private void Error(string message, Expression expression, Exception exception)
        {
            if (null == exception)
                File.AppendAllText(_fileName, $"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}] ERROR: {CreateLogMessage(message, expression)}; Exception: {exception.ToString()}\n");
            else
                File.AppendAllText(_fileName, $"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}] ERROR: {CreateLogMessage(message, expression)}\n");
        }

        private void Fatal(string message, Expression expression, Exception exception)
        {
            if (null == exception)
                File.AppendAllText(_fileName, $"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}] FATAL: {CreateLogMessage(message, expression)}; Exception: {exception.ToString()}\n");
            else
                File.AppendAllText(_fileName, $"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}] FATAL: {CreateLogMessage(message, expression)}\n");
        }

        private void Info(string message, Expression expression)
        {
            var msg = CreateLogMessage(message, expression);
            File.AppendAllText(_fileName, $"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}] INFO: {msg}\n");
        }

        private void Warn(string message, Expression expression)
        {
            var msg = CreateLogMessage(message, expression);
            File.AppendAllText(_fileName, $"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}] WARN: {msg}\n");
        }

        private string AnalyzeExpression(Expression expression)
        {
            var result = new object[2];

            var memExpr = expression as MemberExpression;
            if (memExpr != null)
            {
                result[0] = memExpr.Member.Name;
                result[1] = memExpr.Expression.Type.FullName;
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
            return !string.IsNullOrEmpty(callerInfo) ? string.Format("{0}\t\"{1}\"", callerInfo, message) : string.Format("\"{0}\"", message);
        }

        public void Dispose()
        {
        }
    }
}
