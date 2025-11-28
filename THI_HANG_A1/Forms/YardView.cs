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

            // Khi dữ liệu sân thay đổi -> update UI
            san.OnChanged += SanChanged;
        }

        private void YardView_Load(object sender, EventArgs e)
        {
            UpdateUI();
        }

        // Đảm bảo thread-safe
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
            

            chkSensor1.Checked = san.Sensor1;
            chkSensor2.Checked = san.Sensor2;
            chkSensor3.Checked = san.Sensor3;
            chkOngHoi.Checked = san.OngHoi;
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (!san.IsConnected)
            {
                san.Connect();          // nếu chưa kết nối → kết nối
            }
            else
            {
                san.Disconnect();       // nếu đang kết nối → ngắt kết nối
            }

            UpdateUI(); // cập nhật giao diện sau thao tác
            button2.Text = san.IsConnected ? "Disconnect" : "Connect";
        }



        private void chkSensor3_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
