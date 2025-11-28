using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        //public string TrangThai { get; set; } = "Chưa bắt đầu";
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

        // --- Lịch sử lỗi ---
        public List<LoiChiTiet> NhatKyLoi { get; set; } = new List<LoiChiTiet>();

        // Hàm thêm lỗi
        public void ThemLoi(string tenLoi, int diemTru)
        {
            SoLoi++;
            this.DiemTru += diemTru;
            this.DiemConLai -= diemTru;

            NhatKyLoi.Add(new LoiChiTiet
            {
                ThoiGian = DateTime.Now,
                TenLoi = tenLoi,
                DiemTru = diemTru,
                DiemConLai = this.DiemConLai
            });

            TrangThai = (DiemConLai >= 80) ? "Đạt" : "Không đạt";
        }
    }

    // Class lưu chi tiết lỗi
    public class LoiChiTiet
    {
        public DateTime ThoiGian { get; set; }
        public string TenLoi { get; set; }
        public int DiemTru { get; set; }
        public int DiemConLai { get; set; }
    }
}

