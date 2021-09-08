using System;
using System.Globalization;

namespace TheLittleHelp.System.DateTimeHelp
{
    /// <summary>
    /// Class for help for usial work with datetime
    /// </summary>
    public static class TheLittleDateTimeHelper
    {
        /// <summary>
        /// Print datetime to string in invariant culture
        /// <code lang="csharp">
        ///     new DateTime(2001, 06, 22, 23, 53, 12).DateToInv() //"06/22/2001 23:53:12"
        /// </code>
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string DateToInv(this DateTime date) => date.ToString(CultureInfo.InvariantCulture);
        /// <summary>
        /// Print date only to string in invariant culture
        /// <code lang="csharp">
        ///     new DateTime(2001, 06, 22, 23, 53, 12).DateToInvShort() //"06/22/2001"
        /// </code>
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string DateToInvShort(this DateTime date) => date.ToString("d", CultureInfo.InvariantCulture);
        /// <summary>
        /// Check current culture is invariant
        /// <code lang="csharp">
        ///     CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("RU-ru");
        ///     Debug.WriteLine(IsInvariant); //false
        ///     CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        ///     Debug.WriteLine(IsInvariant); //true
        /// </code> 
        /// </summary>
        public static readonly bool IsInvariant = Equals(CultureInfo.CurrentCulture, CultureInfo.InvariantCulture);
        /// <summary>
        /// Convert string printed from datetime with invariant culture to string printed from the same datetime with current culture
        /// <code lang="csharp">
        ///      "22.06.2001 23:53:12".InvToCur() //"22.06.2001 23:53:12"
        /// </code>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string InvToCur(this string value) => IsInvariant ? value : value.InvToDateTime()?.ToString();
        /// <summary>
        /// Convert string printed from datetime with invariant culture to string printed from the same datetime with current culture
        /// <code lang="csharp">
        ///     "22.06.2001 23:53:12".CurToInv() //"06/22/2001 23:53:12"
        /// </code>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string CurToInv(this string value) => IsInvariant ? value : value.ToDateTime()?.ToString(CultureInfo.InvariantCulture);
        /// <summary>
        /// Convert invariant string to nulluble datetime (<value>null</value> if can't convert)
        /// <code lang="csharp">
        ///      "06/22/2001 23:53:12".InvToDateTime()
        /// </code>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DateTime? InvToDateTime(this string value) => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime datetime) ? (DateTime?)datetime : null;
        /// <summary>
        /// Convert string of current culture to nulluble datetime (<value>null</value> if can't convert)
        /// <code lang="csharp">
        ///      "22.06.2001 23:53:12".ToDateTime()
        /// </code>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DateTime? ToDateTime(this string value) => DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime datetime) ? (DateTime?)datetime : null;
    }
}
