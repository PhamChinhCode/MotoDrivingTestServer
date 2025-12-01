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
            socketConn.OnDataReceivedBytes += SocketDataHandler;
        }

        private void TriggerUI() => OnChanged?.Invoke();

        // ================= STATES ======================
        public bool IsConnected { get; private set; }

        private bool _sensor1;
        public bool Sensor1 { get => _sensor1; set { _sensor1 = value; TriggerUI(); } }

        private bool _sensor2;
        public bool Sensor2 { get => _sensor2; set { _sensor2 = value; TriggerUI(); } }

        private bool _sensor3;
        public bool Sensor3 { get => _sensor3; set { _sensor3 = value; TriggerUI(); } }

        private bool _sensor4;
        public bool Sensor4 { get => _sensor4; set { _sensor4 = value; TriggerUI(); } }

        private bool _sensor5;
        public bool Sensor5 { get => _sensor5; set { _sensor5 = value; TriggerUI(); } }

        private bool _sensor6;
        public bool Sensor6 { get => _sensor6; set { _sensor6 = value; TriggerUI(); } }

        private bool _sensor7;
        public bool Sensor7 { get => _sensor7; set { _sensor7 = value; TriggerUI(); } }

        private bool _sensor8;
        public bool Sensor8 { get => _sensor8; set { _sensor8 = value; TriggerUI(); } }

        private bool _onghoi;
        public bool OngHoi { get => _onghoi; set { _onghoi = value; TriggerUI(); } }

        private string _mes;
        public string Mes { get => _mes; set { _mes = value; TriggerUI(); } }


        // ================= CONNECT ======================
        public void Connect()
        {
            // XÓA SOCKET CŨ – BẮT BUỘC
            if (socketConn != null)
            {
                socketConn.OnDataReceivedBytes -= SocketDataHandler;
                socketConn.Disconnect();
            }

            // TẠO SOCKETHOÀN TOÀN MỚI
            socketConn = new SocketHandler();
            socketConn.OnDataReceivedBytes += SocketDataHandler;

            bool ok = socketConn.Connect(IP, PORT);

            IsConnected = ok;
            TriggerUI();
        }


        public void Disconnect()
        {
            if (socketConn != null)
            {
                socketConn.OnDataReceivedBytes -= SocketDataHandler;

                try
                {
                    // GỬI 1 BYTE ĐỂ SERVER NHẬN BIẾT NGẮT KẾT NỐI
                    socketConn.SendBytes(new byte[] { 0xFF });
                }
                catch { }

                socketConn.Disconnect();
            }

            IsConnected = false;

            // RESET UI ngay lập tức
            Sensor1 = Sensor2 = Sensor3 = Sensor4 = false;
            Sensor5 = Sensor6 = Sensor7 = Sensor8 = false;

            Mes = "DISCONNECTED";

            TriggerUI();
        }




        // ============== FRAME PARSER FOR ESP32 ================
        private void SocketDataHandler(byte[] buffer, int len)
        {
            if (len < 10) return;

            if (buffer[0] != 0x30) return;     // START_BYTE
            if (buffer[9] != 0x31) return;     // STOP_BYTE

            // Raw frame string
            Mes = BitConverter.ToString(buffer, 0, len).Replace("-", " ");

            byte key = buffer[1];
            byte type = buffer[2];
            byte id = buffer[3];

            // Parse data (32-bit)
            uint value =
                ((uint)buffer[4] << 24) |
                ((uint)buffer[5] << 16) |
                ((uint)buffer[6] << 8) |
                 buffer[7];

            // CRC check
            byte crcFrame = buffer[8];
            byte crcCalc = CRC8(buffer, 0, 8);

            if (crcCalc != crcFrame)
            {
                Mes += " ❌ CRC";
                return;
            }

            // ========== SENSOR BIT MAPPING ==============
            // ESP32 tạo data bằng cách shift trước → sensor1 ở BIT 7
            Sensor1 = (value & (1u << 0)) != 0;
            Sensor2 = (value & (1u << 1)) != 0;
            Sensor3 = (value & (1u << 2)) != 0;
            Sensor4 = (value & (1u << 3)) != 0;
            Sensor5 = (value & (1u << 4)) != 0;
            Sensor6 = (value & (1u << 5)) != 0;
            Sensor7 = (value & (1u << 6)) != 0;
            Sensor8 = (value & (1u << 7)) != 0;

            

            TriggerUI();
        }


        // ============= CRC8 giống hệt ESP32 ================
        private byte CRC8(byte[] data, int start, int len)
        {
            byte crc = 0x00;
            byte poly = 0x07;

            for (int i = start; i < len; i++)
            {
                crc ^= data[i];
                for (int b = 0; b < 8; b++)
                {
                    bool msb = (crc & 0x80) != 0;
                    crc <<= 1;
                    if (msb)
                        crc ^= poly;
                }
            }
            return crc;
        }
    }
}
