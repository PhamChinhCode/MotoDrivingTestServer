using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THI_HANG_A1.Managers;

namespace THI_HANG_A1.Models
{
    public class ThiSinhDangThi
    {
        public bool? DaKiemTraXe { get; set; }   // null: vừa cấp xe, false: đã chuẩn bị, true: đã kiểm tra xong

        public string TrangThai { get; set; }   // Đã cấp xe / Chuẩn bị thi / Đang thi / Đạt / Không đạt
        public string HoDem { get; set; }
        public string Ten { get; set; }
        public int SoBaoDanh { get; set; }
        public string HangGPLX { get; set; }

        // --- Xe được phân ---
        public string Xe { get; set; } = "";
        public Moto XeObj { get; set; }

        // --- Trạng thái bài thi ---
        public DateTime GioBatDau { get; set; }
        public DateTime GioKetThuc { get; set; }

        // --- Điểm ---
        public int DiemBanDau { get; set; } = 100;
        public int DiemConLai { get; set; } = 100;
        public int DiemTru { get; set; } = 0;
        public int SoLoi { get; set; } = 0;

        // --- Các bài thi (tùy bạn dùng hay không) ---
        public string So8 { get; set; } = "";
        public string DuongThang { get; set; } = "";
        public string ZicZac { get; set; } = "";
        public string GoGhe { get; set; } = "";

        public int SessionID { get; set; }
        public int BaiThiHienTaiID { get; set; }

        public byte LastStatus { get; set; } = ConstantKeys.STATUS_FREE;
        public int LastError { get; set; } = 0;
        public int LastErrorRecordId { get; set; } = 0;
        public bool WaitingImage { get; set; } = false;
        public int LastSentMark { get; set; } = 100;

        public byte[] LastImageBytes { get; set; }

        // handler để gỡ event
        public Action XeChangedHandler { get; set; }
        public Action SanChangedHandler { get; set; }
        public Action<byte[]> XeImageHandler { get; set; }

        // --- Lịch sử lỗi ---
        public List<ChiTietLoi> NhatKyLoi { get; set; } = new List<ChiTietLoi>();
    }

    // Class lưu chi tiết lỗi
}

