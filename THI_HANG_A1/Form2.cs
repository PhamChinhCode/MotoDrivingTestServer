using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace THI_HANG_A1
{
    public partial class Capxe : Form
    {
        public int i;
        public Capxe()
        {
            InitializeComponent();

        }
        public Capxe(int Sodb, string hang)
        {
            InitializeComponent();
            lblHanglx.Text = hang;
            lblSobd.Text = Sodb.ToString();
        }

        private void Capxe_Load(object sender, EventArgs e)
        {

        }

        private void btnboqua_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btndongy_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnxe1_Click(object sender, EventArgs e)
        {
            i = 1;
        }

        private void btnxe2_Click(object sender, EventArgs e)
        {
            i = 2;
        }

        private void btnxe3_Click(object sender, EventArgs e)
        {
            i = 3;
        }

        private void btnxe4_Click(object sender, EventArgs e)
        {
            i = 4;
        }

        private void btnxe5_Click(object sender, EventArgs e)
        {
            i = 5;
        }
    }
}
