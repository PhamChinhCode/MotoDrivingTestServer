using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THI_HANG_A1.Managers
{
    public static class ImageManager
    {
        public static string SaveImageToFile(string rootFolder, string tenThiSinh, int errorRecordId, byte[] bytes)
        {
            try
            {
                string baseFolder = @"D:\";

                // Thư mục chính: D:/rootFolder
                string finalRoot = Path.Combine(baseFolder, rootFolder);

                if (!Directory.Exists(finalRoot))
                    Directory.CreateDirectory(finalRoot);

                // --- Thư mục theo ngày ---
                string dateFolder = DateTime.Now.ToString("dd_MM_yyyy");
                string finalDateFolder = Path.Combine(finalRoot, dateFolder);

                if (!Directory.Exists(finalDateFolder))
                    Directory.CreateDirectory(finalDateFolder);

                // Chuẩn hóa tên thí sinh
                string tenFile = tenThiSinh
                    .Replace(" ", "_")
                    .Replace(":", "")
                    .Replace("/", "_")
                    .Replace("\\", "_")
                    .Replace("\"", "")
                    .Replace("'", "");

                // Đường dẫn file cuối
                string filePath = Path.Combine(finalDateFolder, $"{tenFile}_{errorRecordId}.jpg");

                // Lưu file
                File.WriteAllBytes(filePath, bytes);

                return filePath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SaveImageToFile ERROR: " + ex.Message);
                return null;
            }
        }
    }
}
