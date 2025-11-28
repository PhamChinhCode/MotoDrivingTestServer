using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THI_HANG_A1.Models
{
    public class ThiSinhXml
    {
        public string MaDangKy { get; set; }
        public string HoTen { get; set; }
        public string Hodem { get; set; }
        public string Ten { get; set; }
        public string NgaySinh { get; set; }
        public string SoCMT { get; set; }
        public string AnhChanDung { get; set; }
        public byte[] AnhBytes { get; set; }
        public string HangGPLX { get; set; }
        public string KySatHach { get; set; }
        public string TenKySatHach { get; set; }
        public int SoBaoDanh { get; set; }

        public int Diem_L { get; set; }
        public int DiemChuan_L { get; set; }

        public int Diem_M { get; set; }
        public int Diem_H { get; set; }
        public int Diem_D { get; set; }
    }
}
