using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THI_HANG_A1.Models
{
    public class ThiSinhDaThi
    {
        public long SBD { get; set; }
        public int SessionID { get; set; }
        public string HoDem { get; set; }
        public string Ten { get; set; }
        public string HangGPLX { get; set; }
        public string DeviceID { get; set; }
        public DateTime StartTime { get; set; }
        public int Duration { get; set; }
        public int SoLanThi { get; set; }
        public int Mark { get; set; }

        public int DiemTru_BT1 { get; set; }
        public int DiemTru_BT4 { get; set; }
        public int DiemTru_BT5 { get; set; }
        public int DiemTru_BT6 { get; set; }
        public int DiemTru_BT7 { get; set; }

        public string ImagePath {get; set; }

        // ⭐ DANH SÁCH LỖI CHI TIẾT
        public List<ChiTietLoi> NhatKyLoi { get; set; } = new List<ChiTietLoi>();
    }
}
