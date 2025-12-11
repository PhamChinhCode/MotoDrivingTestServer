using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using THI_HANG_A1.Helpers;
using Color = System.Drawing.Color;
using Image = System.Drawing.Image;

namespace THI_HANG_A1
{
    public partial class FormInKetQua : Form
    {
        private readonly string cnn = THI_HANG_A1.Properties.Settings.Default.Conn;
        private int _sessionId;
        private DataTable _thongTin;
        private DataTable _chiTietLoi;
        private List<Image> _imgs;


        public FormInKetQua(int sessionId)
        {
            InitializeComponent();
            _sessionId = sessionId;
            LoadData();
        }
        private void LoadData()
        {
            _thongTin = LoadThongTinThiSinhPrint(_sessionId);
            _chiTietLoi = LoadChiTietLoi(_sessionId);
            _imgs = LoadAllImages(_sessionId);

            if (_thongTin.Rows.Count == 0)
            {
                MessageBox.Show("Không tìm thấy dữ liệu thí sinh!", "Lỗi");
                this.Close();
                return;
            }
            BindInfo();
            GeneratePictureBoxes();
            BindChiTietLoi();
        }
        private void GeneratePictureBoxes()
        {
            flowImages.Controls.Clear();
            foreach (var img in _imgs)
            {
                PictureBox pb = new PictureBox();
                pb.Width = 96;
                pb.Height = 130;
                pb.BackColor = Color.White;
                pb.SizeMode = PictureBoxSizeMode.StretchImage;

                // Cách nhau 40px (40px margin bên phải)
                pb.Margin = new Padding(0, 0, 40, 0);

                pb.Image = img;

                flowImages.Controls.Add(pb);
            }
            CenterFlowImages();
        }

        private void CenterFlowImages()
        {
            if (flowImages.Controls.Count == 0) return;

            int totalWidth = 0;

            foreach (Control c in flowImages.Controls)
                totalWidth += c.Width + c.Margin.Left + c.Margin.Right;

            // khoảng còn dư để căn giữa
            int space = flowImages.Width - totalWidth;

            if (space > 0)
                flowImages.Padding = new Padding(space / 2, 0, 0, 0);
            else
                flowImages.Padding = new Padding(0);
        }


        private void BindInfo()
        {
            DataRow r = _thongTin.Rows[0];

            // Thông tin thí sinh
            lblHoTen.Text = r["HoTen"].ToString();
            lbNgaySinh.Text = Convert.ToDateTime(r["NgaySinh"]).ToString("dd/MM/yyyy");
            lblSoCCCD.Text = r["SoCCCD"].ToString();
            lblLanThi.Text = r["SoLanThi"].ToString();
            lblHang.Text = r["HangGPLX"].ToString();
            lblSBD.Text = r["SBD"].ToString();

            // Khóa sát hạch
            lblKhoaSH.Text = r["TenKSH"].ToString();
            lblNgaySH.Text = Convert.ToDateTime(r["NgayThi"]).ToString("dd/MM/yyyy");

            // Thời gian
            lblThoiGianBD.Text = Convert.ToDateTime(r["StartTime"]).ToString("HH:mm:ss");
            lblThoiGianKT.Text = Convert.ToDateTime(r["EndTime"]).ToString("HH:mm:ss");
            lblTongThoiGian.Text = r["Duration_mmss"].ToString();   // mm:ss

            // Số xe (DeviceID)
            lblSoXe.Text = r["DeviceID"].ToString();

            // Kết quả (Điểm)
            int mark = Convert.ToInt32(r["Mark"]);

            if (mark >= 80)
            {
                lblDat.Text = "✔";
                lblKhongDat.Text = "";
            }
            else
            {
                lblDat.Text = "";
                lblKhongDat.Text = "✔";
            }
            lblSoDiem.Text = mark.ToString();
        }
        private List<Image> LoadAllImages(int sessionId)
        {
            List<Image> list = new List<Image>();

            // 1) Ảnh chân dung
            if (_thongTin.Rows.Count > 0 && _thongTin.Rows[0]["AnhChanDung"] != DBNull.Value)
            {
                string pathChanDung = _thongTin.Rows[0]["AnhChanDung"].ToString();

                if (!string.IsNullOrWhiteSpace(pathChanDung) && File.Exists(pathChanDung))
                {
                    try
                    {
                        list.Add(Image.FromFile(pathChanDung));
                    }
                    catch { }
                }
            }

            // 2) Ảnh lỗi từ ChiTietLoi
            string sql = @"
                SELECT ImagePath 
                FROM ChiTietLoi 
                WHERE SessionID = @SID AND ImagePath IS NOT NULL
                ORDER BY ThoiGian ASC
            ";

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@SID", sessionId);

                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            string path = rd["ImagePath"].ToString();
                            if (File.Exists(path))
                                list.Add(Image.FromFile(path));
                        }
                    }
                }
            }

            return list;
        }


        private void BindChiTietLoi()
        {
            DataTable dt = _chiTietLoi;

            tblChiTietLoi.SuspendLayout();
            tblChiTietLoi.Controls.Clear();
            tblChiTietLoi.RowStyles.Clear();
            tblChiTietLoi.RowCount = 0;

            // ============================
            // TẠO HEADER
            // ============================
            AddHeaderCell("Thời gian");
            AddHeaderCell("Chi tiết lỗi");
            AddHeaderCell("Điểm trừ");

            tblChiTietLoi.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tblChiTietLoi.RowCount++;
            foreach (DataRow r in dt.Rows)
            {
                AddDataCell(Convert.ToDateTime(r["ThoiGian"]).ToString("HH:mm:ss"));
                AddDataCell(r["ChiTiet"].ToString());
                AddDataCell(r["DiemTru"].ToString());

                tblChiTietLoi.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                tblChiTietLoi.RowCount++;
            }

            tblChiTietLoi.ResumeLayout();
        }
        private void AddHeaderCell(string text)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold);
            lbl.Padding = new Padding(4);

            lbl.AutoSize = false;
            lbl.Dock = DockStyle.Fill;
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            lbl.AutoEllipsis = true;

            int col = tblChiTietLoi.Controls.Count % tblChiTietLoi.ColumnCount;
            tblChiTietLoi.Controls.Add(lbl, col, tblChiTietLoi.RowCount);
        }
        private void AddDataCell(string text)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular);
            lbl.Padding = new Padding(4);

            lbl.AutoSize = false;
            lbl.Dock = DockStyle.Fill;
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            lbl.AutoEllipsis = true;

            int col = tblChiTietLoi.Controls.Count % tblChiTietLoi.ColumnCount;
            tblChiTietLoi.Controls.Add(lbl, col, tblChiTietLoi.RowCount);
        }

        private DataTable LoadThongTinThiSinhPrint(int? sessionId = null)
        {
            DataTable dt = new DataTable();

            string sql = @"
                SELECT 
                    TS.SBD,
                    TS.HoDem + ' ' + TS.Ten AS HoTen,
                    TS.HangGPLX,
                    TS.NgaySinh,
	                TS.AnhChanDung,
                    TS.SoCCCD,
                    KSH.TenKSH,
                    KSH.NgayThi,

                    S.ID AS SessionID,
                    S.DeviceID,
                    S.StartTime,
                    DATEADD(SECOND, S.Duration, S.StartTime) AS EndTime,

                    RIGHT(CONVERT(varchar, DATEADD(SECOND, S.Duration, 0), 108), 5) AS Duration_mmss,
                    S.Time AS SoLanThi,
                    S.Mark,
                    CASE WHEN S.Mark >= 80 THEN 1 ELSE 0 END AS KetQua

                FROM Sessions S
                INNER JOIN ThiSinhSH TS ON TS.SBD = S.SBD
                LEFT JOIN KySatHach KSH ON KSH.KySatHach = TS.KySatHach
                WHERE S.IsFinish = 1
            ";

            // Nếu có truyền SessionID → thêm điều kiện WHERE
            if (sessionId.HasValue)
                sql += " AND S.ID = @SessionID";

            sql += " ORDER BY S.StartTime DESC";

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    if (sessionId.HasValue)
                        cmd.Parameters.AddWithValue("@SessionID", sessionId.Value);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }

            return dt;
        }

        private DataTable LoadChiTietLoi(int sessionId)
        {
            DataTable dt = new DataTable();

            string sql = @"
                SELECT 
                    CT.ThoiGian,
                    CT.ChiTiet,
                    CT.DiemTru
                FROM ChiTietLoi CT
                WHERE CT.SessionID = @SessionID
                    AND CT.DiemTru > 0 
                ORDER BY CT.ThoiGian ASC;
    ";

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@SessionID", sessionId);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }

            return dt;
        }

        private void btnInPDF_Click(object sender, EventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog
            {
                Filter = "PDF File|*.pdf",
                FileName = "BienBanSatHach.pdf"
            };

            if (save.ShowDialog() == DialogResult.OK)
            {
                Bitmap img = RenderFlowMainWithoutButton();
                ExportBitmapToPDF(img, save.FileName);

                MessageBox.Show("Xuất PDF thành công!",
                                "Thành công",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
        }
        private Bitmap RenderFlowMainWithoutButton()
        {
            int width = flowMain.Width;
            int height = 0;

            foreach (Control ctrl in flowMain.Controls)
            {
                if (ctrl.Name == "panelInPDF") continue;
                height += ctrl.Height;
            }

            Bitmap bmp = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);

                int y = 0;

                foreach (Control ctrl in flowMain.Controls)
                {
                    if (ctrl == btnInPDF) continue;

                    using (Bitmap cb = new Bitmap(ctrl.Width, ctrl.Height))
                    {
                        ctrl.DrawToBitmap(cb, new Rectangle(0, 0, ctrl.Width, ctrl.Height));
                        g.DrawImage(cb, new Point(0, y));
                    }

                    y += ctrl.Height;
                }
            }

            return bmp;
        }


        public void ExportBitmapToPDF(Bitmap bmp, string pdfPath)
        {
            var imgBytes = (byte[])(new ImageConverter().ConvertTo(bmp, typeof(byte[])));

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Content().Image(imgBytes);
                });
            })
            .GeneratePdf(pdfPath);
        }

        private string FormatDate(object o) => Convert.ToDateTime(o).ToString("dd/MM/yyyy");
        private string FormatTime(object o) => Convert.ToDateTime(o).ToString("HH:mm:ss");

        private void FormInKetQua_Load(object sender, EventArgs e)
        {
            FixTableChiTietLoi();
        }
        private void FixTableChiTietLoi()
        {
            tblChiTietLoi.AutoSize = true;
            tblChiTietLoi.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tblChiTietLoi.Dock = DockStyle.Top;

            flowMain.HorizontalScroll.Maximum = 0;
            flowMain.AutoScroll = true;
            flowMain.HorizontalScroll.Maximum = 0;
            flowMain.HorizontalScroll.Visible = false;

            flowMain.VerticalScroll.Maximum = 0;
            flowMain.VerticalScroll.Visible = false;

            flowMain.Scroll += (s, e) =>
            {
                flowMain.HorizontalScroll.Visible = false;
                flowMain.VerticalScroll.Visible = false;
            };
        }

    }
}
