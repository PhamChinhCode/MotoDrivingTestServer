using ImageMagick;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
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
        private string ModeTabFilter = "DangThi";
        private bool isWaitingImage = false;
        private string kySatHachFolder = "";
        private Image _defaultImage;

        private List<San> sanList = new List<San>();

        // Theo dõi thí sinh nào đang giữ xe nào
        private Dictionary<string, ThiSinhDangThi> xeDangDung = new Dictionary<string, ThiSinhDangThi>();
        private Dictionary<string, TrangThaiXe> trangThaiXe = new Dictionary<string, TrangThaiXe>();
        BindingList<ThiSinhDangThi> ds = new BindingList<ThiSinhDangThi>();
        private List<Moto> xes = new List<Moto>();
        private QuanLyXe fxe;

        private List<Moto> LoadMotoFromDatabase()
        {
            // lấy tạm dictionary để check tồn tại
            Dictionary<byte, Moto> oldMap = xes.ToDictionary(x => x.Id, x => x);
            List<Moto> newList = new List<Moto>();

            string sql = "SELECT ID, Name, IPAddress FROM Devices ORDER BY ID";

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {

                    while (rd.Read())
                    {
                        byte id = Convert.ToByte(rd["ID"]);

                        if (oldMap.ContainsKey(id))
                        {
                            // --- UPDATE XE CŨ ---
                            Moto existing = oldMap[id];
                            existing.Name = rd["Name"].ToString();
                            existing.Ip = rd["IPAddress"].ToString();

                            newList.Add(existing);
                        }
                        else
                        {
                            // --- XE MỚI ---
                            newList.Add(new Moto()
                            {
                                Id = id,
                                Name = rd["Name"].ToString(),
                                Ip = rd["IPAddress"].ToString(),
                                Port = 123
                            });
                        }
                    }

                    xes = newList;
                }
            }
            return newList;
        }

        private QLiSan fsan;
        public Form1()
        {
            InitializeComponent();
            xes = LoadMotoFromDatabase();
            fxe = new QuanLyXe(xes);

            sanList = new List<San>();
            sanList.Add(new San("San 1", "192.168.0.150", 123));
            sanList[0].Connect();
            //fxe.ShowDialog();
            //xes[0].Connect();

            GridThi();
            dgvThi.AutoGenerateColumns = false;
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
            dgvThi.DataSource = ds;
            dgvKetQuaChung.AutoGenerateColumns = false;
            dgvNhatKyLoi.AutoGenerateColumns = false;
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
            _defaultImage = pictureBox1.Image;
            this.dBKySatHachTableAdapter.Fill(this.mCDV2A1DataSet2.DBKySatHach);
            // GIỮ NGUYÊN ĐOẠN NÀY NHƯ BẠN YÊU CẦU
            //this.examineesTableAdapter.Fill(this.mCDV2A1DataSet.Examinees);
            LoadComboboxKySatHach();
            Loaf();                     // đọc từ SQL vào dgv + nạp vào ExamDataManager
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
            kySatHachFolder = comboBox1.SelectedValue.ToString();
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
            LoadMotoFromDatabase();
            Capxe frm = new Capxe(xes, x.SoBaoDanh, x.HangGPLX);
            frm.StartPosition = FormStartPosition.CenterParent;

            DialogResult result = frm.ShowDialog();
            if (result != DialogResult.OK || frm.XeDuocChon == null)
                return;

            Moto xeChon = frm.XeDuocChon;

            int sbd = Convert.ToInt32(drv["SBD"]);
            string hang = drv["HangGPLX"].ToString();
            string hoDem = drv["Hodem"].ToString();
            string ten = drv["Ten"].ToString();
            string soXe = xeChon.Id.ToString();

            // Kiểm tra trạng thái xe trong từ điển
            if (!trangThaiXe.ContainsKey(soXe))
                trangThaiXe[soXe] = TrangThaiXe.Ranh;

            if (trangThaiXe[soXe] != TrangThaiXe.Ranh)
            {
                MessageBox.Show($"{soXe} đang được dùng cho thí sinh khác.\nVui lòng chọn xe khác.",
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
                DiemConLai = 100,
                DiemTru = 0,
                SoLoi = 0,

                // --- QUAN TRỌNG: KHÔNG ĐƯỢC DÙNG DateTime.Now ---
                GioBatDau = DateTime.MinValue, // Để Timer không tự chạy
                TrangThai = "Đã cấp xe",       // Trạng thái chờ
                DaKiemTraXe = true,
                BaiThiHienTaiID = 0
            };
            ds.Add(d);
            CapXeChoThiSinh(d, xeChon);
            // 6. Tạo SESSION trong database
            d.SessionID = CreateSession(d.SoBaoDanh, Convert.ToInt32(d.XeObj.Id));
        }
        private void CapXeChoThiSinh(ThiSinhDangThi ts, Moto xeChon)
        {
            if (ts == null || xeChon == null)
                return;

            string soXe = xeChon.Id.ToString();

            // 1. Nếu xe mới đang phục vụ người khác → gỡ sự kiện người cũ
            if (xeDangDung.TryGetValue(soXe, out var oldD))
            {
                CleanupEvents(oldD);
                ds.Remove(oldD);
            }

            // 3. Gán xe mới vào thí sinh
            ts.Xe = soXe;
            ts.XeObj = xeChon;

            // 5. Lưu xe đang dùng
            xeDangDung[ts.Xe] = ts;

            // ===========================================================
            // 6. GẮN EVENT CHO XE MỚI
            // ===========================================================

            // a. Event trạng thái bài thi
            ts.XeChangedHandler = () =>
            {
                byte st = xeChon.Status;
                byte errId = xeChon.ErrorId;

                BaiThiHelper.CapNhatBaiThiHienTai(ts, st);
                if (!BaiThiHelper.IsInValidContest1_4(st)) return;

                // HOÀN THÀNH TẤT CẢ BÀI
                if (st == ConstantKeys.STATUS_FREE &&
                    BaiThiHelper.IsInValidContest1_4(ts.LastStatus))
                {
                    ChupAnh(xeChon, ts);

                    InsertErrorToDatabase(
                        ts.SoBaoDanh, ts.SessionID,
                        $"{ts.HoDem} {ts.Ten}", ts.Xe,
                        "Kết thúc bài thi", 0,
                        "Hoàn thành bài thi",
                        null, null, ts
                    );

                    UpdateMarkSession(ts.SessionID, ts.DiemConLai, true);
                    Task.Delay(400).ContinueWith(_ =>
                    {
                        CleanupEvents(ts);
                        ds.Remove(ts);
                    });
                    return;
                }

                // VÀO BÀI MỚI
                if (st != ts.LastStatus && BaiThiHelper.IsInValidContest1_4(st))
                {
                    string tenBai = BaiThiHelper.GetName(st);
                    ts.TrangThai = "Đang thi";

                    ChupAnh(xeChon, ts);

                    ts.LastErrorRecordId = InsertErrorToDatabase(
                        ts.SoBaoDanh, ts.SessionID,
                        $"{ts.HoDem} {ts.Ten}", ts.Xe,
                        $"Vào bài: {tenBai}", 0,
                        $"Bắt đầu bài {tenBai}",
                        null, ts.BaiThiHienTaiID, ts
                    );

                    if (ts.GioBatDau == DateTime.MinValue)
                        ts.GioBatDau = DateTime.Now;

                    ts.LastStatus = st;
                    ts.LastError = 0;
                }

                // LỖI
                if (errId != 0 &&
                    errId != ts.LastError &&
                    FaultDefinitions.FaultByErrorId.TryGetValue(errId, out var fault))
                {

                    //
                    if (errId == 229)
                    {
                        return;
                    }
                    ts.LastError = errId;

                    string moTa = BaiThiHelper.GetName(st);
                    string chiTiet = $"{fault.moTa} – Tại bài: {moTa}";

                    InsertErrorToDatabase(
                        ts.SoBaoDanh, ts.SessionID,
                        $"{ts.HoDem} {ts.Ten}", ts.Xe,
                        fault.moTa, fault.diemTru, chiTiet,
                        fault.id, ts.BaiThiHienTaiID, ts
                    );

                    ts.SoLoi++;
                    ts.DiemTru += fault.diemTru;
                    ts.DiemConLai -= fault.diemTru;

                    if (ts.LastSentMark != ts.DiemConLai)
                    {
                        UpdateMarkSession(ts.SessionID, ts.DiemConLai);
                        ts.XeObj.sendCommand(ConstantKeys.MARK_KEY, ConstantKeys.BYTE_SET, (byte)ts.DiemConLai);
                        ts.LastSentMark = ts.DiemConLai;
                    }
                    CheckStopCondition(ts);
                }
                else ts.LastError = 0;

                SafeUI(() =>
                {
                    dgvThi.Refresh();
                    HienThiThongTinThiSinh(ts);
                });
            };

            xeChon.OnChanged += ts.XeChangedHandler;

            // b. Event nhận ảnh
            ts.XeImageHandler = (byte[] bytes) =>
            {
                if (!ts.WaitingImage) return;

                ts.WaitingImage = false;
                ts.LastImageBytes = bytes;

                string rootFolder = kySatHachFolder;

                if (ts.LastErrorRecordId > 0 && !string.IsNullOrEmpty(rootFolder))
                {
                    Task.Run(() =>
                    {
                        string path = ImageManager.SaveImageToFile(
                            rootFolder, ts.Ten, ts.LastErrorRecordId, bytes
                        );
                        UpdateChiTietLoiImage(ts.LastErrorRecordId, path);
                    });
                }
            };

            xeChon.OnImageReceived += ts.XeImageHandler;

            // c. Event Sân
            ts.SanChangedHandler = () =>
            {
                San san = sanList[0];
                int baiId = BaiThiHelper.GetId(xeChon.Status);
                string baiMoTa = BaiThiHelper.GetName(xeChon.Status);

                List<(string name, bool val)> sensors = new List<(string name, bool val)>();
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

                foreach (var s in sensors)
                {
                    if (s.val)
                    {
                        string chiTiet = $"Đè vạch {s.name} – Bài: {baiMoTa}";

                        InsertErrorToDatabase(
                            ts.SoBaoDanh, ts.SessionID,
                            $"{ts.HoDem} {ts.Ten}", ts.Xe,
                            chiTiet, 5, chiTiet,
                            null, baiId, ts
                        );
                    }
                }
            };

            sanList[0].OnChanged += ts.SanChangedHandler;

            // Cập nhật UI
            dgvThi.Refresh();
            HienThiThongTinThiSinh(ts);
        }
        void ChupAnh(Moto xe, ThiSinhDangThi ts)
        {
            if (isWaitingImage) return;

            isWaitingImage = true;
            ts.WaitingImage = true;

            xe.sendCommand(ConstantKeys.IMAGE_KEY,
                           ConstantKeys.BYTE_GET,
                           ConstantKeys.KEY_NULL);

            Task.Delay(1000).ContinueWith(_ => isWaitingImage = false);
        }
        private void CheckStopCondition(ThiSinhDangThi d)
        {
            if (d.DiemConLai >= 80)
                return;

            // 1) Gỡ event NGAY LẬP TỨC để xe không bắn thêm trạng thái sai
            CleanupEvents(d);

            // 2) STOP XE
            d.XeObj?.sendCommand(
                ConstantKeys.CONTROL_KEY,
                ConstantKeys.BYTE_SET,
                ConstantKeys.CONTROL_STOP
            );

            // 3) LOG kết thúc
            InsertErrorToDatabase(
                d.SoBaoDanh, d.SessionID,
                $"{d.HoDem} {d.Ten}", d.Xe,
                "Kết thúc bài thi",
                0,
                "Thí sinh đạt điểm dưới 80",
                null, d.BaiThiHienTaiID, d
            );

            // 4) Cập nhật session (IsFinish = true)
            UpdateMarkSession(d.SessionID, d.DiemConLai, true);

            // 5) XÓA khỏi danh sách đang thi (PHẢI trong UI thread)
            SafeUI(() =>
            {
                ds.Remove(d);
                HienThiThongTinThiSinh(d);
            });
        }

        public void CleanupEvents(ThiSinhDangThi d)
        {
            if (d == null) return;
            if (d.XeObj != null)
            {
                if (d.XeChangedHandler != null)
                    d.XeObj.OnChanged -= d.XeChangedHandler;

                if (d.XeImageHandler != null)
                    d.XeObj.OnImageReceived -= d.XeImageHandler;

                d.XeObj.Status = ConstantKeys.STATUS_FREE;
                d.XeObj.ErrorId = 0;
            }

            if (sanList.Count > 0 && d.SanChangedHandler != null)
                sanList[0].OnChanged -= d.SanChangedHandler;

            d.LastStatus = ConstantKeys.STATUS_FREE;
            d.LastError = 0;
            d.LastErrorRecordId = 0;
            d.WaitingImage = false;
            d.LastImageBytes = null;
            d.GioBatDau = DateTime.MinValue;
            d.GioKetThuc = DateTime.MinValue;

            d.XeChangedHandler = null;
            d.XeImageHandler = null;
            d.SanChangedHandler = null;

            trangThaiXe[d.Xe] = TrangThaiXe.Ranh;

            if (!string.IsNullOrEmpty(d.Xe) && trangThaiXe.ContainsKey(d.Xe))
                trangThaiXe[d.Xe] = TrangThaiXe.Ranh;
        }

        private void dgvThi_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var obj = dgvThi.Rows[e.RowIndex].DataBoundItem;
            ThiSinhDangThi ts = null;
            ThiSinhDaThi daThi = null;
            if (ModeTabFilter == "DangThi")
                ts = obj as ThiSinhDangThi;
            else if (ModeTabFilter == "DaThi")
                daThi = obj as ThiSinhDaThi;

            // Nếu không cast được → thoát
            if (ts == null && daThi == null)
                return;


            if (ModeTabFilter == "DaThi")
            {
                HienThiThongTinThiSinhDaThi(daThi);
                return;
            }

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
            if (!BaiThiHelper.IsInValidContest1_4(ts.XeObj.Status))
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
                baiThiId,
                ts
            );

            CheckStopCondition(ts);

            dgvThi.Refresh();
        }
        private void dgvThi_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var ts = dgvThi.Rows[e.RowIndex].DataBoundItem as ThiSinhDangThi;
            if (ts == null) return;

            // ====== 1. VẼ TRẠNG THÁI XE (DOT COLOR) ======
            if (dgvThi.Columns[e.ColumnIndex].Name == "colTrangThaiXe")
            {
                using (Brush backBrush = new SolidBrush(Color.White))
                {
                    e.Graphics.FillRectangle(backBrush, e.CellBounds);
                }

                e.Paint(e.CellBounds, DataGridViewPaintParts.Border);

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

                e.Handled = true;
                return; // CHỈ return ở đây
            }
        }

        private void HienThiThongTinThiSinh(ThiSinhDangThi ts)
        {
            if (ts.DiemConLai < 80)
                ts.TrangThai = "Không đạt";
            lblHoTen.Text = $"{ts.HoDem} {ts.Ten}";
            lblSBD.Text = ts.SoBaoDanh.ToString();
            lblSoLoi.Text = ts.SoLoi.ToString();
            lblDiemTruu.Text = ts.DiemTru.ToString();  // Cập nhật điểm trừ
            lblDiemConLai.Text = ts.DiemConLai.ToString();  // Cập nhật điểm còn lại
            lblTrangThai.Text = ts.TrangThai;
            dsChiTietLoi.Clear();
            foreach (var loi in ts.NhatKyLoi)
                dsChiTietLoi.Add(loi);
            dgvchitietloi.Refresh();
        }
        private void HienThiThongTinThiSinhDaThi(ThiSinhDaThi ts)
        {
            pictureBox1.Image = Image.FromFile(ts.ImagePath);
            pictureBox1.BackColor = ColorTranslator.FromHtml("#0062FD");
            lblHoTen.Text = $"{ts.HoDem} {ts.Ten}";
            lblSBD.Text = ts.SBD.ToString();

            // Trạng thái: Đạt / Không đạt (dựa vào Mark)
            lblTrangThai.Text = ts.Mark >= 80 ? "Đạt" : "Không đạt";
            int tongDiemTru = ts.DiemTru_BT1 + ts.DiemTru_BT4 + ts.DiemTru_BT5 + ts.DiemTru_BT6 + ts.DiemTru_BT7;

            lblDiemTruu.Text = tongDiemTru.ToString();
            lblDiemConLai.Text = (100 - tongDiemTru).ToString();

            lblSoLoi.Text = ts.NhatKyLoi?.Count.ToString() ?? "0";

            // Load chi tiết lỗi
            dsChiTietLoi.Clear();  // không reset UI
            foreach (var loi in ts.NhatKyLoi)
                dsChiTietLoi.Add(loi);
            dgvchitietloi.Refresh();
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
                "Chuẩn bị",
                null, null, ts
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
                "Bắt đầu", null, null, ts
            );

            // Bật timer nếu chưa chạy
            if (!timerCapNhatThoiGian.Enabled)
                timerCapNhatThoiGian.Start();
            dgvThi.Refresh();
        }
        private void mnuKetThuc_Click(object sender, EventArgs e)
        {
            if (_currentRowIndex < 0) return;
            var d = dgvThi.Rows[_currentRowIndex].DataBoundItem as ThiSinhDangThi;
            if (d == null) return;
            // STOP XE
            if (d.XeObj != null)
            {
                try
                {
                    d.XeObj.sendCommand(
                        ConstantKeys.CONTROL_KEY,
                        ConstantKeys.BYTE_SET,
                        ConstantKeys.CONTROL_STOP
                    );
                }
                catch { /* ignore lỗi nếu xe đã mất kết nối */ }
            }
            // LOG kết thúc
            InsertErrorToDatabase(
                d.SoBaoDanh, d.SessionID,
                $"{d.HoDem} {d.Ten}", d.Xe,
                "Kết thúc bài thi",
                0,
                "Giám khảo đã kết thúc bài thi",
                null, d.BaiThiHienTaiID, d
            );

            // Cập nhật session
            UpdateMarkSession(d.SessionID, d.DiemConLai, true);
            // Gỡ các event liên quan đến TS này
            CleanupEvents(d);
            // Remove khỏi danh sách thi
            SafeUI(() =>
            {
                ds.Remove(d);
            });
        }
        private void mnuThayDoiXe_Click(object sender, EventArgs e)
        {
            if (_currentRowIndex < 0) return;

            var ts = dgvThi.Rows[_currentRowIndex].DataBoundItem as ThiSinhDangThi;
            if (ts == null) return;

            LoadMotoFromDatabase();
            Capxe frm = new Capxe(xes, ts.SoBaoDanh, ts.HangGPLX);
            frm.StartPosition = FormStartPosition.CenterParent;

            if (frm.ShowDialog() != DialogResult.OK)
                return;

            Moto xeChon = frm.XeDuocChon;
            if (xeChon == null) return;

            // 1. Gỡ event xe cũ (nếu có)
            CleanupEvents(ts);

            // 2. Trả xe cũ về trạng thái rảnh
            if (!string.IsNullOrEmpty(ts.Xe) && trangThaiXe.ContainsKey(ts.Xe))
                trangThaiXe[ts.Xe] = TrangThaiXe.Ranh;

            // 3. Gán xe mới
            ts.Xe = xeChon.Id.ToString();
            ts.XeObj = xeChon;

            // 5. Gắn lại event cho xe mới (giống lúc cấp xe)
            CapXeChoThiSinh(ts, xeChon);

            // 6. Cập nhật UI
            dgvThi.Refresh();
            HienThiThongTinThiSinh(ts);
        }

        // Trạng thái xe

        private BindingList<ChiTietLoi> dsChiTietLoi = new BindingList<ChiTietLoi>();

        private void kiểmTraKếtNốiXeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadMotoFromDatabase();
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
            ModeTabFilter = "DaThi";
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
        private void LoadThiSinhDaThi()
        {
            string sql = @"
                SELECT 
                    TS.SBD,
                    S.ID as SessionID,
                    TS.HoDem,
                    TS.Ten,
                    TS.HangGPLX,
                    TS.AnhChanDung,
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
                ORDER BY S.StartTime DESC;
            ";

            var list = new List<ThiSinhDaThi>();

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        var ts = new ThiSinhDaThi()
                        {
                            SBD = Convert.ToInt64(rd["SBD"]),
                            SessionID = Convert.ToInt32(rd["SessionID"]),
                            HoDem = rd["HoDem"].ToString(),
                            Ten = rd["Ten"].ToString(),
                            HangGPLX = rd["HangGPLX"].ToString(),
                            ImagePath = rd["AnhChanDung"].ToString(),
                            DeviceID = rd["DeviceID"].ToString(),
                            StartTime = Convert.ToDateTime(rd["StartTime"]),
                            Duration = Convert.ToInt32(rd["Duration"]),
                            SoLanThi = Convert.ToInt32(rd["SoLanThi"]),
                            Mark = Convert.ToInt32(rd["Mark"]),
                            DiemTru_BT1 = Convert.ToInt32(rd["DiemTru_BT1"]),
                            DiemTru_BT4 = Convert.ToInt32(rd["DiemTru_BT4"]),
                            DiemTru_BT5 = Convert.ToInt32(rd["DiemTru_BT5"]),
                            DiemTru_BT6 = Convert.ToInt32(rd["DiemTru_BT6"]),
                            DiemTru_BT7 = Convert.ToInt32(rd["DiemTru_BT7"]),
                        };

                        // ⭐ Load NhatKyLoi cho mỗi thí sinh
                        ts.NhatKyLoi = LoadChiTietLoiTheoSession(ts.SessionID);

                        list.Add(ts);
                    }
                }
            }

            dgvThi.DataSource = null;
            dgvThi.DataSource = list;
        }

        private List<ChiTietLoi> LoadChiTietLoiTheoSession(int sessionId)
        {
            var list = new List<ChiTietLoi>();

            string sql = @"
                SELECT Id, ThoiGian, SuKien, DiemTru, ChiTiet
                FROM ChiTietLoi
                WHERE SessionID = @SessionID
                ORDER BY ThoiGian";

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@SessionID", sessionId);

                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            list.Add(new ChiTietLoi()
                            {
                                ThoiGian = rd.GetDateTime(1),
                                SuKien = rd.GetString(2),
                                DiemTru = rd.GetInt32(3),
                                ChiTiet = rd.IsDBNull(4) ? "" : rd.GetString(4)
                            });
                        }
                    }
                }
            }

            return list;
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
            dgvThi.ContextMenuStrip = null;
            dgvThi.Columns.Clear();
            dgvThi.AutoGenerateColumns = false;

            // ===== TRẠNG THÁI XE =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "TT",
                DataPropertyName = "TrangThai",
                Name = "colTrangThaiXe",
                Width = 40
            });

            // ===== XE =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Xe",
                DataPropertyName = "Xe",
                Width = 50
            });

            // ===== HỌ ĐỆM =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Họ đệm",
                DataPropertyName = "HoDem",
                Width = 120
            });

            // ===== TÊN =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Tên",
                DataPropertyName = "Ten",
                Width = 80
            });

            // ===== SBD =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "SBD",
                DataPropertyName = "SoBaoDanh",
                Width = 60
            });

            // ===== HẠNG GPLX =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Hạng",
                DataPropertyName = "HangGPLX",
                Width = 70
            });

            // ===== ĐIỂM CÒN LẠI =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Điểm",
                DataPropertyName = "DiemConLai",
                Width = 60
            });

            // ===== THỜI GIAN (chạy timer UI) =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Thời gian",
                Name = "colThoiGian",
                Width = 80,
                ReadOnly = true
            });

            // ===== BUTTON CHỐNG CHÂN ===== 
            dgvThi.Columns.Add(new DataGridViewButtonColumn()
            {
                HeaderText = "Chống chân",
                Text = "Chống chân",
                UseColumnTextForButtonValue = true,
                Width = 80
            });

            // ===== BUTTON ĐỔ XE =====
            dgvThi.Columns.Add(new DataGridViewButtonColumn()
            {
                HeaderText = "Đổ xe",
                Text = "Đổ xe",
                UseColumnTextForButtonValue = true,
                Width = 80
            });

            // ===== BUTTON NGOÀI HÌNH =====
            dgvThi.Columns.Add(new DataGridViewButtonColumn()
            {
                HeaderText = "Ngoài hình",
                Text = "Ngoài hình",
                UseColumnTextForButtonValue = true,
                Width = 80
            });

            // ===== SỐ 8 =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Số 8",
                DataPropertyName = "So8",
                Width = 70
            });

            // ===== ĐƯỜNG THẲNG =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Đường thẳng",
                DataPropertyName = "DuongThang",
                Width = 100
            });

            // ===== ZIC ZẮC =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Zic zắc",
                DataPropertyName = "ZicZac",
                Width = 80
            });

            // ===== GỒ GHỀ =====
            dgvThi.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Gồ ghề",
                DataPropertyName = "GoGhe",
                Width = 80
            });

            dgvThi.CellPainting += dgvThi_CellPainting;

            // ==== STYLE ====
            dgvThi.ReadOnly = true;
            dgvThi.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvThi.DefaultCellStyle.SelectionBackColor = Color.White;
            dgvThi.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvThi.AllowUserToResizeRows = false;
            dgvThi.AllowUserToResizeColumns = false;
        }


        private void btnFindDangThi_Click(object sender, EventArgs e)
        {
            ModeTabFilter = "DangThi";
            lblDangThi.Text = "ĐANG THI";
            GridThi();
            dgvThi.DataSource = null;
            dgvThi.DataSource = ds;
            pictureBox1.BackColor = SystemColors.Control;
            pictureBox1.Image = _defaultImage;
            dsChiTietLoi.Clear();
        }

        private int InsertErrorToDatabase(
            long sbd,
            int sessionId,
            string ten,
            string xe,
            string suKien,
            int diemTru,
            string chiTiet,
            int? faultId = null,
            int? baiThiId = null,
            ThiSinhDangThi ts = null)
        {
            int newId = 0;

            string sql = @"
                INSERT INTO ChiTietLoi 
                    (SoBaoDanh, SessionID, Ten, Xe, ThoiGian, SuKien, DiemTru, ChiTiet, FaultID, BaiThiID)
                OUTPUT INSERTED.Id
                VALUES 
                    (@SBD, @SessionID, @Ten, @Xe, GETDATE(), @SuKien, @DiemTru, @ChiTiet, @FaultID, @BaiThiID);
            ";

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

                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                        newId = Convert.ToInt32(result);
                }
            }

            // Đồng bộ UI
            SafeUI(() =>
            {
                var loi = new ChiTietLoi()
                {
                    ThoiGian = DateTime.Now,
                    SuKien = suKien,
                    DiemTru = diemTru,
                    ChiTiet = chiTiet,
                };

                ts.NhatKyLoi.Add(loi);  // thêm vào model
                dsChiTietLoi.Add(loi);
                dgvchitietloi.Refresh();
            });
            return newId;
        }
        public void UpdateChiTietLoiImage(int errorRecordId, string imagePath)
        {
            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();

                string sql = @"
                    UPDATE ChiTietLoi
                    SET ImagePath = @ImagePath
                    WHERE Id = @ErrorID
                ";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ErrorID", errorRecordId);
                    cmd.Parameters.AddWithValue("@ImagePath", imagePath ?? (object)DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }
        }


        private void SafeUI(Action action)
        {
            if (this.InvokeRequired)
                this.Invoke(action);
            else
                action();
        }

        private void quảnLýXeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Tạo mới Form3
            Form3 formQuanLy = new Form3();

            // Cách 1: Mở dạng hộp thoại (Khuyên dùng)
            // (Bắt buộc phải đóng Form3 thì mới bấm được Form1 => Tránh lỗi mở nhiều cái)
            formQuanLy.ShowDialog();

            // Cách 2: Mở song song (Nếu bạn muốn vừa xem Form1 vừa xem Form3)
            // formQuanLy.Show();
        }


    }

}