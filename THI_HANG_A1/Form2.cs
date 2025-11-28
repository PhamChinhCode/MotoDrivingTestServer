using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using THI_HANG_A1.Helpers;
using THI_HANG_A1.Models;

namespace THI_HANG_A1
{
    public partial class Capxe : Form
    {
        private readonly List<Moto> danhSachXe;

        // Trả về Moto được chọn
        public Moto XeDuocChon { get; private set; }
        private Button _buttonDangChon = null;

        public Capxe(List<Moto> xes, int Sodb, string hang)
        {
            InitializeComponent();

            danhSachXe = xes;

            lblHanglx.Text = hang;
            lblSobd.Text = Sodb.ToString();
        }

        private void Capxe_Load(object sender, EventArgs e)
        {
            GenerateXeButtons();
            CapNhatScrollPanel();
        }
        private void CapNhatScrollPanel()
        {
            // Tắt scroll trước để panel không làm gì bậy
            panelXe.AutoScroll = false;
            panelXe.HorizontalScroll.Visible = false;
            panelXe.HorizontalScroll.Enabled = false;
            panelXe.VerticalScroll.Visible = false;
            panelXe.VerticalScroll.Enabled = false;

            int content = tableXe.Height;
            int panel = panelXe.ClientSize.Height;

            // Nếu nội dung cao hơn panel -> bật scroll
            if (content > panel)
            {
                panelXe.AutoScroll = true;
                panelXe.AutoScrollMinSize = new Size(0, content - panel);  // chuẩn
            }
            else
            {
                // Không scroll
                panelXe.AutoScroll = false;
                panelXe.AutoScrollMinSize = new Size(0, 0);
            }
        }


        private void GenerateXeButtons()
        {
            tableXe.SuspendLayout();
            tableXe.Controls.Clear();

            int soCot = tableXe.ColumnCount = 5;
            int soXe = danhSachXe.Count;

            tableXe.ColumnStyles.Clear();
            for (int i = 0; i < soCot; i++)
                tableXe.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / soCot));

            int col = 0, row = 0;

            tableXe.RowCount = 1;
            tableXe.RowStyles.Clear();
            tableXe.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

            foreach (var moto in danhSachXe)
            {
                Button btn = new Button();
                btn.Text = moto.Name;

                btn.BackColor = MotoHelper.GetMotoColor(moto);
                btn.Dock = DockStyle.Fill;
                btn.Margin = new Padding(5);

                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Color.Gray;

                btn.Tag = moto;
                btn.Click += BtnXe_Click;

                tableXe.Controls.Add(btn, col, row);

                col++;

                if (col >= soCot)
                {
                    col = 0;
                    row++;
                    tableXe.RowCount++;
                    tableXe.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
                }
            }

            // ⭐ CHỈ set Height nếu cần xuống hàng (nhiều hơn 1 hàng)
            if (soXe > soCot)
            {
                tableXe.Height = tableXe.RowCount * 50;
            }
            else
            {
                tableXe.Height = 50;  // mặc định 1 hàng
            }

            tableXe.ResumeLayout();
        }


        private void BtnXe_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            Moto moto = btn.Tag as Moto;

            if (moto.Status != 0xc1)
            {
                MessageBox.Show($"Xe {moto.Name} đang không sẵn sàng.\nKhông thể cấp xe!",
                                "Không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            XeDuocChon = moto;

            // --- RESET BORDER CŨ ---
            if (_buttonDangChon != null)
            {
                _buttonDangChon.FlatAppearance.BorderSize = 1;
                _buttonDangChon.FlatAppearance.BorderColor = Color.Gray;
                _buttonDangChon.FlatStyle = FlatStyle.Flat;
            }

            // --- SET BORDER NÚT ĐƯỢC CHỌN ---
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 3;
            btn.FlatAppearance.BorderColor = Color.DarkBlue;

            _buttonDangChon = btn;
        }

        private void btnboqua_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btndongy_Click(object sender, EventArgs e)
        {
            // Chỉ đóng nếu đã chọn xe
            if (XeDuocChon == null)
            {
                MessageBox.Show("Bạn chưa chọn xe!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
