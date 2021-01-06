using Moq;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Events;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Test.Mock
{
    public class EventAggregatorProviderMock : Mock<IEventAggregatorProvider>
    {
        private Mock<IEventAggregator> _eventAggregatorMock;

        public EventAggregatorProviderMock()
        {
            RaisedEventCounts = new Dictionary<Type, int>();

            RaisedEventCounts.Add(typeof(ShowProgressEvent), 0);
            _eventAggregatorMock = new Mock<IEventAggregator>();
            _eventAggregatorMock.Setup(ea => ea.GetEvent<ShowProgressEvent>()).Returns(new ShowProgressEvent()).Callback(() =>
            {
                RaisedEventCounts[typeof(ShowProgressEvent)]++;
            });

            RaisedEventCounts.Add(typeof(ShowLoginScreenEvent), 0);
            _eventAggregatorMock.Setup(ea => ea.GetEvent<ShowLoginScreenEvent>()).Returns(new ShowLoginScreenEvent()).Callback(() =>
            {
                RaisedEventCounts[typeof(ShowLoginScreenEvent)]++;
            });

            this.Setup(eap => eap.Instance).Returns(this._eventAggregatorMock.Object);
        }

        public Dictionary<Type, int> RaisedEventCounts { get; private set; }
    }
}
