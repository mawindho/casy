using System;
using System.Collections.Generic;

namespace OLS.Casy.Core.Config.Api
{
    public class ConfigurationChangedEventArgs : EventArgs
    {
        private IEnumerable<string> _changedItemNames;

        public ConfigurationChangedEventArgs(IEnumerable<string> changedItemNames)
            : base()
        {
            this._changedItemNames = changedItemNames;
        }

        public IEnumerable<string> ChangedItemNames
        {
            get { return _changedItemNames; }
        }
    }
}
