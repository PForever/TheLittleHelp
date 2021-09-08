using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheLittleHelp.MultilevelList
{
    public static class TheLittleMultilevelListHelper
    {
        //TODO занести кастомный генератор нумерованных списков
        //private static readonly char Separator = PrintHelp.Separator[0];
        public static IList<int> ToNumbers(this string point, char separator = '.')
        {
            var list = new List<int>(point.Length);
            var spanPoint = point.AsSpan();
            while (spanPoint.Length > 1)
            {
                var len = spanPoint.IndexOf(separator) + 1;
                if (!int.TryParse(spanPoint.Slice(0, len - 1).ToString(), out int number)) throw new ArgumentException("Not valid point", nameof(point));
                list.Add(number);
                spanPoint = spanPoint.Slice(len);
            }
            return list;
        }
    }
}
