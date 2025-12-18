using System;
using System.Drawing;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace THI_HANG_A1.Managers
{
    //public class SocketHandler
    //{
    //    private TcpClient _client;
    //    private NetworkStream _stream;
    //    private Thread _receiveThread;
    //    public string IPAddress { get; set; }
    //    public int IPPort { get; set; }
    //    public event Action<Command> OnDataReceivedCommand;
    //    public event Action<byte[]> OnDataReceivedImage;


    //    public bool IsConnected => _client != null && _client.Connected;

    //    // Sự kiện đẩy dữ liệu ra Form
    //    public event Action<byte[], int> OnDataReceived;
    //    public event Action OnDisconnected;
    //    private System.Windows.Forms.Timer timerOnline;

    //    // ================================================================
    //    // KẾT NỐI
    //    // ================================================================
    //    public SocketHandler()
    //    {
    //        timerOnline = new System.Windows.Forms.Timer();
    //        timerOnline.Interval = 2000;
    //        timerOnline.Tick += async (s, e) =>
    //        {
    //            bool ok = await PingAsync(IPAddress, 1000);
    //            if (!ok && IsConnected)
    //                MessageBox.Show("no connect !!!!");
    //        };

    //    }
    //    public bool Connect(string ip, int port)
    //    {
    //        IPAddress = ip;
    //        IPPort = port;
    //        try
    //        {
    //            if (_client == null)
    //            {
    //                _client = new TcpClient();
    //            }
    //            if (!_client.Connected)
    //            {
    //                _client.Connect(ip, port);
    //                if (_client.Connected)
    //                {
    //                    _stream = _client.GetStream();

    //                    // Bắt đầu Thread nhận dữ liệu
    //                    StartReceiveThread();
    //                    //}
    //                    return true;
    //                }
    //                else
    //                {
    //                    return false;
    //                }
    //            }
    //            else
    //            {
    //                return true;
    //            }

    //        }
    //        catch (Exception)
    //        {
    //            MessageBox.Show("Khong ket noi dc");
    //            return false;
    //        }
    //    }
    //    public async Task<bool> ConnectWithTimeout(string ip, int port, int timeoutMs = 5000)
    //    {
    //        IPAddress = ip;
    //        IPPort = port;
    //        timerOnline.Start();
    //        try
    //        {
    //            using (var cts = new CancellationTokenSource(timeoutMs))
    //            {
    //                // TcpClient async
    //                _client = new TcpClient();

    //                var connectTask = _client.ConnectAsync(ip, port);

    //                // Chờ connect hoặc timeout
    //                var completed = await Task.WhenAny(connectTask, Task.Delay(timeoutMs, cts.Token));

    //                if (completed != connectTask)
    //                {
    //                    // Timeout
    //                    _client?.Close();
    //                    return false;
    //                }

    //                // Connected
    //                await connectTask;  // đảm bảo throw đúng exception nếu có
    //                //_stream.ReadTimeout = 5000;
    //                _stream = _client.GetStream();
    //                StartReceiveThread();
    //                return true;
    //            }
    //        }
    //        catch
    //        {
    //            _client?.Close();
    //            return false;
    //        }
    //    }

    //    public bool Connect()
    //    {
    //        return Connect(IPAddress, IPPort);
    //    }
    //    // ================================================================
    //    // NGẮT KẾT NỐI
    //    // ================================================================
    //    public void Disconnect()
    //    {

    //        try
    //        {
    //            _receiveThread?.Abort();
    //        }
    //        catch { }

    //        try { _stream?.Close(); } catch { }
    //        try { _client?.Close(); } catch { }

    //        OnDisconnected?.Invoke();
    //        //MessageBox.Show(" Mất kết nối tới: " + Convert.ToString(IPAddress));
    //    }
    //    private async Task<bool> PingAsync(string ip, int timeoutMs = 1000)
    //    {
    //        try
    //        {
    //            using (var ping = new Ping())
    //            {
    //                var reply = await ping.SendPingAsync(ip, timeoutMs);
    //                return reply.Status == IPStatus.Success;
    //            }
    //        }
    //        catch
    //        {
    //            return false;
    //        }
    //    }
    //    public bool Ping(string ip, int timeoutMs = 1000)
    //    {
    //        try
    //        {
    //            using (Ping ping = new Ping())
    //            {
    //                PingReply reply = ping.Send(ip, timeoutMs);

    //                return reply.Status == IPStatus.Success;
    //            }
    //        }
    //        catch
    //        {
    //            return false;
    //        }
    //    }
    //    // ================================================================
    //    // GỬI DỮ LIỆU
    //    // ================================================================
    //    public void SendString(string msg)
    //    {
    //        if (!IsConnected) return;

    //        byte[] data = Encoding.UTF8.GetBytes(msg);
    //        _stream.Write(data, 0, data.Length);
    //    }

    //    public void SendBytes(byte[] data)
    //    {
    //        if (!IsConnected) return;

    //        _stream.Write(data, 0, data.Length);
    //    }

    //    // ================================================================
    //    // NHẬN DỮ LIỆU (THREAD)
    //    // ================================================================
    //    private void StartReceiveThread()
    //    {
    //        _receiveThread = new Thread(ReceiveLoop);
    //        _receiveThread.IsBackground = true;
    //        _receiveThread.Start();
    //    }

    //    private void ReceiveLoop()
    //    {
    //        byte[] buffer = new byte[1024];
    //        Command cmd = new Command();



    //        while (true)
    //        {


    //            try
    //            {
    //                int data = _stream.ReadByte();

    //                if (data <= 0)
    //                {
    //                    Disconnect();
    //                    return;
    //                }
    //                if (data != ConstantKeys.BYTE_START) return;
    //                buffer = ReadExact(_stream, 9);
    //            }
    //            catch (IOException)
    //            {
    //                // ReadTimeout hoặc socket lỗi
    //                Disconnect();
    //                return;
    //            }


    //            if (buffer[8] != ConstantKeys.BYTE_STOP && buffer[8] != ConstantKeys.BYTE_PAYLOAD) return;

    //            cmd.key = buffer[0];
    //            cmd.type = buffer[1];
    //            cmd.value = (UInt32)buffer[3] << 24 | (UInt32)buffer[4] << 16 | (UInt32)buffer[5] << 8 | (UInt32)buffer[6];

    //            if (buffer[8] == ConstantKeys.BYTE_PAYLOAD && buffer[0] == ConstantKeys.IMAGE_KEY)
    //            {
    //                byte[] image = new byte[cmd.value];
    //                image = ReadExact(_stream, (int)cmd.value);

    //                if (_stream.ReadByte() != ConstantKeys.BYTE_STOP) return;

    //                Bitmap bmp = ByteArrayToBitmap(image);
    //                OnDataReceivedImage?.Invoke(image);

    //            }
    //            OnDataReceivedCommand?.Invoke(cmd);
    //        }
    //    }
    //    private byte[] ReadExact(NetworkStream stream, int size)
    //    {
    //        byte[] buf = new byte[size];
    //        int offset = 0;

    //        while (offset < size)
    //        {
    //            int n = stream.Read(buf, offset, size - offset);
    //            if (n <= 0) throw new Exception("Disconnected");
    //            offset += n;
    //        }
    //        return buf;

    //    }
    //    public Bitmap ByteArrayToBitmap(byte[] bytes)
    //    {
    //        using (var ms = new MemoryStream(bytes))
    //        {
    //            return new Bitmap(ms);
    //        }
    //    }

    //}
    public class SocketHandlerAsync
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;

        public string IPAddress { get; set; }
        public int IPPort { get; set; }
        private bool Actioned = true;
        public bool IsConnected =>
            _client != null &&
            _client.Connected;

        // ================= EVENTS =================
        public event Action<Command> OnDataReceivedCommand;
        public event Action<byte[]> OnDataReceivedImage;
        public event Action<byte[], int> OnDataReceived;
        public event Action OnDisconnected;

        // ================= TIMER =================
        private readonly System.Windows.Forms.Timer _timerOnline;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);


        // ================================================================
        // CONSTRUCTOR
        // ================================================================
        public SocketHandlerAsync()
        {
            _timerOnline = new System.Windows.Forms.Timer();
            _timerOnline.Interval = 1000; // 2s
            _timerOnline.Tick += async (s, e) => await CheckOnlineAsync();
        }

        // ================================================================
        // CONNECT (ASYNC)
        // ================================================================
        public async Task<bool> ConnectAsync(string ip, int port, int timeoutMs = 3000)
        {
            IPAddress = ip;
            IPPort = port;

            try
            {
                Disconnect();

                _client = new TcpClient();
                _cts = new CancellationTokenSource();

                var connectTask = _client.ConnectAsync(ip, port);
                var timeoutTask = Task.Delay(timeoutMs);

                if (await Task.WhenAny(connectTask, timeoutTask) != connectTask)
                {
                    _client.Close();
                    return false;
                }

                _stream = _client.GetStream();
                Actioned = false;

                _timerOnline.Start();
                _ = ReceiveLoopAsync(_cts.Token); // fire & forget

                return true;
            }
            catch
            {
                Disconnect();
                return false;
            }
        }

        // ================================================================
        // DISCONNECT
        // ================================================================
        public void Disconnect()
        {
            try { _timerOnline.Stop(); } catch { }

            try { _cts?.Cancel(); } catch { }

            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }

            _stream = null;
            _client = null;

            if (!Actioned)
            {
                Actioned = true;
                OnDisconnected?.Invoke();

            }
        }

        // ================================================================
        // ONLINE CHECK (PING)
        // ================================================================
        private async Task CheckOnlineAsync()
        {
            if (string.IsNullOrEmpty(IPAddress))
                return;

            bool pingOk = await PingAsync(IPAddress, 5000);

            if (!pingOk && IsConnected)
            {

                //Disconnect();
            }
        }

        private async Task<bool> PingAsync(string ip, int timeoutMs)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(ip, timeoutMs);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        // ================================================================
        // SEND
        // ================================================================
        public void SendBytesAsync(byte[] data)
        {
            // Fire-and-forget có kiểm soát
            _ = SendCommandInternalAsync(data);
        }
        private async Task SendCommandInternalAsync(byte[] data)
        {
            if (!IsConnected) return;

            try
            {
                await _sendLock.WaitAsync();

                await _stream.WriteAsync(data, 0, data.Length);
            }
            catch
            {
                Disconnect();
            }
            finally
            {
                _sendLock.Release();
            }
        }


        // ================================================================
        // RECEIVE LOOP (ASYNC – NO THREAD)
        // ================================================================
        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            byte[] buffer;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    int start = await ReadByteAsync(_stream, token);
                    if (start <= 0) break;

                    if (start != ConstantKeys.BYTE_START)
                        continue;

                    buffer = await ReadExactAsync(_stream, 9, token);

                    if (buffer[8] != ConstantKeys.BYTE_STOP &&
                        buffer[8] != ConstantKeys.BYTE_PAYLOAD)
                        continue;

                    Command cmd = new Command
                    {
                        key = buffer[0],
                        type = buffer[1],
                        value = (uint)(
                            buffer[3] << 24 |
                            buffer[4] << 16 |
                            buffer[5] << 8 |
                            buffer[6])
                    };

                    if (buffer[8] == ConstantKeys.BYTE_PAYLOAD &&
                        buffer[0] == ConstantKeys.IMAGE_KEY)
                    {
                        byte[] image = await ReadExactAsync(_stream, (int)cmd.value, token);
                        int stop = await ReadByteAsync(_stream, token);
                        if (stop != ConstantKeys.BYTE_STOP)
                            continue;

                        OnDataReceivedImage?.Invoke(image);
                    }

                    OnDataReceivedCommand?.Invoke(cmd);
                }
            }
            catch
            {
                // socket lỗi
            }
            finally
            {
                Disconnect();
            }
        }
        private async Task<int> ReadByteAsync(NetworkStream stream, CancellationToken token)
        {
            byte[] buf = new byte[1];
            int n = await stream.ReadAsync(buf, 0, 1, token);
            if (n == 0) return -1;
            return buf[0];
        }
        private async Task<byte[]> ReadExactAsync(NetworkStream stream, int size, CancellationToken token)
        {
            byte[] buf = new byte[size];
            int offset = 0;

            while (offset < size)
            {
                int n = await stream.ReadAsync(buf, offset, size - offset, token);
                if (n <= 0)
                    throw new IOException("Disconnected");

                offset += n;
            }
            return buf;
        }

        public Bitmap ByteArrayToBitmap(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                return new Bitmap(ms);
            }
        }
    }
    public struct Command
    {
        public byte key;
        public UInt32 value;
        public byte type;
    }
    public static class ConstantKeys
    {
        public const int HEADER_LENGTH = 10; // 9 byte Header (từ START đến CRC)
        public const byte BYTE_START = 0x30;
        public const byte BYTE_STOP = 0x31;
        public const byte BYTE_SET = 0x32;
        public const byte BYTE_GET = 0x33;
        public const byte BYTE_PAYLOAD = 0x34;

        public const byte KEY_NULL = 0xff;


        // Status
        public const byte STATUS_KEY = 0xc0;

        public const byte STATUS_READY = 0xc1;      // sẵn sàng thi
        public const byte STATUS_TESTING = 0xc2;    // đang thi bỏ
        public const byte STATUS_FREE = 0xc3;       // đnag rảnh
        public const byte STATUS_CONTEST1 = 0xc4;   // bài số 8
        public const byte STATUS_CONTEST2 = 0xc5;   // bài đường thẳng
        public const byte STATUS_CONTEST3 = 0xc6;   // bài ziczac
        public const byte STATUS_CONTEST4 = 0xc7;   // bài gồ gề

        //canera key
        public const byte IMAGE_KEY = 0xB1;

        // mark key
        public const byte MARK_KEY = 0xB0;
        public const byte IDNUMBER_COMMAND = 0xB2;
        public const byte REALTIME_COMMAND = 0xB3;


        // Control command
        public const byte CONTROL_KEY = 0xA0;

        public const byte CONTROL_START = 0xA1;     // bắt đầu thi
        public const byte CONTROL_STOP = 0xA2;      // dừng bài thi
        public const byte CONTROL_READY = 0xA3;     // sẵn sàng thi

        // Error
        public const byte ERROR_KEY = 0xE0;

        public const byte ERROR_DE_VACH_XP = 0xE1;
        public const byte ERROR_DE_VACH_CNV = 0xE2;
        public const byte ERROR_CHAM_CHAN = 0xE3;
        public const byte ERROR_QUA_TG_THI = 0xE4;
        public const byte ERROR_DI_SAI_DUONG = 0xE5;
        public const byte ERROR_DO_XE = 0xE6;
        public const byte ERROR_DI_RA_NGOAI = 0xE7;
        public const byte ERROR_TAT_MAY = 0xE8;
        public const byte ERROR_KHONG_DOI_MU = 0xE9;
        public const byte ERROR_KHONG_XI_NHAN_VAO = 0xEA;
        public const byte ERROR_QUA_THOI_GIAN_XP = 0xEB;

        // Yard key
        public const byte VALUE_KEY = 0x80;
        public const byte VALUE_YARD = 0x81;
    }
    public class FrameCnvert
    {
        private byte[] frame;
        private int len;
        public byte key { get; set; }
        public UInt32 value { get; set; }
        public byte type { get; set; }
        public FrameCnvert() { }
        public FrameCnvert(byte[] data) { }
        public void setFrame(byte[] data, int l)
        {
            frame = data;
            this.len = l;
            value = ConstantKeys.KEY_NULL;
            key = ConstantKeys.KEY_NULL;
            convert();

        }
        private void convert()
        {
            if (frame == null) return;
            if (frame[0] != ConstantKeys.BYTE_START) return;
            if (frame[2] != ConstantKeys.BYTE_GET && frame[2] != ConstantKeys.BYTE_SET) return;

            key = frame[1];
            type = frame[2];
            value = ((UInt32)frame[4] << 24) | ((UInt32)frame[5] << 16) | ((UInt32)frame[6] << 8) | (UInt32)frame[7];
        }
        public void encode()
        {
            frame[0] = ConstantKeys.BYTE_START;
            frame[1] = key;
            frame[2] = ConstantKeys.BYTE_GET;

        }
    }
}
