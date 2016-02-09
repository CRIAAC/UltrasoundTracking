using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using UltrasoundTracking.Photons;


namespace UltrasoundTracking.Sensors
{
    /// <summary>
    /// Logique d'interaction pour SensorManagement.xaml
    /// </summary>
    public partial class SensorManagement : Window
    {
        private Dictionary<string, PhotonGUI> _photons;

        public SensorManagement(Dictionary<string, PhotonGUI> photons)
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            _photons = photons;

            PhotonsCombo.ItemsSource = _photons.Keys;
            PhotonsCombo.SelectedIndex = 0;

            Closing += SensorManagement_Closing;
        }

        // Event: Window closing 
        private void SensorManagement_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MainWindow.WriteConfToJson();
        }

        // Event: New photon selected
        private void photonsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedPhoton = (sender as ComboBox).SelectedItem.ToString();
            SensorsGrid.ItemsSource = _photons[selectedPhoton].Sensors;
        }

        // Add a sensor
        private void buttonAddSensor_Click(object sender, RoutedEventArgs e)
        {
            SensorAdd sensorAddModal = new SensorAdd(PhotonsCombo.SelectedIndex);
            sensorAddModal.ShowDialog();
            SensorsGrid.Items.Refresh();
        }

        // Delete selected sensor
        private void buttonDeleteSensor_Click(object sender, RoutedEventArgs e)
        {
            int currentRowIndex = SensorsGrid.SelectedIndex;
            string selectedPhoton = PhotonsCombo.SelectedItem.ToString();

            try
            {
                _photons[selectedPhoton].Sensors.RemoveAt(currentRowIndex);
                SensorsGrid.Items.Refresh();
            } 
            catch (Exception ex)
            {
                new PopupMessage(ex.ToString()).ShowDialog();
            }
        }

        // Save sensors
        private void buttonSaveSensors_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.WriteConfToJson();
        }

    }
}
