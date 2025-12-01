using System;
using System.Collections.Generic;
using System.Windows.Forms;
using THI_HANG_A1.Models;

namespace THI_HANG_A1.Forms
{
    public partial class QLiSan : Form
    {
        private readonly List<San> sans;
        private readonly List<YardView> sanControls;

        public QLiSan(List<San> s)
        {
           
            InitializeComponent();
            sans = s ?? new List<San>();
            sanControls = new List<YardView>();
        }

        private void QLiSan_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < sans.Count; i++)
            {
                flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
                flowLayoutPanel1.AutoScroll = true;
                flowLayoutPanel1.WrapContents = false;

                flowLayoutPanel1.Controls.Clear();

                foreach (var san in sans)
                {
                    var view = new YardView(san);
                    sanControls.Add(view);
                    flowLayoutPanel1.Controls.Add(view);
                }
            }
        }
    }
}
