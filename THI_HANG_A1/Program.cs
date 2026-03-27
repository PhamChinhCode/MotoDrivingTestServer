using QuestPDF.Infrastructure;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace THI_HANG_A1
{
    internal static class Program
    {


        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetDllDirectory(string lpPathName);

        [STAThread]
        static void Main()
        {

            string dllPath = @"D:\Work\MotoDrivingTestServer\THI_HANG_A1\libs";
            SetDllDirectory(dllPath);

            QuestPDF.Settings.License = LicenseType.Community;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}