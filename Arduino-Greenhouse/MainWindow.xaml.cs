using Arduino_Greenhouse.Model;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Http;
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
        SensorReading[] ArraySensorReadings = new SensorReading[6];
        int readingCount = 0;
        bool uploadToServer = false;


        public MainWindow()
        {
            InitializeComponent();
            ComPort.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived_1);
            
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
                ComPort.BaudRate = 9600;
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
            InputData = ComPort.ReadLine();
            if (!string.IsNullOrEmpty(InputData))
            {
                //Console.WriteLine(InputData);
                try
                {
                    SensorReading sensorReading = JsonConvert.DeserializeObject<SensorReading>(InputData);
                    ArraySensorReadings[readingCount]= sensorReading;
                    readingCount++;
                    if (readingCount == 6)
                        UploadDataToServerAsync();
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

        private async void UploadDataToServerAsync()
        {
            Console.WriteLine("Uploading to server");
            int[] avglight = new int[6];
            float[] avgRH1 = new float[6];
            float[] avgRH2 = new float[6];
            float[] temp1 = new float[6];
            float[] temp2 = new float[6];
            long[] timestamp = new long[6];

            readingCount = 0;
            for (int i = 0; i < 6; i++)
            {
                avglight[i] = ArraySensorReadings[i].light;
                avgRH1[i] = ArraySensorReadings[i].rh1;
                avgRH2[i] = ArraySensorReadings[i].rh2;
                temp1[i] = ArraySensorReadings[i].temp1;
                temp2[i] = ArraySensorReadings[i].temp2;
                timestamp[i] = ArraySensorReadings[i].timestamp;
            }
            SensorReading averageReading = new SensorReading();
            averageReading.light = (int)avglight.Average();
            averageReading.rh1 = avgRH1.Average();
            averageReading.rh2 = avgRH2.Average();
            averageReading.temp1 = temp1.Average();
            averageReading.temp2 = temp2.Average();
            averageReading.timestamp = timestamp.Max();

            Console.WriteLine($"light: {avglight.Average()}, RH1: {avgRH1.Sum()}");

            if(uploadToServer==true)
            {
                var client = new RestClient("https://tylerday.net");
                client.Authenticator = new HttpBasicAuthenticator(Privates.CREDENTIALS[0], Privates.CREDENTIALS[1]);

                var request = new RestSharp.RestRequest("garden/api/readings.json", RestSharp.DataFormat.Json);
                //request.JsonSerializer = new NewtonsoftJsonSerializer();
                request.AddJsonBody(averageReading);
                //var response = client.Get(request);
                var response = client.Post(request);
                Console.WriteLine(response);
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
                lblRH1.Content = String.Format("{0}% RH", sensorReading.rh1);
                lblRH2.Content = String.Format("{0}% RH", sensorReading.rh2);
            });
            
        }

        private void cbUpload_Checked(object sender, RoutedEventArgs e)
        {
            uploadToServer = (bool)cbUpload.IsChecked;
        }
    }
}
