using OLS.Casy.Models;

namespace OLS.Casy.Ui.Base.Models
{
    public class AdditinalGridColumnInfo : ModelBase
    {
        private bool _isSelectedRow = false;

        public string Binding { get; set; }
        public bool IsTwoWayBinding { get; set; }
        public string Header { get; set; }
        public string Style { get; set; }
        public string Category { get; set; }
        public int Order { get; set; }
        public string CursorName { get; set; }
        public bool IsSelectedRow
        {
            get { return _isSelectedRow; }
            set
            {
                if(value != _isSelectedRow)
                {
                    this._isSelectedRow = value;
                    NotifyOfPropertyChange();
                }
            }
        }
    }
}
