using System;
using System.Windows.Forms;
using THI_HANG_A1.Models;

namespace THI_HANG_A1.Forms
{
    public partial class MotoView : UserControl
    {
        private Moto moto;
        public MotoView(Moto m)
        {
            InitializeComponent();
            moto = m;
            moto.OnChanged += MotoOnChanged;
        }

        private void UserControl1_Load(object sender, EventArgs e)
        {
            checkBox2.Text = "SignedLeft";
            checkBox3.Text = "Engine";
            checkBox4.Text = "null";
            checkBox1.Text = "Hall";
            button1.Text = "Edit";
            button2.Text = "Connect";
            UpdateUI();
        }


        private void MotoOnChanged()
        {
            // Cập nhật UI nếu gọi từ thread UI
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateUI));
            }
            else
            {
                UpdateUI();
            }
        }

        private void UpdateUI()
        {

            label1.Text = moto.Name;
            label2.Text = moto.Ip;
            label3.Text = moto.EncoderCount.ToString();
            label3.Text = moto.Mes;

            checkBox1.Checked = moto.Hall;
            checkBox2.Checked = moto.SignalLeft;
            checkBox3.Checked = moto.Engine;
            checkBox4.Checked = false;

        }

        private void button2_Click(object sender, EventArgs e)
        {
            moto.Connect();
        }
    }
}
