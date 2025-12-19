using System;
using System.Collections.Concurrent;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using THI_HANG_A1.Models;

namespace THI_HANG_A1.Managers
{
    /// <summary>
    /// CHUYÊN GIA ÂM THANH
    /// Chỉ làm một việc: Phát âm thanh
    /// </summary>
    /// 

    public class AudioManager
    {
        private static readonly string SOUND_PATH = Path.Combine(FindProjectRootWithResources(), "Resources", "Sounds");
        private readonly ConcurrentQueue<string> _soundQueue = new ConcurrentQueue<string>();
        /// <summary>
        /// Sự kiện này được dùng để gửi log về Form1
        /// </summary>
        public event Action<string> OnLogMessage;

        private bool _isProcessing = false;

        public AudioManager()
        {
            if (!Directory.Exists(SOUND_PATH))
            {
                MessageBox.Show("Thư mục không đúng: \r\n" + SOUND_PATH);
            }
        }

        /// <summary>
        /// Phát âm thanh (Bất đồng bộ - không chờ)
        /// </summary>
        public void PhatAmThanh(ThiSinhDangThi ts, string tenSuKien)
        {
            if (ts == null || ts.XeObj == null)
                return;

            // Ví dụ: Xe01.wav
            string xeFile = $"Xe0{ts.XeObj.Id}.wav";

            // Ví dụ: ChamVach.wav
            string suKienFile = $"{tenSuKien}.wav";

            string xePath = Path.Combine(SOUND_PATH, xeFile);
            string suKienPath = Path.Combine(SOUND_PATH, suKienFile);

            // 👉 ENQUEUE THEO ĐÚNG THỨ TỰ
            _soundQueue.Enqueue(xePath);
            _soundQueue.Enqueue(suKienPath);


            if (!_isProcessing)
                _ = ProcessQueueAsync();
        }

        private async Task ProcessQueueAsync()
        {
            if (_isProcessing)
                return;

            _isProcessing = true;

            try
            {
                await Task.Run(() =>
                {
                    while (_soundQueue.TryDequeue(out var path))
                    {
                        if (!File.Exists(path))
                            continue;

                        using (var player = new SoundPlayer(path))
                        {
                            player.Load();
                            player.PlaySync();
                        }
                    }
                });
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private static string FindProjectRootWithResources()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "Resources", "Sounds")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("Không tìm thấy thư mục Resources/Sounds");
        }
    }
}
