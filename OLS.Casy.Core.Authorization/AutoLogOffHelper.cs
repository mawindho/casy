using System;
using System.Timers;
using System.Windows.Interop;

namespace OLS.Casy.Core.Authorization
{
    /// <summary>
	///     This class holds a timer to auto logOff function.
	/// </summary>
	public static class AutoLogOffHelper
    {
        private static Timer _timer;

        /// <summary>
        ///     Property for the log off time
        /// </summary>
        public static int LogOffTime { get; set; }

        /// <summary>
        ///     Event raised when auto log off has performed
        /// </summary>
        public static event EventHandler MakeAutoLogOffEvent;

        /// <summary>
        ///     Start of auto logoff function
        /// </summary>
        public static void StartAutoLogoffFunction()
        {
            ComponentDispatcher.ThreadIdle += DispatcherQueueEmptyHandler;

            if (_timer != null)
            {
                _timer.Enabled = true;
            }
        }

        /// <summary>
        ///     Stop of auto logoff function
        /// </summary>
        public static void StopAutoLogoffFunction()
        {
            ComponentDispatcher.ThreadIdle -= DispatcherQueueEmptyHandler;

            if (_timer != null)
            {
                _timer.Enabled = false;
                _timer = null;
            }
        }

        /// <summary>
        ///     Reset of logoff timer
        /// </summary>
        public static void ResetLogoffTimer()
        {
            if (_timer != null)
            {
                _timer.Enabled = false;
                _timer.Enabled = true;
                _timer.Start();
            }
        }

        /// <summary>
        ///     Stop of logoff timer
        /// </summary>
        public static void StopLogoffTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Enabled = false;
                _timer.Enabled = true;
            }
        }

        private static void DispatcherQueueEmptyHandler(object sender, EventArgs eventArgs)
        {
            if (_timer == null)
            {
                _timer = new Timer { Interval = TimeSpan.FromMinutes(LogOffTime).TotalMilliseconds };
                _timer.Elapsed += TimerOnElapsed;
                _timer.Enabled = true;
                _timer.Start();
            }
            else if (_timer.Enabled == false)
            {
                _timer.Enabled = true;
            }
        }

        private static void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (_timer != null)
            {
                ComponentDispatcher.ThreadIdle -= DispatcherQueueEmptyHandler;
                _timer.Stop();
                _timer = null;
                if (MakeAutoLogOffEvent != null)
                {
                    MakeAutoLogOffEvent.Invoke(sender, EventArgs.Empty);
                }
            }
        }
    }
}
