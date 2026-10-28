namespace AIQuizHSG.Infrastructure.Seeding;

internal sealed record SubjectSeed(string Code, string Name, string Description);

internal sealed record TopicSeed(string SubjectCode, string Name, int Sort, string? Parent = null);

internal sealed record QuestionSeed(
    string SubjectCode,
    string Topic,
    byte Level,
    string Content,
    int Correct,
    string A,
    string B,
    string C,
    string D);

// Môn, chuyên đề và câu trắc nghiệm mẫu. Câu trùng nội dung sẽ không được thêm lại.
internal static class SampleBank
{
    public static readonly SubjectSeed[] Subjects =
    [
        new("TIN", "Tin học", "Môn Tin học THCS"),
        new("TOAN", "Toán", "Môn Toán THCS"),
        new("VAN", "Ngữ văn", "Môn Ngữ văn THCS"),
        new("ANH", "Tiếng Anh", "Môn Tiếng Anh THCS"),
        new("KHTN", "Khoa học tự nhiên", "Môn Khoa học tự nhiên THCS"),
        new("SU", "Lịch sử", "Môn Lịch sử THCS")
    ];

    public static readonly TopicSeed[] Topics =
    [
        new("TIN", "Biểu diễn thông tin", 1),
        new("TIN", "Thuật toán", 2),
        new("TIN", "Ngôn ngữ lập trình", 3),
        new("TIN", "Cấu trúc dữ liệu", 4),
        new("TIN", "Mạng máy tính", 5),
        new("TIN", "Câu lệnh rẽ nhánh", 1, "Ngôn ngữ lập trình"),
        new("TIN", "Vòng lặp", 2, "Ngôn ngữ lập trình"),
        new("TOAN", "Số học", 1),
        new("TOAN", "Phân số", 2),
        new("TOAN", "Hình học", 3),
        new("TOAN", "Phương trình", 4),
        new("VAN", "Tiếng Việt", 1),
        new("VAN", "Đọc hiểu", 2),
        new("VAN", "Tập làm văn", 3),
        new("VAN", "Văn bản", 4),
        new("ANH", "Ngữ pháp", 1),
        new("ANH", "Từ vựng", 2),
        new("ANH", "Đọc hiểu", 3),
        new("ANH", "Giao tiếp", 4),
        new("KHTN", "Vật lý", 1),
        new("KHTN", "Hóa học", 2),
        new("KHTN", "Sinh học", 3),
        new("KHTN", "Trái Đất", 4),
        new("SU", "Lịch sử Việt Nam", 1),
        new("SU", "Lịch sử thế giới", 2),
        new("SU", "Cổ trung đại", 3),
        new("SU", "Cận hiện đại", 4)
    ];

    public static readonly QuestionSeed[] Questions =
    [
        Q("TIN", "Biểu diễn thông tin", 1, "1 byte bằng bao nhiêu bit?", 1, "4 bit", "8 bit", "16 bit", "32 bit"),
        Q("TIN", "Biểu diễn thông tin", 1, "Đơn vị thông tin nhỏ nhất trong máy tính là gì?", 0, "Bit", "Byte", "Kilobyte", "Megabyte"),
        Q("TIN", "Biểu diễn thông tin", 1, "Hệ nhị phân sử dụng những chữ số nào?", 2, "0 đến 9", "0 đến 7", "0 và 1", "A đến F"),
        Q("TIN", "Mạng máy tính", 1, "Thiết bị nào sau đây dùng để nhập văn bản?", 0, "Bàn phím", "Màn hình", "Loa", "Máy in"),
        Q("TIN", "Biểu diễn thông tin", 1, "Bộ nhớ RAM có đặc điểm nào?", 3, "Giữ dữ liệu khi tắt máy", "Chỉ dùng để in", "Là bộ nhớ ngoài", "Mất dữ liệu khi tắt nguồn"),

        Q("TIN", "Thuật toán", 2, "Thuật toán là gì?", 0, "Một dãy bước hữu hạn để giải quyết bài toán", "Một ngôn ngữ lập trình", "Một thiết bị phần cứng", "Một hệ điều hành"),
        Q("TIN", "Biểu diễn thông tin", 2, "1 KB bằng bao nhiêu byte?", 1, "1000", "1024", "512", "8"),
        Q("TIN", "Biểu diễn thông tin", 2, "Số nhị phân 1010 bằng số thập phân nào?", 1, "8", "10", "12", "16"),
        Q("TIN", "Thuật toán", 2, "Tính dừng của thuật toán nghĩa là gì?", 2, "Chương trình chạy mãi", "Không cần dữ liệu vào", "Kết thúc sau một số bước hữu hạn", "Chỉ viết được trên giấy"),
        Q("TIN", "Thuật toán", 2, "Trong sơ đồ khối, hình thoi biểu diễn thành phần nào?", 2, "Bắt đầu", "Kết thúc", "Điều kiện", "Nhập xuất"),
        Q("TIN", "Ngôn ngữ lập trình", 2, "Biến trong chương trình dùng để làm gì?", 0, "Lưu giá trị có thể thay đổi", "Vẽ hình lên màn hình", "Kết nối Internet", "Nén tệp"),
        Q("TIN", "Ngôn ngữ lập trình", 2, "Nếu a đang bằng 5, sau lệnh a ← a + 1 thì a bằng bao nhiêu?", 2, "4", "5", "6", "1"),
        Q("TIN", "Mạng máy tính", 2, "Địa chỉ IP dùng để làm gì?", 0, "Định danh thiết bị trên mạng", "Lưu mật khẩu", "Dịch chương trình", "In tài liệu"),
        Q("TIN", "Biểu diễn thông tin", 2, "Tệp có phần mở rộng .png thường chứa nội dung gì?", 1, "Âm thanh", "Hình ảnh", "Bảng tính", "Mã nguồn"),
        Q("TIN", "Mạng máy tính", 2, "Giao thức nào thường dùng để xem trang web?", 1, "FTP", "HTTP", "SMTP", "SSH"),

        Q("TIN", "Vòng lặp", 3, "Vòng lặp for thường dùng khi nào?", 0, "Khi biết trước số lần lặp", "Khi không bao giờ cần dừng", "Chỉ khi viết hàm đệ quy", "Khi chương trình chỉ chạy một lệnh"),
        Q("TIN", "Ngôn ngữ lập trình", 3, "Cho a = 3. Sau lệnh a ← a * 2, a bằng bao nhiêu?", 1, "5", "6", "8", "9"),
        Q("TIN", "Vòng lặp", 3, "Vòng lặp for i từ 1 đến 4 thực hiện bao nhiêu lần?", 1, "3", "4", "5", "1"),
        Q("TIN", "Câu lệnh rẽ nhánh", 3, "Nếu x = 7, điều kiện x > 10 có giá trị nào?", 1, "Đúng", "Sai", "Bằng 10", "Không xác định"),
        Q("TIN", "Thuật toán", 3, "Tổng 1 + 2 + 3 + 4 bằng bao nhiêu?", 2, "8", "9", "10", "12"),
        Q("TIN", "Thuật toán", 3, "Số lớn nhất trong dãy 4, 9, 2, 7 là số nào?", 2, "4", "7", "9", "2"),
        Q("TIN", "Thuật toán", 3, "17 chia 5 được số dư bằng bao nhiêu?", 0, "2", "3", "4", "5"),
        Q("TIN", "Cấu trúc dữ liệu", 3, "Mảng có 6 phần tử, chỉ số bắt đầu từ 0. Chỉ số cuối là bao nhiêu?", 0, "5", "6", "7", "1"),
        Q("TIN", "Câu lệnh rẽ nhánh", 3, "Với n = 8, nhánh n chia hết cho 2 in ra kết quả nào?", 1, "Lẻ", "Chẵn", "8", "0"),
        Q("TIN", "Vòng lặp", 3, "s bắt đầu bằng 0. Cộng lần lượt i = 1, 2, 3 vào s thì s bằng bao nhiêu?", 1, "3", "6", "9", "1"),
        Q("TIN", "Biểu diễn thông tin", 3, "Số thập phân 15 viết ở hệ nhị phân là số nào?", 1, "1101", "1111", "1010", "1000"),
        Q("TIN", "Ngôn ngữ lập trình", 3, "Độ dài xâu \"HSG\" là bao nhiêu?", 1, "2", "3", "4", "1"),
        Q("TIN", "Ngôn ngữ lập trình", 3, "Giá trị của biểu thức (2 + 3) * 4 là bao nhiêu?", 1, "14", "20", "24", "9"),
        Q("TIN", "Mạng máy tính", 3, "Modem trong mạng máy tính dùng để làm gì?", 0, "Chuyển đổi tín hiệu số và tín hiệu tương tự", "Soạn thảo văn bản", "Lưu cơ sở dữ liệu", "Biên dịch chương trình"),
        Q("TIN", "Ngôn ngữ lập trình", 3, "a là biến số nguyên. Lệnh gán nào hợp lệ?", 1, "a ← \"A\"", "a ← 12", "a ← đúng", "a ← \"12cm\""),
        Q("TIN", "Thuật toán", 3, "Dãy 2, 4, 8, 16 có số tiếp theo là số nào?", 2, "18", "24", "32", "20"),
        Q("TIN", "Thuật toán", 3, "Số nhỏ nhất trong 5, 1, 8, 3 là số nào?", 1, "5", "1", "8", "3"),
        Q("TIN", "Vòng lặp", 3, "x bắt đầu bằng 0. Khi x < 3 thì tăng x thêm 1. Vòng lặp chạy mấy lần?", 1, "2", "3", "4", "0"),

        Q("TIN", "Thuật toán", 4, "Tìm kiếm nhị phân chỉ áp dụng đúng khi dãy đã được sắp xếp. Điều này đúng hay sai?", 0, "Đúng", "Sai", "Chỉ đúng với dãy giảm", "Chỉ đúng với dãy có số âm"),
        Q("TIN", "Cấu trúc dữ liệu", 4, "Ngăn xếp hoạt động theo nguyên tắc nào?", 2, "Vào trước ra trước", "Lấy phần tử ở giữa trước", "Vào sau ra trước", "Lấy ngẫu nhiên"),
        Q("TIN", "Vòng lặp", 4, "Vòng lặp ngoài chạy 3 lần, mỗi lần vòng trong chạy 4 lần. Tổng số lần thân vòng trong chạy là bao nhiêu?", 1, "7", "12", "16", "64"),
        Q("TIN", "Thuật toán", 4, "Một lượt duyệt nổi bọt so sánh các cặp kề nhau để làm gì?", 1, "Đếm số phần tử", "Đưa phần tử lớn hơn về phía sau", "Xóa phần tử trùng", "Đảo ngược xâu"),
        Q("TIN", "Mạng máy tính", 4, "Địa chỉ 192.168.1.1 thuộc loại địa chỉ nào?", 0, "Địa chỉ IP riêng", "Địa chỉ email", "Tên miền", "Địa chỉ MAC"),
        Q("TIN", "Thuật toán", 4, "Hàm đệ quy bắt buộc phải có thành phần nào để dừng?", 1, "Vòng lặp for", "Điều kiện dừng", "Mảng hai chiều", "Kết nối mạng"),
        Q("TIN", "Thuật toán", 4, "Tìm tuyến tính một phần tử trong danh sách n phần tử, trường hợp xấu nhất phải xét bao nhiêu phần tử?", 3, "1", "n/2", "log n", "n"),
        Q("TIN", "Cấu trúc dữ liệu", 4, "Đảo ngược mảng [1, 2, 3, 4] được mảng nào?", 2, "[1, 3, 2, 4]", "[4, 3, 1, 2]", "[4, 3, 2, 1]", "[2, 1, 4, 3]"),
        Q("TIN", "Thuật toán", 4, "Ước chung lớn nhất của 12 và 18 là bao nhiêu?", 1, "3", "6", "9", "36"),
        Q("TIN", "Biểu diễn thông tin", 4, "8 bit biểu diễn được bao nhiêu giá trị khác nhau?", 2, "8", "128", "256", "512"),
        Q("TIN", "Thuật toán", 4, "Thuật toán s ← 0 rồi cộng i từ 1 đến n đang tính đại lượng nào?", 0, "Tổng các số từ 1 đến n", "Tích các số từ 1 đến n", "Số nguyên tố lớn nhất", "Trung bình cộng của n số 0"),
        Q("TIN", "Cấu trúc dữ liệu", 4, "Biết nút đầu của danh sách liên kết, thêm một phần tử vào đầu mất bao nhiêu bước cơ bản?", 0, "Một số bước không phụ thuộc độ dài danh sách", "Đúng bằng số phần tử", "Phải duyệt đến nút cuối", "Phải sắp xếp lại cả danh sách"),

        Q("TIN", "Thuật toán", 5, "Dãy đã sắp xếp có 16 phần tử. Tìm kiếm nhị phân cần nhiều nhất bao nhiêu lần chia đôi?", 2, "2", "8", "4", "16"),
        Q("TIN", "Thuật toán", 5, "Sắp xếp chọn đưa phần tử nhỏ nhất còn lại vào vị trí kế tiếp. Sau lượt đầu của dãy 4, 1, 3, dãy trở thành dãy nào?", 1, "[4, 1, 3]", "[1, 4, 3]", "[1, 3, 4]", "[3, 1, 4]"),
        Q("TIN", "Thuật toán", 5, "Số cách chọn 2 phần tử khác nhau từ 4 phần tử là bao nhiêu?", 2, "4", "8", "6", "2"),
        Q("TIN", "Thuật toán", 5, "Đệ quy tính giai thừa thường dừng khi n bằng giá trị nào?", 1, "n", "0 hoặc 1", "2", "10"),
        Q("TIN", "Cấu trúc dữ liệu", 5, "Hàng đợi lấy phần tử theo thứ tự nào?", 0, "Vào trước ra trước", "Vào sau ra trước", "Lấy phần tử lớn nhất", "Lấy phần tử ở giữa"),

        Q("TOAN", "Số học", 1, "15 + 27 bằng bao nhiêu?", 2, "32", "40", "42", "52"),
        Q("TOAN", "Số học", 1, "Số nguyên tố nhỏ nhất là số nào?", 0, "2", "1", "3", "4"),
        Q("TOAN", "Phân số", 2, "3/4 + 1/4 bằng bao nhiêu?", 1, "1/2", "1", "3/8", "2"),
        Q("TOAN", "Hình học", 2, "Chu vi hình vuông cạnh 5 cm là bao nhiêu?", 3, "10 cm", "15 cm", "25 cm", "20 cm"),
        Q("TOAN", "Phương trình", 3, "Nếu x + 5 = 12 thì x bằng bao nhiêu?", 1, "5", "7", "17", "6"),
        Q("TOAN", "Hình học", 3, "Diện tích hình chữ nhật dài 6 cm, rộng 4 cm là bao nhiêu?", 2, "10 cm²", "20 cm²", "24 cm²", "48 cm²"),
        Q("TOAN", "Phương trình", 4, "Phương trình 2x - 4 = 10 có nghiệm x bằng bao nhiêu?", 1, "3", "7", "8", "14"),
        Q("TOAN", "Hình học", 4, "Tam giác vuông có hai cạnh góc vuông 3 cm và 4 cm. Cạnh huyền dài bao nhiêu?", 0, "5 cm", "6 cm", "7 cm", "12 cm"),
        Q("TOAN", "Số học", 5, "Số chính phương nhỏ nhất lớn hơn 20 là số nào?", 2, "21", "24", "25", "36"),
        Q("TOAN", "Số học", 5, "Tổng các số nguyên từ 1 đến 10 bằng bao nhiêu?", 3, "45", "50", "60", "55"),

        Q("VAN", "Tiếng Việt", 1, "Từ nào là danh từ?", 1, "Chạy", "Trường", "Nhanh", "Rất"),
        Q("VAN", "Tiếng Việt", 1, "Câu \"Trời mưa.\" thuộc kiểu câu nào?", 0, "Câu kể", "Câu hỏi", "Câu cầu khiến", "Câu cảm"),
        Q("VAN", "Đọc hiểu", 2, "Ý chính của đoạn văn thường nằm ở đâu?", 2, "Chỉ ở câu giữa", "Chỉ ở chú thích", "Câu nêu nội dung bao trùm đoạn", "Ở tên tác giả"),
        Q("VAN", "Văn bản", 2, "Thể thơ lục bát có câu lục mấy tiếng?", 1, "8", "6", "7", "5"),
        Q("VAN", "Tập làm văn", 3, "Mở bài của bài văn thuyết minh cần làm gì?", 0, "Giới thiệu đối tượng cần thuyết minh", "Kể một câu chuyện vui", "Nêu cảm xúc cá nhân là chính", "Liệt kê tài liệu tham khảo"),
        Q("VAN", "Đọc hiểu", 3, "Biện pháp so sánh có dấu hiệu nào?", 2, "Dùng từ láy", "Đảo trật tự từ", "Nêu nét giống nhau giữa hai sự vật", "Lặp lại một từ"),
        Q("VAN", "Tập làm văn", 4, "Bố cục bài nghị luận xã hội thường gồm những phần nào?", 1, "Đề, đáp án, bảng điểm", "Mở bài, thân bài, kết bài", "Nhân vật, tình huống, cao trào", "Mục lục, chú thích, phụ lục"),
        Q("VAN", "Văn bản", 4, "Truyện ngắn khác tiểu thuyết chủ yếu ở điểm nào?", 0, "Dung lượng ngắn, thường xoay quanh một tình huống", "Không có nhân vật", "Không dùng lời thoại", "Chỉ viết bằng thơ"),
        Q("VAN", "Đọc hiểu", 5, "Giọng điệu trữ tình trong văn bản bộc lộ điều gì?", 3, "Số liệu khoa học", "Lời hướng dẫn thí nghiệm", "Danh sách nhân vật", "Cảm xúc của người viết hoặc nhân vật"),
        Q("VAN", "Tập làm văn", 5, "Khi nghị luận một vấn đề, luận điểm cần có tính chất nào?", 1, "Mơ hồ, chưa cần chứng minh", "Rõ ràng và được làm sáng tỏ bằng lí lẽ, dẫn chứng", "Chỉ là một câu cảm thán", "Trùng với đề bài nguyên văn"),

        Q("ANH", "Từ vựng", 1, "Nghĩa của từ \"book\" là gì?", 0, "Quyển sách", "Cái bàn", "Ngôi nhà", "Con mèo"),
        Q("ANH", "Giao tiếp", 1, "Câu nào dùng để chào buổi sáng?", 1, "Good night", "Good morning", "Goodbye", "Thank you"),
        Q("ANH", "Ngữ pháp", 2, "She ___ a student.", 2, "are", "am", "is", "be"),
        Q("ANH", "Từ vựng", 2, "Từ trái nghĩa của \"hot\" là từ nào?", 0, "cold", "warm", "big", "fast"),
        Q("ANH", "Ngữ pháp", 3, "They ___ football every Sunday.", 1, "plays", "play", "played", "playing"),
        Q("ANH", "Đọc hiểu", 3, "\"Nam goes to school by bike\" cho biết Nam đến trường bằng gì?", 3, "Xe buýt", "Ô tô", "Đi bộ", "Xe đạp"),
        Q("ANH", "Ngữ pháp", 4, "If it rains, we ___ at home.", 1, "stayed", "will stay", "stays", "staying"),
        Q("ANH", "Đọc hiểu", 4, "\"The library closes at 5 p.m.\" Thư viện đóng cửa lúc mấy giờ?", 2, "5 giờ sáng", "4 giờ chiều", "5 giờ chiều", "6 giờ tối"),
        Q("ANH", "Ngữ pháp", 5, "Câu bị động đúng của \"They built this bridge in 1990\" là câu nào?", 0, "This bridge was built in 1990", "This bridge built in 1990", "This bridge is build in 1990", "This bridge were built in 1990"),
        Q("ANH", "Đọc hiểu", 5, "\"Although he was tired, he finished his homework.\" Cậu ấy đã làm gì?", 1, "Bỏ bài tập vì mệt", "Vẫn hoàn thành bài tập dù mệt", "Đi ngủ ngay", "Nhờ bạn làm hộ"),

        Q("KHTN", "Vật lý", 1, "Đơn vị đo nhiệt độ thường dùng trong đời sống ở Việt Nam là đơn vị nào?", 1, "Kilôgam", "Độ C", "Mét", "Giây"),
        Q("KHTN", "Hóa học", 1, "Kí hiệu hóa học của nước là chất nào?", 0, "H2O", "CO2", "NaCl", "O2"),
        Q("KHTN", "Sinh học", 2, "Quá trình nào của cây xanh tạo ra khí ôxi?", 2, "Hô hấp", "Thoát hơi nước", "Quang hợp", "Nảy mầm"),
        Q("KHTN", "Trái Đất", 2, "Trái Đất quay quanh ngôi sao nào?", 0, "Mặt Trời", "Mặt Trăng", "Sao Hỏa", "Sao Kim"),
        Q("KHTN", "Vật lý", 3, "Một vật đứng yên trên mặt đất. Lực nào cân bằng trọng lực?", 1, "Lực ma sát trượt", "Phản lực của mặt đất", "Lực đẩy Ác-si-mét", "Lực căng dây"),
        Q("KHTN", "Hóa học", 3, "Khí nào chiếm tỉ lệ lớn nhất trong không khí?", 3, "Ôxi", "Cacbon đioxit", "Hiđro", "Nitơ"),
        Q("KHTN", "Vật lý", 4, "Công suất 100 W trong 2 giờ tiêu thụ điện năng bao nhiêu?", 1, "50 Wh", "200 Wh", "100 Wh", "2 Wh"),
        Q("KHTN", "Sinh học", 4, "Hiện tượng nào là cơ chế chính giúp cơ thể chống lại vi sinh vật lạ?", 0, "Miễn dịch", "Tiêu hóa", "Bài tiết", "Vận động"),
        Q("KHTN", "Hóa học", 5, "Phản ứng trung hòa axit và bazơ tạo thành những chất nào?", 2, "Chỉ khí hiđro", "Kim loại và ôxi", "Muối và nước", "Chỉ khí cacbonic"),
        Q("KHTN", "Vật lý", 5, "Một mạch nối tiếp có hai điện trở 2 Ω và 4 Ω. Điện trở tương đương bằng bao nhiêu?", 1, "2 Ω", "6 Ω", "8 Ω", "4 Ω"),

        Q("SU", "Lịch sử Việt Nam", 1, "Nhà nước Văn Lang gắn với vua nào?", 0, "Hùng Vương", "Lý Thái Tổ", "Quang Trung", "Lê Lợi"),
        Q("SU", "Lịch sử thế giới", 1, "Kim tự tháp là di sản tiêu biểu của nền văn minh nào?", 1, "Lưỡng Hà", "Ai Cập", "Trung Hoa", "Hy Lạp"),
        Q("SU", "Cận hiện đại", 2, "Cách mạng tháng Tám năm 1945 diễn ra ở nước nào?", 0, "Việt Nam", "Trung Quốc", "Nhật Bản", "Pháp"),
        Q("SU", "Lịch sử thế giới", 2, "Chiến tranh thế giới thứ hai kết thúc vào năm nào?", 2, "1939", "1942", "1945", "1954"),
        Q("SU", "Lịch sử Việt Nam", 3, "Chiến thắng Điện Biên Phủ diễn ra vào năm nào?", 1, "1945", "1954", "1968", "1975"),
        Q("SU", "Cận hiện đại", 3, "Ngày 30 tháng 4 năm 1975 gắn với sự kiện nào?", 3, "Tuyên ngôn Độc lập", "Chiến thắng Điện Biên Phủ", "Hiệp định Giơ-ne-vơ", "Giải phóng miền Nam, thống nhất đất nước"),
        Q("SU", "Cổ trung đại", 4, "Nhà Lý dời đô từ Hoa Lư về đâu?", 0, "Thăng Long", "Phú Xuân", "Phong Châu", "Cổ Loa"),
        Q("SU", "Lịch sử thế giới", 4, "Cách mạng công nghiệp bắt đầu sớm nhất ở nước nào?", 1, "Pháp", "Anh", "Mỹ", "Đức"),
        Q("SU", "Cận hiện đại", 5, "Hội nghị Giơ-ne-vơ năm 1954 công nhận điều gì đối với Việt Nam?", 2, "Việt Nam là thuộc địa của Pháp", "Chia Việt Nam thành ba miền", "Độc lập, chủ quyền, thống nhất và toàn vẹn lãnh thổ", "Sáp nhập Việt Nam vào Trung Quốc"),
        Q("SU", "Lịch sử Việt Nam", 5, "Bình Ngô đại cáo do ai viết?", 1, "Nguyễn Trãi viết dưới thời nhà Trần", "Nguyễn Trãi", "Nguyễn Du", "Hồ Xuân Hương")
    ];

    private static QuestionSeed Q(
        string subject,
        string topic,
        byte level,
        string content,
        int correct,
        string a,
        string b,
        string c,
        string d) => new(subject, topic, level, content, correct, a, b, c, d);
}
