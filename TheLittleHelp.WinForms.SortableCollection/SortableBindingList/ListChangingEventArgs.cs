using System.ComponentModel;

namespace TheLittleHelp.WinForms.SortableCollection.SortableBindingList
{
    public delegate void ListChangingEventHandler<T>(object sender, ListChangingEventArgs<T> args);
    public class ListChangingEventArgs<T> : CancelEventArgs
    {
        public ListChangedType ListChangedType { get; }
        public T Value1 { get; set; }
        public T Value2 { get; set; }
        public int Index1 { get; set; }
        public int Index2 { get; set; }
        public ListChangingEventArgs(ListChangedType listChangedType, T value, int index)
        {
            ListChangedType = listChangedType;
            Value1 = value;
            Index1 = index;
            Index2 = -1;
        }

        public ListChangingEventArgs(ListChangedType listChangedType, T value)
        {
            ListChangedType = listChangedType;
            Value1 = value;
            Index1 = -1;
            Index2 = -1;
        }

        public ListChangingEventArgs(ListChangedType listChangedType, T value1, int index1, T value2, int index2)
        {
            ListChangedType = listChangedType;
            Value1 = value1;
            Index1 = index1;
            Value2 = value2;
            Index2 = index2;
        }

        public ListChangingEventArgs(ListChangedType listChangedType)
        {
            ListChangedType = listChangedType;
        }
    }
}
