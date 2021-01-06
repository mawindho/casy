using OLS.Casy.Models.Enums;
using System.Collections.Generic;

namespace OLS.Casy.Models
{
    public class ErrorResult
    {
        private IList<ErrorDetails> _fatalErrorDetails;
        private IList<ErrorDetails> _softErrorDetails;
        private bool _hasCanceled;

        public ErrorResult()
        {
            _fatalErrorDetails = new List<ErrorDetails>();
            _softErrorDetails = new List<ErrorDetails>();
        }

        public ErrorResultType ErrorResultType { get; set; }

        public IList<ErrorDetails> FatalErrorDetails
        {
            get { return _fatalErrorDetails; }
        }

        public IList<ErrorDetails> SoftErrorDetails
        {
            get { return _softErrorDetails; }
        }

        public bool HasCanceled
        {
            get { return _hasCanceled; }
            set { this._hasCanceled = value; }
        }
    }
}
