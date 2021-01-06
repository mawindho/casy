using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.IO.SQLite
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DateTimeKindAttribute : Attribute
    {
        private readonly DateTimeKind _kind;

        public DateTimeKindAttribute(DateTimeKind kind)
        {
            _kind = kind;
        }

        public DateTimeKind Kind
        {
            get { return _kind; }
        }

        public static void Apply(object entity)
        {
            if (entity == null)
                return;

            var properties = entity.GetType().GetProperties()
                .Where(x => x.PropertyType == typeof(DateTime) || x.PropertyType == typeof(DateTime?));

            foreach (var propertyInfo in properties)
            {
                var attr = propertyInfo.GetCustomAttribute<DateTimeKindAttribute>();
                if (attr == null)
                    continue;

                var dt = propertyInfo.PropertyType == typeof(DateTime?)
                    ? (DateTime?) propertyInfo.GetValue(entity)
                    : (DateTime) propertyInfo.GetValue(entity);

                if(dt == null) continue;
                
                propertyInfo.SetValue(entity, DateTime.SpecifyKind(dt.Value, attr.Kind));
            }
        }
    }
}
