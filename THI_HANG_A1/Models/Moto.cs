using System;
using THI_HANG_A1.Managers;

namespace THI_HANG_A1.Models
{
    public class Moto
    {
        public event Action OnChanged;
        public string Name { set; get; }
        public string Ip { set; get; }
        public int Port { set; get; }
        private int encoderCount;
        public int EncoderCount
        {
            get => encoderCount;
            set { encoderCount = value; OnChanged?.Invoke(); }
        }

        private bool hall;
        public bool Hall
        {
            get => hall;
            set { hall = value; OnChanged?.Invoke(); }
        }

        private bool signalLeft;
        public bool SignalLeft
        {
            get => signalLeft;
            set { signalLeft = value; OnChanged?.Invoke(); }
        }

        private bool engine;
        public bool Engine
        {
            get => engine;
            set { engine = value; OnChanged?.Invoke(); }
        }
        private string mes;
        public string Mes
        {
            get => mes;
            set { mes = value; OnChanged?.Invoke(); }
        }


        public SocketHandler socketConn;


        public Moto(string name, string ip, int port)
        {
            Name = name;
            Ip = ip;
            Port = port;
            socketConn = new SocketHandler();
        }
        public void Connect()
        {
            socketConn.Connect(Ip, Port);

            socketConn.OnDataReceived += SocketDataHandler;
        }
        private void SocketDataHandler(byte[] buffer, int len)
        {
            byte mid = buffer[3];
            byte mkey = buffer[1];
            byte mtype = buffer[2];
            UInt32 mvalue = ((UInt32)buffer[4] << 24) | ((UInt32)buffer[5] << 16) | ((UInt32)buffer[6] << 8) | ((UInt32)buffer[7]);




            //Mes = msg;
            // 🔥 Ví dụ ESP32 gửi: "MES=1"
            // hoặc "MES:1"
            // hoặc "HALL=1;ENGINE=0;MES=1"

            // Trường hợp 1: Frame có nhiều key-value
            //if (msg.Contains(";"))
            //{
            //    var parts = msg.Split(';');  // ví dụ: "HALL=1"

            //    foreach (var p in parts)
            //    {
            //        ParseAndUpdate(p);
            //    }
            //}
            //else
            //{
            //    // Chỉ 1 key-value
            //    ParseAndUpdate(msg);
            //}
        }


    }
}
