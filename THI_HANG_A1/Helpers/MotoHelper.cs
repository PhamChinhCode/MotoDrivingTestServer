using System.Drawing;
using THI_HANG_A1.Managers;
using THI_HANG_A1.Models;

namespace THI_HANG_A1.Helpers
{
    public static class MotoHelper
    {
        public static Color GetMotoColor(Moto moto)
        {
            switch (moto.Status)
            {
                case ConstantKeys.STATUS_FREE:
                    return Color.LightGreen;     // Xe rảnh → xanh

                case ConstantKeys.STATUS_READY:
                    return Color.Gold;           // Xe đã cấp xe → vàng

                case ConstantKeys.STATUS_TESTING:
                case ConstantKeys.STATUS_CONTEST1:
                case ConstantKeys.STATUS_CONTEST2:
                case ConstantKeys.STATUS_CONTEST3:
                case ConstantKeys.STATUS_CONTEST4:
                    return Color.Orange;
                default:
                    return Color.White;
            }
        }
    }
}
