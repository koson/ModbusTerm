using System;
using System.Configuration;
using System.IO.Ports;
using Modbus.Device;

namespace ModbusTerm
{
    public static class ModbusRtuMaster
    {
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
        public static void OpenPort(SerialPort port)
        {
            port.Open();
        }

        /// <summary>
        /// Close COM-port.
        /// </summary>
        /// <param name="port">Serial port.</param>
        public static void ClosePort(SerialPort port)
        {
            port.Close();
        }

        /// <summary>
        /// Read registers using Modbus RTU.
        /// </summary>
        /// <param name="port">Serial port.</param>
        /// <param name="slaveAddress">Slave device address.</param>
        /// <param name="startAddress">Start address for reading.</param>
        /// <param name="numRegisters">Counter of registers that should be read.</param>
        /// <returns>Values of registers.</returns>
        public static ushort[] ReadRegisters(SerialPort port, byte slaveAddress, ushort startAddress,
                                             ushort numRegisters)
        {
            var registers = new ushort[numRegisters];
            if (port.IsOpen)
            {
                try
                {
                    var master = ModbusSerialMaster.CreateRtu(port);
                    master.Transport.ReadTimeout = ReadTimeout;
                    master.Transport.Retries = AttemptsModbus;
                    //registers = master.ReadHoldingRegisters(slaveAddress, startAddress, numRegisters);
                    registers = master.ReadInputRegisters(slaveAddress, startAddress, numRegisters);
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
        public static void WriteRegisters(SerialPort port, byte slaveAddress, ushort startAddress,
                                          params ushort[] registers)
        {
            if (port.IsOpen)
            {
                try
                {
                    var master = ModbusSerialMaster.CreateRtu(port);
          
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