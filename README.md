# THI_HANG_A1
# 🚦 ỨNG DỤNG THI SÁT HẠCH LÁI XE MÁY (A1)

Hệ thống thi sát hạch lái xe máy A1 bao gồm:
- Thiết bị phần cứng gắn trên xe (ESP32/W5500)
- Phần mềm quản lý trung tâm thi (Windows .NET)
- Hệ thống truyền nhận dữ liệu thời gian thực qua Socket TCP
- Giao diện giám sát và chấm điểm tự động

## 📑 Mục lục
- [Giới thiệu](#giới-thiệu)
- [Tính năng chính](#tính-năng-chính)
- [Kiến trúc hệ thống](#kiến-trúc-hệ-thống)
- [Mô tả thiết bị gắn trên xe](#mô-tả-thiết-bị-gắn-trên-xe)
- [Mô tả phần mềm trung tâm](#mô-tả-phần-mềm-trung-tâm)
- [Giao thức truyền thông](#giao-thức-truyền-thông)
- [Cài đặt & Chạy ứng dụng](#cài-đặt--chạy-ứng-dụng)
- [Cấu trúc dự án](#cấu-trúc-dự-án)
- [Ảnh minh họa](#ảnh-minh-họa)
- [Tác giả](#tác-giả)

---

## 🧭 Giới thiệu
Hệ thống thi sát hạch lái xe máy được xây dựng nhằm:
- Tự động hóa việc thu thập dữ liệu trên xe (tốc độ, tín hiệu xi-nhan, dừng xe, lỗi…)
- Truyền dữ liệu thời gian thực về trung tâm
- Tự động chấm điểm theo từng bài thi
- Lưu trữ và quản lý toàn bộ thí sinh, bài thi, kết quả thi
- Tăng độ chính xác, minh bạch và giảm nhân lực giám sát thủ công

Hệ thống sử dụng kiến trúc **Client–Server**:
- Xe máy = Client  
- Phần mềm Windows = Server

---

## ⚙️ Tính năng chính

### 🔸 Trên thiết bị xe:
- Đọc tín hiệu:
  - Encoder bánh xe
  - Cảm biến Hall
  - Cảm biến phanh
  - Xi-nhan trái/phải
  - Khởi động/dừng xe
- Truyền dữ liệu liên tục qua TCP Socket
- Gửi mã lỗi theo chuẩn hệ thống sát hạch
- Chạy ổn định với kết nối WiFi hoặc Ethernet (W5500)

### 🔸 Trên phần mềm trung tâm (WinForms/WPF):
- Quản lý danh sách xe
- Giám sát trạng thái xe theo thời gian thực
- Hiển thị số vòng quay, tốc độ, lỗi vi phạm
- Quản lý thí sinh – bài thi – kết quả thi
- Tự động chấm điểm theo từng phần thi A1
- Kết nối nhiều xe cùng lúc
- Xuất báo cáo, lưu trữ SQL

---

## 🏗 Kiến trúc hệ thống
Hệ thống gồm 3 phần:
[ESP32 / W5500 gắn trên xe]
|
TCP Socket
|
[Server Windows (.NET)]
|
SQL Database

---

## 🔧 Mô tả thiết bị gắn trên xe

### Phần cứng:
- ESP32 / ESP32-WROOM / ESP32-S3
- Module Ethernet W5500 hoặc WiFi
- Encoder bánh sau
- Cảm biến Hall
- Tín hiệu xi-nhan
- Tín hiệu phanh / đề máy

### Phần mềm:
- Arduino / ESP-IDF
- Gửi dữ liệu dạng raw packet theo frame chuẩn

---

## 💻 Mô tả phần mềm trung tâm (Windows)
- Ngôn ngữ: **C# .NET 8.0**
- Mô hình đa luồng nhận dữ liệu Socket
- Dùng WinForms/WPF
- Lưu trữ bằng SQL Server / LocalDB
- Quản lý:
  - Xe
  - Thí sinh
  - Bài thi
  - Lỗi phát sinh
  - Thời gian – điểm số

---

## 🔌 Giao thức truyền thông
Giao tiếp qua **TCP Socket**:

**Frame mẫu (8 byte):**
| Byte | Ý nghĩa |
|------|--------|
| 0    | Start byte |
| 1    | Key |
| 2    | Type |
| 3    | Motor ID |
| 4-7  | Data (UInt32) |

Phần mềm sẽ giải mã theo từng loại gói.

---

## 🚀 Cài đặt & Chạy ứng dụng

### 🔹 1. Clone dự án
https://github.com/PhamChinhCode/MotoDrivingTestServer.git

### 🔹 2. Mở bằng Visual Studio
- Yêu cầu .NET 8.0 trở lên
- Cài đặt NuGet cần thiết

### 🔹 3. Cấu hình SQL
- Update-Database (nếu dùng EF Core Migrations)
- Hoặc nhập file .mdf thủ công

### 🔹 4. Kết nối xe
- Chạy server trong phần mềm
- ESP32 kết nối vào IP và PORT đã cấu hình
- Theo dõi dữ liệu real-time

---

## 📂 Cấu trúc dự án 


---

## 🖼 Ảnh minh họa



---

## 👨‍💻 Tác giả
**PhamChinhCode**  
Liên hệ hỗ trợ: *phamvanchinh203@gmail.com*

---


