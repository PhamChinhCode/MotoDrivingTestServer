using System;
using System.Runtime.InteropServices;
namespace THI_HANG_A1.Camera.Models
{
    public class CHCNetSDK
    {
        public const int NET_DVR_DEV_ADDRESS_MAX_LEN = 129;
        public const int NET_DVR_LOGIN_USERNAME_MAX_LEN = 64;
        public const int NET_DVR_LOGIN_PASSWD_MAX_LEN = 64;
        public const int STREAM_ID_LEN = 32;

        [StructLayout(LayoutKind.Sequential)]
        public struct NET_DVR_DEVICEINFO_V30
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
            public byte[] sSerialNumber;
            public byte byAlarmInPortNum;
            public byte byAlarmOutPortNum;
            public byte byDiskNum;
            public byte byDVRType;
            public byte byChanNum;
            public byte byStartChan;
            public byte byAudioChanNum;
            public byte byIPChanNum;
        }
        public const int MAX_PRO_PATH = 256; //最大协议路径长度
        //预览V40接口
        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct NET_DVR_PREVIEWINFO
        {
            public Int32 lChannel;
            public uint dwStreamType;
            public uint dwLinkMode;
            public IntPtr hPlayWnd;
            public bool bBlocked;
            public bool bPassbackRecord;
            public byte byPreviewMode;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = STREAM_ID_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] byStreamID;
            public byte byProtoType;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.I1)]
            public byte[] byRes1;
            public uint dwDisplayBufNum;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 216, ArraySubType = UnmanagedType.I1)]
            public byte[] byRes;
        }

        [DllImport(@"HCNetSDK.dll")]
        public static extern int NET_DVR_RealPlay_V40(int iUserID, ref NET_DVR_PREVIEWINFO lpPreviewInfo, REALDATACALLBACK fRealDataCallBack_V30, IntPtr pUser);
        [DllImport("HCNetSDK.dll", CharSet = CharSet.Ansi)]
        public static extern IntPtr NET_DVR_GetErrorMsg(ref int pErrorNo);

        [DllImport("HCNetSDK.dll")]
        public static extern bool NET_DVR_Init();

        [DllImport("HCNetSDK.dll")]
        public static extern int NET_DVR_Login_V30(
            string sDVRIP,
            int wDVRPort,
            string sUserName,
            string sPassword,
            ref NET_DVR_DEVICEINFO_V30 lpDeviceInfo);

        [DllImport("HCNetSDK.dll")]
        public static extern int NET_DVR_RealPlay_V40(
            int lUserID,
            ref NET_DVR_PREVIEWINFO lpPreviewInfo,
            IntPtr pUser);

        [DllImport("HCNetSDK.dll")]
        public static extern bool NET_DVR_StopRealPlay(int lRealHandle);

        [DllImport("HCNetSDK.dll")]
        public static extern bool NET_DVR_Logout(int lUserID);

        [DllImport("HCNetSDK.dll")]
        public static extern bool NET_DVR_Cleanup();


        [DllImport("HCNetSDK.dll")]
        public static extern uint NET_DVR_GetLastError();


        public delegate void REALDATACALLBACK(Int32 lRealHandle, UInt32 dwDataType, IntPtr pBuffer, UInt32 dwBufSize, IntPtr pUser);


        [StructLayout(LayoutKind.Sequential)]
        public struct NET_DVR_JPEGPARA
        {
            public ushort wPicSize;
            public ushort wPicQuality;
        }

        [DllImport("HCNetSDK.dll")]
        public static extern bool NET_DVR_CaptureJPEGPicture(
            int lUserID,
            int lChannel,
            ref NET_DVR_JPEGPARA lpJpegPara,
            string sPicFileName
        );


    }
}