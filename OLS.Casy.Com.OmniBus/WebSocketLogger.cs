using OLS.Casy.Core.Logging.Api;
using OLS.Com.WebSockets.Common;
using System;
using System.ComponentModel.Composition;

namespace OLS.Casy.Com.OmniBus
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IWebSocketLogger))]
    public class WebSocketLogger : IWebSocketLogger
    {
        private ILogger _logger;

        [ImportingConstructor]
        public WebSocketLogger(ILogger logger)
        {
            this._logger = logger;
        }

        public void Information(Type type, string format, params object[] args)
        {
            _logger.Info(string.Format(format, args));
        }

        public void Warning(Type type, string format, params object[] args)
        {
            _logger.Warn(string.Format(format, args), () => Warning(type, format, args));
        }

        public void Error(Type type, string format, params object[] args)
        {
            _logger.Error(string.Format(format, args), () => Warning(type, format, args));
        }

        public void Error(Type type, Exception exception)
        {
            Error(type, "{0}", exception);
        }
    }
}
