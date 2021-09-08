using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TheLittleHelper.WinForms
{
    public static class TheLittleWinFormsHelper
    {
        public static CheckState ToCheckState(this bool? value)
        {
            switch (value)
            {
                case true:
                    return CheckState.Checked;
                case false:
                    return CheckState.Unchecked;
                case null:
                    return CheckState.Indeterminate;
                default: throw new Exception();
            }
        }
        public static bool? ToBool(this CheckState value)
        {
            switch (value)
            {
                case CheckState.Checked:
                    return true;
                case CheckState.Unchecked:
                    return false;
                case CheckState.Indeterminate:
                    return default;
                default: throw new Exception();
            }
        }
    }
}
