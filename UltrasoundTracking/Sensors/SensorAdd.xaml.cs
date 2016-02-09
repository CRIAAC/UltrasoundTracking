using System.Collections.Generic;
using System.Windows;

namespace UltrasoundTracking.Sensors
{
    /// <summary>
    /// Logique d'interaction pour SensorAdd.xaml
    /// </summary>
    public partial class SensorAdd : Window
    {
        public SensorAdd()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            List<string> data = new List<string> {"Up", "Down", "Left", "Right"};

            ComboPhoton.ItemsSource = MainWindow.DictPhoton.Keys;
            ComboPhoton.SelectedIndex = 0;

            Orientation.ItemsSource = data;
            Orientation.SelectedIndex = 0;
        }

        public SensorAdd(int photonComboIndex) : this()
        {
            ComboPhoton.SelectedIndex = photonComboIndex;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string photon = ComboPhoton.SelectedItem.ToString();
            foreach (Sensor s in MainWindow.DictPhoton[photon].Sensors)
                if (s.SensorID == int.Parse(SensorId.Text))
                {
                    new PopupMessage("A sensor with this ID already exists for this photon. Please check your input or delete the existing entry.").ShowDialog();
                    return;
                }

            Sensor.Direction dir;
            switch (Orientation.SelectedItem.ToString().ToUpper())
            {
                case "UP":
                    dir = Sensor.Direction.Up;
                    break;
                case "DOWN":
                    dir = Sensor.Direction.Down;
                    break;
                case "LEFT":
                    dir = Sensor.Direction.Left;
                    break;
                case "RIGHT":
                    dir = Sensor.Direction.Right;
                    break;
                default:
                    dir = Sensor.Direction.Down;
                    break;

            }
            MainWindow.DictPhoton[photon].Sensors.Add(new Sensor(
                int.Parse(SensorId.Text),
                double.Parse(X.Text),
                double.Parse(Y.Text),
                double.Parse(Z.Text),
                dir,
                true,
                5));

            Close();
        }
    }
}
