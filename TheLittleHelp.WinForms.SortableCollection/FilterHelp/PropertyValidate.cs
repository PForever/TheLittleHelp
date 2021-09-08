using System;
using System.Linq;

namespace TheLittleHelp.WinForms.SortableCollection.FilterHelp
{
    public class PropertyValidate
    {
        public PropertyNamePosition Key { get; }
        public Type PropertyType { get; }
        public Delegate Validator { get; }
        public ILookup<string, object> SourceList { get; }
        public object Value { get; set; }

        public PropertyValidate(Delegate validator, object value, Type propertyType, PropertyNamePosition key, ILookup<string, object> sourceList)
        {
            SourceList = sourceList;
            Validator = validator ?? throw new ArgumentNullException(nameof(validator));
            Value = value;
            PropertyType = propertyType;
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public PropertyValidate(Delegate validator, Type propertyType, PropertyNamePosition key) : this((Func<object, bool>)(v => true), default, propertyType, key, null) { }
        public PropertyValidate(object value, Type propertyType, PropertyNamePosition key) : this((Func<object, bool>)(v => true), value, propertyType, key, null) { }
        public PropertyValidate(Type propertyType, PropertyNamePosition key) : this((Func<object, bool>)(v => true), null, propertyType, key, null) { }
        public PropertyValidate(PropertyNamePosition key) : this((Func<object, bool>)(v => true), null, typeof(object), key, null) { }

        public PropertyValidate(object value, Type propertyType, PropertyNamePosition key, ILookup<string, object> sourceList) : this((Func<object, bool>)(v => true), value, propertyType, key, sourceList) { }
        public PropertyValidate(Type propertyType, PropertyNamePosition key, ILookup<string, object> sourceList) : this((Func<object, bool>)(v => true), null, propertyType, key, sourceList) { }
        public PropertyValidate(PropertyNamePosition key, ILookup<string, object> sourceList) : this((Func<object, bool>)(v => true), null, typeof(object), key, sourceList) { }
    }
}
