using ImageMagick;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using THI_HANG_A1.Managers;
using THI_HANG_A1.Models;

namespace THI_HANG_A1
{
    // Form1 GIỜ ĐÂY CHỈ LÀM NHIỆM VỤ GIAO DIỆN VÀ ĐIỀU PHỐI
    public partial class Form1 : Form
    {
        // 1. Khai báo các "Quản lý"
        private readonly ExamDataManager examManager;
        private readonly SerialManager serialManager;
        private readonly AudioManager audioManager;

        // SQL
        private readonly string cnn = THI_HANG_A1.Properties.Settings.Default.Conn;
        private SqlDataAdapter da;
        private DataTable dt;


        public Form1()
        {
            InitializeComponent();
            //dgvDangThi.DataSource = null;
            //dgvDangThi.Visible = false;
            GridThi();
            dgvThi.AutoGenerateColumns = false;
            dgvThi.DataSource = null;

            // 2. Khởi tạo các manager
            audioManager = new AudioManager();
            serialManager = new SerialManager();
            examManager = new ExamDataManager(serialManager, audioManager);

            // 3. Kết nối các sự kiện từ Manager về Form1
            serialManager.OnLogMessage += AppendLog;
            audioManager.OnLogMessage += AppendLog;
            examManager.OnLogMessage += AppendLog;
            examManager.OnMessageBoxShow += (message, caption) =>
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);

            KhoiTaoGiaoDienVaDuLieu();           // chỉ gọi 1 lần
            serialManager.KetNoiSerial("COM1", 115200); // THAY CỔNG COM NẾU CẦN
        }

        /// <summary>
        /// Bind các DataGridView với BindingList trong ExamManager
        /// </summary>
        private void KhoiTaoGiaoDienVaDuLieu()
        {
            dgvchitietloi.AutoGenerateColumns = true; // Bạn đã tạo tay

            dgvKetQuaChung.AutoGenerateColumns = false;
            dgvNhatKyLoi.AutoGenerateColumns = false;

            dgvchitietloi.DataSource = examManager.DanhSachChuanBiThi;
            dgvThi.DataSource = examManager.DanhSachDangThi;
            dgvKetQuaChung.DataSource = examManager.DanhSachKetQuaChung;
            dgvNhatKyLoi.DataSource = examManager.DanhSachLoiViPham;

            this.dgvNhatKyLoi.CellFormatting += dgvNhatKyLoi_CellFormatting;
            CapNhatDanhSachXeRanhUI();
            examManager.OnDataChanged += (s, e) => CapNhatDanhSachXeRanhUI();
        }
        /// <summary>
        /// Cập nhật ComboBox xe rảnh từ dữ liệu trong ExamManager
        /// </summary>
        private void CapNhatDanhSachXeRanhUI()
        {
        }

        #region === LOG GIAO DIỆN ===

        private void AppendLog(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(AppendLog), message);
            }
            else
            {
                if (this.txtSerialLog == null) return;
                if (txtSerialLog.Lines.Length > 100)
                {
                    var newLines = txtSerialLog.Lines.Skip(10).ToList();
                    txtSerialLog.Lines = newLines.ToArray();
                }
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                txtSerialLog.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
            }
        }

        #endregion

        #region === NÚT ĐIỀU KHIỂN, LỖI ===

        /// <summary>
        /// Giao xe cho thí sinh được chọn ở bảng CHUẨN BỊ THI
        /// </summary>

        /// <summary>
        /// Bắt đầu lượt thi cho thí sinh đang chọn trong bảng ĐANG THI
        /// </summary>
        private void btnBatDau_Click(object sender, EventArgs e)
        {
            if (dgvThi.CurrentRow?.DataBoundItem is ThiSinh ts)
            {
                examManager.BatDauLuotThi(ts);

                if (!timerCapNhatThoiGian.Enabled)
                    timerCapNhatThoiGian.Start();
            }
            else
            {
                MessageBox.Show("Vui lòng CHỌN thí sinh trong bảng 'ĐANG THI' để bắt đầu.",
                    "Chưa chọn thí sinh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnKetThucLuot_Click(object sender, EventArgs e)
        {
            if (dgvThi.CurrentRow?.DataBoundItem is ThiSinh ts)
            {
                var xacNhan = MessageBox.Show(
                    $"Xác nhận kết thúc lượt thi của {ts.HoTen} với điểm số là {ts.DiemTongHop}?",
                    "Xác nhận kết thúc", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (xacNhan == DialogResult.Yes)
                {
                    _ = examManager.KetThucLuotThiThuCong(ts);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn thí sinh cần kết thúc bài thi.",
                    "Chưa chọn thí sinh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Các nút lỗi (nhẹ/nặng) – GIỮ NGUYÊN, chỉ sửa grid thành dgvdangthii

        private void btnLoiChamVach_Click(object sender, EventArgs e)
        {
            if (dgvThi.CurrentRow?.DataBoundItem is ThiSinh ts)
                examManager.GhiNhanLoiThuCong(ts, 5, $"Chạm vạch lần {ts.LoiChamVach + 1}", "ChamVach", t => t.LoiChamVach++);
            else
                MessageBox.Show("Vui lòng chọn thí sinh đang thi.", "Chưa chọn thí sinh");
        }

        private void btnLoiChetMay_Click(object sender, EventArgs e)
        {
            if (dgvThi.CurrentRow?.DataBoundItem is ThiSinh ts)
                examManager.GhiNhanLoiThuCong(ts, 5, $"Chết máy lần {ts.LoiChetMay + 1}", "ChetMay", t => t.LoiChetMay++);
            else
                MessageBox.Show("Vui lòng chọn thí sinh đang thi.", "Chưa chọn thí sinh");
        }

        private void btnLoiKhongXiNhan_Click(object sender, EventArgs e)
        {
            if (dgvThi.CurrentRow?.DataBoundItem is ThiSinh ts)
                examManager.GhiNhanLoiThuCong(ts, 5, $"Không xi nhan lần {ts.LoiKhongXiNhan + 1}", "KhongXiNhan", t => t.LoiKhongXiNhan++);
            else
                MessageBox.Show("Vui lòng chọn thí sinh đang thi.", "Chưa chọn thí sinh");
        }

        private async void btnLoiNgaDo_Click(object sender, EventArgs e)
        {
            if (dgvThi.CurrentRow?.DataBoundItem is ThiSinh ts)
            {
                var xacNhan = MessageBox.Show(
                    "Xác nhận thí sinh bị lỗi 'Ngã/đổ xe' và bị loại trực tiếp?",
                    "Xác nhận lỗi loại", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (xacNhan == DialogResult.Yes)
                {
                    ts.LoiNgaDo = 1;
                    await examManager.LoaiTrucTiep(ts, ts.DiemTongHop, "Ngã/đổ xe (Loại trực tiếp)", "DoXe");
                }
            }
            else
                MessageBox.Show("Vui lòng chọn thí sinh đang thi.", "Chưa chọn thí sinh");
        }

        private async void btnLoiSaiHinh_Click(object sender, EventArgs e)
        {
            if (dgvThi.CurrentRow?.DataBoundItem is ThiSinh ts)
            {
                var xacNhan = MessageBox.Show(
                    "Xác nhận thí sinh bị lỗi 'Chạy sai hình' và bị loại trực tiếp?",
                    "Xác nhận lỗi loại", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (xacNhan == DialogResult.Yes)
                {
                    ts.LoiChaySaiHinh = 1;
                    await examManager.LoaiTrucTiep(ts, ts.DiemTongHop, "Chạy sai hình (Loại trực tiếp)", "SaiHinh");
                }
            }
            else
                MessageBox.Show("Vui lòng chọn thí sinh đang thi.", "Chưa chọn thí sinh");
        }

        private async void btnLoiQuaTocDo_Click(object sender, EventArgs e)
        {
            if (dgvThi.CurrentRow?.DataBoundItem is ThiSinh ts)
            {
                var xacNhan = MessageBox.Show(
                    "Xác nhận thí sinh bị lỗi 'Vượt quá tốc độ' và bị loại trực tiếp?",
                    "Xác nhận lỗi loại", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (xacNhan == DialogResult.Yes)
                {
                    ts.LoiQuaTocDo = 1;
                    await examManager.LoaiTrucTiep(ts, ts.DiemTongHop, "Vượt quá tốc độ (Loại trực tiếp)", "VuotTocDo");
                }
            }
            else
                MessageBox.Show("Vui lòng chọn thí sinh đang thi.", "Chưa chọn thí sinh");
        }

        private void btnQuaVongSo8_Click(object sender, EventArgs e)
        {
            if (dgvThi.CurrentRow?.DataBoundItem is ThiSinh ts)
                examManager.QuaVongSo8(ts);
            else
                MessageBox.Show("Vui lòng chọn thí sinh trong bảng 'ĐANG THI'.",
                    "Chưa chọn thí sinh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        #endregion

        #region === TIMER CẬP NHẬT THỜI GIAN ===

        private void timerCapNhatThoiGian_Tick(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvThi.Rows)
            {
                if (row.DataBoundItem is ThiSinh ts)
                {
                    var cell = row.Cells["colThoiGian"];
                    if (cell == null) continue;

                    if (ts.ThoiGianBatDau != DateTime.MinValue)
                    {
                        TimeSpan thoiGianTroiQua = DateTime.Now - ts.ThoiGianBatDau;
                        cell.Value = thoiGianTroiQua.ToString(@"mm\:ss");
                    }
                    else
                    {
                        cell.Value = "--:--";
                    }
                }
            }
        }

        #endregion

        #region === GRID SỰ KIỆN PHỤ ===

        private void dgvChuanbi_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                // double click vào là giao xe luôn
            }
        }

        private void dgvNhatKyLoi_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 1) return;

            if (dgvNhatKyLoi.Rows[e.RowIndex].DataBoundItem is LoiViPham currentRow &&
                dgvNhatKyLoi.Rows[e.RowIndex - 1].DataBoundItem is LoiViPham previousRow)
            {
                if (currentRow.SBD == previousRow.SBD)
                {
                    string columnName = dgvNhatKyLoi.Columns[e.ColumnIndex].Name;

                    if (columnName == "colLoi_HoTen" || columnName == "colLoi_SBD" ||
                        columnName == "colLoi_Xe" || columnName == "colLoi_Hang" ||
                        columnName == "colLoi_DiemTru")
                    {
                        e.Value = string.Empty;
                        e.FormattingApplied = true;
                    }
                }
            }
        }

        // Hàm rỗng cho designer
        private void dgvDangThi_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            x = new ThiSinhXml();
            int i = e.RowIndex;
            x.SoBaoDanh = Int32.Parse(dgv.Rows[i].Cells[0].Value?.ToString());
            x.Hodem = dgv.Rows[i].Cells[1].Value?.ToString();
            x.Ten = dgv.Rows[i].Cells[2].Value?.ToString();

        }
        private void textBox2_TextChanged(object sender, EventArgs e) { }
        private void textBox1_TextChanged(object sender, EventArgs e) { }
        private void txtSerialLog_TextChanged(object sender, EventArgs e) { }

        #endregion

        #region === LOAD FORM & SQL ===
        private void LoadComboboxKySatHach()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(cnn))
                {
                    conn.Open();
                    // Lấy Mã và Tên kỳ sát hạch
                    string query = "SELECT KySatHach, TenKSH FROM KySatHach ORDER BY NgayThi DESC";

                    SqlDataAdapter daCombo = new SqlDataAdapter(query, conn);
                    DataTable dtCombo = new DataTable();
                    daCombo.Fill(dtCombo);

                    // Gán dữ liệu vào ComboBox
                    comboBox1.DataSource = dtCombo;
                    comboBox1.DisplayMember = "TenKSH";     // Hiển thị tên cho dễ nhìn
                    comboBox1.ValueMember = "KySatHach";    // Giá trị ngầm là Mã (để dùng lọc SQL)

                    // Mặc định không chọn cái nào (để người dùng tự chọn) hoặc chọn cái đầu tiên
                    if (dtCombo.Rows.Count > 0)
                        comboBox1.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách kỳ thi: " + ex.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // TODO: This line of code loads data into the 'mCDV2A1DataSet2.DBKySatHach' table. You can move, or remove it, as needed.
            this.dBKySatHachTableAdapter.Fill(this.mCDV2A1DataSet2.DBKySatHach);
            // GIỮ NGUYÊN ĐOẠN NÀY NHƯ BẠN YÊU CẦU
            //this.examineesTableAdapter.Fill(this.mCDV2A1DataSet.Examinees);
            LoadComboboxKySatHach();
            Loaf();                     // đọc từ SQL vào dgv + nạp vào ExamDataManager
            dgvThi.AutoGenerateColumns = true;
            dgvThi.DataSource = examManager.DanhSachDangThi;
            if (dgvThi.Columns["colThoiGian"] == null)
            {
                dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
                {
                    Name = "colThoiGian",
                    HeaderText = "Thời gian",
                    ReadOnly = true
                });
            }
        }

        /// <summary>
        /// Chuyển dữ liệu từ dt (SQL) -> List&lt;ThiSinh&gt; -> ExamDataManager
        /// </summary>
        //private void NapDanhSachThiSinhTuSQLVaoExamManager()
        //{
        //    if (dt == null || dt.Rows.Count == 0)
        //        return;

        //    var danhSach = new List<ThiSinh>();

        //    foreach (DataRow row in dt.Rows)
        //    {
        //        var ts = new ThiSinh
        //        {
        //            SBD = row["IDCardNo"]?.ToString(),  // dùng IDCardNo làm SBD
        //            HoTen = row["Name"]?.ToString(),
        //            KetquaLT = row["Traloidung"]?.ToString(),
        //            CCCD = row["IDCardNo"]?.ToString()
        //        };


        //        var dobRaw = row["DateOfBirth"]?.ToString()?.Trim();
        //        DateTime ns;

        //        string[] formats =
        //        {
        //            "dd/MM/yyyy",
        //            "d/M/yyyy",
        //            "dd-MM-yyyy",
        //            "d-M-yyyy",
        //            "dd.MM.yyyy",
        //            "d.M.yyyy",

        //        };

        //        if (!string.IsNullOrEmpty(dobRaw) &&
        //            DateTime.TryParseExact(
        //                dobRaw,
        //                formats,
        //                CultureInfo.InvariantCulture,
        //                DateTimeStyles.None,
        //                out ns))
        //        {
        //            ts.NgaySinh = ns;
        //        }
        //        else
        //        {

        //            if (DateTime.TryParse(dobRaw, new CultureInfo("vi-VN"), DateTimeStyles.None, out ns))
        //                ts.NgaySinh = ns;
        //        }

        //        //  THÊM THÍ SINH VÀO DANH SÁCH
        //        danhSach.Add(ts);
        //    }

        //    //  CHỈ GỌI NẠP 1 LẦN SAU KHI ĐÃ LẤY ĐỦ DANH SÁCH
        //    examManager.NapDuLieuMoi(danhSach);
        //}

        /// <summary>
        /// Đọc danh sách thí sinh thi lý thuyết từ SQL, gán vào dgv
        /// và nạp vào ExamDataManager
        /// </summary>
        public void Loaf()
        {
            //try
            //{
            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();
                string query = "SELECT SBD,Hodem,Ten,NgaySinh,SoCCCD,HangGPLX,QUOCTICH,AnhChanDung,MaDangKy,NoiCT,KySatHach FROM ThiSinhSH order by SBD";

                da = new SqlDataAdapter(query, conn);
                dt = new DataTable();
                da.Fill(dt);

                dgv.DataSource = dt;
            }

            dgv.ReadOnly = false;
            dgv.AllowUserToAddRows = true;
            dgv.AllowUserToDeleteRows = true;
            dgv.Columns[0].Width = 60;

            // Sau khi dt đã có dữ liệu -> nạp vào ExamManager
            //NapDanhSachThiSinhTuSQLVaoExamManager();
            //}
            //catch (Exception ex)
            // {
            //    MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
            // }
        }


        #endregion

        /// <summary>
        /// Load danh sách thí sinh tu XML
        /// </summary>
        /// <param name="xmlPath">Đường dẫn tới file XML</param>
        /// <returns> Tra ve 1 bang du lieu danh sach thi sinh</returns>
        public List<ThiSinhXml> LoadThiSinh(string xmlPath)
        {
            XDocument doc = XDocument.Load(xmlPath);

            var danhSach = doc.Root
                .Elements("THI_SINH")
                .Select(x => new ThiSinhXml
                {
                    MaDangKy = (string)x.Element("MA_DANG_KY"),
                    HoTen = (string)x.Element("HO_TEN"),
                    NgaySinh = (string)x.Element("NGAY_SINH"),
                    SoCMT = (string)x.Element("SO_CMT"),
                    AnhChanDung = (string)x.Element("ANH_CHAN_DUNG"),
                    HangGPLX = (string)x.Element("HANG_GPLX"),
                    KySatHach = (string)x.Element("KY_SAT_HACH"),
                    TenKySatHach = (string)x.Element("TEN_KY_SAT_HACH"),
                    SoBaoDanh = (int?)x.Element("SO_BAO_DANH") ?? 0,

                    // Kết quả
                    Diem_L = (int?)x.Element("KETQUA_SATHACH_L")?.Element("DIEM_DAT_DUOC") ?? -1,
                    DiemChuan_L = (int?)x.Element("KETQUA_SATHACH_L")?.Element("DIEM_CHUAN") ?? -1,

                    Diem_M = (int?)x.Element("KETQUA_SATHACH_M")?.Element("DIEM_DAT_DUOC") ?? -1,
                    Diem_H = (int?)x.Element("KETQUA_SATHACH_H")?.Element("DIEM_DAT_DUOC") ?? -1,
                    Diem_D = (int?)x.Element("KETQUA_SATHACH_D")?.Element("DIEM_DAT_DUOC") ?? -1
                })
                .ToList();

            return danhSach;
        }
        private void InputXML_Click(object sender, EventArgs e)
        {
            string filePath = "";
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                dlg.Title = "Chọn file thí sinh đuôi XML";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    filePath = dlg.FileName;
                }
                else return;
            }

            List<ThiSinhXml> thiSinhXmls = new List<ThiSinhXml>();
            thiSinhXmls = LoadThiSinh(filePath);
            //txtTmp.Text = ""; // Reset log

            // Vòng lặp lưu ảnh ra ổ D (Code cũ của bạn)
            foreach (ThiSinhXml r in thiSinhXmls)
            {
                // txtTmp.Text += r.SoBaoDanh + "    " + r.HoTen + "    " + r.NgaySinh + "\r\n";

                if (!Directory.Exists("D:\\" + r.KySatHach))
                {
                    Directory.CreateDirectory("D:\\" + r.KySatHach);
                }
                string file = "D:\\" + r.KySatHach + $"\\anh_{r.SoBaoDanh}.jpg";
                BitmapImage img = AnhImage(r.AnhChanDung);
                SaveBitmapImage(img, file);
            }

            // 1. Lưu Kỳ sát hạch vào bảng sát hạch
            // 2. Lưu DS thí sinh vào bảng ThiSinhSH
            // 3. Load danh sách thí sinh thi theo 1 kỳ sát hạch vào bảng bên trái 
            // 4. Ds chuẩn bị có 5 thí sinh.

            // Gọi hàm xử lý trọn gói 4 đầu việc trên:
            XuLyLuuVaHienThi(thiSinhXmls);

            // =======================================================================
        }
        public void SaveBitmapImage(BitmapImage image, string filePath)
        {
            if (image == null)
            {
                MessageBox.Show("Ảnh rỗng, không thể lưu!");
                return;
            }

            BitmapEncoder encoder;

            // Chọn encoder theo đuôi file
            if (filePath.EndsWith(".jpg") || filePath.EndsWith(".jpeg"))
                encoder = new JpegBitmapEncoder();
            else
                encoder = new PngBitmapEncoder();

            encoder.Frames.Add(BitmapFrame.Create(image));

            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fs);
            }
        }
        public BitmapImage AnhImage(string AnhBase64)
        {
            if (string.IsNullOrWhiteSpace(AnhBase64))
                return null;

            try
            {
                // Xóa các ký tự xuống dòng
                string cleanBase64 = AnhBase64
                    .Replace("\n", "")
                    .Replace("\r", "")
                    .Replace("\t", "")
                    .Replace(" ", "");

                byte[] bytes = Convert.FromBase64String(cleanBase64);

                // Đọc JPEG2000 bằng Magick.NET
                using (var ms = new MemoryStream(bytes))
                using (var img = new MagickImage(ms)) // HỖ TRỢ JP2
                {
                    img.Format = MagickFormat.Png; // Chuyển sang PNG để WPF đọc được

                    using (var ms2 = new MemoryStream())
                    {
                        img.Write(ms2);   // ghi PNG vào stream
                        ms2.Position = 0;

                        // Tạo BitmapImage cho WPF
                        BitmapImage bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = ms2;
                        bmp.EndInit();
                        bmp.Freeze(); // fix lỗi multi-thread

                        return bmp;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi giải mã JPEG2000: " + ex.Message);
                return null;
            }
        }
        // --- HÀM XỬ LÝ CHÍNH CHO CÁC GHI CHÚ CỦA BẠN ---
        // --- HÀM XỬ LÝ CHÍNH: LƯU SQL & HIỂN THỊ ---
        private void XuLyLuuVaHienThi(List<ThiSinhXml> listXml)
        {
            if (listXml == null || listXml.Count == 0) return;

            var info = listXml[0];
            string maKySH = info.KySatHach;
            string tenKySH = info.TenKySatHach;

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();
                SqlTransaction tran = conn.BeginTransaction();

                try
                {
                    // 1. LƯU BẢNG KỲ SÁT HẠCH (KySatHach)
                    string sqlKySH = @"IF NOT EXISTS (SELECT * FROM KySatHach WHERE KySatHach = @Ma)
                               INSERT INTO KySatHach (KySatHach, TenKSH, NgayThi) VALUES (@Ma, @Ten, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(sqlKySH, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@Ma", maKySH);
                        cmd.Parameters.AddWithValue("@Ten", tenKySH);
                        cmd.ExecuteNonQuery();
                    }

                    // 2. LƯU BẢNG THÍ SINH (ThiSinhSH)
                    // Đã thêm cột AnhChanDung vào câu lệnh INSERT và UPDATE
                    string sqlTS = @"IF NOT EXISTS (SELECT * FROM ThiSinhSH WHERE SBD = @SBD AND KySatHach = @MaKySH)
                             BEGIN
                                INSERT INTO ThiSinhSH (SBD, HoDem, Ten, NgaySinh, SoCCCD, HangGPLX, AnhChanDung, MaDangKy, KySatHach)
                                VALUES (@SBD, @HoDem, @Ten, @NgaySinh, @CCCD, @Hang, @AnhChanDung, @MaDK, @MaKySH)
                             END
                             ELSE
                             BEGIN
                                -- Update lại nếu đã tồn tại
                                UPDATE ThiSinhSH 
                                SET HoDem=@HoDem, Ten=@Ten, NgaySinh=@NgaySinh, SoCCCD=@CCCD, AnhChanDung=@AnhChanDung
                                WHERE SBD = @SBD AND KySatHach = @MaKySH
                             END";

                    foreach (var item in listXml)
                    {
                        using (SqlCommand cmd = new SqlCommand(sqlTS, conn, tran))
                        {
                            // Tách Họ và Tên
                            string hoTen = item.HoTen.Trim();
                            string hoDem = "";
                            string ten = hoTen;
                            int idx = hoTen.LastIndexOf(' ');
                            if (idx > 0)
                            {
                                hoDem = hoTen.Substring(0, idx);
                                ten = hoTen.Substring(idx + 1);
                            }

                            // Chuyển SBD sang bigint (long)
                            long sbd = 0;
                            long.TryParse(item.SoBaoDanh.ToString(), out sbd);
                            cmd.Parameters.AddWithValue("@SBD", sbd);

                            cmd.Parameters.AddWithValue("@HoDem", hoDem);
                            cmd.Parameters.AddWithValue("@Ten", ten);
                            cmd.Parameters.AddWithValue("@CCCD", item.SoCMT ?? "");
                            cmd.Parameters.AddWithValue("@Hang", item.HangGPLX ?? "");
                            cmd.Parameters.AddWithValue("@MaDK", item.MaDangKy ?? "");
                            cmd.Parameters.AddWithValue("@MaKySH", maKySH);

                            // --- QUAN TRỌNG: LƯU ĐƯỜNG DẪN ẢNH ---
                            // Đường dẫn này trỏ tới file ảnh bạn đã lưu ra ổ D ở hàm InputXML_Click
                            string duongDanAnh = $"D:\\{maKySH}\\anh_{item.SoBaoDanh}.jpg";
                            cmd.Parameters.AddWithValue("@AnhChanDung", duongDanAnh);

                            DateTime ns;
                            if (!DateTime.TryParseExact(item.NgaySinh, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out ns))
                                ns = DateTime.Now;
                            cmd.Parameters.AddWithValue("@NgaySinh", ns);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    tran.Commit();
                    MessageBox.Show("Đã lưu dữ liệu thành công!", "Thông báo");
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    MessageBox.Show("Lỗi lưu SQL: " + ex.Message + "\n(Vui lòng kiểm tra lại cột AnhChanDung và KySatHach trong Database)");
                    return;
                }
            }

            // 3. LOAD LÊN GRID TRÁI
            LoadDanhSachLenGrid(maKySH);

            // 4. Nạp 5 người đầu tiên vào hàng chờ
            Nap5NguoiChuanBi(listXml);
        }

        private void LoadDanhSachLenGrid(string maKySH)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(cnn))
                {
                    conn.Open();
                    // Nối Họ Đệm + Tên để hiển thị full tên
                    string sql = "SELECT SBD, (HoDem + ' ' + Ten) as HoTen, NgaySinh, SoCCCD FROM ThiSinhSH WHERE KySatHach = @Ma";
                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    da.SelectCommand.Parameters.AddWithValue("@Ma", maKySH);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    dgv.DataSource = dt; // Hiển thị lên DataGridView
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hiển thị Grid: " + ex.Message);
            }
        }


        private void Nap5NguoiChuanBi(List<ThiSinhXml> listXml)
        {
            // Xóa danh sách chuẩn bị cũ
            examManager.DanhSachChuanBiThi.Clear();

            int dem = 0;
            foreach (var item in listXml)
            {
                if (dem >= 5) break; // Chỉ lấy 5 người đầu tiên

                ThiSinh ts = new ThiSinh();
                ts.SBD = item.SoBaoDanh.ToString();
                ts.HoTen = item.Hodem;
                ts.CCCD = item.SoCMT;

                // Gán đường dẫn ảnh
                string path = $"D:\\{item.KySatHach}\\anh_{item.SoBaoDanh}.jpg";
                if (File.Exists(path)) ts.AnhChanDung = path;

                // --- [ĐOẠN SỬA LỖI] ---
                // Thay vì gọi hàm ThemVaoDSChuanBi, ta Add trực tiếp vào list:
                examManager.DanhSachChuanBiThi.Add(ts);

                dem++;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 1. Kiểm tra nếu chưa chọn gì hoặc giá trị rỗng thì thoát
            if (comboBox1.SelectedValue == null) return;

            // Xử lý lấy MaKySH (tránh lỗi khi combobox đang load dữ liệu dạng Object)
            string maKySH = "";
            if (comboBox1.SelectedValue is DataRowView drv)
            {
                // Nếu nó đang là một dòng dữ liệu, lấy cột KySatHach
                maKySH = drv["KySatHach"].ToString();
            }
            else
            {
                // Nếu nó đã là chuỗi (ValueMember hoạt động)
                maKySH = comboBox1.SelectedValue.ToString();
            }

            // 2. Viết câu lệnh SQL lọc theo WHERE
            string query = "SELECT SBD, Hodem, Ten, NgaySinh, SoCCCD, HangGPLX, QUOCTICH, AnhChanDung, MaDangKy, NoiCT, KySatHach " +
                           "FROM ThiSinhSH " +
                           "WHERE KySatHach = @MaKySH";

            try
            {
                using (SqlConnection conn = new SqlConnection(cnn))
                {
                    conn.Open();

                    // Gán vào biến toàn cục 'da' và 'dt' của bạn để tái sử dụng
                    da = new SqlDataAdapter(query, conn);
                    da.SelectCommand.Parameters.AddWithValue("@MaKySH", maKySH);

                    dt = new DataTable();
                    da.Fill(dt);

                    // Hiển thị lên DataGridView
                    dgv.DataSource = dt;
                    if (dgv.Rows.Count > 0)
                    {
                        dgv.ClearSelection();                 // bỏ chọn tất cả
                        dgv.Rows[0].Selected = true;          // chọn dòng đầu tiên
                        dgv.CurrentCell = dgv.Rows[0].Cells[0]; // đặt ô hiện tại vào cột đầu tiên
                    }
                }

                // 3. QUAN TRỌNG: Nạp lại dữ liệu vào ExamManager
                // Để danh sách "Chuẩn bị thi" và "Đang thi" được cập nhật theo kỳ mới này
                //NapDanhSachThiSinhTuSQLVaoExamManager();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lọc dữ liệu: " + ex.Message);
            }
        }
        private List<ThiSinhXml> listXml = new List<ThiSinhXml>();
        private ThiSinhXml x;
        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Nếu click tiêu đề cột thì bỏ qua
            if (e.RowIndex < 0) return;
            x = new ThiSinhXml();
            int i = e.RowIndex;
            x.SoBaoDanh = Int32.Parse(dgv.Rows[i].Cells[0].Value?.ToString());
            x.Hodem = dgv.Rows[i].Cells[1].Value?.ToString();
            x.Ten = dgv.Rows[i].Cells[2].Value?.ToString();
            x.NgaySinh = dgv.Rows[i].Cells[3].Value?.ToString();
            x.SoCMT = dgv.Rows[i].Cells[4].Value?.ToString();
            x.HangGPLX = dgv.Rows[i].Cells[5].Value?.ToString();
            x.AnhChanDung = dgv.Rows[i].Cells[7].Value?.ToString();

        }

        private void chuotphaichonxe_Click(object sender, EventArgs e)
        {

        }
        // Dùng BindingList để DataGridView tự update
        BindingList<ThiSinhDangThi> ds = new BindingList<ThiSinhDangThi>();

        private void capxeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Mở form cấp xe
            Capxe frm = new Capxe(x.SoBaoDanh, x.HangGPLX);
            frm.StartPosition = FormStartPosition.CenterParent;
            frm.ShowDialog();

            // Nếu người dùng không chọn xe thì không thêm
            if (frm.i == 0)
                return;

            // Tạo đối tượng thí sinh đang thi
            ThiSinhDangThi d = new ThiSinhDangThi()
            {
                Xe = frm.i.ToString(),
                HoDem = x.Hodem,
                Ten = x.Ten,
                SoBaoDanh = x.SoBaoDanh,
                HangGPLX = x.HangGPLX,

                // Điểm theo class mới
                DiemBanDau = 100,
                DiemConLai = 100,
                DiemTru = 0,
                SoLoi = 0,

                // Thời gian bắt đầu lúc được cấp xe
                GioBatDau = DateTime.Now,

                // Các bài thi
                So8 = "CB",
                DuongThang = "",
                ZicZac = "",
                GoGhe = ""
            };


            // Thêm vào ds -> DataGridView TỰ ĐỘNG cập nhật
            ds.Add(d);

            dgvThi.AutoGenerateColumns = false;
            dgvThi.DataSource = ds; // Gán chỉ 1 lần duy nhất
        }

        public void GridThi()
        {
            // Xe
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Xe",
                Width = 50,
                DataPropertyName = "Xe"
            });

            // Họ đệm
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Họ đệm",
                DataPropertyName = "HoDem"
            });

            // Tên
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Tên",
                DataPropertyName = "Ten"
            });

            // Số báo danh
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "SBD",
                DataPropertyName = "SoBaoDanh"
            });

            // Hạng GPLX
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Hạng",
                DataPropertyName = "HangGPLX"
            });

            // Điểm
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Điểm",
                DataPropertyName = "Diem"
            });

            // Thời gian
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Thời gian",
                DataPropertyName = "ThoiGian"
            });
            var btnChongChan = new DataGridViewButtonColumn();
            btnChongChan.HeaderText = "Chống chân";
            btnChongChan.Text = "Chống chân";
            btnChongChan.UseColumnTextForButtonValue = true;
            dgvThi.Columns.Add(btnChongChan);

            var btnDoXe = new DataGridViewButtonColumn();
            btnDoXe.HeaderText = "Đổ xe";
            btnDoXe.Text = "Đổ xe";
            btnDoXe.UseColumnTextForButtonValue = true;
            dgvThi.Columns.Add(btnDoXe);

            var btnNgoaiHinh = new DataGridViewButtonColumn();
            btnNgoaiHinh.HeaderText = "Ngoài hình";
            btnNgoaiHinh.Text = "Ngoài hình";
            btnNgoaiHinh.UseColumnTextForButtonValue = true;
            dgvThi.Columns.Add(btnNgoaiHinh);

            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Số 8",
                DataPropertyName = "So8"
            });

            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Đường thẳng",
                DataPropertyName = "DuongThang"
            });

            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Zic zắc",
                DataPropertyName = "ZicZac"
            });

            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Gồ ghề",
                DataPropertyName = "GoGhe"
            });

        }

        private void dgvThi_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var ts = dgvThi.Rows[e.RowIndex].DataBoundItem as ThiSinhDangThi;
            if (ts == null) return;

            string cot = dgvThi.Columns[e.ColumnIndex].HeaderText;

            switch (cot)
            {
                case "Chống chân":
                    ts.SoLoi += 1;
                    ts.DiemTru += 5;
                    ts.DiemConLai -= 5;
                    break;

                case "Đổ xe":
                    ts.SoLoi += 1;
                    ts.DiemTru += 25;
                    ts.DiemConLai -= 25;
                    break;

                case "Ngoài hình":
                    ts.SoLoi += 1;
                    ts.DiemTru += 25;
                    ts.DiemConLai -= 25;
                    break;
            }

            // Cập nhật trạng thái
            ts.TrangThai = ts.DiemConLai >= 80 ? "Đạt" : "Không đạt";

            // Cập nhật label ngay
            HienThiThongTinThiSinh(ts);

            // Refresh lại DataGridView
            dgvThi.Refresh();
        }

        private void HienThiThongTinThiSinh(ThiSinhDangThi ts)
        {
            lblHoTen.Text = $"{ts.HoDem} {ts.Ten}";
            lblSBD.Text = ts.SoBaoDanh.ToString();
            lblSoLoi.Text = ts.SoLoi.ToString();
            lblDiemTru.Text = ts.DiemTru.ToString();
            lblDiemConLai.Text = ts.DiemConLai.ToString();
            lblTrangThai.Text = ts.TrangThai;
        }

        private void dgvThi_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var ts = dgvThi.Rows[e.RowIndex].DataBoundItem as ThiSinhDangThi;
            if (ts != null)
                HienThiThongTinThiSinh(ts);
        }


    }

}