using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THI_HANG_A1.Forms;

namespace THI_HANG_A1.Models
{
    public class SessionThi
    {
        public int SessionID { get; set; }
        public int LanThi { get; set; }
        public DateTime StartTime { get; set; }
        public int Mark { get; set; }
        public LyDoKetThuc? LyDoKetThuc { get; set; }

        public string TenHienThi
        {
            get
            {
                if (LyDoKetThuc.HasValue)
                    return $"Lần {LanThi} - Không đạt";

                return $"Lần {LanThi} - {(Mark >= 80 ? "Đạt" : "Không đạt")}";
            }
        }
    }
    public class SessionInfo
    {
        public int SessionID { get; set; }
        public long SBD { get; set; }

        // Thông tin hiển thị / ghi log
        public string Ten { get; set; }      // Họ tên thí sinh
        public string Xe { get; set; }       // DeviceID / số xe

        // (tuỳ chọn – nếu sau này cần)
        public DateTime? LastUpdate { get; set; }
    }


}
