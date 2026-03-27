using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using THI_HANG_A1.Camera.Models;
using static THI_HANG_A1.Camera.Models.CHCNetSDK;

namespace THI_HANG_A1.Camera.Services
{
    public class HikvisionCameraService : IDisposable
    {
        private int _userID = -1;
        private int _realPlayID = -1;
        private CHCNetSDK.NET_DVR_DEVICEINFO_V30 _deviceInfo;
        private bool _isInit = false;

        /// <summary>
        /// Init SDK
        /// </summary>
        public bool Init()
        {
            if (_isInit) return true;

            _isInit = CHCNetSDK.NET_DVR_Init();
            return _isInit;
        }

        /// <summary>
        /// Login camera
        /// </summary>
        public bool Login(string ip, int port, string user, string password)
        {
            if (!_isInit)
                throw new Exception("SDK not initialized");

            _deviceInfo = new CHCNetSDK.NET_DVR_DEVICEINFO_V30();

            _userID = CHCNetSDK.NET_DVR_Login_V30(
                ip,
                port,
                user,
                password,
                ref _deviceInfo);

            return _userID >= 0;
        }

        /// <summary>
        /// Start preview
        /// </summary>
        ///

        private CHCNetSDK.REALDATACALLBACK _realDataCallback;

        private void RealDataCallBack(
            int lRealHandle,
            uint dwDataType,
            IntPtr pBuffer,
            uint dwBufSize,
            IntPtr pUser)
        {
            // Không xử lý gì cũng được
        }

        public bool StartPreview(IntPtr hwnd, int channel)
        {
            StopPreview();

            if (_userID < 0)
                throw new Exception("Not logged in");

            if (hwnd == IntPtr.Zero)
                throw new Exception("Invalid hwnd");

            if (_realDataCallback == null)
                _realDataCallback = new CHCNetSDK.REALDATACALLBACK(RealDataCallBack);

            CHCNetSDK.NET_DVR_PREVIEWINFO info = new CHCNetSDK.NET_DVR_PREVIEWINFO();

            info.lChannel = channel;          // 🔥 = 1
            info.dwStreamType = 0;            // 0 = main stream (demo dùng main)
            info.dwLinkMode = 0;              // TCP
            info.hPlayWnd = hwnd;             // 🔥 render trực tiếp
            info.bBlocked = true;
            info.bPassbackRecord = false;
            info.byPreviewMode = 0;

            info.byStreamID = new byte[32];   // ⚠️ BẮT BUỘC
            info.byProtoType = 0;
            info.byRes1 = new byte[2];        // ⚠️ BẮT BUỘC
            info.dwDisplayBufNum = 1;
            info.byRes = new byte[216];       // ⚠️ BẮT BUỘC

            Debug.WriteLine($"StartPreview: channel={channel}, hwnd={hwnd}");

            _realPlayID = CHCNetSDK.NET_DVR_RealPlay_V40(
                _userID,
                ref info,
                _realDataCallback,
                IntPtr.Zero
            );

            if (_realPlayID < 0)
            {
                int err = 0;
                IntPtr pMsg = CHCNetSDK.NET_DVR_GetErrorMsg(ref err);
                string msg = Marshal.PtrToStringAnsi(pMsg);

                MessageBox.Show(
                    $"NET_DVR_RealPlay_V40 failed\nError={err}\nMsg={msg}",
                    "Hikvision",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                return false;
            }

            return true;
        }



        /// <summary>
        /// Stop preview
        /// </summary>
        public void StopPreview()
        {
            if (_realPlayID >= 0)
            {
                Debug.WriteLine("Stopping old preview: " + _realPlayID);
                CHCNetSDK.NET_DVR_StopRealPlay(_realPlayID);
                _realPlayID = -1;
            }
        }

        /// <summary>
        /// Logout camera
        /// </summary>
        public void Logout()
        {
            if (_userID >= 0)
            {
                CHCNetSDK.NET_DVR_Logout(_userID);
                _userID = -1;
            }
        }

        /// <summary>
        /// Cleanup SDK
        /// </summary>
        public void Cleanup()
        {
            if (_isInit)
            {
                CHCNetSDK.NET_DVR_Cleanup();
                _isInit = false;
            }
        }

        public void Dispose()
        {
            StopPreview();
            Logout();
            Cleanup();
        }


        public Bitmap CaptureFrame(int channel)
        {
            if (_userID < 0)
                throw new Exception("Not logged in");

            string tempFile = Path.Combine(
                Path.GetTempPath(),
                $"cam_{_userID}_{channel}.jpg"
            );

            CHCNetSDK.NET_DVR_JPEGPARA para = new CHCNetSDK.NET_DVR_JPEGPARA
            {
                wPicQuality = 2, // 0~2 (2 = best)
                wPicSize = 0     // 0 = auto
            };

            bool ok = CHCNetSDK.NET_DVR_CaptureJPEGPicture(
                _userID,
                channel,
                ref para,
                tempFile
            );

            if (!ok)
            {
                int err = (int)CHCNetSDK.NET_DVR_GetLastError();
                throw new Exception($"Capture failed, err={err}");
            }

            // load bitmap
            using (var fs = new FileStream(tempFile, FileMode.Open, FileAccess.Read))
            {
                return new Bitmap(fs);
            }
        }

    }

}
