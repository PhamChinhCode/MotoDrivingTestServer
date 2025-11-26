using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace THI_HANG_A1.Managers
{
    public class SocketHandler
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        public string IPAddress { get; set; }
        public int IPPort { get; set; }


        public bool IsConnected => _client != null && _client.Connected;

        // Sự kiện đẩy dữ liệu ra Form
        public event Action<byte[], int> OnDataReceived;
        public event Action OnDisconnected;

        // ================================================================
        // KẾT NỐI
        // ================================================================
        public bool Connect(string ip, int port)
        {
            try
            {
                if (_client != null)
                {
                    Disconnect();
                }
                //if (!_client.Connected)
                //{
                _client = new TcpClient();
                _client.Connect(ip, port);
                _stream = _client.GetStream();

                // Bắt đầu Thread nhận dữ liệu
                StartReceiveThread();
                //}
                return true;

            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool Connect()
        {
            return Connect(IPAddress, IPPort);
        }
        // ================================================================
        // NGẮT KẾT NỐI
        // ================================================================
        public void Disconnect()
        {
            try
            {
                _receiveThread?.Abort();
            }
            catch { }

            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }

            OnDisconnected?.Invoke();
        }

        // ================================================================
        // GỬI DỮ LIỆU
        // ================================================================
        public void SendString(string msg)
        {
            if (!IsConnected) return;

            byte[] data = Encoding.UTF8.GetBytes(msg);
            _stream.Write(data, 0, data.Length);
        }

        public void SendBytes(byte[] data)
        {
            if (!IsConnected) return;

            _stream.Write(data, 0, data.Length);
        }

        // ================================================================
        // NHẬN DỮ LIỆU (THREAD)
        // ================================================================
        private void StartReceiveThread()
        {
            _receiveThread = new Thread(ReceiveLoop);
            _receiveThread.IsBackground = true;
            _receiveThread.Start();
        }

        private void ReceiveLoop()
        {
            byte[] buffer = new byte[1024];

            while (true)
            {
                try
                {
                    int len = _stream.Read(buffer, 0, buffer.Length);

                    if (len <= 0)
                    {
                        Disconnect();
                        return;
                    }

                    string msg = Encoding.UTF8.GetString(buffer, 0, len);

                    // Đưa dữ liệu về Form
                    OnDataReceived?.Invoke(buffer, len);
                }
                catch
                {
                    Disconnect();
                    return;
                }
            }
        }
    }
}
