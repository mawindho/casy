using DevExpress.Mvvm;
using System;
using System.Runtime.CompilerServices;
using OLS.Casy.Core;

namespace OLS.Casy.Ui.Base
{
    public class OmniDelegateCommand : DelegateCommand
    {
        private readonly string _sourceFilePath;
        private readonly string _callerMemberName;
        private readonly string _methodName;

        public OmniDelegateCommand(Action executeMethod,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string callerMemberName = "") 
            : base(executeMethod)
        {
            _sourceFilePath = sourceFilePath;
            _methodName = executeMethod.Method.Name;
            _callerMemberName = callerMemberName;
        }

        public OmniDelegateCommand(Action executeMethod, 
            bool useCommandManager,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string callerMemberName = "") 
            : base(executeMethod, useCommandManager)
        {
            _methodName = executeMethod.Method.Name;
            _callerMemberName = callerMemberName;
            _sourceFilePath = sourceFilePath;
        }

        public OmniDelegateCommand(Action executeMethod, 
            Func<bool> canExecuteMethod, 
            bool? useCommandManager = null,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string callerMemberName = "") 
            : base(executeMethod, canExecuteMethod, useCommandManager)
        {
            _methodName = executeMethod.Method.Name;
            _callerMemberName = callerMemberName;
            _sourceFilePath = sourceFilePath;
        }

        public override void Execute(object parameter)
        {
            InteractionLogProvider.AddInteraction($"File: {_sourceFilePath}; Member: {_callerMemberName}; Method: {_methodName}");
            base.Execute(parameter);
        }
    }

    public class OmniDelegateCommand<T> : DelegateCommand<T>
    {
        private readonly string _callerMemberName;
        private readonly string _methodName;
        private readonly string _sourceFilePath;

        public OmniDelegateCommand(Action<T> executeMethod,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string callerMemberName = "") : base(executeMethod)
        {
            _methodName = executeMethod.Method.Name;
            _callerMemberName = callerMemberName;
            _sourceFilePath = sourceFilePath;
        }

        public OmniDelegateCommand(Action<T> executeMethod, bool useCommandManager,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string callerMemberName = "") : base(executeMethod, useCommandManager)
        {
            _methodName = executeMethod.Method.Name;
            _callerMemberName = callerMemberName;
            _sourceFilePath = sourceFilePath;
        }

        public OmniDelegateCommand(Action<T> executeMethod, Func<T, bool> canExecuteMethod, bool? useCommandManager = null,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string callerMemberName = "") : base(executeMethod, canExecuteMethod, useCommandManager)
        {
            _methodName = executeMethod.Method.Name;
            _callerMemberName = callerMemberName;
            _sourceFilePath = sourceFilePath;
        }

        public override void Execute(T parameter)
        {
            InteractionLogProvider.AddInteraction($"File: {_sourceFilePath}; Member: {_callerMemberName}; Method: {_methodName}");
            base.Execute(parameter);
        }
    }
}
