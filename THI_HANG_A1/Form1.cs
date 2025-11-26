using ImageMagick;
using System;
using System.Collections.Generic;
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


        private List<Moto> xes;
        private QuanLyXe fxe;
        public Form1()
        {
            InitializeComponent();
            xes = new List<Moto>();
            xes.Add(new Moto("Xe số 1", "192.168.51.16", 123));
            xes.Add(new Moto("Xe số 2", "192.168.100.51", 123));
            xes.Add(new Moto("Xe số 3", "192.168.100.52", 123));
            xes.Add(new Moto("Xe số 4", "192.168.100.53", 123));
            fxe = new QuanLyXe(xes);
            fxe.ShowDialog();

            dgvDangThi.DataSource = null;
            dgvDangThi.Visible = false;

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
            dgvChuanbi.AutoGenerateColumns = true; // Bạn đã tạo tay

            dgvKetQuaChung.AutoGenerateColumns = false;
            dgvNhatKyLoi.AutoGenerateColumns = false;

            dgvChuanbi.DataSource = examManager.DanhSachChuanBiThi;
            dgvdangthii.DataSource = examManager.DanhSachDangThi;
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
            var xeRanh = examManager.GetXeRanh();
            cboChonXe.DataSource = null;
            cboChonXe.DataSource = xeRanh;
            cboChonXe.DisplayMember = "MaXe";
            cboChonXe.ValueMember = "MaXe";
            if (xeRanh.Count == 0)
            {
                cboChonXe.Text = "Hết xe";
            }
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
        private void btnGiaoXe_Click(object sender, EventArgs e)
        {
            if (dgvChuanbi.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một thí sinh từ bảng 'CHUẨN BỊ THI'.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cboChonXe.SelectedItem == null)
            {
                MessageBox.Show("Đã hết xe rảnh hoặc bạn chưa chọn xe.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var tsChuanBi = dgvChuanbi.SelectedRows[0].DataBoundItem as ThiSinh;
            var xeDaChon = cboChonXe.SelectedItem as Xe;

            examManager.GiaoXeChoThiSinh(tsChuanBi, xeDaChon);
        }

        /// <summary>
        /// Bắt đầu lượt thi cho thí sinh đang chọn trong bảng ĐANG THI
        /// </summary>
        private void btnBatDau_Click(object sender, EventArgs e)
        {
            if (dgvdangthii.CurrentRow?.DataBoundItem is ThiSinh ts)
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
            if (dgvdangthii.CurrentRow?.DataBoundItem is ThiSinh ts)
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
            if (dgvdangthii.CurrentRow?.DataBoundItem is ThiSinh ts)
                examManager.GhiNhanLoiThuCong(ts, 5, $"Chạm vạch lần {ts.LoiChamVach + 1}", "ChamVach", t => t.LoiChamVach++);
            else
                MessageBox.Show("Vui lòng chọn thí sinh đang thi.", "Chưa chọn thí sinh");
        }

        private void btnLoiChetMay_Click(object sender, EventArgs e)
        {
            if (dgvdangthii.CurrentRow?.DataBoundItem is ThiSinh ts)
                examManager.GhiNhanLoiThuCong(ts, 5, $"Chết máy lần {ts.LoiChetMay + 1}", "ChetMay", t => t.LoiChetMay++);
            else
                MessageBox.Show("Vui lòng chọn thí sinh đang thi.", "Chưa chọn thí sinh");
        }

        private void btnLoiKhongXiNhan_Click(object sender, EventArgs e)
        {
            if (dgvdangthii.CurrentRow?.DataBoundItem is ThiSinh ts)
                examManager.GhiNhanLoiThuCong(ts, 5, $"Không xi nhan lần {ts.LoiKhongXiNhan + 1}", "KhongXiNhan", t => t.LoiKhongXiNhan++);
            else
                MessageBox.Show("Vui lòng chọn thí sinh đang thi.", "Chưa chọn thí sinh");
        }

        private async void btnLoiNgaDo_Click(object sender, EventArgs e)
        {
            if (dgvdangthii.CurrentRow?.DataBoundItem is ThiSinh ts)
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
            if (dgvdangthii.CurrentRow?.DataBoundItem is ThiSinh ts)
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
            if (dgvdangthii.CurrentRow?.DataBoundItem is ThiSinh ts)
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
            if (dgvdangthii.CurrentRow?.DataBoundItem is ThiSinh ts)
                examManager.QuaVongSo8(ts);
            else
                MessageBox.Show("Vui lòng chọn thí sinh trong bảng 'ĐANG THI'.",
                    "Chưa chọn thí sinh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        #endregion

        #region === TIMER CẬP NHẬT THỜI GIAN ===

        private void timerCapNhatThoiGian_Tick(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvdangthii.Rows)
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
                btnGiaoXe_Click(sender, e);
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
        private void dgvDangThi_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void textBox2_TextChanged(object sender, EventArgs e) { }
        private void textBox1_TextChanged(object sender, EventArgs e) { }
        private void txtSerialLog_TextChanged(object sender, EventArgs e) { }

        #endregion

        #region === LOAD FORM & SQL ===

        private void Form1_Load(object sender, EventArgs e)
        {

            // GIỮ NGUYÊN ĐOẠN NÀY NHƯ BẠN YÊU CẦU

            this.examineesTableAdapter.Fill(this.mCDV2A1DataSet.Examinees);
            Loaf();                     // đọc từ SQL vào dgv + nạp vào ExamDataManager
            groupBox1.BringToFront();
            groupBox2.BringToFront();
            dgvdangthii.AutoGenerateColumns = true;
            dgvdangthii.DataSource = examManager.DanhSachDangThi;
            if (dgvdangthii.Columns["colThoiGian"] == null)
            {
                dgvdangthii.Columns.Add(new DataGridViewTextBoxColumn()
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
        private void NapDanhSachThiSinhTuSQLVaoExamManager()
        {
            if (dt == null || dt.Rows.Count == 0)
                return;

            var danhSach = new List<ThiSinh>();

            foreach (DataRow row in dt.Rows)
            {
                var ts = new ThiSinh
                {
                    SBD = row["IDCardNo"]?.ToString(),  // dùng IDCardNo làm SBD
                    HoTen = row["Name"]?.ToString(),
                    KetquaLT = row["Traloidung"]?.ToString(),
                    CCCD = row["IDCardNo"]?.ToString()
                };


                var dobRaw = row["DateOfBirth"]?.ToString()?.Trim();
                DateTime ns;

                string[] formats =
                {
    "dd/MM/yyyy",
    "d/M/yyyy",
    "dd-MM-yyyy",
    "d-M-yyyy",
    "dd.MM.yyyy",
    "d.M.yyyy",

};

                if (!string.IsNullOrEmpty(dobRaw) &&
                    DateTime.TryParseExact(
                        dobRaw,
                        formats,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out ns))
                {
                    ts.NgaySinh = ns;
                }
                else
                {

                    if (DateTime.TryParse(dobRaw, new CultureInfo("vi-VN"), DateTimeStyles.None, out ns))
                        ts.NgaySinh = ns;
                }

                //  THÊM THÍ SINH VÀO DANH SÁCH
                danhSach.Add(ts);
            }

            //  CHỈ GỌI NẠP 1 LẦN SAU KHI ĐÃ LẤY ĐỦ DANH SÁCH
            examManager.NapDuLieuMoi(danhSach);
        }

        /// <summary>
        /// Đọc danh sách thí sinh thi lý thuyết từ SQL, gán vào dgv
        /// và nạp vào ExamDataManager
        /// </summary>
        public void Loaf()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(cnn))
                {
                    conn.Open();
                    string query = "SELECT Name,DateOfBirth,IDCardNo,Traloidung,Traloisai, times FROM Examinees";

                    da = new SqlDataAdapter(query, conn);
                    dt = new DataTable();
                    da.Fill(dt);

                    dgv.DataSource = dt;
                }

                dgv.ReadOnly = false;
                dgv.AllowUserToAddRows = true;
                dgv.AllowUserToDeleteRows = true;

                // Sau khi dt đã có dữ liệu -> nạp vào ExamManager
                NapDanhSachThiSinhTuSQLVaoExamManager();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
            }
        }


        #endregion

        /// <summary>
        /// Load danh sách thí sinh
        /// </summary>
        /// <param name="xmlPath">Đường dẫn tới file XML</param>
        /// <returns></returns>
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
                    filePath = dlg.FileName; // <-- đây là đường dẫn bạn cần
                    // Bạn có thể load trực tiếp: // XDocument doc = XDocument.Load(filePath);
                }
            }
            List<ThiSinhXml> thiSinhXmls = new List<ThiSinhXml>();
            thiSinhXmls = LoadThiSinh(filePath);
            foreach (ThiSinhXml r in thiSinhXmls)
            {
                txtTmp.Text += r.SoBaoDanh + "   " + r.HoTen + "   " + r.NgaySinh + "\r\n";
                // Lưu Kỳ sát hạch vào bảng sát hạch
                // Lưu DS thí sinh vào bảng Thi sinh
                // Load danh sách thí sinh thi theo 1 kỳ sát hạch vào bảng bên trái 
                // Ds chuẩn bị có 5 thí sinh.
                //r.AnhBytes = ConvertFromBase64((string)r.AnhChanDung);
                //if (r.AnhChanDung == null) continue;
                if (!Directory.Exists("D:\\" + r.KySatHach))
                {
                    Directory.CreateDirectory("D:\\" + r.KySatHach);
                }
                string file = "D:\\" + r.KySatHach + $"\\anh_{r.SoBaoDanh}.jpg";
                //Image img = ConvertFromBase64(r.AnhChanDung);

                //img.Save(file, ImageFormat.Jpeg);
                BitmapImage img = AnhImage(r.AnhChanDung);
                SaveBitmapImage(img, file);

            }

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

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
    }
}