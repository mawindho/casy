using System;

namespace OLS.Casy.Models
{
    [Serializable]
    public class AnnotationType
    {
        [NonSerialized]
        private int _annotationTypeId = -1;
        private string _annotationTypeName;

        public int AnnotationTypeId
        {
            get { return _annotationTypeId; }
            set { this._annotationTypeId = value; }
        }

        public string AnnottationTypeName
        {
            get { return _annotationTypeName; }
            set { this._annotationTypeName = value; }
        }
    }
}
