using DevExpress.Mvvm.DataAnnotations;
using OLS.Casy.Models;
using System;

namespace OLS.Casy.Ui.MainControls.Models
{
    public class AnnotationTypeModel : IComparable, IComparable<AnnotationTypeModel>
    {
        private AnnotationType _associatedAnnotationType;

        public AnnotationTypeModel()
        {

        }

        [HiddenAttribute]
        public AnnotationType AssociatedAnnotationType
        {
            get
            {
                if (_associatedAnnotationType == null)
                {
                    _associatedAnnotationType = new AnnotationType();
                }
                return _associatedAnnotationType;
            }
            set { _associatedAnnotationType = value; }
        }

        public string Name
        {
            get { return AssociatedAnnotationType.AnnottationTypeName; }
            set
            {
                this.AssociatedAnnotationType.AnnottationTypeName = value;
            }
        }

        public int CompareTo(AnnotationTypeModel other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return 1;
            }

            return other.AssociatedAnnotationType.AnnotationTypeId.CompareTo(this.AssociatedAnnotationType.AnnotationTypeId);
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }
            AnnotationTypeModel other = obj as AnnotationTypeModel;
            if (other == null)
            {
                throw new ArgumentException("An AnnotationTypeModel object is required for comparison.", "obj");
            }
            return this.CompareTo(other);
        }

        public override bool Equals(object obj)
        {
            AnnotationTypeModel other = obj as AnnotationTypeModel;
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }
            return this.CompareTo(other) == 0;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + this.AssociatedAnnotationType.AnnotationTypeId.GetHashCode();
                return hash;
            }
        }

        public static int Compare(AnnotationTypeModel left, AnnotationTypeModel right)
        {
            if (object.ReferenceEquals(left, right))
            {
                return 0;
            }
            if (object.ReferenceEquals(left, null))
            {
                return -1;
            }
            return left.CompareTo(right);
        }


        public static bool operator ==(AnnotationTypeModel left, AnnotationTypeModel right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return object.ReferenceEquals(right, null);
            }
            return left.Equals(right);
        }
        public static bool operator !=(AnnotationTypeModel left, AnnotationTypeModel right)
        {
            return !(left == right);
        }
        public static bool operator <(AnnotationTypeModel left, AnnotationTypeModel right)
        {
            return (Compare(left, right) < 0);
        }
        public static bool operator >(AnnotationTypeModel left, AnnotationTypeModel right)
        {
            return (Compare(left, right) > 0);
        }

    }
}
