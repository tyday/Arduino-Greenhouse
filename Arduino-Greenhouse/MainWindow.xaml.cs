using Arduino_Greenhouse.Model;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Arduino_Greenhouse
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort ComPort = new SerialPort();

        delegate void SetTextCallback(string text);
        string InputData = String.Empty;

        public MainWindow()
        {
            InitializeComponent();
            ComPort.DataReceived +=
              new System.IO.Ports.SerialDataReceivedEventHandler(port_DataReceived_1);
        }

        private void btnGetPorts_Click(object sender, RoutedEventArgs e)
        {
            string[] ArrayComPortsNames = null;
            int index = -1;
            string ComPortName = null;

            ArrayComPortsNames = SerialPort.GetPortNames();
            do
            {
                index += 1;
                cboPorts.Items.Add(ArrayComPortsNames[index]);
            }
            while (!((ArrayComPortsNames[index] == ComPortName) ||
                    (index == ArrayComPortsNames.GetUpperBound(0))));
            Array.Sort(ArrayComPortsNames);

            if (index == ArrayComPortsNames.GetUpperBound(0))
            {
                ComPortName = ArrayComPortsNames[0];
            }
            cboPorts.Text = ArrayComPortsNames[0];
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (btnConnect.Content.ToString() == "Connect")
            {
                btnConnect.Content = "Close";
                ComPort.PortName = Convert.ToString(cboPorts.Text);
                //ComPort.BaudRate = Convert.ToInt32(cboBaudRate.Text);
                ComPort.BaudRate = 9600;
                //ComPort.DataBits = Convert.ToInt16(cboDataBits.Text);
                //ComPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), cboStopBits.Text);
                //ComPort.Handshake = (Handshake)Enum.Parse(typeof(Handshake), cboHandShaking.Text);
                //ComPort.Parity = (Parity)Enum.Parse(typeof(Parity), cboParity.Text);
                ComPort.Open();
            }
            else if (btnConnect.Content.ToString() == "Close")
            {
                btnConnect.Content = "Connect";
                ComPort.Close();
            }
        }

        private void port_DataReceived_1(object sender, SerialDataReceivedEventArgs e)
        {
            //InputData = ComPort.ReadExisting();
            InputData = ComPort.ReadLine();
            if (!string.IsNullOrEmpty(InputData))
            {
                //Console.WriteLine(InputData);
                try
                {
                    SensorReading sensorReading = JsonConvert.DeserializeObject<SensorReading>(InputData);
                    SetText(sensorReading);

                    string dbName = "Sensor.db";
                    string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string dbPath = System.IO.Path.Combine(folderPath, dbName);

                    using (SQLiteConnection connection = new SQLiteConnection(dbPath))
                    {
                        connection.CreateTable<SensorReading>();
                        connection.Insert(sensorReading);
                    }

                }
                catch (Newtonsoft.Json.JsonReaderException) { }
                catch (Newtonsoft.Json.JsonSerializationException) { }

            }
        }

        private void SetText(SensorReading sensorReading)
        {
            //Console.WriteLine(data);

            this.Dispatcher.Invoke(() =>
            {
                string temp1 = String.Format("{0:0.#} F", sensorReading.getFahr(sensorReading.temp1));
                string temp2 = String.Format("{0:0.#} F", sensorReading.getFahr(sensorReading.temp2));
                lblAL1.Content = sensorReading.light;
                lblTemp1.Content = temp1;
                lblTemp2.Content = temp2;
                lblRH1.Content = String.Format("{0}% RH", sensorReading.RH1);
                lblRH2.Content = String.Format("{0}% RH", sensorReading.RH2);
            });
            
        }
    }
}
