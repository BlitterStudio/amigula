using System.Windows;

namespace Amigula
{
    /// <summary>
    /// Interaction logic for inputBox.xaml
    /// </summary>
    public partial class InputBox
    {
        public string TextValue
        {
            get { return txtBox.Text; }
        }

        public InputBox(ref string value)
        {
            InitializeComponent();
            txtBox.Text = value;
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
