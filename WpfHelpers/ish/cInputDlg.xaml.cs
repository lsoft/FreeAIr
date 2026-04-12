#nullable disable
using System;
using System.Windows;

namespace WpfHelpers.ish
{
    /// <summary>
    /// Interaction logic for cInputDlg.xaml
    /// </summary>
    public partial class cInputDlg : Window
    {
        public enum InputValueType  {Text, Date };
        readonly InputValueType _inputValueType;
        readonly Func<object, string> _validator;
        public object Value { get; private set;  }
        Func<object> _getValue;

        public cInputDlg( string Label, object defaultValue = null,  string DlgTitle = "Введите значение",  InputValueType valueType = InputValueType.Text, Func<object,string> validator = null )
        {
            InitializeComponent();
            Title = DlgTitle;
            lbLabel.Content = Label;
            _inputValueType = valueType;
            _validator = validator;

            switch (_inputValueType)
            {
                case InputValueType.Date:
                    dpValue.Visibility = Visibility.Visible;
                    if (defaultValue != null && defaultValue is DateTime dt) dpValue.SelectedDate = dt;
                    dpValue.Focus();
                    _getValue = _getDatePickerValue;
                    break;
                default:
                    tbValue.Visibility = Visibility.Visible;
                    if (defaultValue != null && defaultValue is string s) tbValue.Text = s;
                    tbValue.Focus();
                    _getValue = _getTextValue;
                    break;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            object value = _getValue();
            string  errmsg = _validator?.Invoke(value);
            if (!string.IsNullOrEmpty(errmsg))
            {
                ErrorBlock.Text = errmsg;
                ErrorBlock.Visibility = Visibility.Visible;
                return;
            }

            Value = value;
            DialogResult = true;
            Close();
        }

        object _getTextValue() => tbValue.Text;
        object _getDatePickerValue() => (dpValue.SelectedDate is DateTime dt) ? dt : null;
    }
}
