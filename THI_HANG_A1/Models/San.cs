using System;
using THI_HANG_A1.Managers;

namespace THI_HANG_A1.Models
{
    public class San
    {
        public event Action OnChanged;

        public string Name { get; private set; }
        public string IP { get; private set; }
        public int PORT { get; private set; }

        public SocketHandler socketConn;

        public San(string name, string ip, int port)
        {
            Name = name;
            IP = ip;
            PORT = port;

            socketConn = new SocketHandler();
            socketConn.OnDataReceivedBytes += SocketDataHandler;   // Dùng dạng byte[]
        }

        // =============================================================
        // STATES
        // =============================================================
        private bool _ongHoi;
        public bool OngHoi
        {
            get => _ongHoi;
            set { _ongHoi = value; OnChanged?.Invoke(); }
        }

        private bool _sensor1;
        public bool Sensor1
        {
            get => _sensor1;
            set { _sensor1 = value; OnChanged?.Invoke(); }
        }

        private bool _sensor2;
        public bool Sensor2
        {
            get => _sensor2;
            set { _sensor2 = value; OnChanged?.Invoke(); }
        }

        private bool _sensor3;
        public bool Sensor3
        {
            get => _sensor3;
            set { _sensor3 = value; OnChanged?.Invoke(); }
        }
        private string _Mes;
        public string Mes
        {
            get => _Mes;
            set { _Mes = value; OnChanged?.Invoke(); }
        }
        public bool IsConnected { get; private set; }
        private bool _sensor4;
        public bool Sensor4
        {
            get => _sensor4;
            set { _sensor4 = value; OnChanged?.Invoke(); }
        }
        private bool _sensor5;
        public bool Sensor5
        {
            get => _sensor5;
            set { _sensor5 = value; OnChanged?.Invoke(); }
        }
        private bool _sensor6;
        public bool Sensor6
        {
            get => _sensor6;
            set { _sensor6 = value; OnChanged?.Invoke(); }
        }
        private bool _sensor7;
        public bool Sensor7
        {
            get => _sensor7;
            set { _sensor7 = value; OnChanged?.Invoke(); }
        }
        private bool _sensor8;
        public bool Sensor8
        {
            get => _sensor8;
            set { _sensor8 = value; OnChanged?.Invoke(); }
        }


        // =============================================================
        // KẾT NỐI
        // =============================================================
        public void Connect()
        {
            bool ok = socketConn.Connect(IP, PORT);
            if (!ok)
            {
                // Cập nhật sự kiện để UI thấy lỗi
                OngHoi = false;
                return;
            }
            IsConnected = true;
            OnChanged?.Invoke();
        }
        public void Disconnect()
        {
            // Code ngắt kết nối ở đây...
            IsConnected = false;
            OnChanged?.Invoke();
        }

        // =============================================================
        // PARSE DATA BYTE[]
        // =============================================================
        private void SocketDataHandler(byte[] buffer, int len)
        {

            Mes = BitConverter.ToString(buffer, 0, len).Replace("-", " ");




            if (len < 8) return;
            if (buffer[0] != 0xAA) return;
            byte key = buffer[1];

            UInt32 value =
                ((UInt32)buffer[4] << 24) |
                ((UInt32)buffer[5] << 16) |
                ((UInt32)buffer[6] << 8) |
                ((UInt32)buffer[7]);

            switch (key)
            {
              //  case 1: Sensor1 = value == 1; break;
                case 2: Sensor2 = value == 1; break;
                case 3: Sensor3 = value == 1; break;
                case 4: OngHoi = value == 1; break;
            }
            
        }
         


    }
}
