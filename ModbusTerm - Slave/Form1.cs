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

namespace ModbusTerm___Slave
{
    public partial class Form1 : Form
    {

        Thread slaveThread;

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

        }

        private void button1_Click(object sender, EventArgs e)
        {
            // RTU Slave

            slaveThread = new Thread(rtu_slave);
            slaveThread.IsBackground = true;
            slaveThread.Start();
        }

        public void rtu_slave()
        {
            using (SerialPort slavePort = new SerialPort(comboBox1.SelectedItem.ToString()))
            {
                // configure serial port
                slavePort.BaudRate = int.Parse(comboBox2.SelectedItem.ToString());
                slavePort.DataBits = 8;
                slavePort.Parity = Parity.None;
                slavePort.StopBits = StopBits.One;
                slavePort.Open();

                byte unitId = (byte)numericUpDown1.Value;

                // create modbus slave
                ModbusSlave slave = ModbusSerialSlave.CreateRtu(unitId, slavePort);
                slave.DataStore = DataStoreFactory.CreateDefaultDataStore();
                slave.ModbusSlaveRequestReceived += (obj, args) => { Console.WriteLine("[MODBUS SLAVE] I got it!!!"); };

                slave.Listen();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            slaveThread.Abort();
        }

    }
}
