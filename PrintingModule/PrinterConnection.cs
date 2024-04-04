using System.IO.Ports;

namespace PrintingModule
{
    class PrinterConnection
    {

        bool printing = false;
        SerialPort _serialPort;
        Semaphore semaphore = new(0, 1);
        public object locker = new();
        public double pecentage = 0;
        double cnt = 0;
        bool gotOk = false;

        public bool FindPrinter()
        {
            var ports = SerialPort.GetPortNames();
            foreach (var port in ports)
            {
                _serialPort = new SerialPort(port, 115200, Parity.None, 8, StopBits.One);
                _serialPort.DataReceived += Port_DataReceived;

                _serialPort.ReadTimeout = 10000;
                _serialPort.WriteTimeout = 1000;

                semaphore = new Semaphore(0, 1);
                try
                {
                    _serialPort.Open();
                    _serialPort.WriteLine("M105"); //Get Extruder Temp, if it returns 'ok' then it is printer)
                    Thread.Sleep(500);
                    if (gotOk)
                    {
                        return true;
                    }
                }
                catch { }
                _serialPort.Close();
            }
            return false;
        }

        public string Print(List<string> data)
        {
            if (!FindPrinter())
                return "No printer(";

            if (!printing)
            {
                printing = true;
            }
            else
            {
                return "Already printing";
            }

            var len = data.Count;
            for (var i = 0; i < len; i++)
            {
                _serialPort.WriteLine(data[i]);
                semaphore.WaitOne();
                lock(locker)
                {
                    cnt++;
                    pecentage = cnt / len;
                }
            }

            _serialPort.Close();
            return "Complete";
        }

        void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType == SerialData.Chars)
            {
                var message = _serialPort.ReadLine();
                Console.WriteLine(message);
                if (message.StartsWith("ok"))
                {
                    gotOk = true;
                    semaphore.Release();
                }
                
            }
        }
    }
}
