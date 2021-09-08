using System;
using System.ComponentModel;

namespace TheLittleHelp.Collections.Old
{
    public class SortEventArgs : EventArgs
    {
        public SortEventArgs((string Property, ListSortDirection Direction) order)
        {
            Order = order;
        }
        public (string Property, ListSortDirection Direction) Order { get; }
    }
}
