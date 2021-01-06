using System.ComponentModel.Composition;
using System.IO;
using System.Threading;

namespace OLS.Casy.Ui.Base.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(PdfPreviewViewModel))]
    public class PdfPreviewViewModel : ViewModelBase
    {
        private Stream _documentStream;

        [ImportingConstructor]
        public PdfPreviewViewModel()
        {

        }

        public Stream DocumentStream
        {
            get { return _documentStream; }
            set
            {
                if(value != this._documentStream)
                {
                    this._documentStream = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public ManualResetEvent Awaiter { get; set; }
    }
}
