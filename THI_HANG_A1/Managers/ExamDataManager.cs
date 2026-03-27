using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using THI_HANG_A1.Models;

namespace THI_HANG_A1.Managers
{
    /// <summary>
    /// BỘ NÃO CỦA ỨNG DỤNG (ĐÃ NÂNG CẤP LÊN KHUNG TRUYỀN MỚI)
    /// </summary>
    public class ExamDataManager
    {
        // === HẰNG SỐ ===
        private const int SO_LUONG_CHO = 5;

        // === QUẢN LÝ CÁC MANAGER KHÁC ===
        private readonly SerialManager _serialManager;
        private readonly AudioManager _audioManager;

        // === DANH SÁCH DỮ LIỆU CỐT LÕI ===
        public BindingList<ThiSinh> DanhSachChinh { get; private set; }
        public BindingList<ThiSinh> DanhSachChuanBiThi { get; private set; }
        public BindingList<ThiSinh> DanhSachDangThi { get; private set; }
        public BindingList<ThiSinh> DanhSachKetQuaChung { get; private set; }
        public BindingList<LoiViPham> DanhSachLoiViPham { get; private set; }
        public List<Xe> DanhSachXe { get; private set; }

        // === EVENTS ĐỂ GỬI TÍN HIỆU VỀ FORM1 ===
        public event Action<string> OnLogMessage;
        public event Action<string, string> OnMessageBoxShow;
        public event EventHandler OnDataChanged;

        public ExamDataManager(SerialManager serialManager, AudioManager audioManager)
        {
            _serialManager = serialManager;
            _audioManager = audioManager;

            DanhSachChinh = new BindingList<ThiSinh>();
            DanhSachChuanBiThi = new BindingList<ThiSinh>();
            DanhSachDangThi = new BindingList<ThiSinh>();
            DanhSachKetQuaChung = new BindingList<ThiSinh>();
            DanhSachLoiViPham = new BindingList<LoiViPham>();

            DanhSachXe = new List<Xe>
            {
                new Xe { MaXe = "01", DangRanh = true },
                new Xe { MaXe = "02", DangRanh = true },
                new Xe { MaXe = "03", DangRanh = true },
                new Xe { MaXe = "04", DangRanh = true }
            };

            // Kết nối sự kiện: Khi SerialManager báo có 1 khung truyền HỢP LỆ...
            _serialManager.OnFrameReceived += GiaiMaKhungTruyen;
        }

        public void NapDuLieuMoi(List<ThiSinh> danhSachMoi)
        {
            DanhSachChinh.Clear();
            DanhSachChuanBiThi.Clear();
            DanhSachDangThi.Clear();
            DanhSachKetQuaChung.Clear();
            DanhSachLoiViPham.Clear();

            foreach (var ts in danhSachMoi)
            {
                DanhSachChinh.Add(ts);
            }

            CapNhatDanhSachChuanBi();
            OnDataChanged?.Invoke(this, EventArgs.Empty);
        }
        public List<Xe> GetXeRanh()
        {
            return DanhSachXe.Where(x => x.DangRanh).ToList();
        }


        /// <summary>
        /// Logic chính: Giao xe cho thí sinh (ĐÃ CẬP NHẬT)
        /// </summary>
        public void GiaoXeChoThiSinh(ThiSinh ts, Xe xe)
        {
            if (ts == null || xe == null) return;
            ts.MaXeDaChon = xe.MaXe;
            xe.DangRanh = false;
            xe.SBDThiSinhHienTai = ts.SBD;
            xe.GiaiDoan = 1;
            ts.DiemTongHop = 100;
            ts.LoiChamVach = 0;
            ts.LoiChetMay = 0;
            ts.LoiKhongXiNhan = 0;
            ts.LoiNgaDo = 0;
            ts.LoiChaySaiHinh = 0;
            ts.LoiQuaTocDo = 0;
            DanhSachDangThi.Add(ts);
            DanhSachChuanBiThi.Remove(ts);

            //_audioManager.PhatAmThanh(ts, "ChuanBi");

            // GỬI LỆNH VỚI PAYLOAD 1-BYTE
            if (byte.TryParse(xe.MaXe, out byte maXeByte))
            {
                _serialManager.GuiKhungTruyen(SerialConstants.KEY_LENH_DIEU_KHIEN, SerialConstants.BYTE_SET, maXeByte,
                    new byte[] { SerialConstants.CMD_CHUAN_BI_THI });
            }

            OnMessageBoxShow?.Invoke($"Đã giao xe {xe.MaXe} cho thí sinh {ts.HoTen}.\nHãy chọn thí sinh trong bảng 'ĐANG THI' và nhấn 'Bắt Đầu'.", "Giao xe thành công");

            CapNhatDanhSachChuanBi();
            OnDataChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Logic chính: Bắt đầu một lượt thi (ĐÃ CẬP NHẬT)
        /// </summary>
        public void BatDauLuotThi(ThiSinh ts)
        {
            if (ts.ThoiGianBatDau != DateTime.MinValue)
            {
                OnMessageBoxShow?.Invoke("Thí sinh này đã bắt đầu bài thi rồi.", "Thông báo");
                return;
            }

            ts.ThoiGianBatDau = DateTime.Now;
            //_audioManager.PhatAmThanh(ts, "BatDau");

            // GỬI LỆNH VỚI PAYLOAD 1-BYTE
            if (byte.TryParse(ts.MaXeDaChon, out byte maXeByte))
            {
                _serialManager.GuiKhungTruyen(SerialConstants.KEY_LENH_DIEU_KHIEN, SerialConstants.BYTE_SET, maXeByte,
                    new byte[] { SerialConstants.CMD_BAT_DAU_THI });
            }

            // 🔥 SỬA LỖI: Báo cho Form1 bật Timer TRƯỚC KHI hiện MessageBox
            OnDataChanged?.Invoke(this, EventArgs.Empty);

            OnMessageBoxShow?.Invoke($"Bắt đầu tính giờ cho thí sinh {ts.HoTen}.", "Bắt đầu thi");
        }

        /// <summary>
        /// Logic chính: Hoàn thành một lượt thi (ĐÃ CẬP NHẬT)
        /// </summary>
        private void HoanThanhBaiThi(ThiSinh tsDaThiXong)
        {
            if (tsDaThiXong == null) return;

            string ketQua = (tsDaThiXong.DiemTongHop >= 80) ? "Đạt" : "Không đạt";
            tsDaThiXong.DiemThi = tsDaThiXong.DiemTongHop;
            tsDaThiXong.KetQua = ketQua;
            tsDaThiXong.ThoiGianThi = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");

            DanhSachKetQuaChung.Add(tsDaThiXong);
            DanhSachDangThi.Remove(tsDaThiXong);

            if (!string.IsNullOrEmpty(tsDaThiXong.MaXeDaChon))
            {
                // GỬI LỆNH VỚI PAYLOAD 1-BYTE
                if (byte.TryParse(tsDaThiXong.MaXeDaChon, out byte maXeByte))
                {
                    _serialManager.GuiKhungTruyen(SerialConstants.KEY_LENH_DIEU_KHIEN, SerialConstants.BYTE_SET, maXeByte,
                        new byte[] { SerialConstants.CMD_KET_THUC_THI });
                }

                Xe xeCanTra = DanhSachXe.FirstOrDefault(x => x.MaXe == tsDaThiXong.MaXeDaChon);
                if (xeCanTra != null)
                {
                    xeCanTra.DangRanh = true;
                    xeCanTra.SBDThiSinhHienTai = null;
                    xeCanTra.GiaiDoan = 0;
                }
            }

            CapNhatDanhSachChuanBi();
            OnDataChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Logic ghi nhận lỗi (cho cả tự động và thủ công)
        /// </summary>
        private void GhiNhanLoi(ThiSinh ts, int diemTru, string chiTietLoi)
        {
            if (ts == null || ts.ThoiGianBatDau == DateTime.MinValue) return;
            TimeSpan thoiGianTroiQua = DateTime.Now - ts.ThoiGianBatDau;
            var loi = new LoiViPham
            {
                HoTen = ts.HoTen,
                SBD = ts.SBD,
                MaXeDaChon = ts.MaXeDaChon,
                DiemTru = diemTru,

              
                ThoiGianLoi = thoiGianTroiQua.ToString(@"mm\:ss"), 

                ChiTietLoi = chiTietLoi
            };
            DanhSachLoiViPham.Add(loi);
        }

        public void GhiNhanLoiThuCong(ThiSinh ts, int diemTru, string chiTietLoi, string amThanh, Action<ThiSinh> updateCounter)
        {
            if (ts == null) return;
            GhiNhanLoi(ts, diemTru, chiTietLoi);
            //_audioManager.PhatAmThanh(ts, amThanh);
            updateCounter?.Invoke(ts);
            ts.DiemTongHop -= diemTru;
            DanhSachDangThi.ResetBindings();
        }
        public async Task LoaiTrucTiep(ThiSinh ts, int diemTru, string chiTietLoi, string amThanhLoi)
        {
            GhiNhanLoi(ts, diemTru, chiTietLoi);
            ts.DiemTongHop = 0;
            DanhSachDangThi.ResetBindings();
            await Task.Delay(1500);
            await Task.Delay(1500);
            HoanThanhBaiThi(ts);
        }
        private void CapNhatDanhSachChuanBi()
        {
            var sbdDaGoi = DanhSachChuanBiThi.Select(ts => ts.SBD);
            var sbdDangThi = DanhSachDangThi.Select(ts => ts.SBD);
            var sbdDaThi = DanhSachKetQuaChung.Select(ts => ts.SBD);
            var tatCaSbdDaDung = sbdDaGoi.Concat(sbdDangThi).Concat(sbdDaThi).ToList();
            var thiSinhCoTheGoi = DanhSachChinh
                .Where(ts => !tatCaSbdDaDung.Contains(ts.SBD))
                .ToList();
            while (DanhSachChuanBiThi.Count < SO_LUONG_CHO && thiSinhCoTheGoi.Count > 0)
            {
                var tsTiepTheo = thiSinhCoTheGoi.First();
                DanhSachChuanBiThi.Add(tsTiepTheo);
                thiSinhCoTheGoi.Remove(tsTiepTheo);
            }
        }


        #region === SERIAL LOGIC (Nằm trong ExamManager - ĐÃ CẬP NHẬT) ===

        /// <summary>
        /// Hàm này xử lý sự kiện khi SerialManager báo có khung truyền HỢP LỆ
        /// </summary>
        private void GiaiMaKhungTruyen(byte[] frame)
        {
            byte key = frame[1];
            byte set_get = frame[2];
            byte maXe = frame[3];

            // 1. Tính toán độ dài payload (Total - Header - Stop)
            int payloadLength = frame.Length - SerialConstants.HEADER_LENGTH - 1;

            string maXeStr = maXe.ToString("D2");
            ThiSinh ts = DanhSachDangThi.FirstOrDefault(t => t.MaXeDaChon == maXeStr);

            // 2. Lấy byte đầu tiên của payload (nếu có)
            // Đây là nơi chứa Mã Lỗi, Mã Lệnh ACK, Mã Trạng thái...
            byte dataByte = (payloadLength > 0) ? frame[SerialConstants.HEADER_LENGTH] : (byte)0x00;

            if (key == SerialConstants.KEY_LOI_PHAN_HOI) // KEY LỖI (0xE0)
            {
                byte maLoi = dataByte; // Lấy mã lỗi từ payload
                OnLogMessage?.Invoke($"[ERROR] Xe {maXeStr} báo lỗi: {maLoi:X2}");
                if (ts == null) return;

                XuLyLoiTuDong(ts, maLoi);
            }
            else if (key == SerialConstants.KEY_TRANG_THAI_DIEU_KHIEN) // KEY TRẠNG THÁI (0xC0)
            {
                byte maTrangThai = dataByte; // Lấy mã trạng thái từ payload
                if (set_get == SerialConstants.BYTE_SET) // Phản hồi ACK
                {
                    OnLogMessage?.Invoke($"[ACK] Xe {maXeStr} đã nhận lệnh: {maTrangThai:X2}");
                }
                else if (set_get == SerialConstants.BYTE_GET) // Báo cáo trạng thái định kỳ (Heartbeat)
                {
                    OnLogMessage?.Invoke($"[HEARTBEAT] Xe {maXeStr} đang ở trạng thái: {maTrangThai:X2}");
                }
            }
            // else if (key == KEY_GUI_ANH (0xF0)) // TODO: Xử lý khi nhận ảnh
            // {
            //    byte[] imageData = frame.Skip(SerialConstants.HEADER_LENGTH).Take(payloadLength).ToArray();
            //    OnLogMessage?.Invoke($"[IMAGE] Nhận được ảnh từ xe {maXeStr}, {imageData.Length} bytes.");
            //    // Gọi hàm hiển thị ảnh...
            // }
        }

        /// <summary>
        /// Xử lý logic nghiệp vụ khi xe báo lỗi tự động (ĐÃ CẬP NHẬT)
        /// </summary>
        private void XuLyLoiTuDong(ThiSinh ts, byte maLoi)
        {
            int diemTru = 0;
            string amThanhLoi = "";
            string chiTietLoi = "Lỗi tự động";
            bool loaiTrucTiep = false;

            // Logic switch-case y hệt như cũ
            switch (maLoi)
            {
                case SerialConstants.LOI_DE_VACH_XUAT_PHAT:
                case SerialConstants.LOI_DE_VACH_CHUONG_NGAI:
                case SerialConstants.LOI_CHAM_CHAN:
                    diemTru = 5; amThanhLoi = "ChamVach"; chiTietLoi = "Chạm vạch/chân"; ts.LoiChamVach++; break;
                case SerialConstants.LOI_TAT_MAY:
                    diemTru = 5; amThanhLoi = "ChetMay"; chiTietLoi = "Tắt máy/Chết máy"; ts.LoiChetMay++; break;
                case SerialConstants.LOI_QUA_THOI_GIAN:
                    loaiTrucTiep = true; diemTru = ts.DiemTongHop; amThanhLoi = "VuotThoiGian"; chiTietLoi = "Quá thời gian thi (Loại trực tiếp)"; break;
                case SerialConstants.LOI_DO_XE:
                    loaiTrucTiep = true; diemTru = ts.DiemTongHop; amThanhLoi = "DoXe"; chiTietLoi = "Đổ xe (Loại trực tiếp)"; ts.LoiNgaDo = 1; break;
                case SerialConstants.LOI_DI_SAI_DUONG:
                case SerialConstants.LOI_DI_RA_NGOAI:
                    loaiTrucTiep = true; diemTru = ts.DiemTongHop; amThanhLoi = "SaiHinh"; chiTietLoi = "Chạy sai hình/ra ngoài (Loại trực tiếp)"; ts.LoiChaySaiHinh = 1; break;
                case SerialConstants.LOI_KHONG_VI_DOI_MUI:
                case SerialConstants.LOI_XI_NHAN:
                    loaiTrucTiep = true; diemTru = ts.DiemTongHop; amThanhLoi = "KhongXiNhan"; chiTietLoi = "Không xi nhan (Loại trực tiếp)"; ts.LoiKhongXiNhan = 1; break;
                default:
                    OnLogMessage?.Invoke($"[WARNING] Xe {ts.MaXeDaChon} báo mã lỗi không xác định: {maLoi:X2}");
                    return;
            }

            if (loaiTrucTiep)
            {
                // Dùng _ để chạy "Fire-and-forget"
                _ = LoaiTrucTiep(ts, diemTru, chiTietLoi, amThanhLoi);
            }
            else
            {
                // Lỗi trừ điểm
                GhiNhanLoi(ts, diemTru, chiTietLoi);
                //_audioManager.PhatAmThanh(ts, amThanhLoi);
                ts.DiemTongHop -= diemTru;
                DanhSachDangThi.ResetBindings(); // Cập nhật UI
            }
        }
        #endregion
    }
}

