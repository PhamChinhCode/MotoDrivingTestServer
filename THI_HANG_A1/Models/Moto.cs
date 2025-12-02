using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using THI_HANG_A1.Managers;

namespace THI_HANG_A1.Models
{
    public class Moto
    {
        public event Action OnChanged;
        public event Action onImage;
        public event Action onRecvCommand;
        public byte Id { get; set; }
        public string Name { set; get; }

        public string Ip { set; get; }
        public int Port { set; get; }

        public Bitmap image { set; get; }
        private bool connected;
        public bool Connected
        {
            get => connected;
            set { connected = value; OnChanged?.Invoke(); }
        }

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

        private byte status;
        public byte Status
        {
            get => status;
            set { status = value; OnChanged?.Invoke(); }
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
        private byte errorId;
        public byte ErrorId
        {
            get => errorId;
            set { errorId = value; OnChanged?.Invoke(); }
        }


        public SocketHandler socketConn;
        //private FrameCnvert frameConvertor;
        public List<LogMoto> log { get; set; } = new List<LogMoto>();




        public Moto(string name, string ip, int port)
        {
            Name = name;
            Ip = ip;
            Port = port;
            socketConn = new SocketHandler();

        }

        public Moto()
        {

            socketConn = new SocketHandler();
        }

        public async Task Connect()
        {
            //bool ok = socketConn.Connect(Ip, Port);
            bool ok = await socketConn.ConnectWithTimeout(Ip, Port);

            if (!ok)
            {
                Mes = "Không thể kết nối tới ESP32";
                return;
            }
            Connected = ok;
            socketConn.OnDataReceivedImage += onRecvImage;
            socketConn.OnDataReceivedCommand += onRecv;
            socketConn.OnDataReceived += SocketDataHandler;
            socketConn.OnDisconnected += disConnectHandler;
        }
        private void onRecvImage(byte[] array)
        {
            image = ByteArrayToBitmap(array);
            onImage?.Invoke();
        }
        public Bitmap ByteArrayToBitmap(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                return new Bitmap(ms);
            }
        }
        private void onRecv(Command cmd)
        {
            log.Add(new LogMoto(cmd.key, cmd.type, cmd.value, DateTime.Now));
            switch (cmd.key)
            {
                case ConstantKeys.ERROR_KEY:
                    ErrorId = (byte)cmd.value;
                    break;
                case ConstantKeys.STATUS_KEY:
                    Status = (byte)cmd.value;
                    break;
                case ConstantKeys.IMAGE_KEY:
                    break;
                case ConstantKeys.CONTROL_KEY:
                    //MessageBox.Show("Moto setted mode :" + Convert.ToString(cmd.value, 16));
                    break;

            }
            onRecvCommand?.Invoke();

        }
        private void disConnectHandler()
        {
            Connected = false;
        }
        public void sendCommand(byte key, byte type, UInt32 value)
        {
            if (!Connected) return;
            byte[] buff = new byte[10];
            buff[0] = ConstantKeys.BYTE_START;
            buff[1] = key;
            buff[2] = type;
            buff[3] = Id;
            buff[4] = (byte)((value >> 24) & 0xff);
            buff[5] = (byte)((value >> 16) & 0xff);
            buff[6] = (byte)((value >> 8) & 0xff);
            buff[7] = (byte)(value & 0xff);
            buff[8] = 0;
            buff[9] = ConstantKeys.BYTE_STOP;
            socketConn.SendBytes(buff);
        }
        private void SocketDataHandler(byte[] buffer, int len)
        {
            //Status = buffer[0];
            //byte mid = buffer[3];
            //byte mkey = buffer[1];
            //byte mtype = buffer[2];
            //UInt32 mvalue = ((UInt32)buffer[4] << 24) | ((UInt32)buffer[5] << 16) | ((UInt32)buffer[6] << 8) | ((UInt32)buffer[7]);
            FrameCnvert frameConvertor = new FrameCnvert();
            frameConvertor.setFrame(buffer, len);
            log.Add(new LogMoto(frameConvertor.key, frameConvertor.type, frameConvertor.value, DateTime.Now));
            switch (frameConvertor.key)
            {
                case ConstantKeys.ERROR_KEY:
                    ErrorId = (byte)frameConvertor.value;
                    break;
                case ConstantKeys.STATUS_KEY:
                    Status = (byte)frameConvertor.value;
                    break;
                case ConstantKeys.IMAGE_KEY:
                    break;

            }


        }
        public class LogMoto
        {
            public DateTime ThoiGian { get; set; }

            public byte key { get; set; }
            public byte type { get; set; }
            public UInt32 value { get; set; }

            public LogMoto(byte key, byte type, UInt32 value, DateTime time)
            {
                this.key = key;
                this.type = type;
                this.value = value;
                this.ThoiGian = time;
            }
            public override string ToString()
            {
                string s = Convert.ToString(key, 16) + "\t" + Convert.ToString(type, 16) + "\t" + Convert.ToString(value, 16) + "\t" + ThoiGian.ToString();
                return s;
            }
        }

    }
}
