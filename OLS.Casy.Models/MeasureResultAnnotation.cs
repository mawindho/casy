using System;

namespace OLS.Casy.Models
{
    [Serializable]
    public class MeasureResultAnnotation : ModelBase
    {
        [NonSerialized]
        private int _measureResultAnnotationId = -1;
        private string _value;
        private AnnotationType _annotationType;

        public int MeasureResultAnnotationId
        {
            get { return _measureResultAnnotationId; }
            set { this._measureResultAnnotationId = value; }
        }

        public string Value
        {
            get { return _value; }
            set
            {
                this._value = value;
                NotifyOfPropertyChange();
            }
        }

        public AnnotationType AnnotationType
        {
            get { return _annotationType; }
            set { this._annotationType = value; }
        }
        
        public MeasureResult MeasureResult { get; set; }
    }
}
