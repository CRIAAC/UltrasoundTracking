using UDPPhotonLibrary;
using UltrasoundTracking.Photons;
using UltrasoundTracking.Sensors;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Controls;
using UltrasoundTracking.Commons;
using Newtonsoft.Json;

namespace UltrasoundTracking
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private enum AcquisitionState { On, Off }
        private readonly DispatcherTimer _aTimer;
        public static Dictionary<string, PhotonGUI> DictPhoton; // Photons
        //public static Dictionary<string,string> ComputedData { get; set; }  // Computed positions, speeds, ... 
        public static List<MovingEntity> MovingEntities;

        private readonly PhotonManager _manager;

        private bool _isCalibrating;
        private readonly int _calibrationLength = 5;

        private double _mapScale;
        private double _sensorCircleSize;
        private AcquisitionState _acquisitionState;

        public MainWindow()
        {
            // Window initialization
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            CultureInfo customCulture = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            Thread.CurrentThread.CurrentCulture = customCulture;

            // Timer
            _aTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
            _aTimer.Tick += _timer_Tick;

            // Photons
            DictPhoton = new Dictionary<string, PhotonGUI>();

            // Computed data
            //ComputedData = new Dictionary<string, string>();

            // Other items to display on map
            MovingEntities = new List<MovingEntity>();

            // Photon manager
            _manager = new PhotonManager();
            _manager.DataReceived += ManagerDataReceived;
            _acquisitionState = AcquisitionState.Off;

            // Calibration state initialised to false
            _isCalibrating = false;

            // Read conf from local json file
            ReadConfFromJson();

            RefreshManagerPhotons();
            UpdateUI();

            // Window events
            Loaded += MainWindow_Loaded;
            SizeChanged += MainWindow_Resized;
            StateChanged += MainWindow_StateChanged;
            Closing += MainWindow_Closing;
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            DataTest.Text = "";
            MovingEntities.Clear();
            // Update moving entities on the map
            List<MovingEntity> newMovingEntities = DataCompute.NormalizeMovingEntities();
            if (MovingEntities.Count == 0)
                MovingEntities.AddRange(newMovingEntities);
            else
            {
                foreach (MovingEntity movingEntity in MovingEntities)
                {
                    foreach (MovingEntity newMovingEntity in newMovingEntities)
                    {
                        // If previous point wasn't moving and newMovingEntity is close to movingEntity
                        if (movingEntity.IsIdle &&
                            DataCompute.DistanceBetweenPoints(movingEntity.Position, newMovingEntity.Position) <= 30)
                        {
                            // Update movingEntity with newMovingEntity's position
                            movingEntity.UpdatePosition(newMovingEntity.Position.X, newMovingEntity.Position.Y);
                        }
                        // Else, if movingEntity was moving and newMovingEntity is going in the same direction
                        else if (!movingEntity.IsIdle &&
                                 DataCompute.AngleBetweenVectors(movingEntity, newMovingEntity) < 45 &&
                                 DataCompute.MagnitudeBetweenVectors(movingEntity, newMovingEntity) < 20)
                        {
                            // Update movingEntity with newMovingEntity's position
                            movingEntity.UpdatePosition(newMovingEntity.Position.X, newMovingEntity.Position.Y);
                        }
                    }
                }
            }

            // Refresh the Raw Data tabs with values from the sensors + lines on the canvas
            // TODO Optimization: Manage this directly through the XAML (see other TODO there)
            foreach (string photonKey in DictPhoton.Keys)
            {
                foreach (Sensor sensor in DictPhoton[photonKey].Sensors)
                {
                    int tabIndex = -1;
                    List<TabItem> tabItems = PhotonsTabs.Items.OfType<TabItem>().ToList();

                    for (int i = 0; i < tabItems.Count; i++)
                    {
                        if (
                            ((Label)((StackPanel)tabItems[i].Header).Children[0]).Content.ToString()
                                .Contains(photonKey))
                        {
                            tabIndex = i;
                            break;
                        }
                    }

                    if (tabIndex >= 0)
                    {
                        ListBoxItem item = ((ListBox)((TabItem)PhotonsTabs.Items[tabIndex]).Content).Items
                            .OfType<ListBoxItem>()
                            .ToList()
                            .Find(x => x.Name.Equals("sensorItem" + sensor.SensorID));

                        if (item != null)
                            item.Content = sensor.SensorID + ": " + sensor.SignalLength + " (" + sensor.StandByValue + ")";
                    }
                }
            }
            DrawSensorLine();

            // Draw detected entities on the canvas
            DrawMovingEntities();

            DataTest.Text = MovingEntities.Count > 0 ? MovingEntities.Count +" "+ MovingEntities[0].DeltaX +" "+ MovingEntities[0].DeltaY +" "+ MovingEntities[0]._lastKnownPositions.Count: "";
        }



        // Actions when window loaded
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateUI();
            MinHeight = ActualHeight;
        }

        private void MainWindow_Resized(object sender, RoutedEventArgs e)
        {
            UpdateUI();
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            switch (WindowState)
            {
                case WindowState.Maximized:
                    UpdateUI();
                    break;
                case WindowState.Normal:
                    UpdateUI();
                    break;
                case WindowState.Minimized:
                    break;
            }
        }

        // Actions when closing the window
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            _aTimer.Stop();
            _manager.Stop();
            Application.Current.Shutdown();
        }

        // Open config
        private void MenuItem_File_Config_Click(object sender, RoutedEventArgs e)
        {

        }

        // Exit application (File > Exit menu)
        private void MenuItem_File_Exit_Click(object sender, RoutedEventArgs e)
        {
            _aTimer.Stop();
            _manager.Stop();
            Application.Current.Shutdown();
        }

        // Add sensor (modal)
        private void SensorsAdd_Click(object sender, RoutedEventArgs e)
        {
            StopAcquisition();

            SensorAdd sensorAddModal = new SensorAdd();
            sensorAddModal.ShowDialog();

            WriteConfToJson();
            UpdateUI();
        }

        // Manage sensors (modal)
        private void SensorsMgmt_Click(object sender, RoutedEventArgs e)
        {
            StopAcquisition();

            SensorManagement sensorManageModal = new SensorManagement(DictPhoton);
            sensorManageModal.ShowDialog();

            WriteConfToJson();
            UpdateUI();
        }

        // Add photon (modal)
        private void MenuItem_Photons_Add_Click(object sender, RoutedEventArgs e)
        {
            StopAcquisition();

            PhotonAdd photonAddModal = new PhotonAdd(DictPhoton);
            photonAddModal.ShowDialog();

            WriteConfToJson();
            RefreshManagerPhotons();
            UpdateUI();
        }

        // Manage photons (modal)
        private void MenuItem_Photons_Management_Click(object sender, RoutedEventArgs e)
        {
            StopAcquisition();

            try
            {
                PhotonManagement photonManagementModal = new PhotonManagement(DictPhoton);
                photonManagementModal.ShowDialog();

                WriteConfToJson();
                RefreshManagerPhotons();
                UpdateUI();
                PhotonsTabs.Items.Refresh();
            }
            catch (Exception ex)
            {
                new PopupMessage(ex.Message).ShowDialog();
            }
        }

        // Toggle acquisition On/Off
        private void MenuToggleAcquisition_OnClick(object sender, RoutedEventArgs e)
        {
            ToggleAcquisition();
        }

        // Raw data display On/Off
        private void MenuRawData_Click(object sender, RoutedEventArgs e)
        {
            if (MenuRawData.IsChecked != null)
            {
                if ((bool)MenuRawData.IsChecked)
                    RawDataPanel.Height = double.NaN;
                else
                    RawDataPanel.Height = 0;
            }

            UpdateUI();
        }

        // Raw computed data On/Off
        private void MenuComputedData_Click(object sender, RoutedEventArgs e)
        {
            if (MenuComputedData.IsChecked != null)
            {
                if ((bool)MenuComputedData.IsChecked)
                    ComputedDataPanel.Width = double.NaN;
                else
                    ComputedDataPanel.Width = 0;
            }

            UpdateUI();
        }

        private void MenuCalibrate_Click(object sender, RoutedEventArgs e)
        {
            Window.IsEnabled = false;

            if (_acquisitionState == AcquisitionState.Off)
                ToggleAcquisition();

            _isCalibrating = true;
            Thread.Sleep(_calibrationLength * 1000);
            _isCalibrating = false;

            foreach (var photonKey in DictPhoton.Keys)
            {
                foreach (Sensor sensor in DictPhoton[photonKey].Sensors)
                {
                    if (sensor.calibrationValues != null && sensor.calibrationValues.Count > 0)
                    {
                        sensor.calibrationValues.Sort();
                        if (sensor.calibrationValues.Count % 2 == 0 && sensor.calibrationValues.Count > 0)
                        {
                            sensor.StandByValue =
                                (sensor.calibrationValues[sensor.calibrationValues.Count / 2] +
                                 sensor.calibrationValues[sensor.calibrationValues.Count / 2] - 1) / 2;
                        }
                        else
                        {
                            sensor.StandByValue = sensor.calibrationValues[sensor.calibrationValues.Count / 2];
                        }
                    }
                }
            }

            WriteConfToJson();
            Window.IsEnabled = true;

        }

        // Update Map sensors + data displayed
        private void UpdateUI()
        {
            _mapScale = RoomMap.ActualWidth / 392; // inches (bathroom included, 327in if bathroom isn't on the map)
            _sensorCircleSize = _mapScale * 8;

            PhotonsTabs.Items.Clear();

            foreach (string photonKey in DictPhoton.Keys)
            {
                // New tab with name equal to the photon key
                TabItem newTab = new TabItem();
                StackPanel tabHeaderPanel = new StackPanel { Orientation = Orientation.Horizontal };

                Label tabLabel = new Label { Content = photonKey };
                tabHeaderPanel.Children.Add(tabLabel);

                Ellipse tabSensorColor = new Ellipse();
                tabSensorColor.Width = tabSensorColor.Height = 10;
                tabSensorColor.Fill = new SolidColorBrush(DictPhoton[photonKey].Col);
                tabSensorColor.Stroke = new SolidColorBrush(DictPhoton[photonKey].Col);

                tabHeaderPanel.Children.Add(tabSensorColor);

                newTab.Header = tabHeaderPanel;

                // New listbox which will contain all ou 
                ListBox photonRawInfo = new ListBox();
                photonRawInfo.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);

                FrameworkElementFactory factoryPanel = new FrameworkElementFactory(typeof(WrapPanel));
                factoryPanel.SetValue(WrapPanel.OrientationProperty, Orientation.Vertical);

                ItemsPanelTemplate template = new ItemsPanelTemplate { VisualTree = factoryPanel };
                photonRawInfo.ItemsPanel = template;

                //// Running through registered sensors
                for (int i = 0; i < DictPhoton[photonKey].Sensors.Count; i++)
                {
                    Sensor s = DictPhoton[photonKey].Sensors[i];
                    if (s.Enabled)
                    {
                        // Adds entry to the photon tab
                        ListBoxItem sensorItem = new ListBoxItem
                        {
                            Content = i + ": ",
                            Width = 100,
                            Name = "sensorItem" + i
                        };
                        photonRawInfo.Items.Add(sensorItem);
                    }
                }
                newTab.Content = photonRawInfo;

                PhotonsTabs.Items.Insert(PhotonsTabs.Items.Count, newTab);
            }

            DrawEntities();
        }

        // Draw map entities on the canvas
        private void DrawEntities()
        {
            MapEntitiesCanvas.Children.OfType<Ellipse>().ToList().ForEach(ellipse => MapEntitiesCanvas.Children.Remove(ellipse));
            MapEntitiesCanvas.Children.OfType<Line>().ToList().ForEach(line => MapEntitiesCanvas.Children.Remove(line));

            DrawSensors();
            DrawMovingEntities();
        }

        // Draws lines based on data received from photons
        private void DrawSensors()
        {
            double widthDifference = MapEntitiesCanvas.ActualWidth - RoomMap.ActualWidth;

            foreach (string photonKey in DictPhoton.Keys)
            {
                // Running through registered sensors
                foreach (Sensor s in DictPhoton[photonKey].Sensors)
                {
                    if (s.Enabled)
                    {
                        // Adds dot on the map if enabled
                        Ellipse newSensor = new Ellipse();
                        newSensor.Name = "Sensor";
                        newSensor.Margin =
                            new Thickness(s.Position.X * _mapScale - _sensorCircleSize / 2 + widthDifference / 2,
                                s.Position.Y * _mapScale - _sensorCircleSize / 2, 0, 0);
                        newSensor.Width = newSensor.Height = _sensorCircleSize;
                        newSensor.Fill = new SolidColorBrush(DictPhoton[photonKey].Col);
                        newSensor.Stroke = new SolidColorBrush(DictPhoton[photonKey].Col);

                        MapEntitiesCanvas.Children.Add(newSensor);

                        // Adds line from dot to distance returned by the Photon
                        DrawSensorLine(newSensor, s, photonKey);
                    }
                }
            }
        }

        private void DrawSensorLine()
        {
            double widthDifference = MapEntitiesCanvas.ActualWidth - RoomMap.ActualWidth;
            MapEntitiesCanvas.Children.OfType<Line>().ToList().FindAll(line => line.Name == "SensorLine").ForEach(line => MapEntitiesCanvas.Children.Remove(line));

            Ellipse newSensor = new Ellipse();
            foreach (string photonKey in DictPhoton.Keys)
            {
                // Running through registered sensors
                foreach (Sensor s in DictPhoton[photonKey].Sensors)
                {
                    if (s.Enabled)
                    {
                        newSensor.Margin = new Thickness(s.Position.X * _mapScale - _sensorCircleSize / 2 + widthDifference / 2, s.Position.Y * _mapScale - _sensorCircleSize / 2, 0, 0);
                        DrawSensorLine(newSensor, s, photonKey);
                    }
                }
            }
        }

        private void DrawSensorLine(Ellipse sensorEllipse, Sensor s, string photonKey)
        {
            // Adds line from dot to distance returned by the Photon
            Line sensorLine = new Line
            {
                Name = "SensorLine",
                X1 = sensorEllipse.Margin.Left + _sensorCircleSize / 2,
                Y1 = sensorEllipse.Margin.Top + _sensorCircleSize / 2
            };
            switch (s.SignalDirection)
            {
                case Sensor.Direction.Down:
                    sensorLine.X2 = sensorLine.X1;
                    sensorLine.Y2 = sensorLine.Y1 + s.SignalLength * _mapScale;
                    break;
                case Sensor.Direction.Up:
                    sensorLine.X2 = sensorLine.X1;
                    sensorLine.Y2 = sensorLine.Y1 - s.SignalLength * _mapScale;
                    break;
                case Sensor.Direction.Left:
                    sensorLine.X2 = sensorLine.X1 - s.SignalLength * _mapScale;
                    sensorLine.Y2 = sensorLine.Y1;
                    break;
                case Sensor.Direction.Right:
                    sensorLine.X2 = sensorLine.X1 + s.SignalLength * _mapScale;
                    sensorLine.Y2 = sensorLine.Y1;
                    break;
            }
            sensorLine.Stroke = new SolidColorBrush(DictPhoton[photonKey].Col);
            sensorLine.StrokeDashArray = new DoubleCollection() { 2, 2 };
            MapEntitiesCanvas.Children.Add(sensorLine);
        }

        private void DrawMovingEntities()
        {
            MapEntitiesCanvas.Children.OfType<Ellipse>().ToList().FindAll(ellipse => ellipse.Name == "MovingEntity").ForEach(ellipse => MapEntitiesCanvas.Children.Remove(ellipse));
            double widthDifference = MapEntitiesCanvas.ActualWidth - RoomMap.ActualWidth;
            foreach (MovingEntity movingEntity in MovingEntities)
            {
                Ellipse newEntity = new Ellipse()
                {
                    Name = "MovingEntity",
                    Margin =
                        new Thickness(movingEntity.Position.X * _mapScale - _sensorCircleSize / 2 + widthDifference / 2,
                            movingEntity.Position.Y * _mapScale - _sensorCircleSize / 2, 0, 0),
                    Width = _sensorCircleSize,
                    Height = _sensorCircleSize,
                    Fill = new SolidColorBrush(Colors.Red),
                    Stroke = new SolidColorBrush(Colors.Black)
                };
                MapEntitiesCanvas.Children.Add(newEntity);

                //new PopupMessage("entity size:" + newEntity.Width + " ; " + newEntity.Height).ShowDialog();
            }
        }

        // Manage data received from photons
        private void ManagerDataReceived(PhotonData data)
        {
            // Store data receive to sensors in photons
            List<long> photonData = data.ListMaxSonarSensor;
            string photonKey = DictPhoton.Keys.ToList().Find(x => x.Contains(data.Photon));

            foreach (Sensor sensor in DictPhoton[photonKey].Sensors)
            {
                sensor.SignalLength = photonData[sensor.SensorID];

                if (_isCalibrating)
                {
                    if (sensor.calibrationValues == null)
                        sensor.calibrationValues = new List<double>();

                    sensor.calibrationValues.Add(sensor.SignalLength);
                }
            }
        }

        // Read from file (conf.json)
        private static void ReadConfFromJson()
        {
            try
            {
                StreamReader file = new StreamReader("conf.json");
                string jsonText = file.ReadToEnd();
                file.Close();

                // Clear photons and sensors data
                DictPhoton.Clear();
                DictPhoton = JsonConvert.DeserializeObject<Dictionary<string, PhotonGUI>>(jsonText);
            }
            catch (FileNotFoundException)
            {
                new PopupMessage("File \"conf.json\" cannot be found").ShowDialog();
            }
            catch (Exception e)
            {
                PopupMessage p = new PopupMessage(e.ToString());
                p.ShowDialog();
            }
        }

        // Write protons and sensors conf to file (conf.json)
        public static void WriteConfToJson()
        {
            string jsonText = JsonConvert.SerializeObject(DictPhoton, Formatting.Indented);

            StreamWriter file = new StreamWriter("conf.json");
            file.Write(jsonText);
            file.Close();
        }

        // Refresh photons managed by the PhotonManager
        private void RefreshManagerPhotons()
        {
            _manager.Stop();

            Photon dummy;
            while (!_manager.Photons.IsEmpty)
                _manager.Photons.TryTake(out dummy);

            foreach (string photonKey in DictPhoton.Keys)
                _manager.AddPhoton(DictPhoton[photonKey].IP, DictPhoton[photonKey].Port);

            if (_acquisitionState == AcquisitionState.On)
                _manager.Start();
        }

        private void ToggleAcquisition()
        {
            if (_acquisitionState == AcquisitionState.On)
                StopAcquisition();
            else
                StartAcquisition();
        }

        private void StopAcquisition()
        {
            _aTimer.Stop();
            _manager.Stop();
            _acquisitionState = AcquisitionState.Off;
            AcquisitionStateCircle.Fill = new SolidColorBrush(Colors.Red);
            AcquisitionStateCircle.Stroke = new SolidColorBrush(Colors.Red);
            AcquisitionStatusLabel.Text = "Acquisition OFF";

            foreach (PhotonGUI photon in DictPhoton.Values)
            {
                foreach (Sensor sensor in photon.Sensors)
                {
                    sensor.SignalLength = 0;
                }
            }

            UpdateUI();
        }

        private void StartAcquisition()
        {
            _aTimer.Start();
            _manager.Start();
            _acquisitionState = AcquisitionState.On;
            AcquisitionStateCircle.Fill = new SolidColorBrush(Colors.Green);
            AcquisitionStateCircle.Stroke = new SolidColorBrush(Colors.Green);
            AcquisitionStatusLabel.Text = "Acquisition ON";
        }
    }
}