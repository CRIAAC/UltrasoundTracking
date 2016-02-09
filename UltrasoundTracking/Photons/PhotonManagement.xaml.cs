using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using WPFColorPickerLib;

namespace UltrasoundTracking.Photons
{
    /// <summary>
    /// Logique d'interaction pour PhotonManagement.xaml
    /// </summary>
    public partial class PhotonManagement
    {
        List<PhotonGUI> _photons;

        public PhotonManagement(Dictionary<string, PhotonGUI> photons)
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            _photons = photons.Values.ToList();

            PhotonsGrid.ItemsSource = _photons;
            PhotonsGrid.AutoGeneratingColumn += (sender, args) =>
            {
                args.Column.Visibility = args.PropertyName == "Col2" ? Visibility.Hidden : Visibility.Visible ;
            };
                  

            Loaded += OnLoaded;
            Closing += OnClosing;

            
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            PhotonsGrid.Columns.Where(x => (string)x.Header == "Col").ElementAt(0).IsReadOnly = true;
            PhotonsGrid.Columns.Where(x => (string)x.Header == "Sensors").ElementAt(0).IsReadOnly = true;
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            savePhotons();
        }

        private void buttonAddPhoton_Click(object sender, RoutedEventArgs e)
        {
            PhotonAdd photonAddModal = new PhotonAdd(MainWindow.DictPhoton);
            photonAddModal.ShowDialog();
            _photons = MainWindow.DictPhoton.Values.ToList();
            PhotonsGrid.ItemsSource = null;
            PhotonsGrid.ItemsSource = _photons;
            PhotonsGrid.Items.Refresh();
        }

        private void buttonSavePhotons_Click(object sender, RoutedEventArgs e)
        {
            savePhotons();
        }

        private void buttonDeletePhoton_Click(object sender, RoutedEventArgs e)
        {
            int currentRowIndex = PhotonsGrid.SelectedIndex;
            if (currentRowIndex >= 0)
            {
                try
                {
                    _photons.RemoveAt(currentRowIndex);
                    PhotonsGrid.Items.Refresh();
                }
                catch (Exception ex)
                {
                    new PopupMessage(ex.ToString()).ShowDialog();
                }
            }
        }

        private void PhotonsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PhotonsGrid.SelectedItem == null) return;
            if (PhotonsGrid.CurrentColumn.Header.ToString() != "Col") return;

            var selectedItem = PhotonsGrid.SelectedItem as PhotonGUI;
            ColorDialog colorDialog = new ColorDialog
            {
                SelectedColor = selectedItem.Col,
                Owner = this
            };
            if ((bool)colorDialog.ShowDialog())
                selectedItem.Col = Color.FromArgb(colorDialog.SelectedColor.A, colorDialog.SelectedColor.R, colorDialog.SelectedColor.G, colorDialog.SelectedColor.B);

            PhotonsGrid.Items.Refresh();
        }

        private void savePhotons()
        {
            MainWindow.DictPhoton.Clear();

            foreach (PhotonGUI photon in _photons)
            {
                MainWindow.DictPhoton.Add(photon.IP + ":" + photon.Port, new PhotonGUI(photon.FriendlyName, photon.IP, photon.Port, photon.Sensors, photon.Col));
            }
        }
    }
}
