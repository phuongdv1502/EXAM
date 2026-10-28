# AI Quiz HSG

Phần mềm thi trắc nghiệm trên trình duyệt, phục vụ bồi dưỡng học sinh giỏi Tin học THCS. Máy chủ chạy API và MySQL; giáo viên và học sinh truy cập qua mạng LAN.

Giai đoạn đang làm: **ngân hàng câu hỏi** — môn học, chuyên đề, câu hỏi, duyệt và nhập Excel.

## Quyết định cho bản đầu

- Câu hỏi một đáp án và nhiều đáp án đều có trong mô hình dữ liệu. Chấm điểm nhiều đáp án sẽ tính đúng khi chọn đủ và chỉ các phương án đúng.
- Quản trị viên tạo tài khoản. Học sinh không tự đăng ký.
- Mỗi đề có số lượt thi tối đa, mặc định 1.
- Học sinh chỉ xem kết quả khi đề được công bố (cột `PublishResults`).
- AI nằm sau một dịch vụ riêng và không chặn thi hay chấm điểm. Phần AI sẽ làm ở giai đoạn sau.
- Mục tiêu tải ban đầu là 100 người đồng thời, cần kiểm thử trước khi cam kết.

## Chạy máy chủ

Cần [MySQL 8](https://dev.mysql.com/downloads/mysql/) đang chạy. Sửa chuỗi kết nối trong `src/AIQuizHSG.Api/appsettings.Development.json` nếu mật khẩu `root` không để trống.

```powershell
dotnet run --project src/AIQuizHSG.Api --launch-profile http
```

API lắng nghe `http://localhost:5080`. Swagger UI: `http://localhost:5080/swagger`. Lần chạy đầu tạo database `aiquizhsg`, các bảng và tài khoản quản trị.

| | |
| --- | --- |
| Tên đăng nhập | `admin` |
| Mật khẩu | `Admin@123456` |

Đổi mật khẩu này trước khi dùng với dữ liệu thật. Khóa ký JWT trong môi trường Development chỉ dùng cho máy local.

Tên class, thư mục, thuộc tính, bảng và cột MySQL dùng tiếng Anh. Thông báo lỗi, tên vai trò, tên mức độ và nội dung seed hiển thị trên màn hình vẫn là tiếng Việt.

Đăng nhập: `POST /api/login` với JSON `{"username":"admin","password":"Admin@123456"}`. Gửi kèm header `Authorization: Bearer <token>` cho các API bên dưới. Giáo viên và quản trị mới gọi được ngân hàng câu hỏi.

- `GET/POST /api/subjects`, `PUT /api/subjects/{id}`
- `GET/POST /api/topics`, `PUT /api/topics/{id}`
- `GET/POST /api/questions`, `PUT /api/questions/{id}`
- `POST /api/questions/{id}/submit`, `/approve`, `/reject`
- `GET /api/questions/import-template`, `POST /api/questions/import`

Câu hỏi mới ở trạng thái nháp. Gửi duyệt rồi mới được phê duyệt. Sửa câu đã duyệt sẽ đưa câu về chờ duyệt. Tệp Excel dùng cột `MaMon`, `ChuyenDe`, `NoiDung`, `MucDo`, `Loai`, `LoiGiai`, `PhuongAn1`–`PhuongAn6`, `Dung1`–`Dung6`. Mức độ nhận `Dễ`, `Trung bình`, `Khó`. Loại nhận `Một đáp án` hoặc `Nhiều đáp án`.

## Kiểm thử

```powershell
dotnet test AIQuizHSG.slnx
```
