using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace CuislVSIX
{
    public partial class SeparatorDialog
    {
        public SeparatorDialog()
        {
            this.InitializeComponent();
        }

        public Regex Input
        {
            get
            {
                var txt = this.txtName.Text;
                var isRgx = (Boolean)this.chkRegex.IsChecked;
                return new Regex(String.Format("({0})", isRgx ? txt : Regex.Escape(txt)));
            }
        }

        private void Button_Click(Object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
