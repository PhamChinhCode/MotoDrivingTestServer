using System;
using System.Windows.Forms;
using THI_HANG_A1.Helpers;
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
            moto.onImage += MotoOnChanged;
            moto.onRecvCommand += Moto_onRecvCommand;
        }

        private void Moto_onRecvCommand()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(showLog));
            }
            else
            {
                showLog();
            }

        }
        private void showLog()
        {
            try
            {
                if (textBox1 == null || textBox1.IsDisposed) return;
                if (moto.log == null || moto.log.Count == 0) return;

                int lastIndex = moto.log.Count - 1;
                textBox1.AppendText(lastIndex + "\t" + moto.log[lastIndex] + Environment.NewLine);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void UserControl1_Load(object sender, EventArgs e)
        {

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
            pictureBox1.Image = moto.image;
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

            this.BackColor = MotoHelper.GetMotoColor(moto);

            if (moto.Connected)
            {
                button2.Text = "Ngắt kết nối";
                button2.BackColor = System.Drawing.Color.LightSkyBlue;
            }
            else
            {
                button2.Text = "Kết nối";
                button2.BackColor = System.Drawing.SystemColors.MenuBar;
                this.BackColor = System.Drawing.SystemColors.Window;

            }


        }

        private async void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;

            try
            {
                if (!moto.Connected)
                {
                    await moto.Connect();   // async thật
                }
                else
                {
                    moto.Disconnect();           // sync
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                button2.Enabled = true;
            }
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

        private void MotoView_Load(object sender, EventArgs e)
        {
            checkBox2.Text = "SignedLeft";
            checkBox3.Text = "Engine";
            checkBox4.Text = "null";
            checkBox1.Text = "Hall";
            button1.Text = "Edit";
            button2.Text = "Connect";
            button3.Text = "Stop";
            button4.Text = "Start";
            button5.Text = "Image";

            UpdateUI();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            moto.sendCommand(ConstantKeys.CONTROL_KEY, ConstantKeys.BYTE_SET, ConstantKeys.CONTROL_START);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            moto.sendCommand(ConstantKeys.CONTROL_KEY, ConstantKeys.BYTE_SET, ConstantKeys.CONTROL_STOP);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            moto.sendCommand(ConstantKeys.IMAGE_KEY, ConstantKeys.BYTE_GET, ConstantKeys.KEY_NULL);
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
