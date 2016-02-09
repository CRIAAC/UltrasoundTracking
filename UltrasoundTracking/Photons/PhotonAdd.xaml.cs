using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using WPFColorPickerLib;


namespace UltrasoundTracking.Photons
{
    /// <summary>
    /// Logique d'interaction pour PhotonAdd.xaml
    /// </summary>
    public partial class PhotonAdd
    {
        readonly Dictionary<string, PhotonGUI> _photons;

        public PhotonAdd(Dictionary<string, PhotonGUI> photons)
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _photons = photons;
        }

        private void buttonSelectColor_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog
            {
                SelectedColor = ((SolidColorBrush) PhotonColor.Fill).Color,
                Owner = this
            };
            if ((bool)colorDialog.ShowDialog())
                PhotonColor.Fill = new SolidColorBrush(colorDialog.SelectedColor);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            _photons.Add(
                TextBoxIP.Text + ":" + TextBoxPort.Text, 
                new PhotonGUI(
                    TextBoxIP.Text,
                    int.Parse(TextBoxPort.Text),
                    (Color)ColorConverter.ConvertFromString(PhotonColor.Fill.ToString())
                    )
                );
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
