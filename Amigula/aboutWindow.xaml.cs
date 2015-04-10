using System.Diagnostics;
using System.Windows;

namespace Amigula
{
    /// <summary>
    /// Interaction logic for aboutWindow.xaml
    /// </summary>
    public partial class aboutWindow
    {
        public aboutWindow()
        {
            InitializeComponent();
            LblVersion.Content = "Version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnFeedback_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("mailto:dimitris@blitterstudio.com?subject=Amigula feedback");
        }
    }
}
