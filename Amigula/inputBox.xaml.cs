using System.Windows;

namespace Amigula
{
    /// <summary>
    /// Interaction logic for inputBox.xaml
    /// </summary>
    public partial class inputBox
    {
        public string TextValue => txtBox.Text;

        public inputBox(ref string value)
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
