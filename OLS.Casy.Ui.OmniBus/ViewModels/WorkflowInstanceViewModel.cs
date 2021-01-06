using DevExpress.Mvvm;
using DevExpress.Xpf.LayoutControl;
using OLS.Casy.Ui.Base;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OLS.Casy.Ui.OmniBus.ViewModels
{
    public class WorkflowInstanceViewModel : Base.ViewModelBase
    {
        private object _content;
        private string _header;
        private TileSize _size;
        private bool _isNewGroup;
        private int _order;
        private bool _isMaximized;

        public WorkflowInstanceViewModel()
        {
            Size = TileSize.Small;
            IsNewGroup = false;
        }

        public object Content
        {
            get { return this._content; }
            set
            {
                if(value != _content)
                {
                    this._content = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string Header
        {
            get { return this._header; }
            set
            {
                if(value != this._header)
                {
                    this._header = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public TileSize Size
        {
            get { return this._size; }
            set
            {
                if(value != this._size)
                {
                    this._size = value;
                    NotifyOfPropertyChange();
                }
            }
        }
        public bool IsNewGroup
        {
            get { return this._isNewGroup; }
            set
            {
                if(value != this._isNewGroup)
                {
                    this._isNewGroup = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsMaximized
        {
            get { return this._isMaximized; }
            set
            {
                if (value != this._isMaximized)
                {
                    this._isMaximized = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        

        public int Order
        {
            get;set;
        }
    }
}
