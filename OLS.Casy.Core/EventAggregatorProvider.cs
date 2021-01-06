using OLS.Casy.Core.Api;
using System.ComponentModel.Composition;
using Prism.Events;

namespace OLS.Casy.Core
{
    /// <summary>
    /// Implementation of <see cref="IEventAggregatorProvider"/>.
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IEventAggregatorProvider))]
    public class EventAggregatorProvider : IEventAggregatorProvider
    {
        private readonly IEventAggregator _eventAggregator;

        /// <summary>
        /// MEF importing constructor
        /// </summary>
        [ImportingConstructor]
        public EventAggregatorProvider()
        {
            this._eventAggregator = new EventAggregator();
        }

        /// <summary>
        /// Returns the singleton instance of <see cref="IEventAggregator"/>
        /// </summary>
        public IEventAggregator Instance
        {
            get
            {
                return _eventAggregator;
            }
        }
    }
}