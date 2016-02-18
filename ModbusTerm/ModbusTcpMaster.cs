using System;
using System.Configuration;
using System.IO.Ports;
using Modbus.Device;
using System.Net.Sockets;

namespace ModbusTerm
{
    public static class ModbusTcpMaster
    {
        private static TcpClient _client;

        // Parameters from App.config
        private static readonly int ReadTimeout = 1000;
        private static readonly int WriteTimeout = 1000;
        private static readonly int AttemptsModbus = 3;

        // Counters of errors
        public static int ReadErrors { get; private set; }
        public static int WriteErrors { get; private set; }

        /// <summary>
        /// Open COM-port.
        /// </summary>
        /// <param name="port">Serial port.</param>
        public static void connect(TcpClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Close COM-port.
        /// </summary>
        /// <param name="port">Serial port.</param>
        public static void Close()
        {
            _client.Close();
        }

        /// <summary>
        /// Read registers using Modbus RTU.
        /// </summary>
        /// <param name="port">Serial port.</param>
        /// <param name="slaveAddress">Slave device address.</param>
        /// <param name="startAddress">Start address for reading.</param>
        /// <param name="numRegisters">Counter of registers that should be read.</param>
        /// <returns>Values of registers.</returns>
        public static ushort[] ReadRegisters(byte slaveAddress, ushort startAddress,
                                             ushort numRegisters)
        {
            var registers = new ushort[numRegisters];
            if (_client.Connected)
            {
                try
                {
                    var master = ModbusIpMaster.CreateIp(_client); 
                    master.Transport.ReadTimeout = ReadTimeout;
                    master.Transport.Retries = AttemptsModbus;
                    registers = master.ReadHoldingRegisters(slaveAddress, startAddress, numRegisters);
                }
                catch
                {
                    ++ReadErrors;
                }
            }
           
            return registers;
        }

        /// <summary>
        /// Wrire registers using Modbus RTU.
        /// </summary>
        /// <param name="port">Serial port.</param>
        /// <param name="slaveAddress">Slave device address.</param>
        /// <param name="startAddress">Start address for writing.</param>
        /// <param name="registers">Values of registers.</param>
        public static void WriteRegisters(byte slaveAddress, ushort startAddress,
                                          params ushort[] registers)
        {
            if (_client.Connected)
            {
                try
                {
                    var master = ModbusIpMaster.CreateIp(_client);
          
                    master.Transport.WriteTimeout = WriteTimeout;
                    master.Transport.Retries = AttemptsModbus;
                    master.WriteMultipleRegisters(slaveAddress, startAddress, registers);
                    
                }
                catch
                {
                    ++WriteErrors;
                }
            }
        }
    }
}