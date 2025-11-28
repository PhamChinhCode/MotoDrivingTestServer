using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THI_HANG_A1.Helpers
{
    public static class FaultDefinitions
    {
        public static readonly Dictionary<string, (int faultId, int diemTru, int baiThiId)> FaultMap =
            new Dictionary<string, (int, int, int)>()
            {
            { "Chống chân", (100, 5, 0) },
            { "Đổ xe",      (6,   25, 0) },
            { "Ngoài hình", (101, 25, 0) }
            };
    }

}
