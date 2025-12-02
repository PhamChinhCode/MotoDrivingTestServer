using ImageMagick;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using THI_HANG_A1.Forms;
using THI_HANG_A1.Helpers;
using THI_HANG_A1.Managers;
using THI_HANG_A1.Models;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;


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
        private ContextMenuStrip cmsThiSinh;   // menu khi nhấp đúp vào thí sinh đang thi
        private int _currentRowIndex = -1;     // lưu dòng đang thao tác
        private int selectedSessionId = -1;
        private List<San> sanList = new List<San>();

        private List<Moto> xes;
        private QuanLyXe fxe;

        private void LoadMotoFromDatabase()
        {
            xes = new List<Moto>();

            string sql = "SELECT ID, Name, IPAddress FROM Devices ORDER BY ID";

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        xes.Add(new Moto()
                        {
                            Id = Convert.ToByte(rd["ID"]),
                            Name = rd["Name"].ToString(),
                            Ip = rd["IPAddress"].ToString(),
                            Port = 123
                        });
                    }
                }
            }
        }

        private QLiSan fsan;
        public Form1()
        {
            InitializeComponent();
            LoadMotoFromDatabase();
            fxe = new QuanLyXe(xes);

            sanList = new List<San>();
            sanList.Add(new San("San 1", "192.168.244.220", 123));
            //fxe.ShowDialog();
            //xes[0].Connect();

            GridThi();
            dgvThi.AutoGenerateColumns = false;
            dgvThi.DataSource = null;
            dgvThi.CellMouseDown += dgvThi_CellMouseDown;

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

            dgvKetQuaChung.AutoGenerateColumns = false;
            dgvNhatKyLoi.AutoGenerateColumns = false;
            dgvThi.DataSource = examManager.DanhSachDangThi;
            dgvKetQuaChung.DataSource = examManager.DanhSachKetQuaChung;
            dgvNhatKyLoi.DataSource = examManager.DanhSachLoiViPham;

            dgvchitietloi.AutoGenerateColumns = true;
            dgvchitietloi.DataSource = dsChiTietLoi;
            SetHeaderChiTietLoi();

            dgvchitietloi.Columns["ThoiGian"].DefaultCellStyle.Format = "HH:mm:ss";
            this.dgvNhatKyLoi.CellFormatting += dgvNhatKyLoi_CellFormatting;
            CapNhatDanhSachXeRanhUI();
            examManager.OnDataChanged += (s, e) => CapNhatDanhSachXeRanhUI();
        }
        private void SetHeaderChiTietLoi()
        {
            dgvchitietloi.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            if (dgvchitietloi.Columns["ThoiGian"] != null)
            {
                dgvchitietloi.Columns["ThoiGian"].HeaderText = "Thời gian";
                dgvchitietloi.Columns["ThoiGian"].Width = 110;
                dgvchitietloi.Columns["ThoiGian"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            }

            if (dgvchitietloi.Columns["SuKien"] != null)
            {
                dgvchitietloi.Columns["SuKien"].HeaderText = "Sự kiện";
                dgvchitietloi.Columns["SuKien"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            if (dgvchitietloi.Columns["DiemTru"] != null)
            {
                dgvchitietloi.Columns["DiemTru"].HeaderText = "Điểm trừ";
                dgvchitietloi.Columns["DiemTru"].Width = 80; 
                dgvchitietloi.Columns["DiemTru"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            }

            if (dgvchitietloi.Columns["ChiTiet"] != null)
            {
                dgvchitietloi.Columns["ChiTiet"].HeaderText = "Chi tiết lỗi";
                dgvchitietloi.Columns["ChiTiet"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }

        /// <summary>
        /// Cập nhật ComboBox xe rảnh từ dữ liệu trong ExamManager
        /// 
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

        //#region === NÚT ĐIỀU KHIỂN, LỖI ===

        ///// <summary>
        ///// Giao xe cho thí sinh được chọn ở bảng CHUẨN BỊ THI
        ///// </summary>

        ///// <summary>
        ///// Bắt đầu lượt thi cho thí sinh đang chọn trong bảng ĐANG THI
        ///// </summary>
        //private void btnBatDau_Click(object sender, EventArgs e)
        //{
        //    if (dgvThi.CurrentRow?.DataBoundItem is ThiSinh ts)
        //    {
        //        examManager.BatDauLuotThi(ts);

        //        if (!timerCapNhatThoiGian.Enabled)
        //            timerCapNhatThoiGian.Start();
        //    }
        //    else
        //    {
        //        MessageBox.Show("Vui lòng CHỌN thí sinh trong bảng 'ĐANG THI' để bắt đầu.",
        //            "Chưa chọn thí sinh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //    }
        //}

        //private void btnKetThucLuot_Click(object sender, EventArgs e)
        //{
        //    if (dgvThi.CurrentRow?.DataBoundItem is ThiSinh ts)
        //    {
        //        var xacNhan = MessageBox.Show(
        //            $"Xác nhận kết thúc lượt thi của {ts.HoTen} với điểm số là {ts.DiemTongHop}?",
        //            "Xác nhận kết thúc", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        //        if (xacNhan == DialogResult.Yes)
        //        {
        //            _ = examManager.KetThucLuotThiThuCong(ts);
        //        }
        //    }
        //    else
        //    {
        //        MessageBox.Show("Vui lòng chọn thí sinh cần kết thúc bài thi.",
        //            "Chưa chọn thí sinh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //    }
        //}

        //// Các nút lỗi (nhẹ/nặng) – GIỮ NGUYÊN, chỉ sửa grid thành dgvdangthii

        //private void btnLoiChamVach_Click(object sender, EventArgs e)
        //{
        //    if (dgvThi.CurrentRow?.DataBoundItem is ThiSinh ts)
        //        examManager.GhiNhanLoiThuCong(ts, 5, $"Chạm vạch lần {ts.LoiChamVach + 1}", "ChamVach", t => t.LoiChamVach++);
        //    else
        //        MessageBox.Show("Vui lòng chọn thí sinh đang thi.", "Chưa chọn thí sinh");
        //}

        //private void btnLoiChetMay_Click(object sender, EventArgs e)
        //{
        //    if (dgvThi.CurrentRow?.DataBoundItem is ThiSinh ts)
        //        examManager.GhiNhanLoiThuCong(ts, 5, $"Chết máy lần {ts.LoiChetMay + 1}", "ChetMay", t => t.LoiChetMay++);
        //    else
        //        MessageBox.Show("Vui lòng chọn thí sinh đang thi.", "Chưa chọn thí sinh");
        //}

        //private void btnLoiKhongXiNhan_Click(object sender, EventArgs e)
        //{
        //    if (dgvThi.CurrentRow?.DataBoundItem is ThiSinh ts)
        //        examManager.GhiNhanLoiThuCong(ts, 5, $"Không xi nhan lần {ts.LoiKhongXiNhan + 1}", "KhongXiNhan", t => t.LoiKhongXiNhan++);
        //    else
        //        MessageBox.Show("Vui lòng chọn thí sinh đang thi.", "Chưa chọn thí sinh");
        //}

        //private async void btnLoiNgaDo_Click(object sender, EventArgs e)
        //{
        //    if (dgvThi.CurrentRow?.DataBoundItem is ThiSinh ts)
        //    {
        //        var xacNhan = MessageBox.Show(
        //            "Xác nhận thí sinh bị lỗi 'Ngã/đổ xe' và bị loại trực tiếp?",
        //            "Xác nhận lỗi loại", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        //        if (xacNhan == DialogResult.Yes)
        //        {
        //            ts.LoiNgaDo = 1;
        //            await examManager.LoaiTrucTiep(ts, ts.DiemTongHop, "Ngã/đổ xe (Loại trực tiếp)", "DoXe");
        //        }
        //    }
        //    else
        //        MessageBox.Show("Vui lòng chọn thí sinh đang thi.", "Chưa chọn thí sinh");
        //}

        //private async void btnLoiSaiHinh_Click(object sender, EventArgs e)
        //{
        //    if (dgvThi.CurrentRow?.DataBoundItem is ThiSinh ts)
        //    {
        //        var xacNhan = MessageBox.Show(
        //            "Xác nhận thí sinh bị lỗi 'Chạy sai hình' và bị loại trực tiếp?",
        //            "Xác nhận lỗi loại", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        //        if (xacNhan == DialogResult.Yes)
        //        {
        //            ts.LoiChaySaiHinh = 1;
        //            await examManager.LoaiTrucTiep(ts, ts.DiemTongHop, "Chạy sai hình (Loại trực tiếp)", "SaiHinh");
        //        }
        //    }
        //    else
        //        MessageBox.Show("Vui lòng chọn thí sinh đang thi.", "Chưa chọn thí sinh");
        //}

        //private async void btnLoiQuaTocDo_Click(object sender, EventArgs e)
        //{
        //    if (dgvThi.CurrentRow?.DataBoundItem is ThiSinh ts)
        //    {
        //        var xacNhan = MessageBox.Show(
        //            "Xác nhận thí sinh bị lỗi 'Vượt quá tốc độ' và bị loại trực tiếp?",
        //            "Xác nhận lỗi loại", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        //        if (xacNhan == DialogResult.Yes)
        //        {
        //            ts.LoiQuaTocDo = 1;
        //            await examManager.LoaiTrucTiep(ts, ts.DiemTongHop, "Vượt quá tốc độ (Loại trực tiếp)", "VuotTocDo");
        //        }
        //    }
        //    else
        //        MessageBox.Show("Vui lòng chọn thí sinh đang thi.", "Chưa chọn thí sinh");
        //}

        //private void btnQuaVongSo8_Click(object sender, EventArgs e)
        //{
        //    if (dgvThi.CurrentRow?.DataBoundItem is ThiSinh ts)
        //        examManager.QuaVongSo8(ts);
        //    else
        //        MessageBox.Show("Vui lòng chọn thí sinh trong bảng 'ĐANG THI'.",
        //            "Chưa chọn thí sinh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //}

        //#endregion

        //private void btnQuaVongSo8_Click(object sender, EventArgs e)
        //{
        //    if (dgvThi.CurrentRow?.DataBoundItem is ThiSinh ts)
        //        examManager.QuaVongSo8(ts);
        //    else
        //        MessageBox.Show("Vui lòng chọn thí sinh trong bảng 'ĐANG THI'.",
        //            "Chưa chọn thí sinh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //}

        //#endregion
        #region === TIMER CẬP NHẬT THỜI GIAN ===

        // Khai báo duy nhất một hàm `timerCapNhatThoiGian_Tick`
        private void timerCapNhatThoiGian_Tick(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvThi.Rows)
            {
                if (row.DataBoundItem is ThiSinhDangThi ts) 
                {
                    var cell = row.Cells["colThoiGian"];

                    // 1. Nếu trạng thái kết thúc -> Giữ nguyên, không cập nhật
                    if (ts.TrangThai == "Đạt" || ts.TrangThai == "Không đạt")
                    {
                        continue;
                    }

                    // 2. Rớt tự động nếu dưới 80 điểm
                    if (ts.DiemConLai < 80)
                    {
                        ts.TrangThai = "Không đạt";
                        // cell.Value = "Dừng thi"; // Tùy chọn hiển thị
                        continue;
                    }

                    // 3. CHỈ TÍNH GIỜ KHI: Trạng thái là "Đang thi" VÀ Giờ bắt đầu đã được gán
                    if (ts.TrangThai == "Đang thi" && ts.GioBatDau != DateTime.MinValue)
                    {
                        TimeSpan thoiGianTroiQua = DateTime.Now - ts.GioBatDau;
                        cell.Value = thoiGianTroiQua.ToString(@"mm\:ss");
                    }
                    else
                    {
                        // Trường hợp: "Đã cấp xe", "Chuẩn bị"... -> Hiện gạch ngang
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
            LoadInitData();
        }
        private void LoadInitData()
        {
            this.dBKySatHachTableAdapter.Fill(this.mCDV2A1DataSet2.DBKySatHach);
            // GIỮ NGUYÊN ĐOẠN NÀY NHƯ BẠN YÊU CẦU
            //this.examineesTableAdapter.Fill(this.mCDV2A1DataSet.Examinees);
            LoadComboboxKySatHach();
            Loaf();                     // đọc từ SQL vào dgv + nạp vào ExamDataManager
            dgvThi.AutoGenerateColumns = false;
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
            BaiThiHelper.LoadBaiThi();
            FaultDefinitions.LoadFaults();

            LoadingComponent();
        }
        private void LoadingComponent()
        {
            // Làm mờ nền
            panelOverlay.BackColor = Color.FromArgb(120, 0, 0, 0);
            panelOverlay.Dock = DockStyle.Fill;
            panelOverlay.Visible = false;

            // Giữ nguyên kích thước ảnh gốc
            picLoadingg.Image = Properties.Resources.Loading_icon;
            picLoadingg.SizeMode = PictureBoxSizeMode.AutoSize;

            // Nền trắng nếu panel lớn hơn ảnh
            picLoadingg.BackColor = Color.White;

            // Đặt ảnh vào một Panel con để dễ căn giữa
            picLoadingg.Parent = panelOverlay;

            // Căn giữa ảnh
            picLoadingg.Location = new Point(
                (panelOverlay.Width - picLoadingg.Width) / 2,
                (panelOverlay.Height - picLoadingg.Height) / 2
            );

            // Xử lý khi panel overlay thay đổi kích thước
            panelOverlay.Resize += (s, e) =>
            {
                picLoadingg.Location = new Point(
                    (panelOverlay.Width - picLoadingg.Width) / 2,
                    (panelOverlay.Height - picLoadingg.Height) / 2
                );
            };
        }

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
        private async void InputXML_Click(object sender, EventArgs e)
        {
            string filePath = "";
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                dlg.Title = "Chọn file thí sinh đuôi XML";

                if (dlg.ShowDialog() == DialogResult.OK)
                    filePath = dlg.FileName;
                else
                    return;
            }


            panelOverlay.Visible = true;
            panelOverlay.BringToFront();

            List<ThiSinhXml> thiSinhXmls = null;

            try
            {

                thiSinhXmls = await Task.Run(() =>
                {
                    return LoadThiSinh(filePath);
                });


                await Task.Run(() =>
                {
                    foreach (ThiSinhXml r in thiSinhXmls)
                    {
                        string folder = "D:\\" + r.KySatHach;
                        if (!Directory.Exists(folder))
                            Directory.CreateDirectory(folder);

                        string file = folder + $"\\anh_{r.SoBaoDanh}.jpg";

                        BitmapImage img = AnhImage(r.AnhChanDung);
                        SaveBitmapImage(img, file);
                    }
                });


                XuLyLuuVaHienThi(thiSinhXmls);
            }
            finally
            {

                panelOverlay.Visible = false;
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

        // Dùng BindingList để DataGridView tự update
        BindingList<ThiSinhDangThi> ds = new BindingList<ThiSinhDangThi>();

        private void capxeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 1. Kiểm tra dòng chọn hợp lệ
            if (dgv.CurrentRow == null || dgv.CurrentRow.Index < 0)
            {
                MessageBox.Show("Vui lòng chọn một thí sinh trước.");
                return;
            }

            // 2. Lấy dữ liệu từ DataRowView
            DataRowView drv = dgv.CurrentRow.DataBoundItem as DataRowView;
            if (drv == null)
                return;

            // 3. Mở form cấp xe
            Capxe frm = new Capxe(xes, x.SoBaoDanh, x.HangGPLX);
            frm.StartPosition = FormStartPosition.CenterParent;

            DialogResult result = frm.ShowDialog();
            if (result != DialogResult.OK || frm.XeDuocChon == null)
                return;

            Moto xeChon = frm.XeDuocChon;

            string soXe = xeChon.Name;
            int sbd = 0;
            string hang = "";
            string hoDem = "";
            string ten = "";

            try
            {
                sbd = Convert.ToInt32(drv["SBD"]);
                hang = drv["HangGPLX"].ToString();
                hoDem = drv["Hodem"].ToString();
                ten = drv["Ten"].ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Dữ liệu thí sinh thiếu hoặc lỗi: " + ex.Message);
                return;
            }

            // Kiểm tra trạng thái xe trong từ điển
            if (!trangThaiXe.ContainsKey(soXe))
                trangThaiXe[soXe] = TrangThaiXe.Ranh;

            if (trangThaiXe[soXe] != TrangThaiXe.Ranh)
            {
                MessageBox.Show($"Xe số {soXe} đang được dùng cho thí sinh khác.\nVui lòng chọn xe khác.",
                    "Xe đang bận", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 5. Tạo đối tượng Thí Sinh Đang Thi
            ThiSinhDangThi d = new ThiSinhDangThi()
            {
                Xe = soXe,
                XeObj = xeChon,
                HoDem = hoDem,
                Ten = ten,
                SoBaoDanh = sbd,
                HangGPLX = hang,
                DiemBanDau = 100,
                DiemConLai = 100,
                DiemTru = 0,
                SoLoi = 0,

                // --- QUAN TRỌNG: KHÔNG ĐƯỢC DÙNG DateTime.Now ---
                GioBatDau = DateTime.MinValue, // Để Timer không tự chạy
                TrangThai = "Đã cấp xe",       // Trạng thái chờ
                                               // -----------------------------------------------

                DaKiemTraXe = true,
                So8 = "CB",
                DuongThang = "",
                ZicZac = "",
                GoGhe = "",
                BaiThiHienTaiID = 0
            };
            // 7. Tạo SESSION trong database
            int sessionId = CreateSession(sbd, Convert.ToInt32(d.XeObj.Id));

            // 8. Gán SessionID vào đối tượng thí sinh
            d.SessionID = sessionId;

            int lastStatus = -1;
            int lastError = 0;

            // ==========================
            //  GẮN SỰ KIỆN THAY ĐỔI TỪ XE
            // ==========================


            xeChon.OnChanged += () =>
            {
                byte st = xeChon.Status;
                byte errId = xeChon.ErrorId;

                // Cập nhật bài thi hiện tại
                BaiThiHelper.CapNhatBaiThiHienTai(d, st);

                // ---------------------------
                //  BỎ QUA STATUS KHÔNG HỢP LỆ
                // ---------------------------
                if (!BaiThiHelper.IsInValidContest(st))
                    return;

                // ---------------------------
                //  XỬ LÝ STATUS – CHỈ LOG KHI ĐỔI BÀI
                // ---------------------------
                if (st != lastStatus && BaiThiHelper.IsInValidContest1_4(st))
                {
                    string tenBaiThi = BaiThiHelper.GetName(st);

                    InsertErrorToDatabase(
                        d.SoBaoDanh,
                        d.SessionID,
                        $"{d.HoDem} {d.Ten}",
                        d.Xe,
                        $"Vào bài thi: {tenBaiThi}",
                        0,
                        $"Bắt đầu bài thi {tenBaiThi}",
                        null,
                        d.BaiThiHienTaiID
                    );
                    lastStatus = st;
                    lastError = 0;          // reset lỗi khi chuyển bài
                }

                // ---------------------------
                //  XỬ LÝ ERROR – CHỈ LOG 1 LẦN DUY NHẤT
                // ---------------------------
                if (errId != 0)
                {
                    if (errId != lastError && FaultDefinitions.FaultByErrorId.TryGetValue(errId, out var err))
                    {
                        lastError = errId; // ghi nhớ lỗi vừa log

                        string baiThiMoTa = BaiThiHelper.GetName(st);
                        string chiTietLoi = $"{err.moTa} – Tại bài: {baiThiMoTa}";

                        int? baiThiId = d.BaiThiHienTaiID == 0 ? (int?)null : d.BaiThiHienTaiID;

                        InsertErrorToDatabase(
                            d.SoBaoDanh,
                            d.SessionID,
                            $"{d.HoDem} {d.Ten}",
                            d.Xe,
                            err.moTa,
                            err.diemTru,
                            chiTietLoi,
                            err.id,
                            baiThiId
                        );

                        // Cập nhật điểm
                        d.SoLoi++;
                        d.DiemTru += err.diemTru;
                        d.DiemConLai -= err.diemTru;


                        // thiếu case kết thúc bài thi
                        UpdateMarkSession(d.SessionID, d.DiemConLai);

                        xeChon.sendCommand(ConstantKeys.MARK_KEY, ConstantKeys.BYTE_SET, (byte)d.DiemConLai);

                        SafeUI(() => HienThiThongTinThiSinh(d));
                    }
                }
                else
                {
                    // Nếu lỗi trở về 0 → reset lỗi cho phép log lỗi mới
                    lastError = 0;
                }

                SafeUI(() => dgvThi.Refresh());
            };


            // đọc lỗi từ sân
            San san = sanList[0];
            san.OnChanged += () =>
            {
                int baiThiId = BaiThiHelper.GetId(xeChon.Status);
                string baiThiMoTa = BaiThiHelper.GetName(xeChon.Status);

                // Map sensor theo bài thi
                List<(string name, bool value)> sensors = new List<(string name, bool value)>();

                switch (xeChon.Status)
                {
                    case ConstantKeys.STATUS_CONTEST1:
                        sensors.Add(("Sensor1", san.Sensor1));
                        sensors.Add(("Sensor2", san.Sensor2));
                        sensors.Add(("Sensor3", san.Sensor3));
                        break;

                    case ConstantKeys.STATUS_CONTEST2:
                    case ConstantKeys.STATUS_CONTEST3:
                    case ConstantKeys.STATUS_CONTEST4:
                        sensors.Add(("Sensor4", san.Sensor4));
                        sensors.Add(("Sensor5", san.Sensor5));
                        break;
                }

                // Tìm sensor nào đang bật
                var triggered = sensors.Where(s => s.value).ToList();

                if (triggered.Any())
                {
                    foreach (var sensor in triggered)
                    {
                        string chiTietLoi = $"Đè vạch tại {sensor.name} – Bài: {baiThiMoTa}";

                        Debug.WriteLine($"[DEBUG] Lỗi {sensor.name}, bài: {baiThiMoTa}");

                        InsertErrorToDatabase(
                            d.SoBaoDanh,
                            d.SessionID,
                            $"{d.HoDem} {d.Ten}",
                            d.Xe,
                            chiTietLoi,
                            5,
                            chiTietLoi,
                            null,
                            baiThiId
                        );
                    }
                }
            };


            // 6. Cập nhật vào danh sách và Grid
            ds.Add(d);

            // Reset datasource để tránh lỗi hiển thị
            dgvThi.DataSource = null;
            dgvThi.DataSource = ds;

            // Cập nhật Label thông tin ở dưới
            HienThiThongTinThiSinh(d);
        }



        private void dgvThi_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var ts = dgvThi.Rows[e.RowIndex].DataBoundItem as ThiSinhDangThi;
            if (ts == null) return;

            // đẩy ts sang form in ket qua thi
            string cot = dgvThi.Columns[e.ColumnIndex].HeaderText;

            bool laCotLoi =
                cot == "Chống chân" ||
                cot == "Đổ xe" ||
                cot == "Ngoài hình";

            if (!laCotLoi)
            {
                HienThiThongTinThiSinh(ts);
                return;
            }
            if (ts.BaiThiHienTaiID <= 0)
            {
                MessageBox.Show("Chưa vào bài thi nên không thể ghi lỗi!",
                                "Chưa vào bài", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (ts.TrangThai == "Đã cấp xe" ||
                ts.TrangThai == "Chuẩn bị" ||
                ts.TrangThai == "Đạt" ||
                ts.TrangThai == "Không đạt")
            {
                MessageBox.Show("Bạn chưa thể ghi lỗi vì bài thi CHƯA bắt đầu!",
                                "Không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (ts.DaKiemTraXe != true)
            {
                MessageBox.Show("Bạn phải kiểm tra xe trước khi ghi lỗi!",
                                "Chưa kiểm tra xe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (ts.GioBatDau == DateTime.MinValue)
            {
                ts.GioBatDau = DateTime.Now;
                ts.TrangThai = "Đang thi";

                if (!timerCapNhatThoiGian.Enabled)
                    timerCapNhatThoiGian.Start();
            }

            //===============================
            //   DÙNG MAP ĐỂ LẤY LỖI
            //===============================
            string fullName = FaultDefinitions.FaultUIMap[cot];
            var err = FaultDefinitions.FaultMap[fullName];
            int faultId = err.id;
            int diemTru = err.diemTru;
            int baiThiId = ts.BaiThiHienTaiID;
            string baiThiMoTa = BaiThiHelper.GetNameByBaiThiId(baiThiId);
            string chiTietLoi = $"{err.moTa} – Tại vị trí: {baiThiMoTa}";

            //===============================
            //  CẬP NHẬT ĐIỂM
            //===============================
            ts.SoLoi++;
            ts.DiemTru += diemTru;
            ts.DiemConLai -= diemTru;

            //===============================
            //  LƯU LỖI VÀO DATABASE
            //===============================
            InsertErrorToDatabase(
                ts.SoBaoDanh,
                ts.SessionID,
                $"{ts.HoDem} {ts.Ten}",
                ts.Xe,
                err.moTa,
                diemTru,
                chiTietLoi,
                faultId,
                baiThiId
            );

            if (ts.DiemConLai < 80)
                ts.TrangThai = "Không đạt";
            else
                ts.TrangThai = "Đang thi";

            HienThiThongTinThiSinh(ts);
            dgvThi.Refresh();
        }


        private void dgvThi_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            // 1. Chỉ xử lý đúng cột trạng thái và không phải dòng tiêu đề
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvThi.Columns[e.ColumnIndex].Name != "colTrangThaiXe") return;

            // 2. Lấy dữ liệu thí sinh
            var ts = dgvThi.Rows[e.RowIndex].DataBoundItem as ThiSinhDangThi;

            using (Brush backBrush = new SolidBrush(Color.White))
            {
                e.Graphics.FillRectangle(backBrush, e.CellBounds);
            }
            // Vẽ nền trắng cho ô (xóa các nội dung cũ)
            e.Paint(e.CellBounds, DataGridViewPaintParts.Border);

            if (ts == null)
            {
                e.Handled = true;
                return;
            }

            Color dot = BaiThiHelper.mapMau[BaiThiHelper.ParseTrangThai(ts.TrangThai)];

            if (dot != Color.Transparent)
            {
                int size = 14;
                int x = e.CellBounds.X + (e.CellBounds.Width - size) / 2;
                int y = e.CellBounds.Y + (e.CellBounds.Height - size) / 2;

                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var br = new SolidBrush(dot))
                    e.Graphics.FillEllipse(br, x, y, size, size);
            }

            // 5. Ngăn không cho Grid vẽ đè cái gì khác lên nữa
            e.Handled = true;
        }

        private void HienThiThongTinThiSinh(ThiSinhDangThi ts)
        {
            lblHoTen.Text = $"{ts.HoDem} {ts.Ten}";
            lblSBD.Text = ts.SoBaoDanh.ToString();
            lblSoLoi.Text = ts.SoLoi.ToString();
            lblDiemTruu.Text = ts.DiemTru.ToString();  // Cập nhật điểm trừ
            lblDiemConLai.Text = ts.DiemConLai.ToString();  // Cập nhật điểm còn lại
            lblTrangThai.Text = ts.TrangThai;
        }

        private void dgvThi_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var ts = dgvThi.Rows[e.RowIndex].DataBoundItem as ThiSinhDangThi;
            if (ts != null)
                HienThiThongTinThiSinh(ts);
        }

        private ToolStripMenuItem mnuCB, mnuBD, mnuTDX;
        private void TaoMenuThiSinh()
        {
            cmsThiSinh = new ContextMenuStrip();

            mnuCB = new ToolStripMenuItem("Chuẩn bị", null, mnuChuanBi_Click);
            mnuBD = new ToolStripMenuItem("Bắt đầu", null, mnuBatDau_Click);
            mnuTDX = new ToolStripMenuItem("Thay đổi xe", null, mnuThayDoiXe_Click);

            cmsThiSinh.Items.Add(mnuCB);
            cmsThiSinh.Items.Add(mnuBD);
            cmsThiSinh.Items.Add(mnuTDX);

            cmsThiSinh.Items.Add("Hủy", null, mnuHuy_Click);
            cmsThiSinh.Items.Add("Trừ điểm thí sinh", null, mnuTruDiem_Click);
            cmsThiSinh.Items.Add("Ẩn", null, mnuAn_Click);
            cmsThiSinh.Items.Add("In biên bản", null, mnuInBienBan_Click);
            cmsThiSinh.Items.Add("Chụp ảnh", null, mnuChupAnh_Click);
            cmsThiSinh.Items.Add("Kết thúc", null, mnuKetThuc_Click);

            // Gán sự kiện mở menu
            cmsThiSinh.Opening += CmsThiSinh_Opening;
        }

        private void CmsThiSinh_Opening(object sender, CancelEventArgs e)
        {
            if (_currentRowIndex < 0) { e.Cancel = true; return; }

            var ts = dgvThi.Rows[_currentRowIndex].DataBoundItem as ThiSinhDangThi;
            if (ts == null) { e.Cancel = true; return; }

            // Reset tất cả
            mnuCB.Enabled = mnuBD.Enabled = mnuTDX.Enabled = false;

            // Chưa cấp xe ⇒ chỉ cho đổi xe
            if (string.IsNullOrWhiteSpace(ts.Xe))
            {
                mnuTDX.Enabled = true;
                return;
            }

            switch (ts.TrangThai)
            {
                case "Đã cấp xe":
                    mnuCB.Enabled = true;
                    mnuTDX.Enabled = true;
                    break;

                case "Chuẩn bị":
                    mnuBD.Enabled = true;
                    break;

                case "Đang thi":
                    // tất cả disabled — để nguyên
                    break;

                case "Đạt":
                case "Không đạt":
                    mnuTDX.Enabled = true;
                    break;
            }
        }

        private void mnuHuy_Click(object sender, EventArgs e)
        {
            // chưa dùng
        }

        private void mnuTruDiem_Click(object sender, EventArgs e)
        {
            // chưa dùng
        }

        private void mnuAn_Click(object sender, EventArgs e)
        {
            // chưa dùng
        }

        private void mnuInBienBan_Click(object sender, EventArgs e)
        {
            // chưa dùng
        }

        private void mnuChupAnh_Click(object sender, EventArgs e)
        {
            // chưa dùng
        }

        private void dgvThi_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            if (e.RowIndex < 0) return;      // bỏ header
            if (e.ColumnIndex < 0) return;   // bỏ cột ngoài lề

            _currentRowIndex = e.RowIndex;

            // Chọn cả dòng đang được double click
            dgvThi.ClearSelection();
            dgvThi.Rows[_currentRowIndex].Selected = true;

            selectedSessionId = GetSessionIdFromRow(_currentRowIndex);

            // ==== LẤY SESSIONID CỦA DÒNG ĐƯỢC CLICK ====
            if (dgvThi.Columns.Contains("SessionID"))
            {
                object value = dgvThi.Rows[_currentRowIndex].Cells["SessionID"].Value;

                if (value != null && value != DBNull.Value)
                {
                    int temp;
                    if (int.TryParse(value.ToString(), out temp))
                    {
                        selectedSessionId = temp;
                    }
                }
            }

            if (cmsThiSinh == null)
                TaoMenuThiSinh();
            // Hiện menu tại vị trí chuột
            cmsThiSinh.Show(Cursor.Position);
        }
        private int GetSessionIdFromRow(int rowIndex)
        {
            int sessionId = 0;

            var cell = dgvThi.Rows[rowIndex].Cells
                .Cast<DataGridViewCell>()
                .FirstOrDefault(c => c.OwningColumn.DataPropertyName == "SessionID");

            if (cell != null && cell.Value != null && cell.Value != DBNull.Value)
            {
                int temp;
                if (int.TryParse(cell.Value.ToString(), out temp))
                    sessionId = temp;
            }

            return sessionId;
        }

        private void mnuChuanBi_Click(object sender, EventArgs e)
        {
            if (_currentRowIndex < 0) return;

            var ts = dgvThi.Rows[_currentRowIndex].DataBoundItem as ThiSinhDangThi;
            if (ts == null) return;

            string soXe = ts.Xe;

            if (string.IsNullOrWhiteSpace(soXe))
            {
                MessageBox.Show("Thí sinh này chưa được cấp xe.");
                return;
            }

            // Nếu đã có trạng thái xe và xe KHÔNG rảnh => không cho chuẩn bị
            if (trangThaiXe.TryGetValue(soXe, out var trangThaiHienTai)
                && trangThaiHienTai != TrangThaiXe.Ranh)
            {
                MessageBox.Show("Xe này đang được dùng cho thí sinh khác.",
                                "Không thể chuẩn bị", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Cho xe sang trạng thái Sẵn sàng
            trangThaiXe[soXe] = TrangThaiXe.SanSang;
            string cot = "Chuẩn bị";  // tên hành động

            var err = FaultDefinitions.FaultMap[cot];
            int faultId = err.id;
            int diemTru = err.diemTru;
            
            InsertErrorToDatabase(
                ts.SoBaoDanh,
                ts.SessionID,
                $"{ts.HoDem} {ts.Ten}",
                ts.Xe,
                cot,
                diemTru,
                "Chuẩn bị"
            );

            // Cập nhật trạng thái thí sinh:
            // Sau khi ấn Chuẩn bị: ô vuông không màu, chưa tích
            ts.TrangThai = "Chuẩn bị";

            dgvThi.Refresh();
        }


        private void mnuBatDau_Click(object sender, EventArgs e)
        {
            if (_currentRowIndex < 0) return;
            var ts = dgvThi.Rows[_currentRowIndex].DataBoundItem as ThiSinhDangThi;
            if (ts == null) return;

            // --- [SỬA QUAN TRỌNG TẠI ĐÂY] ---
            // Bây giờ mới kích hoạt giờ
            ts.GioBatDau = DateTime.Now;
            ts.TrangThai = "Đang thi";
            // --------------------------------

            // Cập nhật trạng thái xe quản lý
            if (!string.IsNullOrWhiteSpace(ts.Xe))
            {
                trangThaiXe[ts.Xe] = TrangThaiXe.DangThi;
            }

            Moto moto = xes.FirstOrDefault(m => m.Id == ts.XeObj.Id);

            moto.sendCommand(ConstantKeys.CONTROL_KEY, ConstantKeys.BYTE_SET, ConstantKeys.CONTROL_START);

            string cot = "Bắt đầu";

            var err = FaultDefinitions.FaultMap[cot];
            int faultId = err.id;
            int diemTru = err.diemTru;

            InsertErrorToDatabase(
                ts.SoBaoDanh,
                ts.SessionID,
                $"{ts.HoDem} {ts.Ten}",
                ts.Xe,
                cot,
                diemTru,
                "Bắt đầu"
            );


            // Bật timer nếu chưa chạy
            if (!timerCapNhatThoiGian.Enabled)
                timerCapNhatThoiGian.Start();
            dgvThi.Refresh();
        }
        private void mnuKetThuc_Click(object sender, EventArgs e)
        {
            // 1. Kiểm tra dòng chọn hợp lệ
            if (_currentRowIndex < 0) return;

            var ts = dgvThi.Rows[_currentRowIndex].DataBoundItem as ThiSinhDangThi;
            if (ts == null) return;

            string soXe = ts.Xe;

            // 2. LOGIC QUAN TRỌNG: 
            // Chỉ chặn kết thúc nếu trạng thái là "Chuẩn bị", "Đã cấp xe" hoặc rỗng.
            // Nếu trạng thái là "Đang thi", "Đạt" hoặc "Không đạt" thì vẫn cho phép Kết thúc.
            if (ts.TrangThai == "Chuẩn bị" || ts.TrangThai == "Đã cấp xe" || string.IsNullOrEmpty(ts.TrangThai))
            {
                MessageBox.Show("Thí sinh chưa bắt đầu thi nên không thể kết thúc!",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 3. Hiện hộp thoại hỏi kết quả
            var dialog = MessageBox.Show($"Xác nhận kết thúc bài thi của thí sinh {ts.HoDem} {ts.Ten}?\n\n- Chọn YES để xác nhận ĐẠT.\n- Chọn NO để xác nhận KHÔNG ĐẠT.\n- Chọn CANCEL để hủy.",
                "Kết thúc bài thi",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            // Nếu chọn Cancel thì thoát
            if (dialog == DialogResult.Cancel)
                return;

            // 4. Cập nhật trạng thái theo lựa chọn Yes/No
            if (dialog == DialogResult.Yes)
            {
                ts.TrangThai = "Đạt";
            }
            else if (dialog == DialogResult.No)
            {
                ts.TrangThai = "Không đạt";
            }

            // 5. Trả xe về trạng thái RẢNH để cấp cho người sau
            if (!string.IsNullOrEmpty(soXe))
            {
                if (trangThaiXe.ContainsKey(soXe))
                {
                    trangThaiXe[soXe] = TrangThaiXe.Ranh;
                }
            }

            // 6. Làm mới bảng để hiện màu xanh/đỏ
            dgvThi.Refresh();
            HienThiThongTinThiSinh(ts);
        }

        private void mnuThayDoiXe_Click(object sender, EventArgs e)
        {
            if (_currentRowIndex < 0) return;

            var ts = dgvThi.Rows[_currentRowIndex].DataBoundItem as ThiSinhDangThi;
            if (ts == null) return;

            Capxe frm = new Capxe(xes, ts.SoBaoDanh, ts.HangGPLX);
            frm.StartPosition = FormStartPosition.CenterParent;
            frm.ShowDialog();

            Moto xeChon = frm.XeDuocChon;

            // Trả xe cũ về trạng thái "Rảnh"
            trangThaiXe[ts.Xe] = TrangThaiXe.Ranh;

            // Gán xe mới
            ts.Xe = xeChon.Name;

            // Đánh dấu xe mới "Sẵn sàng"
            trangThaiXe[ts.Xe] = TrangThaiXe.SanSang;

            dgvThi.Refresh();
            HienThiThongTinThiSinh(ts);
        }

        private enum TrangThaiXe
        {
            Ranh,      // chưa ai dùng / dùng xong
            SanSang,   // đã chuẩn bị cho 1 thí sinh
            DangThi    // đang thi
        }

        // Lưu trạng thái theo số xe, ví dụ "1", "2", "10"...
        private Dictionary<string, TrangThaiXe> trangThaiXe
            = new Dictionary<string, TrangThaiXe>();

        // Trạng thái xe
        
        private BindingList<ChiTietLoi> dsChiTietLoi = new BindingList<ChiTietLoi>();

        private void kiểmTraKếtNốiXeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fxe = new QuanLyXe(xes);
            fxe.ShowDialog();
        }

        private void kiemtraketnoisan_Click(object sender, EventArgs e)
        {
            fsan = new QLiSan(sanList);
            fsan.Show();
        }

        /// <summary>
        /// Hàm tạo phiên thi cho 1 thí sinh
        /// </summary>
        /// <param name="sbd">Số báo danh của thí sinh</param>
        /// <param name="deviceId">ID của thiết bị thi</param>
        /// <returns></returns>

        int CreateSession(int sbd, int deviceId)
        {
            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();

                string sql = @"
                    INSERT INTO Sessions (SBD, DeviceID, StartTime, Duration, Time, Mark)
                    OUTPUT INSERTED.ID
                    VALUES (@SBD, @DeviceID, GETDATE(), 80, 1, 100)";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@SBD", sbd);
                    cmd.Parameters.AddWithValue("@DeviceID", deviceId);

                    int sessionId = (int)cmd.ExecuteScalar();
                    return sessionId;
                }
            }
        }
        /// <summary>
        /// Hàm cập nhật điểm khi có lỗi xảy ra
        /// 
        /// </summary>
        /// <param name="sessionId">Phiên thi</param>
        /// <param name="mark">Điểm còn lại</param>
        /// <param name="isFinish">đánh dấu đã kết thúc bài thi</param>
        private void UpdateMarkSession(int sessionId, int mark, bool? isFinish = null)
        {
            // SQL base
            string sql = @"
                UPDATE Sessions
                SET Mark = @Mark
            ";

            // Nếu isFinish có giá trị → thêm vào SQL
            if (isFinish.HasValue)
                sql += ", IsFinish = @IsFinish";

            sql += " WHERE ID = @SessionId";

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Mark", mark);
                    cmd.Parameters.AddWithValue("@SessionId", sessionId);

                    if (isFinish.HasValue)
                        cmd.Parameters.AddWithValue("@IsFinish", isFinish.Value ? 1 : 0);

                    cmd.ExecuteNonQuery();
                }
            }
        }


        private void btnFindDaThi_Click(object sender, EventArgs e)
        {
            GridDaThi();
            LoadThiSinhDaThi();
        }
        public void GridDaThi()
        {
            dgvThi.ContextMenuStrip = mnuShowDaThi;
            lblDangThi.Text = "ĐÃ THI";
            dgvThi.Columns.Clear();
            dgvThi.AutoGenerateColumns = false;

            // ===== SBD =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "SBD",
                DataPropertyName = "SBD",
                Width = 70
            });
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                Name = "SessionID",
                HeaderText = "SessionID",
                DataPropertyName = "SessionID",
                Width = 70,
                Visible = false
            });

            // ===== Họ đệm =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Họ đệm",
                DataPropertyName = "HoDem",
                Width = 120
            });

            // ===== Tên =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Tên",
                DataPropertyName = "Ten",
                Width = 80
            });

            // ===== Hạng =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Hạng",
                DataPropertyName = "HangGPLX",
                Width = 70
            });

            // ===== Xe (DeviceID) =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Xe",
                DataPropertyName = "DeviceID",
                Width = 50
            });

            // ===== Thời gian bắt đầu =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Bắt đầu",
                DataPropertyName = "StartTime",
                Width = 130
            });

            // ===== Số lần thi =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Số lần",
                DataPropertyName = "SoLanThi",
                Width = 60
            });

            // ===== Điểm còn lại =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Điểm",
                DataPropertyName = "Mark",
                Width = 60
            });

            // ======================
            // 5 Bài thi (pivot lỗi)
            // ======================

            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Sẵn sàng",
                DataPropertyName = "DiemTru_BT1",
                Width = 80
            });

            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Số 8",
                DataPropertyName = "DiemTru_BT4",
                Width = 90
            });

            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Đ. thẳng",
                DataPropertyName = "DiemTru_BT5",
                Width = 90
            });

            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Ziczac",
                DataPropertyName = "DiemTru_BT6",
                Width = 90
            });

            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Gồ ghề",
                DataPropertyName = "DiemTru_BT7",
                Width = 90
            });

            // ======================
            // Style
            // ======================
            dgvThi.ReadOnly = true;
            dgvThi.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvThi.DefaultCellStyle.SelectionBackColor = Color.White;
            dgvThi.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvThi.AllowUserToResizeRows = false;
            dgvThi.AllowUserToResizeColumns = false;
        }
        private DataTable LoadThiSinhDaThi()
        {
            DataTable dt = new DataTable();

            string sql = @"
                SELECT 
                    TS.SBD,
                    S.ID as SessionID,
                    TS.HoDem,
                    TS.Ten,
                    TS.HangGPLX,
                    S.DeviceID,
                    S.StartTime,
                    S.Duration,
                    S.Time as SoLanThi,
                    S.Mark,

                    ISNULL([1], 0) AS DiemTru_BT1,
                    ISNULL([4], 0) AS DiemTru_BT4,
                    ISNULL([5], 0) AS DiemTru_BT5,
                    ISNULL([6], 0) AS DiemTru_BT6,
                    ISNULL([7], 0) AS DiemTru_BT7

                FROM Sessions S
                JOIN Thisinhsh TS ON TS.SBD = S.SBD
                LEFT JOIN (
                    SELECT 
                        SessionID,
                        BaiThiID,
                        SUM(DiemTru) AS dt
                    FROM ChitietLoi
                    GROUP BY SessionID, BaiThiID
                ) AS C
                PIVOT (
                    SUM(dt) FOR BaiThiID IN ([1], [4], [5], [6], [7])
                ) AS P ON S.ID = P.SessionID
                WHERE S.IsFinish = 1
                ORDER BY S.StartTime DESC";

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();
                using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
                {
                    da.Fill(dt);
                }
            }
            dgvThi.DataSource = null;
            dgvThi.DataSource = dt;
            return dt;
        }

        private void btnInKetQua_Click(object sender, EventArgs e)
        {
            if (selectedSessionId <= 0)
            {
                MessageBox.Show("Bạn chưa chọn thí sinh để in kết quả!",
                                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            FormInKetQua f = new FormInKetQua(selectedSessionId);
            f.ShowDialog();
        }


        public void GridThi()
        {
            dgvThi.ContextMenuStrip= null;
            dgvThi.Columns.Clear();
            dgvThi.AutoGenerateColumns = false;

            // ===== CỘT TRẠNG THÁI XE (CheckBox 3 trạng thái) =====
            var colTrangThai = new DataGridViewTextBoxColumn()
            {
                Name = "colTrangThaiXe",
                HeaderText = "",
                Width = 40,
                DataPropertyName = "DaKiemTraXe", // Vẫn giữ binding để lấy dữ liệu nếu cần
                ReadOnly = true // Không cho người dùng gõ chữ vào
            };
            dgvThi.Columns.Add(colTrangThai);

            // ===== CỘT XE =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Xe",
                DataPropertyName = "Xe",
                Width = 50
            });

            // ===== CỘT HỌ ĐỆM =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Họ đệm",
                DataPropertyName = "HoDem"
            });

            // ===== CỘT TÊN =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Tên",
                DataPropertyName = "Ten"
            });

            // ===== CỘT SBD =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "SBD",
                DataPropertyName = "SoBaoDanh"
            });

            // ===== CỘT HẠNG GPLX =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Hạng",
                DataPropertyName = "HangGPLX"
            });

            // ===== CỘT ĐIỂM (dùng DiemConLai) =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Điểm",
                DataPropertyName = "DiemConLai",
                Width = 60
            });

            // ===== CỘT THỜI GIAN (CHO TIMER) =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                Name = "colThoiGian",
                HeaderText = "Thời gian",
                ReadOnly = true,
                Width = 80
            });

            // ===== BUTTON CHỐNG CHÂN =====
            var btnChongChan = new DataGridViewButtonColumn();
            btnChongChan.HeaderText = "Chống chân";
            btnChongChan.Text = "Chống chân";
            btnChongChan.UseColumnTextForButtonValue = true;
            dgvThi.Columns.Add(btnChongChan);

            // ===== BUTTON ĐỔ XE =====
            var btnDoXe = new DataGridViewButtonColumn();
            btnDoXe.HeaderText = "Đổ xe";
            btnDoXe.Text = "Đổ xe";
            btnDoXe.UseColumnTextForButtonValue = true;
            dgvThi.Columns.Add(btnDoXe);

            // ===== BUTTON NGOÀI HÌNH =====
            var btnNgoaiHinh = new DataGridViewButtonColumn();
            btnNgoaiHinh.HeaderText = "Ngoài hình";
            btnNgoaiHinh.Text = "Ngoài hình";
            btnNgoaiHinh.UseColumnTextForButtonValue = true;
            dgvThi.Columns.Add(btnNgoaiHinh);

            // ===== CỘT SỐ 8 =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Số 8",
                DataPropertyName = "So8"
            });

            // ===== CỘT ĐƯỜNG THẲNG =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Đường thẳng",
                DataPropertyName = "DuongThang"
            });

            // ===== CỘT ZIC ZẮC =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Zic zắc",
                DataPropertyName = "ZicZac"
            });

            // ===== CỘT GỒ GHỀ =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Gồ ghề",
                DataPropertyName = "GoGhe"
            });

            // Gỡ handler cũ (nếu có) để tránh gắn nhiều lần
            dgvThi.CellPainting += dgvThi_CellPainting; // Thêm dòng này

            dgvThi.ReadOnly = true;
            dgvThi.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvThi.DefaultCellStyle.SelectionBackColor = Color.White;
            dgvThi.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvThi.AllowUserToResizeRows = false;
            dgvThi.AllowUserToResizeColumns = false;

        }

        private void btnFindDangThi_Click(object sender, EventArgs e)
        {
            lblDangThi.Text = "ĐANG THI";
            GridThi();
            dgvThi.DataSource = ds;
        }

        private void InsertErrorToDatabase(
            long sbd,
            int sessionId,
            string ten,
            string xe,
            string suKien,
            int diemTru,
            string chiTiet,
            int? faultId = null,
            int? baiThiId = null)
        {
            string sql = @"
                INSERT INTO ChiTietLoi 
                (SoBaoDanh, SessionID, Ten, Xe, ThoiGian, SuKien, DiemTru, ChiTiet, FaultID, BaiThiID)
                VALUES 
                (@SBD, @SessionID, @Ten, @Xe, GETDATE(), @SuKien, @DiemTru, @ChiTiet, @FaultID, @BaiThiID)";

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@SBD", SqlDbType.BigInt).Value = sbd;
                    cmd.Parameters.Add("@SessionID", SqlDbType.Int).Value = sessionId;
                    cmd.Parameters.Add("@Ten", SqlDbType.NVarChar, 100).Value = (object)ten ?? DBNull.Value;
                    cmd.Parameters.Add("@Xe", SqlDbType.NVarChar, 20).Value = (object)xe ?? DBNull.Value;
                    cmd.Parameters.Add("@SuKien", SqlDbType.NVarChar, 100).Value = suKien;
                    cmd.Parameters.Add("@DiemTru", SqlDbType.Int).Value = diemTru;
                    cmd.Parameters.Add("@ChiTiet", SqlDbType.NVarChar, 255).Value = (object)chiTiet ?? DBNull.Value;
                    cmd.Parameters.Add("@FaultID", SqlDbType.Int).Value = (object)faultId ?? DBNull.Value;
                    cmd.Parameters.Add("@BaiThiID", SqlDbType.Int).Value = (object)baiThiId ?? DBNull.Value;

                    cmd.ExecuteNonQuery();
                }
            }

            // Thêm vào DataGridView / ObservableCollection
            SafeUI(() =>
            {
                dsChiTietLoi.Add(new ChiTietLoi()
                {
                    ThoiGian = DateTime.Now,
                    SuKien = suKien,
                    DiemTru = diemTru,
                    ChiTiet = chiTiet,
                });
            });
        }

        private void SafeUI(Action action)
        {
            if (this.InvokeRequired)
                this.Invoke(action);
            else
                action();
        }

    }
}