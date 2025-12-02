using System;
using System.Reflection.Emit;
using System.Windows.Forms;
using THI_HANG_A1.Models;

namespace THI_HANG_A1.Forms
{
    public partial class YardView : UserControl
    {
        private San san;

        public YardView(San s)
        {
            InitializeComponent();
            san = s;

            san.OnChanged += SanChanged;
        }

        private void YardView_Load(object sender, EventArgs e)
        {
            UpdateUI();
        }

        // Thread-safe update
        private void SanChanged()
        {
            if (InvokeRequired)
                BeginInvoke(new Action(UpdateUI));
            else
                UpdateUI();
        }

        private void UpdateUI()
        {
            lblName.Text = san.Name;
            label2.Text = san.IP;
            label1.Text = san.Mes;

            if (!san.IsConnected)
            {
                // CLEAR UI khi ngắt kết nối
                chkSensor1.Checked = false;
                chkSensor2.Checked = false;
                chkSensor3.Checked = false;
                chkSensor4.Checked = false;
                chkSensor5.Checked = false;
                chkSensor6.Checked = false;
                chkSensor7.Checked = false;
                chkSensor8.Checked = false;
            }
            else
            {
                chkSensor1.Checked = san.Sensor1;
                chkSensor2.Checked = san.Sensor2;
                chkSensor3.Checked = san.Sensor3;
                chkSensor4.Checked = san.Sensor4;
                chkSensor5.Checked = san.Sensor5;
                chkSensor6.Checked = san.Sensor6;
                chkSensor7.Checked = san.Sensor7;
                chkSensor8.Checked = san.Sensor8;
            }

            // Đồng bộ text nút kết nối
            button2.Text = san.IsConnected ? "Disconnect" : "Connect";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!san.IsConnected)
                san.Connect();
            else
                san.Disconnect();
            UpdateUI();
        }



        private void chkSensor3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void chkSensor8_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
