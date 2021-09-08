using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TheLittleHelp.WinForms.SortableCollection.SortableBindingList
{
    public static class Extentions
    {
        public static SortableBindingList<T> ToSortableList<T>(this IList<T> src) => new SortableBindingList<T>(src);
        public static SortableBindingList<T> ToSortableList<T>(this IEnumerable<T> src) => new SortableBindingList<T>(src.ToList());
        public static async Task<SortableBindingList<T>> ToSortableListAsync<T>(this Task<List<T>> src) => new SortableBindingList<T>(await src);
    }
}
