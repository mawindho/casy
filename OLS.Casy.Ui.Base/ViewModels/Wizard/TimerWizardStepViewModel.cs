using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace OLS.Casy.Ui.Base.ViewModels.Wizard
{
    public sealed class TimerWizardStepViewModel : WizardStepViewModelBase
    {
        private DispatcherTimer _dispatcherTimer;
        private Stopwatch _stopWatch;
        private string _header;
        private string _text;
        private string _timeLeft;

        private TimeSpan _timeSpan;
        private bool _disposed;

        public TimerWizardStepViewModel()
        {
            CanNextButtonCommand = false;
        }

        public override async Task<bool> OnCancelButtonPressed()
        {
            return await Task.Factory.StartNew(() =>
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control ||
                        (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift) return false;
                    _stopWatch.Stop();
                    _dispatcherTimer.Stop();

                    CanNextButtonCommand = true;

                    return true;
                });
            });
        }

        public string Header
        {
            get => _header;
            set
            {
                if (value == _header) return;
                _header = value;
                NotifyOfPropertyChange();
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                if (value == _text) return;
                _text = value;
                NotifyOfPropertyChange();
            }
        }

        public TimeSpan TimeSpan
        {
            get => _timeSpan;
            set
            {
                _timeSpan = value;
                TimeLeft = $"{_timeSpan.Hours:00}:{_timeSpan.Minutes:00}:{_timeSpan.Seconds:00}";
            }
        }

        public string TimeLeft
        {
            get => _timeLeft;
            set
            {
                _timeLeft = value;
                NotifyOfPropertyChange();
            }
        }

        public override void OnIsActivated()
        {
            _stopWatch = new Stopwatch();
            _stopWatch.Start();

            Application.Current.Dispatcher.Invoke(() =>
            {
                _dispatcherTimer = new DispatcherTimer();
                _dispatcherTimer.Tick += timer_Tick;
                _dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
                _dispatcherTimer.Start();

            }, DispatcherPriority.Normal);

            base.OnIsActivated();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (!_stopWatch.IsRunning) return;
            var elapsed = _stopWatch.Elapsed;
            var timeLeft = _timeSpan - elapsed;

            if(timeLeft >= TimeSpan.Zero)
            { 
                TimeLeft = $"{timeLeft.Hours:00}:{timeLeft.Minutes:00}:{timeLeft.Seconds:00}";
            }
            else
            {
                _stopWatch.Stop();
                _dispatcherTimer.Stop();

                CanNextButtonCommand = true;
            }
        }

        public Func<bool> NextButtonPressedAction { get; set; }

        public override async Task<bool> OnNextButtonPressed()
        {
            if (NextButtonPressedAction != null)
            {
                return await Task.Factory.StartNew(NextButtonPressedAction);
            }
            return await base.OnNextButtonPressed();
        }

        

        protected override void Dispose(bool disposing)
        {
            if(!_disposed)
            {
                if(disposing)
                {
                }
            }
            base.Dispose(disposing);
        }
    }
}
