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
        private readonly SemaphoreSlim _soundLock = new SemaphoreSlim(1, 1);
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
                while (_soundQueue.TryDequeue(out var path))
                {
                    if (!File.Exists(path))
                    {
                        OnLogMessage?.Invoke($"Không tìm thấy âm thanh: {path}");
                        continue;
                    }

                    using (var player = new SoundPlayer(path))
                    {
                        player.PlaySync();
                    }
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// Phát âm thanh (Đồng bộ - CÓ CHỜ) và trả về Task
        /// </summary>
        public Task PhatAmThanhSyncTask(ThiSinh ts, string tenSuKien)
        {
            if (ts == null || string.IsNullOrEmpty(ts.MaXeDaChon))
            {
                return Task.CompletedTask;
            }
            return Task.Run(() => PhatAmThanhSync(ts, tenSuKien));
        }

        /// <summary>
        /// Lõi phát âm thanh, luôn chạy đồng bộ (PlaySync)
        /// </summary>
        private void PhatAmThanhSync(ThiSinh ts, string tenSuKien)
        {
            if (ts == null || string.IsNullOrEmpty(ts.MaXeDaChon)) return;

            string fileName = $"Xe{ts.MaXeDaChon}_{tenSuKien}.wav";
            string fullPath = Path.Combine(SOUND_PATH, fileName);

            if (File.Exists(fullPath))
            {
                try
                {
                    using (SoundPlayer soundPlayer = new SoundPlayer(fullPath))
                    {
                        soundPlayer.PlaySync();
                    }
                }
                catch (Exception ex)
                {
                    OnLogMessage?.Invoke($"LỖI PHÁT ÂM THANH {fileName}: {ex.Message}");
                }
            }
            else
            {
                OnLogMessage?.Invoke($"LỖI ÂM THANH: Không tìm thấy file {fileName}");
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
