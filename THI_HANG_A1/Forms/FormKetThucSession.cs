using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace THI_HANG_A1.Forms
{
    public enum LyDoKetThuc
    {
        CanBoDung = 1,
        LoiKyThuat = 2,
        ThiSinhBoThi = 3
    }
    public static class LyDoKetThucMapper
    {
        public static string Ten(this LyDoKetThuc lyDo)
        {
            switch (lyDo)
            {
                case LyDoKetThuc.CanBoDung:
                    return "Giám khảo kết thúc";

                case LyDoKetThuc.LoiKyThuat:
                    return "Giám khảo kết thúc do lỗi kỹ thuật";

                case LyDoKetThuc.ThiSinhBoThi:
                    return "Giám khảo kết thúc do thí sinh bỏ thi";

                default:
                    return "Giám khảo kết thúc";
            }
        }
    }

    public partial class FormKetThucSession : Form
    {
        public LyDoKetThuc LyDoDuocChon { get; private set; }
        public FormKetThucSession()
        {
            InitializeComponent();
            rbCanBoDung.Checked = true; // mặc định
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (rbCanBoDung.Checked)
                LyDoDuocChon = LyDoKetThuc.CanBoDung;
            else if (rbLoiKyThuat.Checked)
                LyDoDuocChon = LyDoKetThuc.LoiKyThuat;
            else if (rbBoThi.Checked)
                LyDoDuocChon = LyDoKetThuc.ThiSinhBoThi;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
