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
using Modbus.Data;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace ModbusTerm___Slave
{
    public partial class Form1 : Form
    {
        SerialPort rtu_port;
        Thread slaveThread;
        TcpListener slaveTcpListener;
        Thread slaveTcp;
        ModbusSlave slaveTcpM;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] portnames = SerialPort.GetPortNames();

            foreach (string portname in portnames)
            {
                if (portname.Length == 4)
                    comboBox1.Items.Add(portname.Substring(0, 4));
                else if (portname.Length >= 5)
                    comboBox1.Items.Add(portname.Substring(0, 5));
                else
                    comboBox1.Items.Add(portname);

            }

            comboBox2.SelectedItem = "9600";
            comboBox1.SelectedItem = comboBox1.Items[0];

            //Network Mode

            IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName()); // Получение ip-адресов
            IPAddress[] addr = ipEntry.AddressList;

            foreach (IPAddress a in addr)
            { // Получение собственного ip
                if (a.ToString().Length <= 15)
                    comboBox3.Items.Add(a);
            }

            comboBox3.SelectedIndex = 0;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            // RTU Slave

            button1.Enabled = false;
            button2.Enabled = true;

            // configure serial port
            rtu_port = new SerialPort(comboBox1.SelectedItem.ToString());
            
            rtu_port.BaudRate = int.Parse(comboBox2.SelectedItem.ToString());
            rtu_port.DataBits = 8;
            rtu_port.Parity = Parity.None;
            rtu_port.StopBits = StopBits.One;
            rtu_port.Open();

            slaveThread = new Thread(rtu_slave);
            slaveThread.IsBackground = true;
            slaveThread.Start();
        }

        public void rtu_slave()
        {

            byte unitId = (byte)numericUpDown1.Value;

            // create modbus slave
            ModbusSlave slave = ModbusSerialSlave.CreateRtu(unitId, rtu_port);
            slave.DataStore = DataStoreFactory.CreateDefaultDataStore();
            slave.ModbusSlaveRequestReceived += (obj, args) => { Console.WriteLine("[MODBUS SLAVE] I got it!!!"); };

            slave.Listen();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            slaveThread.Abort();

            button1.Enabled = true;
            button2.Enabled = false;
        }

        private void button4_Click(object sender, EventArgs e)
        {

            button3.Enabled = true;
            button4.Enabled = false;

            byte slaveID = (byte)numericUpDown3.Value;
            int port = (int)numericUpDown2.Value;
            IPAddress address = IPAddress.Parse(comboBox3.Text); //new IPAddress(new byte[] { 10, 36, 5, 217 });

            // create and start the TCP slave
            slaveTcpListener = new TcpListener(address, port);
            slaveTcpListener.Start();

            slaveTcpM = ModbusTcpSlave.CreateTcp(slaveID, slaveTcpListener);
            slaveTcpM.DataStore = DataStoreFactory.CreateDefaultDataStore();

            slaveTcp = new Thread(slaveTcpM.Listen);
            slaveTcp.IsBackground = true;
            slaveTcp.Start();

            // prevent the main thread from exiting
            //Thread.Sleep(Timeout.Infinite);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            button3.Enabled = false;
            button4.Enabled = true;

            slaveTcpListener.Stop();
            slaveTcpM.Dispose();
            slaveTcp.Abort();
        }

    }
}
