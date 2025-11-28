using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using THI_HANG_A1.Models;

namespace THI_HANG_A1.Helpers
{
    public static class MotoHelper
    {
        public static Color GetMotoColor(Moto moto)
        {
            switch (moto.Status)
            {
                case 0xC1:   // READY
                    return Color.LightGreen;

                case 0xC2:   // RUNNING
                    return Color.Orange;

                case 0xC3:   // IDLE (rảnh)
                    return Color.LightGray;

                default:
                    return Color.White;
            }
        }

    }

}
