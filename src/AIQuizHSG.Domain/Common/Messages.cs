namespace AIQuizHSG.Domain.Common;

// Toàn bộ câu chữ trả về cho người dùng. Chỗ gọi chỉ dùng hằng số, không viết chuỗi trực tiếp.
public static class Messages
{
    public const string InvalidCredentials = "Tên đăng nhập hoặc mật khẩu không đúng.";
    public const string LockedAccount = "Tài khoản đã bị khóa.";
    public const string LoginFailedDetail = "Tên đăng nhập: {0}";
    public const string InvalidSession = "Phiên đăng nhập không hợp lệ.";
    public const string AccountInactive = "Tài khoản không còn hiệu lực.";

    public const string UsernameRequired = "Vui lòng nhập tên đăng nhập.";
    public const string PasswordRequired = "Vui lòng nhập mật khẩu.";

    public const string SubjectCodeRequired = "Vui lòng nhập mã môn học.";
    public const string SubjectCodeTooLong = "Mã môn học tối đa 50 ký tự.";
    public const string SubjectNameRequired = "Vui lòng nhập tên môn học.";
    public const string SubjectNameTooLong = "Tên môn học tối đa 200 ký tự.";
    public const string SubjectCodeExists = "Mã môn học đã tồn tại.";
    public const string SubjectNotFound = "Không tìm thấy môn học.";
    public const string SubjectInactive = "Môn học không tồn tại hoặc đã ngừng dùng.";
    public const string SubjectMissing = "Môn học không tồn tại.";
    public const string SubjectNotFoundByCode = "Không tìm thấy môn học '{0}'.";

    public const string DescriptionTooLong = "Mô tả tối đa 1000 ký tự.";
    public const string SubjectNotSelected = "Chưa chọn môn học.";
    public const string TopicNameRequired = "Vui lòng nhập tên chuyên đề.";
    public const string TopicNameTooLong = "Tên chuyên đề tối đa 200 ký tự.";
    public const string TopicNotFound = "Không tìm thấy chuyên đề.";
    public const string TopicInactive = "Chuyên đề không tồn tại hoặc đã ngừng dùng.";
    public const string TopicNotFoundInSubject = "Không tìm thấy chuyên đề '{0}' trong môn {1}.";
    public const string TopicCycle = "Không thể tạo vòng lặp chuyên đề cha-con.";
    public const string ParentTopicMissing = "Chuyên đề cha không tồn tại.";
    public const string ParentTopicDifferentSubject = "Chuyên đề cha phải cùng môn học.";
    public const string TopicTooDeep = "Cây chuyên đề quá sâu.";

    public const string TopicNotSelected = "Chưa chọn chuyên đề.";
    public const string QuestionContentRequired = "Vui lòng nhập nội dung câu hỏi.";
    public const string QuestionContentTooLong = "Nội dung câu hỏi tối đa 20000 ký tự.";
    public const string LevelInvalid = "Mức độ không hợp lệ.";
    public const string TypeInvalid = "Loại câu hỏi không hợp lệ.";
    public const string ExplanationTooLong = "Lời giải tối đa 20000 ký tự.";
    public const string OptionsRequired = "Câu hỏi cần danh sách phương án.";
    public const string QuestionNotFound = "Không tìm thấy câu hỏi.";
    public const string SubmitNotAllowed = "Chỉ gửi duyệt câu hỏi đang nháp hoặc bị từ chối.";
    public const string ApproveNotAllowed = "Chỉ duyệt câu hỏi đang chờ duyệt.";
    public const string RejectNotAllowed = "Chỉ từ chối câu hỏi đang chờ duyệt.";
    public const string RejectReasonRequired = "Cần nhập lý do từ chối.";
    public const string RejectReasonTooLong = "Lý do từ chối tối đa 1000 ký tự.";

    public const string NeedAtLeastTwoOptions = "Câu hỏi cần ít nhất 2 phương án.";
    public const string TooManyOptions = "Câu hỏi chỉ được tối đa 6 phương án.";
    public const string OptionContentRequired = "Nội dung phương án không được để trống.";
    public const string NeedOneCorrect = "Cần ít nhất một đáp án đúng.";
    public const string NeedOneWrong = "Phải có ít nhất một phương án sai.";
    public const string SingleChoiceOneCorrect = "Câu hỏi một đáp án chỉ được có một đáp án đúng.";

    public const string ExcelInvalid = "Tệp Excel không hợp lệ.";
    public const string ExcelUnreadable = "Không đọc được tệp Excel. Hãy dùng đúng mẫu .xlsx.";
    public const string ExcelNoSheet = "Tệp Excel không có sheet dữ liệu.";
    public const string ExcelMissingColumn = "Thiếu cột bắt buộc '{0}'. Hãy tải mẫu và nhập lại.";
    public const string ExcelRequiredCells = "MaMon, ChuyenDe và NoiDung là bắt buộc.";
    public const string ExcelLevelInvalid = "MucDo không hợp lệ. Dùng Dễ, Trung bình hoặc Khó.";
    public const string ExcelTypeInvalid = "Loai không hợp lệ. Dùng Một đáp án hoặc Nhiều đáp án.";
    public const string ExcelCorrectInvalid = "Dung{0} phải là Đúng hoặc Sai.";
    public const string ExcelNotSelected = "Chưa chọn tệp Excel.";
    public const string ExcelWrongExtension = "Chỉ nhận tệp .xlsx theo đúng mẫu.";
    public const string ImportAudit = "Nhập {0} câu.";

    public const string UsernameTooShort = "Tên đăng nhập tối thiểu 3 ký tự.";
    public const string UsernameTooLong = "Tên đăng nhập tối đa 50 ký tự.";
    public const string UsernameInvalid = "Tên đăng nhập không được chứa khoảng trắng.";
    public const string UsernameTaken = "Tên đăng nhập đã tồn tại.";
    public const string PasswordTooShort = "Mật khẩu tối thiểu 8 ký tự.";
    public const string PasswordTooLong = "Mật khẩu tối đa 100 ký tự.";
    public const string FullNameRequired = "Vui lòng nhập họ tên.";
    public const string FullNameTooLong = "Họ tên tối đa 200 ký tự.";
    public const string EmailTooLong = "Email tối đa 256 ký tự.";
    public const string EmailInvalid = "Email không hợp lệ.";
    public const string StudentRoleMissing = "Chưa có vai trò học sinh trong hệ thống.";
    public const string StudentNotFound = "Không tìm thấy học sinh.";

    public const string ExamNotFound = "Không tìm thấy đề thi.";
    public const string ExamTitleRequired = "Vui lòng nhập tiêu đề đề thi.";
    public const string ExamTitleTooLong = "Tiêu đề đề thi tối đa 300 ký tự.";
    public const string ExamDescriptionTooLong = "Mô tả đề thi tối đa 5000 ký tự.";
    public const string ExamDurationInvalid = "Thời lượng làm bài phải từ 1 đến 300 phút.";
    public const string ExamWindowInvalid = "Thời gian đóng phải sau thời gian mở.";
    public const string ExamAttemptsInvalid = "Số lượt làm tối đa phải từ 1 đến 10.";
    public const string ExamQuestionsRequired = "Đề thi cần ít nhất một câu hỏi.";
    public const string ExamTooManyQuestions = "Đề thi chỉ được tối đa 100 câu.";
    public const string ExamQuestionDuplicate = "Đề thi không được chứa câu hỏi trùng.";
    public const string ExamPointsInvalid = "Điểm mỗi câu phải lớn hơn 0 và không quá 100.";
    public const string ExamQuestionNotApproved = "Chỉ đưa câu đã duyệt, cùng môn với đề thi.";
    public const string ExamParticipantInvalid = "Danh sách thí sinh có tài khoản không phải học sinh đang hoạt động.";
    public const string ExamParticipantDuplicate = "Danh sách thí sinh bị trùng.";
    public const string ExamTooManyParticipants = "Danh sách thí sinh tối đa 500 người.";
    public const string ExamLocked = "Chỉ sửa đề đang ở trạng thái nháp.";
    public const string ExamPublishEmpty = "Chưa có câu hỏi nên chưa phát hành được.";
    public const string ExamPublishNotDraft = "Chỉ phát hành đề đang nháp.";
    public const string ExamCloseNotPublished = "Chỉ đóng đề đã phát hành.";
    public const string ExamNotOpen = "Đề thi chưa mở hoặc đã đóng.";
    public const string ExamNotParticipant = "Bạn không có trong danh sách thí sinh của đề này.";
    public const string ExamAttemptLimit = "Bạn đã hết số lượt làm bài.";
    public const string AttemptNotFound = "Không tìm thấy lượt làm bài.";
    public const string AttemptFinished = "Lượt làm bài đã nộp.";
    public const string AttemptQuestionInvalid = "Câu hỏi không thuộc đề của lượt làm này.";
    public const string AttemptOptionInvalid = "Phương án không thuộc câu hỏi này.";
    public const string AttemptSingleChoice = "Câu một đáp án chỉ được chọn một phương án.";
    public const string AttemptTooManyOptions = "Chỉ được chọn tối đa 6 phương án.";

    public const string DifficultyLevelNotFound = "Mức độ câu hỏi không tồn tại hoặc đã ngừng dùng.";
    public const string DifficultyCatalogMissing = "Chưa có danh mục mức độ câu hỏi.";
    public const string ProfileNotFound = "Không tìm thấy profile độ khó.";
    public const string ProfileNameRequired = "Vui lòng nhập tên profile.";
    public const string ProfileNameTooLong = "Tên profile tối đa 200 ký tự.";
    public const string TargetDifficultyInvalid = "Độ khó mục tiêu phải từ 1 đến 5.";
    public const string ProfileQuestionCountInvalid = "Tổng số câu gốc phải lớn hơn 0.";
    public const string ScalingModeInvalid = "Chế độ ma trận phải là PROPORTIONAL hoặc FIXED_COUNTS.";
    public const string ProfileDistributionInvalid = "Cơ cấu 5 mức không khớp tổng số câu của profile.";
    public const string ProfileLevelInvalid = "Mỗi mức từ 1 đến 5 phải xuất hiện đúng một lần.";
    public const string ProfileNotEditable = "Profile này không còn được sửa.";
    public const string ProfileFixedCount = "Chế độ FIXED_COUNTS không co giãn. Tổng câu phải bằng {0}.";
    public const string GenerateQuestionCountInvalid = "Tổng số câu phải lớn hơn 0.";
    public const string ShortageMessage = "Mức {0} còn thiếu {1} câu hỏi hợp lệ.";
    public const string ExamDistributionInvalid = "Cơ cấu mức độ của đề không khớp profile.";
    public const string IdempotencyConflict = "Mã sinh đề đã được dùng bởi tài khoản khác.";
    public const string IdempotencyKeyTooLong = "Mã sinh đề tối đa 100 ký tự.";
    public const string ExcelDifficultyInvalid = "MucDoCauHoi không hợp lệ. Dùng tên hoặc mã mức từ 1 đến 5.";
    public const string ExamHasNoQuestions = "Đề chưa có câu hỏi.";
}
