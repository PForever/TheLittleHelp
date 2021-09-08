
using TheLittleHelp.WinForms.SortableCollection.FilterHelp;

namespace TheLittleHelp.WinForms.SortableCollection.SortableBindingList
{
    public interface IDynamicFiltrable
    {
        void ApplyFilter(PropertiesFilter filter);
        void RemoveFilter();
    }

}
