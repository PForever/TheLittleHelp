using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheLittleHelp.System.TypeHelp;

namespace TheLittleHelp.WinForms.BaceHelp
{
    public class DisplayNameAttribute : Attribute
    {
        public string Name { get; }

        public DisplayNameAttribute(string name)
        {
            Name = name;
        }
    }
    public class NonDisplayAttribute : Attribute
    {
    }
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
        public static DataGridViewColumn[] CreateColumns<T>(params BindingSource[] sourses)
        {
            
            var src = sourses.ToDictionary(s => s.DataSource is Type t ? t : s.DataSource.GetType().GetGenericArguments()[0]);
            DataGridViewColumn GetColumn(Type t, PropertyInfo p, string displayName)
            {
                if (t == typeof(bool) || t == typeof(bool?)) return CreateCheckBoxColumn(p.Name, displayName);
                if (t.IsSimleType() || !src.ContainsKey(p.PropertyType)) return CreateTextColumn(p.Name, displayName);
                return CreateComboBoxColumn(p.Name, displayName, src[p.PropertyType]);
            }

            return (from p in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    where p.GetCustomAttributes(typeof(NonDisplayAttribute), false).Length == 0
                    let displayName = (p.GetCustomAttributes(typeof(DisplayNameAttribute), false).FirstOrDefault() as DisplayNameAttribute)?.Name ?? p.Name
                    select GetColumn(p.PropertyType, p, displayName)).ToArray();
        }
        public static DataGridViewTextBoxColumn CreateTextColumn(string propertyName, string displayName, bool readOnly = true, DataGridViewAutoSizeColumnMode autoSizeMode = DataGridViewAutoSizeColumnMode.AllCells)
        {
            return new DataGridViewTextBoxColumn()
            {
                Name = propertyName,
                HeaderText = displayName,
                DataPropertyName = propertyName,
                AutoSizeMode = autoSizeMode,
                ReadOnly = readOnly,
            };
        }
        public static DataGridViewCheckBoxColumn CreateCheckBoxColumn(string propertyName, string displayName, bool readOnly = true, DataGridViewAutoSizeColumnMode autoSizeMode = DataGridViewAutoSizeColumnMode.AllCells)
        {
            return new DataGridViewCheckBoxColumn()
            {
                Name = propertyName,
                HeaderText = displayName,
                DataPropertyName = propertyName,
                AutoSizeMode = autoSizeMode,
                ReadOnly = readOnly,
            };
        }
        public static DataGridViewComboBoxColumn CreateComboBoxColumn(string propertyName, string displayName, object dataSource, bool readOnly = true, DataGridViewAutoSizeColumnMode autoSizeMode = DataGridViewAutoSizeColumnMode.AllCells)
        {
            return new DataGridViewComboBoxColumn()
            {
                Name = propertyName,
                HeaderText = displayName,
                DataPropertyName = propertyName,
                AutoSizeMode = autoSizeMode,
                ReadOnly = readOnly,
                DataSource = dataSource,
            };
        }
        //public static DataGridViewComboBoxColumn CreateCheckBoxColumn(string propertyName, string displayName, object dataSource, string displayMember = "Name", string valueMember = "Id", bool readOnly = true, DataGridViewAutoSizeColumnMode autoSizeMode = DataGridViewAutoSizeColumnMode.AllCells)
        //{
        //    return new DataGridViewComboBoxColumn()
        //    {
        //        Name = propertyName,
        //        HeaderText = displayName,
        //        DataPropertyName = propertyName,
        //        DisplayMember = displayMember,
        //        ValueMember = valueMember,
        //        AutoSizeMode = autoSizeMode,
        //        ReadOnly = readOnly,
        //        DataSource = dataSource,
        //    };
        //}


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
        public static void SetSeleted<T>(this DataGridView dgv, T data, Func<T, T, bool> comparer)
        {
            var row = dgv.Rows.Cast<DataGridViewRow>().First(r => comparer((T)r.DataBoundItem, data));
            row.Selected = true;
        }
        public static void SetSeleted<T>(this DataGridView dgv, T data) where T : IEquatable<T>
        {
            var row = dgv.Rows.Cast<DataGridViewRow>().First(r => data.Equals((IEquatable<T>)r.DataBoundItem));
            row.Selected = true;
        }
        //public static Binding GetBinding(string propertyName, object dataSource, string dataMember, object @default)
        //{
        //    var binding = new Binding(propertyName, dataSource, dataMember, true);
        //    var provider = new BindingProvider(@default);
        //    binding.Format += provider.OnFormat;
        //    binding.Parse += provider.OnParse;
        //    return binding;
        //}

        public static void SetNullubleBinding(this Control control, string propertyName, object dataSource, string dataMember, object defaultMember)
        {
            var binding = new Binding(propertyName, dataSource, dataMember);
            control.DataBindings.Add(binding);
            var provider = new BindingProvider(defaultMember);
            binding.Format += provider.OnFormat;
            binding.Parse += provider.OnParse;
        }

        public static void SetNullubleBinding(this Control control, string propertyName, object dataSource, object defaultMember)
        {
            var binding = new Binding(propertyName, dataSource, "");
            control.DataBindings.Add(binding);
            var provider = new BindingProvider(defaultMember);
            binding.Format += provider.OnFormat;
            binding.Parse += provider.OnParse;
        }


        public static void SetNullubleComboBinding(this ComboBox control, object dataObject, string dataMember, object defaultMember, BindingSource dataSource)
        {
            dataSource.Insert(0, defaultMember);
            control.DataSource = dataSource;
            var binding = new Binding(nameof(ComboBox.SelectedItem), dataObject, dataMember, true);
            var provider = new BindingProvider(defaultMember);
            binding.Format += provider.OnFormat;
            binding.Parse += provider.OnParse;
            control.DataBindings.Add(binding);
        }

        public static void SetNullubleComboBinding(this ComboBox control, object dataSource, object defaultMember)
        {
            var binding = new Binding(nameof(ComboBox.SelectedItem), dataSource, "", true);
            control.DataBindings.Add(binding);
            var provider = new BindingProvider(defaultMember);
            binding.Format += provider.OnFormat;
            binding.Parse += provider.OnParse;
        }

        class BindingProvider
        {
            private object _defatultMember;
            public BindingProvider(object defatultMember)
            {
                _defatultMember = defatultMember;
            }

            public void OnParse(object sender, ConvertEventArgs e)
            {
                if (e.Value != null && e.Value.Equals(_defatultMember)) e.Value = null;
            }

            public void OnFormat(object sender, ConvertEventArgs e)
            {
                //if (e.Value != null && e.Value.Equals(_defatultMember)) e.Value = null;
                if (e.Value == null) e.Value = _defatultMember;
            }
        }
        public static T GetCurrent<T>(this DataGridView dgv) where T : class => dgv.SelectedCells is var cells && cells.Count == 1 && cells[0].RowIndex != -1 ? dgv.Rows[cells[0].RowIndex].DataBoundItem as T : default;
        public static void SetTextBoxBinding(this TextBox control, object dataSource, string memeberName)
        {
            control.DataBindings.Add(new Binding(nameof(TextBox.Text), dataSource, memeberName));
        }
        public static void SetBinding(this Control control, string propertyName, object dataSource, string memeberName)
        {
            control.DataBindings.Add(new Binding(propertyName, dataSource, memeberName));
        }
        public static void SetNullubleDateTimeBinding(this DateTimePicker control, string propertyName, object dataSource, string memeberName)
        {
            var binding = new Binding(propertyName, dataSource, memeberName, true);
            var provider = new DateTimeBindingProvider(control);
            binding.Parse += provider.OnParse;
            binding.Format += provider.OnFormat;
            control.DataBindings.Add(binding);
        }
        class DateTimeBindingProvider
        {
            private readonly DateTimePicker _dateTimePicker;
            public DateTimeBindingProvider(DateTimePicker dateTimePicker)
            {
                _dateTimePicker = dateTimePicker;
            }

            public void OnFormat(object sender, ConvertEventArgs e)
            {
                if (e.Value is null)
                {
                    e.Value = _dateTimePicker.Value;
                    _dateTimePicker.Checked = false;
                }
            }

            public void OnParse(object sender, ConvertEventArgs e)
            {
                if (_dateTimePicker.Checked) return;
                e.Value = default(DateTime?);
            }
        }

        public static void SetNullubleNumericUpDownBinding(this NumericUpDown control, CheckBox checkBox, object dataSource, string memeberName)
        {
            var binding = new Binding(nameof(NumericUpDown.Value), dataSource, memeberName, true);
            var provider = new NumericUpDownBindingProvider(control, checkBox, binding);
            binding.Parse += provider.OnParse;
            binding.Format += provider.OnFormat;
            checkBox.CheckStateChanged += provider.OnCheckedChanged;
            control.DataBindings.Add(binding);
        }

        class NumericUpDownBindingProvider
        {
            private readonly NumericUpDown _numericUpDown;
            private readonly CheckBox _checkBox;
            private readonly Binding _binding;

            public NumericUpDownBindingProvider(NumericUpDown numericUpDown, CheckBox checkBox, Binding binding)
            {
                _numericUpDown = numericUpDown;
                _checkBox = checkBox;
                _binding = binding;
            }

            public void OnFormat(object sender, ConvertEventArgs e)
            {
                if (e.Value is null)
                {
                    e.Value = _numericUpDown.Value;
                    if (_checkBox.Checked) _checkBox.Checked = false;
                    else _numericUpDown.Enabled = false;
                }
                else _checkBox.Checked = true;
            }

            public void OnParse(object sender, ConvertEventArgs e)
            {
                if (_checkBox.Checked)
                {
                    e.Value = _numericUpDown.Value;
                }
                else
                {
                    e.Value = null;//default(DateTime?);
                }
            }

            public void OnFormatCheckBox(object sender, ConvertEventArgs e)
            {
                if (e.Value is null)
                {
                    e.Value = false;
                }
                else
                {
                    e.Value = true;
                }
            }
            internal void OnCheckedChanged(object sender, EventArgs e)
            {
                _numericUpDown.Enabled = _checkBox.Checked;
                //_numericUpDown.Value = _numericUpDown.Value;
                _binding.WriteValue();
            }

        }
    }
}
