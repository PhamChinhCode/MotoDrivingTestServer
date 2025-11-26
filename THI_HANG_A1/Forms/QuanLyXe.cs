using System;
using System.Collections.Generic;
using System.Windows.Forms;
using THI_HANG_A1.Forms;
using THI_HANG_A1.Models;

namespace THI_HANG_A1
{
    public partial class QuanLyXe : Form
    {
        private List<Moto> xes;

        private List<MotoView> xecontrol;
        public QuanLyXe(List<Moto> x)
        {
            this.xes = x;
            xecontrol = new List<MotoView>();
            InitializeComponent();
        }

        private void QuanLyXe_Load(object sender, EventArgs e)
        {

            flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanel1.AutoScroll = true;
            flowLayoutPanel1.WrapContents = false;

            label1.Text = xes.Count.ToString();
            for (int i = 0; i < xes.Count; i++)
            {
                xecontrol.Add(new MotoView(xes[i]));
                flowLayoutPanel1.Controls.Add(xecontrol[i]);
            }


        }
    }
}
