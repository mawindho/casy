using OLS.Casy.Ui.Core.Api;

namespace OLS.Casy.Ui.Core.UndoRedo
{
    public class UndoItem : IUndoItem
    {
        private object _modelObject;

        public object ModelObject
        {
            get { return _modelObject; }
            set { _modelObject = value; }
        }

        public virtual bool DoCommand()
        {
            return true;
        }

        public virtual bool PrepareCommand()
        {
            return true;
        }

        public virtual void Undo()
        {
        }

        public virtual void Redo()
        {

        }
    }
}
