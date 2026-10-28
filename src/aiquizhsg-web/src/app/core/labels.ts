export const labels = {
  level: { Easy: 'Dễ', Medium: 'Trung bình', Hard: 'Khó' },
  type: { SingleChoice: 'Một đáp án', MultipleChoice: 'Nhiều đáp án' },
  questionStatus: {
    Draft: 'Nháp',
    PendingReview: 'Chờ duyệt',
    Approved: 'Đã duyệt',
    Rejected: 'Từ chối',
  },
  examStatus: { Draft: 'Nháp', Published: 'Đang mở', Closed: 'Đã đóng' },
  attemptStatus: { InProgress: 'Đang làm', Submitted: 'Đã nộp', Graded: 'Đã chấm' },
  userStatus: { Active: 'Đang hoạt động', Locked: 'Đã khóa' },
  profileStatus: { Draft: 'Nháp', Active: 'Đang dùng', Archived: 'Lưu trữ' },
  yearStatus: { Planned: 'Dự kiến', Open: 'Đang mở', Closed: 'Đã đóng' },
  roomStatus: { Active: 'Đang học', Archived: 'Lưu trữ' },
  studentStatus: { Active: 'Đang học', Inactive: 'Ngừng học' },
  gender: { Male: 'Nam', Female: 'Nữ', Other: 'Khác' },
  scaling: { PROPORTIONAL: 'Tỷ lệ', FIXED_COUNTS: 'Số câu cố định' },
  role: {
    ADMIN: 'Quản trị',
    TEACHER: 'Giáo viên',
    STUDENT: 'Học sinh',
    SCHOOL_ADMIN: 'Quản trị trường',
  },
  omrBatch: {
    Draft: 'Nháp',
    Uploading: 'Đang tải',
    Processing: 'Đang nhận dạng',
    Reviewing: 'Chờ đối soát',
    Approved: 'Đã duyệt',
    Published: 'Đã công bố',
  },
  omrSheet: {
    Uploaded: 'Chờ xử lý',
    Processing: 'Đang nhận dạng',
    NeedsReview: 'Cần đối soát',
    Recognized: 'Đã nhận',
    Confirmed: 'Đã xác nhận',
    Graded: 'Đã chấm',
    Failed: 'Lỗi',
  },
  omrReview: { Pending: 'Chưa đối soát', Confirmed: 'Đã xác nhận' },
  omrMark: { Blank: 'Trống', Marked: 'Đã tô', Ambiguous: 'Mơ hồ' },
  omrSource: { Camera: 'Máy ảnh', Scanner: 'Máy quét', Mixed: 'Hỗn hợp' },
};

export function label(group: object, key: string | null | undefined): string {
  if (!key) return '—';
  const value = (group as Record<string, string>)[key];
  return value ?? key;
}

export const choices = {
  level: [
    { label: 'Dễ', value: 'Easy' },
    { label: 'Trung bình', value: 'Medium' },
    { label: 'Khó', value: 'Hard' },
  ],
  questionStatus: [
    { label: 'Nháp', value: 'Draft' },
    { label: 'Chờ duyệt', value: 'PendingReview' },
    { label: 'Đã duyệt', value: 'Approved' },
    { label: 'Từ chối', value: 'Rejected' },
  ],
  questionType: [
    { label: 'Một đáp án', value: 'SingleChoice' },
    { label: 'Nhiều đáp án', value: 'MultipleChoice' },
  ],
  yearStatus: [
    { label: 'Dự kiến', value: 'Planned' },
    { label: 'Đang mở', value: 'Open' },
    { label: 'Đã đóng', value: 'Closed' },
  ],
  gender: [
    { label: 'Nam', value: 'Male' },
    { label: 'Nữ', value: 'Female' },
    { label: 'Khác', value: 'Other' },
  ],
  scaling: [
    { label: 'Tỷ lệ', value: 'PROPORTIONAL' },
    { label: 'Số câu cố định', value: 'FIXED_COUNTS' },
  ],
  omrSource: [
    { label: 'Máy quét', value: 'Scanner' },
    { label: 'Máy ảnh', value: 'Camera' },
  ],
  omrSheet: [
    { label: 'Chờ xử lý', value: 'Uploaded' },
    { label: 'Đang nhận dạng', value: 'Processing' },
    { label: 'Cần đối soát', value: 'NeedsReview' },
    { label: 'Đã nhận', value: 'Recognized' },
    { label: 'Đã xác nhận', value: 'Confirmed' },
    { label: 'Đã chấm', value: 'Graded' },
    { label: 'Lỗi', value: 'Failed' },
  ],
};

export function tagColor(status: string | null | undefined): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
  switch (status) {
    case 'Approved':
    case 'Published':
    case 'Active':
    case 'Open':
    case 'Graded':
    case 'Marked':
      return 'success';
    case 'PendingReview':
    case 'NeedsReview':
    case 'Reviewing':
    case 'InProgress':
    case 'Processing':
    case 'Uploading':
    case 'Uploaded':
    case 'Planned':
    case 'Draft':
    case 'Pending':
    case 'Ambiguous':
      return 'warn';
    case 'Rejected':
    case 'Locked':
    case 'Closed':
    case 'Inactive':
    case 'Failed':
      return 'danger';
    case 'Submitted':
    case 'Recognized':
    case 'Confirmed':
      return 'info';
    default:
      return 'secondary';
  }
}

export function badgeClass(status: string | null | undefined): string {
  switch (status) {
    case 'Approved':
    case 'Published':
    case 'Active':
    case 'Open':
    case 'Graded':
      return 'badge text-bg-success';
    case 'PendingReview':
    case 'InProgress':
    case 'Planned':
    case 'Draft':
      return 'badge text-bg-warning';
    case 'Rejected':
    case 'Locked':
    case 'Closed':
    case 'Inactive':
      return 'badge text-bg-danger';
    case 'Submitted':
      return 'badge text-bg-primary';
    default:
      return 'badge text-bg-secondary';
  }
}
