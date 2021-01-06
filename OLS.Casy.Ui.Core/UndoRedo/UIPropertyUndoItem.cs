using System;
using System.Reflection;

namespace OLS.Casy.Ui.Core.UndoRedo
{
    public class UIPropertyUndoItem : UndoItem
    {
        private string _propertyStr;
        private PropertyInfo _propInfo = null;
        private object _oldValue = null;
        private object _newValue;

        public override bool PrepareCommand()
        {
            if (_oldValue == null)
            {
                _oldValue = PropInfo.GetValue(ModelObject, null);
            }
            return true;
        }

        public override bool DoCommand()
        {
            PropInfo.SetValue(ModelObject, _newValue, null);

            return true;
        }

        public override void Undo()
        {
            PropInfo.SetValue(ModelObject, _oldValue, null);
        }

        public override void Redo()
        {
            PropInfo.SetValue(ModelObject, _newValue, null);
        }

        public object NewValue
        {
            get { return _newValue; }
            set { _newValue = value; }
        }

        public object OldValue
        {
            get { return _oldValue; }
            set { _oldValue = value; }
        }

        public string PropertyStr
        {
            get { return _propertyStr; }
            set { this._propertyStr = value; }
        }

        private PropertyInfo PropInfo
        {
            get
            {
                if (null == _propInfo)
                {
                    Type type = ModelObject.GetType();
                    this._propInfo = type.GetProperty(PropertyStr);
                }
                return _propInfo;
            }
        }
    }
}
