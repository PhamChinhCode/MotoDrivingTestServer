using System;
using System.Collections.Generic;
using System.Windows.Forms;
using THI_HANG_A1.Models;

namespace THI_HANG_A1.Forms
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
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.RowCount = 0;
            tableLayoutPanel1.AutoScroll = true;
            tableLayoutPanel1.ColumnStyles.Clear();
            tableLayoutPanel1.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F)
            );
            tableLayoutPanel1.RowStyles.Clear();
            tableLayoutPanel1.Controls.Clear();

            label1.Text = xes.Count.ToString();
            for (int i = 0; i < xes.Count; i++)
            {
                xecontrol.Add(new MotoView(xes[i]));

                xecontrol[i].Margin = new Padding(3);
                xecontrol[i].Dock = DockStyle.Fill;
                tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                tableLayoutPanel1.Controls.Add(xecontrol[i], 0, i);
            }
            //tableLayoutPanel1.Controls.Add(xecontrol)
            // this is stable version 1



        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void chỉnhSửaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form3 formQuanLy = new Form3();
            formQuanLy.ShowDialog();
        }
    }
}
