using System.Windows;

namespace UltrasoundTracking
{
    /// <summary>
    /// Logique d'interaction pour PopupMessage.xaml
    /// </summary>
    public partial class PopupMessage : Window
    {
        public PopupMessage()
        {
            InitializeComponent();
        }

        public PopupMessage(string txt)
        {
            InitializeComponent();
            PopupMessageText.Text = txt;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void popupConfirmation_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
