using System;
using System.Windows.Forms;
using THI_HANG_A1.Managers;
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
            button3.Text = "Stop";
            button4.Text = "Start";

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
            string sts;
            switch (moto.Status)
            {
                case ConstantKeys.STATUS_READY:
                    sts = "Chuẩn bị thi";
                    break;
                case ConstantKeys.STATUS_FREE:
                    sts = "Rảnh";
                    break;
                case ConstantKeys.STATUS_CONTEST1:
                    sts = "Bài thi số 1";
                    break;
                case ConstantKeys.STATUS_CONTEST2:
                    sts = "Bài thi số 2";
                    break;
                case ConstantKeys.STATUS_CONTEST3:
                    sts = "Bài thi số 3";
                    break;
                case ConstantKeys.STATUS_CONTEST4:
                    sts = "Bài thi số 4";
                    break;
                default:
                    sts = "No data";
                    break;

            }
            label4.Text = sts;

            checkBox1.Checked = moto.Hall;
            checkBox2.Checked = moto.SignalLeft;
            checkBox3.Checked = moto.Engine;
            checkBox4.Checked = false;

            if (moto.Connected)
            {
                this.BackColor = System.Drawing.Color.LightSkyBlue;
            }
            else
            {
                this.BackColor = System.Drawing.SystemColors.MenuBar;
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            moto.Connect();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            moto.sendCommand(ConstantKeys.CONTROL_KEY, ConstantKeys.BYTE_SET, ConstantKeys.CONTROL_START);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            moto.sendCommand(ConstantKeys.CONTROL_KEY, ConstantKeys.BYTE_SET, ConstantKeys.CONTROL_STOP);
        }
    }
}
