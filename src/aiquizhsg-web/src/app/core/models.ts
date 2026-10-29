export interface UserProfile {
  id: number;
  username: string;
  fullName: string;
  email: string | null;
  roles: string[];
}

export interface Session {
  token: string;
  expiresAtUtc: string;
  user: UserProfile;
}

export interface Subject {
  id: number;
  code: string;
  name: string;
  description: string | null;
  isActive: boolean;
}

export interface Topic {
  id: number;
  subjectId: number;
  parentTopicId: number | null;
  name: string;
  description: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface OptionInput {
  content: string;
  isCorrect: boolean;
}

export interface OptionResponse extends OptionInput {
  id: number;
  sortOrder: number;
}

export interface QuestionSummary {
  id: number;
  topicId: number;
  topicName: string;
  subjectId: number;
  subjectName: string;
  content: string;
  level: string;
  type: string;
  status: string;
  source: string;
  updatedAtUtc: string;
  difficultyLevelId: number | null;
  difficultyCode: string | null;
  difficultyName: string | null;
}

export interface QuestionDetail extends QuestionSummary {
  explanation: string | null;
  rejectionReason: string | null;
  createdById: number;
  options: OptionResponse[];
}

export interface QuestionPage {
  total: number;
  items: QuestionSummary[];
}

export interface SaveQuestion {
  topicId: number;
  content: string;
  level: string;
  difficultyLevelId: number | null;
  type: string;
  explanation: string | null;
  options: OptionInput[];
}

export interface DifficultyLevel {
  id: number;
  code: string;
  name: string;
  description: string | null;
  sortOrder: number;
}

export interface ImportError {
  row: number;
  message: string;
}

export interface ImportResult {
  imported: number;
  errors: ImportError[];
}

export interface ExamDrawSeat {
  studentId: number;
  studentCode: string;
  fullName: string;
  examId: number;
  examTitle: string;
}

export interface ExamDrawResult {
  classroomId: number;
  classroomName: string;
  seats: ExamDrawSeat[];
}

export interface ExamAssignment {
  id: number;
  examId: number;
  classroomId: number | null;
  studentId: number | null;
  assignedAtUtc: string;
  dueAtUtc: string | null;
  classroomName: string | null;
  studentName: string | null;
}

export interface ExamSummary {
  id: number;
  title: string;
  subjectId: number;
  subjectName: string;
  durationMinutes: number;
  status: string;
  questionCount: number;
  participantCount: number;
  publishResults: boolean;
  opensAtUtc: string | null;
  closesAtUtc: string | null;
}

export interface ExamQuestionInput {
  questionId: number;
  points: number;
}

export interface ExamQuestion {
  id: number;
  sourceQuestionId: number | null;
  topicId: number | null;
  content: string;
  type: string;
  level: string;
  points: number;
  sortOrder: number;
  difficultyLevelId: number | null;
  options: OptionResponse[];
}

export interface ExamParticipant {
  userId: number;
  fullName: string;
  username: string;
}

export interface ExamDetail {
  id: number;
  title: string;
  description: string | null;
  subjectId: number;
  subjectName: string;
  durationMinutes: number;
  opensAtUtc: string | null;
  closesAtUtc: string | null;
  shuffleQuestions: boolean;
  shuffleOptions: boolean;
  maxAttempts: number;
  publishResults: boolean;
  status: string;
  publishedAtUtc: string | null;
  questions: ExamQuestion[];
  participants: ExamParticipant[];
}

export interface SaveExam {
  title: string;
  description: string | null;
  subjectId: number;
  durationMinutes: number;
  opensAtUtc: string | null;
  closesAtUtc: string | null;
  shuffleQuestions: boolean;
  shuffleOptions: boolean;
  maxAttempts: number;
  publishResults: boolean;
  questions: ExamQuestionInput[];
  participantUserIds: number[];
}

export interface PaperOption {
  id: number;
  content: string;
}

export interface PaperQuestion {
  examQuestionId: number;
  content: string;
  type: string;
  level: string;
  points: number;
  options: PaperOption[];
  selectedOptionIds: number[];
}

export interface AttemptPaper {
  id: number;
  examId: number;
  title: string;
  attemptNumber: number;
  status: string;
  startedAtUtc: string;
  dueAtUtc: string;
  serverNowUtc: string;
  questions: PaperQuestion[];
}

export interface AttemptSummary {
  id: number;
  examId: number;
  examTitle: string;
  attemptNumber: number;
  status: string;
  startedAtUtc: string;
  dueAtUtc: string;
  submittedAtUtc: string | null;
}

export interface StudentAccount {
  id: number;
  username: string;
  fullName: string;
  email: string | null;
  status: string;
}

export interface AccountRole {
  id: number;
  code: string;
  name: string;
}

export interface AccountUser {
  id: number;
  username: string;
  fullName: string;
  email: string | null;
  status: string;
  roles: AccountRole[];
}

export interface AccountDirectory {
  users: AccountUser[];
  roles: AccountRole[];
}

export interface PermissionItem {
  id: number;
  code: string;
  name: string;
  module: string;
  description: string | null;
}

export interface UserPermissionGrant {
  permissionId: number;
  code: string;
  name: string;
  module: string;
  effect: 'Allow' | 'Deny';
}

export interface ExamProfile {
  id: number;
  name: string;
  targetDifficulty: number;
  targetDifficultyName: string;
  baseQuestionCount: number;
  scalingMode: string;
  isSystemDefault: boolean;
  version: number;
  status: string;
  levels: { level: number; name: string; count: number; ratio: number }[];
}

export interface ProfilePreview {
  targetDifficulty: number;
  questionCount: number;
  scalingMode: string;
  distribution: { level: number; count: number }[];
  isValid: boolean;
  message: string | null;
}

export interface DrawnQuestion {
  id: number;
  content: string;
  topicName: string;
  level: string;
  type: string;
}

export interface Availability {
  canGenerate: boolean;
  requiredQuestionCount: number;
  availableQuestionCount: number;
  shortages: { level: number; required: number; available: number; missing: number }[];
  errors: { code: string; message: string }[];
}

export interface ExamValidation {
  isValid: boolean;
  errors: { code: string; message: string }[];
}

export interface School {
  id: number;
  code: string;
  name: string;
  address: string | null;
  provinceCode: string | null;
  districtCode: string | null;
  phone: string | null;
  email: string | null;
  isActive: boolean;
}

export interface SchoolYear {
  id: number;
  schoolId: number;
  name: string;
  startDate: string;
  endDate: string;
  isCurrent: boolean;
  status: string;
}

export interface GradeLevel {
  id: number;
  code: string;
  name: string;
  educationLevel: string;
  sortOrder: number;
  isActive: boolean;
}

export interface Classroom {
  id: number;
  schoolId: number;
  schoolYearId: number;
  schoolYearName: string;
  gradeLevelId: number;
  gradeLevelName: string;
  classCode: string;
  name: string;
  homeroomTeacherUserId: number | null;
  homeroomTeacherName: string | null;
  capacity: number | null;
  studentCount: number;
  status: string;
}

export interface StudentProfile {
  id: number;
  schoolId: number;
  studentCode: string;
  fullName: string;
  dateOfBirth: string | null;
  gender: string | null;
  email: string | null;
  phone: string | null;
  address: string | null;
  userId: number | null;
  status: string;
  classroomId: number | null;
  classCode: string | null;
}

export interface OmrFormStyle {
  id: number;
  code: string;
  name: string;
  description: string | null;
  sortOrder: number;
}

export interface OmrTemplateSection {
  questionCount: number;
  choiceCount: number;
  kind: string;
  itemCount: number;
}

export function omrChoiceLabel(template: { choiceCount: number; sections?: OmrTemplateSection[] }): string {
  const parts = template.sections ?? [];
  if (!parts.length) return String(template.choiceCount);
  return parts
    .map((part) => {
      if (part.kind === 'DungSai') return `${part.questionCount} đúng/sai`;
      if (part.kind === 'DienSo') return `${part.questionCount} số`;
      return `${part.questionCount}×4`;
    })
    .join(' · ');
}

export interface OmrTemplate {
  id: number;
  code: string;
  version: number;
  questionCount: number;
  choiceCount: number;
  isActive: boolean;
  style: string | null;
  createdAtUtc: string;
  sections?: OmrTemplateSection[];
}

export interface OmrBatch {
  id: number;
  examId: number;
  examTitle: string;
  templateId: number;
  templateCode: string;
  examCode: string;
  sourceType: string;
  status: string;
  totalSheets: number;
  processedSheets: number;
  failedSheets: number;
  needsReviewSheets: number;
  createdAtUtc: string;
}

export interface OmrSheet {
  id: number;
  pageNumber: number;
  fileName: string;
  studentCode: string | null;
  examCode: string | null;
  status: string;
  reviewStatus: string;
  confidence: number;
  failureReason: string | null;
  examAttemptId: number | null;
}

export interface OmrSheetPage {
  items: OmrSheet[];
  total: number;
}

export interface OmrChoice {
  id: number;
  content: string;
  sortOrder: number;
}

export interface OmrAnswer {
  questionNumber: number;
  examQuestionId: number | null;
  content: string | null;
  questionType: string | null;
  recognitionStatus: string;
  confidence: number;
  recognizedOptionId: number | null;
  confirmedOptionIds: number[];
  choices: OmrChoice[];
  recognizedText?: string | null;
}

export interface OmrSheetDetail {
  id: number;
  batchId: number;
  status: string;
  reviewStatus: string;
  confidence: number;
  studentCode: string | null;
  examCode: string | null;
  expectedExamCode: string;
  failureReason: string | null;
  reviewNote: string | null;
  answers: OmrAnswer[];
  batchStatus: string;
}

export interface OmrScanResult {
  accepted: boolean;
  message: string;
  batchId: number | null;
  sheetId: number | null;
  templateCode: string | null;
  examTitle: string | null;
  studentCode: string | null;
  studentName: string | null;
  examCode: string | null;
  score: number | null;
  maxScore: number | null;
  correct: number;
  wrong: number;
  blank: number;
  status: string | null;
}

export interface OmrUploadResult {
  batchId: number;
  sheets: { sheetId: number; fileName: string; pageNumber: number; duplicate: boolean; status: string }[];
}

export interface OmrResultRow {
  sheetId: number;
  studentCode: string | null;
  studentName: string | null;
  examCode: string | null;
  correct: number;
  wrong: number;
  blank: number;
  score: number | null;
  maxScore: number | null;
  status: string;
}

export interface CountItem {
  key: string;
  count: number;
}

export interface SubjectCount {
  name: string;
  count: number;
}

export interface DifficultyCount {
  name: string;
  sortOrder: number;
  count: number;
}

export interface ExamReportLine {
  id: number;
  title: string;
  subjectName: string;
  status: string;
  questionCount: number;
  submitted: number;
  inProgress: number;
  averagePercent: number | null;
}

export interface DashboardReport {
  questionCount: number;
  examCount: number;
  attemptCount: number;
  studentCount: number;
  subjectCount: number;
  omrSheetCount: number;
  omrNeedsReview: number;
  questionStatus: CountItem[];
  questionDifficulty: DifficultyCount[];
  questionsBySubject: SubjectCount[];
  examStatus: CountItem[];
  attemptStatus: CountItem[];
  scoreBands: CountItem[];
  exams: ExamReportLine[];
  omrStatus: CountItem[];
}

export interface ClassroomExamResult {
  studentId: number;
  studentCode: string;
  fullName: string;
  exams: {
    examId: number;
    title: string;
    attemptStatus: string | null;
    score: number | null;
    maxScore: number | null;
  }[];
}
