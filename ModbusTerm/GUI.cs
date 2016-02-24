using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using Modbus.Device;
using Modbus;
using System.Threading;
using System.Management;
using InTheHand.Net;
using InTheHand.Net.Sockets;
using InTheHand.Net.Bluetooth;
using System.IO;
using Modbus.IO;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace ModbusTerm
{
    public partial class GUI : Form
    {
        private SerialPort mport;
        private SerialPort btSerialPort;
        private TcpClient tcp;
        private Thread thr_update_dev;

        public GUI()
        {
            InitializeComponent();
        }

        private void GUI_Load(object sender, EventArgs e)
        {
            // Инициализация библиотеки логирования (нужна для дебага библиотеки Модбаса)
            log4net.Config.XmlConfigurator.Configure();

            // Подготовка элементов GUI
            comboBox2.SelectedIndex = 5;
            serial_prepare();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Подготовка элементов GUI

            if (tabControl1.SelectedIndex == 0)
            {
                comboBox2.SelectedIndex = 5;
                serial_prepare();
            }

            if (tabControl1.SelectedIndex == 1)
            {
                comboBox4.SelectedIndex = 5;
                bt_prepare();
            }

            if (tabControl1.SelectedIndex == 2)
            {
                networkPrepare();
            }

            //tabControl1.SelectedIndex = 0;
        }

        #region SerialMode Code

        private void serial_prepare()
        {
            string[] portnames = SerialPort.GetPortNames();
            
            comboBox1.Items.Clear();

            foreach (string portname in portnames)
            {
                if (portname.Length == 5)
                    comboBox1.Items.Add(portname.Substring(0, 4));
                else if (portname.Length > 5)
                    comboBox1.Items.Add(portname.Substring(0, 5));
                else
                    comboBox1.Items.Add(portname);

            }

            comboBox1.SelectedItem = comboBox1.Items[0];

            groupBox3.Enabled = false;

        }

        private void serial_open()
        {
            try
            {
                mport = new SerialPort(comboBox1.SelectedItem.ToString());

                // configure serial port
                mport.BaudRate = int.Parse(comboBox2.SelectedItem.ToString());
                mport.DataBits = 8;
                mport.Parity = Parity.None;
                mport.StopBits = StopBits.One;
                //mport.Open();
                ModbusRtuMaster.OpenPort(mport);

                if (mport.IsOpen)
                {
                    logsBox.AppendText("[Serial Mode] Port " + comboBox1.SelectedItem.ToString() + " open (" + comboBox2.SelectedItem.ToString() + " baud)\n");
                }

                /*try
                {
                    // create modbus master
                    //master = ModbusSerialMaster.CreateRtu(mport);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }*/
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            // OPEN Button

            serial_open();

            if (mport != null)
            {
                if (mport.IsOpen == true)
                {
                    button5.Enabled = true;
                    button1.Enabled = false;
                    button2.Focus();
                    groupBox3.Enabled = true;
                    //tabControl1.TabPages[1]
                }
                else
                    MessageBox.Show("Ошибка открытия порта!");
            }
            else
                MessageBox.Show("Ошибка открытия порта!");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            // Close Button

            if (mport != null)
            {
                if (mport.IsOpen)
                {
                    mport.DiscardInBuffer();
                    mport.DiscardOutBuffer();
                    //mport.Close();
                    ModbusRtuMaster.ClosePort(mport);

                    button5.Enabled = false;
                    button1.Enabled = true;
                    button1.Focus();
                    groupBox3.Enabled = false;

                    logsBox.AppendText("[Serial Mode] Port " + comboBox1.SelectedItem.ToString() + " closed\n");
                }
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Send manual request Button

            string[] hexValuesSplit = textBox2.Text.Split(' ');
            byte[] regb = new byte[hexValuesSplit.Length];

            for (int i = 0; i < hexValuesSplit.Length; i++)
            {
                regb[i] = Byte.Parse(Convert.ToInt32(hexValuesSplit[i], 16).ToString());
            }

            mport.Write(regb, 0, regb.Length);

            Thread.Sleep(100);

            logsBox.AppendText("[\n");
            logsBox.AppendText("  [Serial Mode] Command: Send full frame\n");
            logsBox.AppendText("  [Serial Mode] Request: " + textBox2.Text + "\n");

            String output = String.Empty;

            String input = mport.ReadExisting();

            foreach (Char c in input)
            {
                output += ((int)c).ToString("X2") + " ";
            }

            if (input == "")
                output = "(some errors)";
            else
            {

                // Parsing
                textBox1.Text = output.Split(' ')[0];
                textBox11.Text = output.Split(' ')[1];
                textBox13.Text = output.Split(' ')[output.Split(' ').Length - 3] + " " + output.Split(' ')[output.Split(' ').Length - 2];

                if (textBox11.Text == "10")
                {
                    textBox9.Text = output.Split(' ')[2] + " " + output.Split(' ')[3];
                    textBox10.Text = Convert.ToInt32(output.Split(' ')[4] + "" + output.Split(' ')[5], 16).ToString();
                    textBox12.Text = "n/a";
                }
                else if (textBox11.Text == "03")
                {
                    textBox9.Text = "n/a";
                    textBox10.Text = "n/a";
                    textBox12.Text = Convert.ToUInt16(output.Split(' ')[2], 16).ToString();
                }
                else
                {
                    textBox9.Text = "n/a";
                    textBox10.Text = "n/a";
                    textBox12.Text = "n/a";
                }
            }

            logsBox.AppendText("  [Serial Mode] Response: " + output + Environment.NewLine);
            logsBox.AppendText("]\n");

        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Write Button

            string[] regs = textBox5.Text.Split(' ');

            ushort[] uregs = new ushort[regs.Length];

            for (int i = 0; i < regs.Length; i++)
            {
                if ((Convert.ToUInt32(regs[i]) <= 65535))
                    uregs[i] = Convert.ToUInt16(regs[i]);
            }

            //master.WriteMultipleRegisters((byte)int.Parse(textBox3.Text), (ushort)int.Parse(textBox4.Text), uregs);
            ModbusRtuMaster.WriteRegisters(mport, (byte)int.Parse(textBox3.Text), (ushort)int.Parse(textBox4.Text), uregs);

            this.WriteOutput("[Serial Mode]", Modbus.Data.DataStore.LastResponse, Modbus.Data.DataStore.LastRequest);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Read Button
            var registers = ModbusRtuMaster.ReadRegisters(mport, (byte)int.Parse(textBox8.Text), (ushort)int.Parse(textBox7.Text), (ushort)int.Parse(textBox6.Text));

            this.ReadOutput("[Serial Mode]", Modbus.Data.DataStore.LastResponse, Modbus.Data.DataStore.LastRequest, registers);

        }

        #endregion

        #region Bluetooth Mode

        private void bt_prepare()
        {
            groupBox6.Enabled = false;
            comboBox6.Items.Clear();

            thr_update_dev = new Thread(update_dev);
            thr_update_dev.IsBackground = true;
            thr_update_dev.Start();

        }

        private void button8_Click(object sender, EventArgs e)
        {
            // Update button

            thr_update_dev.Abort();
            thr_update_dev = new Thread(update_dev);
            thr_update_dev.IsBackground = true;
            thr_update_dev.Start();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            // Open button

            Thread thr = new Thread(bt_pair);
            thr.IsBackground = true;
            thr.Start();

        }

        private void bt_pair()
        {
            BluetoothAddress BtAdress = null;
            BluetoothClient _blueToothClient;
            bool _beginConnect;
            int c = 0;
            string selDev = null;

            try
            {
                _blueToothClient = new BluetoothClient();
            }
            catch { MessageBox.Show("Bluetooth модуль не подключен!"); return; }

            button7.BeginInvoke((MethodInvoker)(() => { button7.Text = "Wait..."; button7.Enabled = false; }));
            comboBox6.BeginInvoke((MethodInvoker)(() => selDev = comboBox6.SelectedItem.ToString()));

            var devices = _blueToothClient.DiscoverDevices();

            while (BtAdress == null)
            {

                foreach (var device in devices.Where(device => device.DeviceName == selDev))
                {
                    BtAdress = device.DeviceAddress;
                    Console.WriteLine("Device found, Address:" + BtAdress.ToString());
                }

                if (BtAdress != null)
                    break;

                if (c > 2)
                {
                    MessageBox.Show("Невозможно подключиться к устройству!");
                    button7.BeginInvoke((MethodInvoker)(() => { button7.Text = "Open"; button7.Enabled = true; }));
                    return;
                }

                devices = _blueToothClient.DiscoverDevices();

                c++;
            }

            BluetoothDeviceInfo _bluetoothDevice = null;
            
            try
            {
                 _bluetoothDevice = new BluetoothDeviceInfo(BtAdress);
            }
            catch (System.ArgumentNullException) { }

            if (BluetoothSecurity.PairRequest(_bluetoothDevice.DeviceAddress, "1111"))
            {
                Console.WriteLine("Pair request result: :D");

                if (_bluetoothDevice.Authenticated)
                {
                    Console.WriteLine("Authenticated result: Cool :D");

                    _blueToothClient.SetPin("1111");

                    _blueToothClient.BeginConnect(_bluetoothDevice.DeviceAddress, BluetoothService.SerialPort, null, _bluetoothDevice);
                    _beginConnect = true;

                    bt_serial(BtAdress.ToString()); // Open Serial port for Bluetooth

                    if (btSerialPort != null)
                    {
                        button7.BeginInvoke((MethodInvoker)(() => button7.Text = "Open"));
                        button6.BeginInvoke((MethodInvoker)(() => button6.Enabled = true));
                        groupBox6.BeginInvoke((MethodInvoker)(() => groupBox6.Enabled = true));
                    }
                }
                else
                {
                    Console.WriteLine("Authenticated: So sad :(");

                    bt_pair();
                }
            }
            else
            {
                Console.WriteLine("PairRequest: Sad :(");

                MessageBox.Show("Невозможно подключиться к устройству!");
                button7.BeginInvoke((MethodInvoker)(() => { button7.Text = "Open"; button7.Enabled = true; }));
                return;
            }
            
        }

        private void bt_serial(string BtAdress)
        {

            // Создаем последовательный порт, используя стороннее приложение btcom.exe

            Process a = Process.Start(Environment.CurrentDirectory + "\\btcom.exe", "-b\"" + BtAdress + "\" -c -s1101");
            
            a.WaitForExit();

            // Поиск названия созданного порта

            const string Win32_SerialPort = "Win32_SerialPort";
            SelectQuery q = new SelectQuery(Win32_SerialPort);
            ManagementObjectSearcher s = new ManagementObjectSearcher(q);
            object id = null;

            foreach (object cur in s.Get())
            {
                ManagementObject mo = (ManagementObject)cur;
                id = mo.GetPropertyValue("DeviceID");
                object pnpId = mo.GetPropertyValue("PNPDeviceID");
                Console.WriteLine("DeviceID:    {0} ", id);
                Console.WriteLine("PNPDeviceID: {0} ", pnpId);
                Console.WriteLine("");
            }

            //id = "COM2";

            if (id != null)
            {
                int bd = 9600;
                comboBox4.BeginInvoke((MethodInvoker)(() => bd = int.Parse(comboBox4.SelectedItem.ToString())));

                // Открываем последовательный порт
                btSerialPort = new SerialPort(id.ToString(), bd);

                try
                {
                    //ModbusRtuMaster.OpenPort(btSerialPort);
                    btSerialPort.Open();
                }
                catch 
                { 
                    MessageBox.Show("Ошибка открытия порта!");
                    button7.BeginInvoke((MethodInvoker)(() => { button7.Enabled = true; button7.Text = "Open"; }));
                    button6.BeginInvoke((MethodInvoker)(() => button6.Enabled = false));
                    groupBox6.BeginInvoke((MethodInvoker)(() => groupBox6.Enabled = false));
                }

                if (btSerialPort.IsOpen)
                {
                    logsBox.BeginInvoke((MethodInvoker)(() => logsBox.AppendText("[Bluetooth Mode] Port " + id.ToString() + " open (" + bd + " baud)\n")));
                    Console.WriteLine("BT Serial Port is open!");
                }

            }
            else
            {
                MessageBox.Show("Ошибка создания COM порта!");
                button7.BeginInvoke((MethodInvoker)(() => { button7.Enabled = true; button7.Text = "Open"; }));
                button6.BeginInvoke((MethodInvoker)(() => button6.Enabled = false));
                groupBox6.BeginInvoke((MethodInvoker)(() => groupBox6.Enabled = false));
            }
        }

        private void update_dev()
        {
            BluetoothClient bt_client = null;

            try
            {
                bt_client = new BluetoothClient();
            }
            catch { MessageBox.Show("Bluetooth модуль не подключен!"); return; }

            button8.BeginInvoke((MethodInvoker)(() => { button8.Text = "Wait..."; button8.Enabled = false; }));
            comboBox6.BeginInvoke((MethodInvoker)(() => comboBox6.Items.Clear()));

            //ComboBox.ObjectCollection dev = new ComboBox.ObjectCollection(comboBox6);
            List<string> dev = new List<string>();

            foreach (BluetoothDeviceInfo device in bt_client.DiscoverDevices(100, true, true, true, true))
            {
                //comboBox6.BeginInvoke((MethodInvoker)(() => dev.Add(device.DeviceName)));
                dev.Add(device.DeviceName);
                
            }

            comboBox6.BeginInvoke((MethodInvoker)(() => comboBox6.Items.AddRange(dev.ToArray())));
            //comboBox6.BeginInvoke((MethodInvoker)(() => comboBox6.SelectedIndex = 0));
            button8.BeginInvoke((MethodInvoker)(() => { button8.Text = "Update"; button8.Enabled = true;}));
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // Close button

            button7.Enabled = true;
            button6.Enabled = false;
            groupBox6.Enabled = false;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            // Write Button

            string[] regs = textBox17.Text.Split(' ');

            ushort[] uregs = new ushort[regs.Length];

            for (int i = 0; i < regs.Length; i++)
            {
                if ((Convert.ToUInt32(regs[i]) <= 65535))
                    uregs[i] = Convert.ToUInt16(regs[i]);
            }

            //master.WriteMultipleRegisters((byte)int.Parse(textBox3.Text), (ushort)int.Parse(textBox4.Text), uregs);
            ModbusRtuMaster.WriteRegisters(btSerialPort, (byte)int.Parse(textBox19.Text), (ushort)int.Parse(textBox18.Text), uregs);

            this.WriteOutput("[Bluetooth Mode]", Modbus.Data.DataStore.LastResponse, Modbus.Data.DataStore.LastRequest);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            // Read Button
            var registers = ModbusRtuMaster.ReadRegisters(btSerialPort, (byte)int.Parse(textBox16.Text), (ushort)int.Parse(textBox15.Text), (ushort)int.Parse(textBox14.Text));

            this.ReadOutput("[Bluetooth Mode]", Modbus.Data.DataStore.LastResponse, Modbus.Data.DataStore.LastRequest, registers);

        }

        #endregion

        #region Network Code
        private void networkPrepare()
        {
            comboBox7.Items.Clear();
            textBox21.Text = "502"; // Порт по умолчанию
            //groupBox8.Enabled = false;
            textBox22.Enabled = false;
            textBox23.Enabled = false;
            textBox24.Enabled = false;
            textBox25.Enabled = false;
            textBox26.Enabled = false;
            textBox27.Enabled = false;
            button11.Enabled = false;
            button14.Enabled = false;
            button16.Enabled = false;

            IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName()); // Получение ip-адресов
            IPAddress[] addr = ipEntry.AddressList; // Список адресов
            IPAddress localIp = null; // Собственный адрес
            foreach (IPAddress a in addr)
            { // Получение собственного ip
                if (a.ToString().Length <= 15)
                    localIp = a;
            }
            // Разделяем строку адреса на части
            string[] ipParts = localIp.ToString().Split('.');
            // Берем все части, кроме последней
            string ipPattern = ipParts[0] + "." + ipParts[1] + "." + ipParts[2] + ".";

            AutoResetEvent waiter = new AutoResetEvent(false); //создаем класс управления событиями в потоке
            byte[] buffer = Encoding.ASCII.GetBytes("test ping"); //записываем в массив байт сконвертированную в байтовый массив строку для отправки в пинг запросе
            
            for (int i = 1; i < 255; i++)
            {
                Ping p = new Ping();
                p.PingCompleted += new PingCompletedEventHandler(p_PingCompleted); //указываем на метод, который будет выполняться в результате получения ответа на асинхронный запрос пинга
                PingOptions options = new PingOptions(64, true); //выставляем опции для пинга (непринципиально)
                IPAddress ip = IPAddress.Parse(ipPattern + i); // формируем ip-адрес
                p.SendAsync(ip, 500, buffer, options, waiter); //посылаем асинхронный пинг на адрес с таймаутом 500мс, в нем передаем массив байт сконвертированных их строки в начале кода, используем при передаче указанные опции, после обращаемся к классу управления событиями
            }
            comboBox7.Sorted = true; // Сортировка по возрастанию
        }
        
        private void p_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            if (e.Reply.Status == IPStatus.Success)
            {
                //Добавляем адрес, с которого пришел ответ в comboBox
                comboBox7.Items.Add(e.Reply.Address.ToString());
                comboBox7.SelectedIndex = 0;
            }
            // Let the main thread resume.
            ((AutoResetEvent)e.UserState).Set();
        }

        private void button13_Click(object sender, EventArgs e)
        { // Открытие порта
            tcp = new TcpClient(comboBox7.Text, int.Parse(textBox21.Text));
            if (tcp != null)
            {
                ModbusTcpMaster.connect(tcp);
                button13.Enabled = false;
                button12.Enabled = true;
                //groupBox8.Enabled = true;
                textBox22.Enabled = true;
                textBox23.Enabled = true;
                textBox24.Enabled = true;
                textBox25.Enabled = true;
                textBox26.Enabled = true;
                textBox27.Enabled = true;
                button11.Enabled = true;
                button14.Enabled = true;
                comboBox7.Enabled = false;
                textBox21.Enabled = false;
            }
        }

        private void button12_Click(object sender, EventArgs e)
        { // Закрытие порта
            button13.Enabled = true;
            button12.Enabled = false;
            //groupBox8.Enabled = false;
            textBox22.Enabled = false;
            textBox23.Enabled = false;
            textBox24.Enabled = false;
            textBox25.Enabled = false;
            textBox26.Enabled = false;
            textBox27.Enabled = false;
            button11.Enabled = false;
            button14.Enabled = false;

            comboBox7.Enabled = true;
            textBox21.Enabled = true;
            if (tcp != null)
            {
                ModbusTcpMaster.disconnect();
            }
        }

        private void button14_Click(object sender, EventArgs e)
        { // Read button
            // Значение указанных регистров
            ushort[] registers = ModbusTcpMaster.ReadRegisters(byte.Parse(textBox24.Text), ushort.Parse(textBox23.Text), ushort.Parse(textBox22.Text));
            // Запрос и ответ
            byte[] response = new byte[Modbus.Data.DataStore.LastResponse.Length - 6];
            byte[] request = new byte[Modbus.Data.DataStore.LastRequest.Length - 6];
            // Копирование тела запроса и ответа
            Array.Copy(Modbus.Data.DataStore.LastResponse, 6, response, 0, Modbus.Data.DataStore.LastResponse.Length - 6);
            Array.Copy(Modbus.Data.DataStore.LastRequest, 6, request, 0, Modbus.Data.DataStore.LastRequest.Length - 6);
            // Подсчет crc для запроса и ответа
            byte[] responseCrc = Modbus.Utility.ModbusUtility.CalculateCrc(response);
            byte[] requestCrc = Modbus.Utility.ModbusUtility.CalculateCrc(request);
            // Запрос с crc
            byte[] responseWithCrc = new byte[Modbus.Data.DataStore.LastResponse.Length - 4];
            byte[] requestWithCrc = new byte[Modbus.Data.DataStore.LastRequest.Length - 4];
            // Копирование запроса без crc
            Array.Copy(Modbus.Data.DataStore.LastResponse, 6, responseWithCrc, 0, Modbus.Data.DataStore.LastResponse.Length - 6);
            Array.Copy(Modbus.Data.DataStore.LastRequest, 6, requestWithCrc, 0, Modbus.Data.DataStore.LastRequest.Length - 6);
            // Добавление crc в строки запроса и ответа
            responseWithCrc[responseWithCrc.Length - 2] = responseCrc[0];
            responseWithCrc[responseWithCrc.Length - 1] = responseCrc[1];
            requestWithCrc[requestWithCrc.Length - 2] = requestCrc[0];
            requestWithCrc[requestWithCrc.Length - 1] = requestCrc[1];
            // Вывод в лог-бокс
            this.ReadOutput("[Network Mode]", responseWithCrc, requestWithCrc, registers);
        }

        private void button11_Click(object sender, EventArgs e)
        { // Write button
            string[] regs = textBox25.Text.Split(' ');
            ushort[] uregs = new ushort[regs.Length];
            for (int i = 0; i < regs.Length; i++)
            { // Парсинг значений регистров из строки
                uregs[i] = Convert.ToUInt16(regs[i]);
            }
            // Отправка запроса
            ModbusTcpMaster.WriteRegisters(byte.Parse(textBox27.Text), ushort.Parse(textBox26.Text), uregs);
            // Запрос и ответ
            byte[] response = new byte[Modbus.Data.DataStore.LastResponse.Length - 4];
            byte[] request = new byte[Modbus.Data.DataStore.LastRequest.Length - 4];
            // Копирование тела запроса и ответа
            Array.Copy(Modbus.Data.DataStore.LastResponse, 6, response, 0, Modbus.Data.DataStore.LastResponse.Length - 6);
            Array.Copy(Modbus.Data.DataStore.LastRequest, 6, request, 0, Modbus.Data.DataStore.LastRequest.Length - 6);
            // Подсчет crc для запроса и ответа
            byte[] responseCrc = Modbus.Utility.ModbusUtility.CalculateCrc(response);
            byte[] requestCrc = Modbus.Utility.ModbusUtility.CalculateCrc(request);
            // Запрос с crc
            byte[] responseWithCrc = new byte[Modbus.Data.DataStore.LastResponse.Length - 4];
            byte[] requestWithCrc = new byte[Modbus.Data.DataStore.LastRequest.Length - 4];
            // Копирование запроса без crc
            Array.Copy(Modbus.Data.DataStore.LastResponse, 6, responseWithCrc, 0, Modbus.Data.DataStore.LastResponse.Length - 6);
            Array.Copy(Modbus.Data.DataStore.LastRequest, 6, requestWithCrc, 0, Modbus.Data.DataStore.LastRequest.Length - 6);
            // Добавление crc в строки запроса и ответа
            responseWithCrc[responseWithCrc.Length - 2] = responseCrc[0];
            responseWithCrc[responseWithCrc.Length - 1] = responseCrc[1];
            requestWithCrc[requestWithCrc.Length - 2] = requestCrc[0];
            requestWithCrc[requestWithCrc.Length - 1] = requestCrc[1];
            // Вывод в лог-бокс
            this.WriteOutput("[Network Mode]", responseWithCrc, requestWithCrc);
        }

        private void button15_Click(object sender, EventArgs e)
        {// Создание ad-hoc-сети
            String name = textBox20.Text;
            String password = textBox28.Text;
            if (!password.Equals("") && !name.Equals("")){
                string command1 = "netsh wlan set hosted mode=allow ssid=\"" + name + "\" key=\"" + password + "\"";
                string command2 = "netsh wlan start hosted";
                System.Diagnostics.Process.Start("cmd.exe", "/C " + command1);
                System.Diagnostics.Process.Start("cmd.exe", "/C " + command2);
                textBox20.Enabled = false;
                textBox28.Enabled = false;
                button15.Enabled = false;
                button16.Enabled = true;
            }
        }

        private void button16_Click(object sender, EventArgs e)
        { // Удаление ad-hoc-сети
            string command = "netsh wlan stop hosted";
            System.Diagnostics.Process.Start("cmd.exe", "/C " + command);
            textBox20.Enabled = true;
            textBox28.Enabled = true;
            button15.Enabled = true;
            button16.Enabled = false;
        }

        #endregion

        private void WriteOutput(string mode_name, byte[] response, byte[] request)
        {
            logsBox.AppendText("[\n");
            logsBox.AppendText("  " + mode_name + " Command: Write\n");
            logsBox.AppendText("  " + mode_name + " Request: " + BitConverter.ToString(request).Replace('-', ' ') + "\n");
            logsBox.AppendText("  " + mode_name + " Response: " + BitConverter.ToString(response).Replace('-', ' ') + "\n");
            logsBox.AppendText("]\n");

            // Parsing
            textBox1.Text = BitConverter.ToString(response).Split('-')[0];
            textBox11.Text = BitConverter.ToString(response).Split('-')[1];
            textBox9.Text = BitConverter.ToString(response).Split('-')[2] + " " + BitConverter.ToString(response).Split('-')[3];
            textBox10.Text = Convert.ToInt32(BitConverter.ToString(response).Split('-')[4] + "" + BitConverter.ToString(response).Split('-')[5], 16).ToString();
            textBox12.Text = "n/a";
            textBox13.Text = BitConverter.ToString(response).Split('-')[6] + " " + BitConverter.ToString(response).Split('-')[7];

        }

        private void ReadOutput(string mode_name, byte[] response, byte[] request, ushort[] registers)
        {
            string regs = "";

            for (int i = 0; i < registers.Length; i++)
                regs += "[" + i + "] => " + registers[i] + "; ";


            logsBox.AppendText("[\n");
            logsBox.AppendText("  " + mode_name + " Command: Read\n");
            logsBox.AppendText("  " + mode_name + " Request: " + BitConverter.ToString(request).Replace('-', ' ') + "\n");
            logsBox.AppendText("  " + mode_name + " Response: " + BitConverter.ToString(response).Replace('-', ' ') + "\n");
            logsBox.AppendText("  " + mode_name + " Registers (dec): " + regs + "\n");
            logsBox.AppendText("]\n");

            // Parsing
            textBox1.Text = BitConverter.ToString(response).Split('-')[0];
            textBox11.Text = BitConverter.ToString(response).Split('-')[1];
            textBox9.Text = "n/a";
            textBox10.Text = "n/a";
            textBox12.Text = Convert.ToUInt16(BitConverter.ToString(response).Split('-')[2], 16).ToString();
            textBox13.Text = BitConverter.ToString(response).Split('-')[BitConverter.ToString(response).Split('-').Length - 2] + " " + BitConverter.ToString(response).Split('-').Last();
        }

        private void onlyNum(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar < 48 || e.KeyChar > 58) && e.KeyChar != 8)
                e.Handled = true;
        }

        private void onlyNumWithSpace(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar < 48 || e.KeyChar > 58) && (e.KeyChar != 8) && (e.KeyChar != 32))
                e.Handled = true;
        }

        private void label17_Click(object sender, EventArgs e)
        {

        }




    }
}
