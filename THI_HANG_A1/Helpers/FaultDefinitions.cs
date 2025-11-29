using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THI_HANG_A1.Helpers
{
    public static class FaultDefinitions
    {
        public static readonly Dictionary<string, (int faultId, int diemTru, int baiThiId, string moTa)> FaultMap =
            new Dictionary<string, (int, int, int, string)>()
            {
            { "Chuẩn bị",  (0,   0, 0, "Chuẩn bị thi") },
            { "Bắt đầu",   (0,   0, 0, "Bắt đầu bài thi") },
            { "Chống chân", (100, 5, 0, "Phạm lỗi chống chân khi đang thi") },
            { "Đổ xe",       (6,  25, 0, "Xe bị đổ hoặc ngã trong bài thi") },
            { "Ngoài hình",  (101,25, 0, "Hai bánh xe đi ra ngoài hình") }
            };
    }
}
