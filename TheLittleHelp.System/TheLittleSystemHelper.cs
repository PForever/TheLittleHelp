using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.IO;

namespace TheLittleHelp.System.SystemHelp
{
    public static class TheLittleSystemHelper
    {
        //public static void OpenFilterEdit(IDynamicFiltrable list, PropertiesFilter filter)
        //{
        //    if (filter == null) return;
        //    var filterEditor = new FilterEditor(filter);
        //    switch (filterEditor.ShowDialog())
        //    {
        //        case DialogResult.OK:
        //            list.ApplyFilter(filter);
        //            break;
        //        case DialogResult.Cancel:
        //            break;
        //        case DialogResult.Abort:
        //            list.RemoveFilter();
        //            break;
        //    }
        //}

        public static Exception GetInnerException(Exception e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            e = e.InnerException;
            while (e.InnerException != null) e = e.InnerException;
            return e;
        }

        [Conditional("DEBUG")]
        public static void ThrowIfDebug<T>(this T e)
            where T : Exception
        {
            throw e;
        }
        [Conditional("DEBUG")]
        public static void InfoIfDebug(string message, Action<string> onInfo = null, [CallerFilePath] string path = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = -1)
        {
            using (var writer = new StringWriter())
            {
                var separator = new string('-', 20);
                writer.WriteLine(separator);
                writer.WriteLine($"Path: {path}");
                writer.WriteLine($"Method: {method}");
                writer.WriteLine($"Line: {line}");
                writer.WriteLine($"Message: \"{message}\"");
                writer.WriteLine(separator);
                if (onInfo == null) Debug.WriteLine(writer.ToString());
                else onInfo(writer.ToString());
            }
        }
        
        public static bool TryParsTo<T>(this object obj, out T result)
        {
            if (obj is T res)
            {
                result = res;
                return true;
            }
            result = default(T);
            return false;
        }
        
        public static T Swith<T>(params (bool ExpresionResult, T Result)[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].ExpresionResult) return values[i].Result;
            }
            return default;
        }
    }
}
