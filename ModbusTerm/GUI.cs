﻿using System;
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

namespace ModbusTerm
{
    public partial class GUI : Form
    {

        private IModbusSerialMaster master;
        private SerialPort mport;

        public GUI()
        {
            InitializeComponent();
        }

        private void GUI_Load(object sender, EventArgs e)
        {
            // Инициализация библиотеки логирования (нужна для дебага библиотеки Модбаса)
            log4net.Config.XmlConfigurator.Configure();

            // Подготовка элементов GUI
            serial_prepare();

        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //tabControl1.SelectedIndex = 0;
        }

        #region SerialMode Code

        private void serial_prepare()
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

                if (textBox11.Text == "10")
                {
                    textBox9.Text = output.Split(' ')[2] + " " + output.Split(' ')[3];
                    textBox10.Text = Convert.ToInt32(output.Split(' ')[4] + "" + output.Split(' ')[5], 16).ToString();
                    textBox12.Text = "n/a";
                    textBox13.Text = output.Split(' ')[6] + " " + output.Split(' ')[7];
                }
                else if (textBox11.Text == "3")
                {
                    textBox9.Text = "n/a";
                    textBox10.Text = "n/a";
                    textBox12.Text = Convert.ToUInt16(output.Split(' ')[2], 16).ToString();
                    textBox13.Text = output.Split(' ')[output.Split(' ').Length - 2] + " " + output.Split(' ').Last();
                }
                else
                {
                    textBox9.Text = "n/a";
                    textBox10.Text = "n/a";
                    textBox12.Text = "n/a";
                    textBox13.Text = "n/a";
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
                uregs[i] = Convert.ToUInt16(regs[i]);
            }

            //master.WriteMultipleRegisters((byte)int.Parse(textBox3.Text), (ushort)int.Parse(textBox4.Text), uregs);
            ModbusRtuMaster.WriteRegisters(mport, (byte)int.Parse(textBox3.Text), (ushort)int.Parse(textBox4.Text), uregs);

            logsBox.AppendText("[\n");
            logsBox.AppendText("  [Serial Mode] Request: " + BitConverter.ToString(Modbus.Data.DataStore.LastRequest).Replace('-', ' ') + "\n");
            logsBox.AppendText("  [Serial Mode] Response: " + BitConverter.ToString(Modbus.Data.DataStore.LastResponse).Replace('-', ' ') + "\n");
            logsBox.AppendText("]\n");

            // Parsing
            textBox1.Text = BitConverter.ToString(Modbus.Data.DataStore.LastResponse).Split('-')[0];
            textBox11.Text = BitConverter.ToString(Modbus.Data.DataStore.LastResponse).Split('-')[1];
            textBox9.Text = BitConverter.ToString(Modbus.Data.DataStore.LastResponse).Split('-')[2] + " " + BitConverter.ToString(Modbus.Data.DataStore.LastResponse).Split('-')[3];
            textBox10.Text = Convert.ToInt32(BitConverter.ToString(Modbus.Data.DataStore.LastResponse).Split('-')[4] + "" + BitConverter.ToString(Modbus.Data.DataStore.LastResponse).Split('-')[5], 16).ToString();
            textBox12.Text = "n/a";
            textBox13.Text = BitConverter.ToString(Modbus.Data.DataStore.LastResponse).Split('-')[6] + " " + BitConverter.ToString(Modbus.Data.DataStore.LastResponse).Split('-')[7];

        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Read Button
            var registers = ModbusRtuMaster.ReadRegisters(mport, (byte)int.Parse(textBox8.Text), (ushort)int.Parse(textBox7.Text), (ushort)int.Parse(textBox6.Text));
            
            string regs = "";

            for (int i = 0; i < registers.Length; i++)
                regs += "[" + i + "] => " + registers[i] + "; ";


            logsBox.AppendText("[\n");
            logsBox.AppendText("  [Serial Mode] Request: " + BitConverter.ToString(Modbus.Data.DataStore.LastRequest).Replace('-', ' ') + "\n");
            logsBox.AppendText("  [Serial Mode] Response: " + BitConverter.ToString(Modbus.Data.DataStore.LastResponse).Replace('-', ' ') + "\n");
            logsBox.AppendText("  [Serial Mode] Registers (dec): " + regs + "\n");
            logsBox.AppendText("]\n");

            // Parsing
            textBox1.Text = BitConverter.ToString(Modbus.Data.DataStore.LastResponse).Split('-')[0];
            textBox11.Text = BitConverter.ToString(Modbus.Data.DataStore.LastResponse).Split('-')[1];
            textBox9.Text = "n/a";
            textBox10.Text = "n/a";
            textBox12.Text = Convert.ToUInt16(BitConverter.ToString(Modbus.Data.DataStore.LastResponse).Split('-')[2], 16).ToString();
            textBox13.Text = BitConverter.ToString(Modbus.Data.DataStore.LastResponse).Split('-')[BitConverter.ToString(Modbus.Data.DataStore.LastResponse).Split('-').Length - 2] + " " + BitConverter.ToString(Modbus.Data.DataStore.LastResponse).Split('-').Last();

        }

        #endregion

        #region Bluetooth Mode

        private void button8_Click(object sender, EventArgs e)
        {

           
        }

        #endregion

    }
}