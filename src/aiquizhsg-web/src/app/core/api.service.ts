import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from './environment';
import {
  AttemptPaper,
  AttemptSummary,
  Availability,
  Classroom,
  ClassroomExamResult,
  DashboardReport,
  DifficultyLevel,
  DrawnQuestion,
  ExamAssignment,
  ExamDetail,
  ExamProfile,
  ExamSummary,
  ExamValidation,
  AccountDirectory,
  AccountRole,
  AccountUser,
  PermissionItem,
  UserPermissionGrant,
  OmrBatch,
  OmrFormStyle,
  OmrResultRow,
  OmrSheet,
  OmrSheetDetail,
  OmrSheetPage,
  OmrTemplate,
  OmrUploadResult,
  GradeLevel,
  ImportResult,
  ProfilePreview,
  QuestionDetail,
  QuestionPage,
  SaveExam,
  SaveQuestion,
  School,
  SchoolYear,
  StudentAccount,
  StudentProfile,
  Subject,
  Topic,
} from './models';

type Query = Record<string, string | number | null | undefined>;

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  dashboard(): Observable<DashboardReport> {
    return this.get('/reports/dashboard');
  }

  subjects(): Observable<Subject[]> {
    return this.get('/subjects');
  }

  saveSubject(body: Omit<Subject, 'id'>, id?: number): Observable<Subject> {
    return id ? this.put(`/subjects/${id}`, body) : this.post('/subjects', body);
  }

  topics(subjectId?: number | null): Observable<Topic[]> {
    return this.get('/topics', { subjectId });
  }

  saveTopic(body: Omit<Topic, 'id'>, id?: number): Observable<Topic> {
    return id ? this.put(`/topics/${id}`, body) : this.post('/topics', body);
  }

  questions(query: Query): Observable<QuestionPage> {
    return this.get('/questions', query);
  }

  question(id: number): Observable<QuestionDetail> {
    return this.get(`/questions/${id}`);
  }

  saveQuestion(body: SaveQuestion, id?: number): Observable<QuestionDetail> {
    return id ? this.put(`/questions/${id}`, body) : this.post('/questions', body);
  }

  submitQuestion(id: number): Observable<QuestionDetail> {
    return this.post(`/questions/${id}/submit`, {});
  }

  approveQuestion(id: number): Observable<QuestionDetail> {
    return this.post(`/questions/${id}/approve`, {});
  }

  rejectQuestion(id: number, reason: string): Observable<QuestionDetail> {
    return this.post(`/questions/${id}/reject`, { reason });
  }

  questionTemplate(): Observable<Blob> {
    return this.blob('/questions/import-template');
  }

  importQuestions(file: File): Observable<ImportResult> {
    return this.upload('/questions/import', file);
  }

  difficultyLevels(): Observable<DifficultyLevel[]> {
    return this.get('/difficulty-levels');
  }

  exams(): Observable<ExamSummary[]> {
    return this.get('/exams');
  }

  exam(id: number): Observable<ExamDetail> {
    return this.get(`/exams/${id}`);
  }

  saveExam(body: SaveExam, id?: number): Observable<ExamDetail> {
    return id ? this.put(`/exams/${id}`, body) : this.post('/exams', body);
  }

  publishExam(id: number): Observable<ExamDetail> {
    return this.post(`/exams/${id}/publish`, {});
  }

  closeExam(id: number): Observable<ExamDetail> {
    return this.post(`/exams/${id}/close`, {});
  }

  validateExam(id: number): Observable<ExamValidation> {
    return this.post(`/exams/${id}/validate`, {});
  }

  generateExam(body: unknown): Observable<ExamDetail> {
    return this.post('/exams/generate', body);
  }

  examAssignments(examId: number): Observable<ExamAssignment[]> {
    return this.get(`/exams/${examId}/assignments`);
  }

  assignExam(examId: number, body: unknown): Observable<ExamAssignment> {
    return this.post(`/exams/${examId}/assignments`, body);
  }

  profiles(): Observable<ExamProfile[]> {
    return this.get('/exam-difficulty-profiles');
  }

  saveProfile(body: unknown): Observable<ExamProfile> {
    return this.post('/exam-difficulty-profiles', body);
  }

  previewProfile(id: number, questionCount: number): Observable<ProfilePreview> {
    return this.post(`/exam-difficulty-profiles/${id}/preview`, { questionCount });
  }

  availability(body: unknown): Observable<Availability> {
    return this.post('/question-bank/availability', body);
  }

  drawQuestions(body: unknown): Observable<{ questions: DrawnQuestion[] }> {
    return this.post('/question-bank/draw', body);
  }

  myExams(): Observable<ExamSummary[]> {
    return this.get('/my-exams');
  }

  startAttempt(examId: number): Observable<AttemptPaper> {
    return this.post(`/my-exams/${examId}/attempts`, {});
  }

  attempts(): Observable<AttemptSummary[]> {
    return this.get('/attempts');
  }

  attempt(id: number): Observable<AttemptPaper> {
    return this.get(`/attempts/${id}`);
  }

  saveAnswer(attemptId: number, examQuestionId: number, optionIds: number[]): Observable<AttemptPaper> {
    return this.put(`/attempts/${attemptId}/answers/${examQuestionId}`, { optionIds });
  }

  submitAttempt(id: number): Observable<AttemptSummary> {
    return this.post(`/attempts/${id}/submit`, {});
  }

  studentAccounts(): Observable<StudentAccount[]> {
    return this.get('/students');
  }

  createStudentAccount(body: unknown): Observable<StudentAccount> {
    return this.post('/students', body);
  }

  updateStudentAccount(id: number, body: unknown): Observable<StudentAccount> {
    return this.put(`/students/${id}`, body);
  }

  lockStudent(id: number): Observable<StudentAccount> {
    return this.post(`/students/${id}/lock`, {});
  }

  unlockStudent(id: number): Observable<StudentAccount> {
    return this.post(`/students/${id}/unlock`, {});
  }

  accounts(): Observable<AccountDirectory> {
    return this.get('/accounts');
  }

  createAccount(body: {
    username: string;
    password: string;
    fullName: string;
    email: string | null;
    roleIds: number[];
  }): Observable<AccountUser> {
    return this.post('/accounts', body);
  }

  updateAccount(id: number, body: { fullName: string; email: string | null; password: string | null }): Observable<AccountUser> {
    return this.put(`/accounts/${id}`, body);
  }

  lockAccount(id: number): Observable<AccountUser> {
    return this.post(`/accounts/${id}/lock`, {});
  }

  unlockAccount(id: number): Observable<AccountUser> {
    return this.post(`/accounts/${id}/unlock`, {});
  }

  roleList(): Observable<AccountRole[]> {
    return this.get('/roles');
  }

  replaceUserRoles(userId: number, roleIds: number[]): Observable<AccountRole[]> {
    return this.put(`/users/${userId}/roles`, { roleIds });
  }

  permissionCatalog(): Observable<PermissionItem[]> {
    return this.get('/permissions');
  }

  rolePermissions(roleId: number): Observable<PermissionItem[]> {
    return this.get(`/roles/${roleId}/permissions`);
  }

  saveRolePermissions(roleId: number, permissionIds: number[]): Observable<PermissionItem[]> {
    return this.put(`/roles/${roleId}/permissions`, { permissionIds });
  }

  userPermissions(userId: number): Observable<UserPermissionGrant[]> {
    return this.get(`/users/${userId}/permissions`);
  }

  saveUserPermissions(userId: number, grants: { permissionId: number; effect: string }[]): Observable<UserPermissionGrant[]> {
    return this.put(`/users/${userId}/permissions`, { grants });
  }

  schools(): Observable<School[]> {
    return this.get('/schools');
  }

  saveSchool(body: unknown, id?: number): Observable<School> {
    return id ? this.put(`/schools/${id}`, body) : this.post('/schools', body);
  }

  schoolYears(schoolId: number): Observable<SchoolYear[]> {
    return this.get(`/schools/${schoolId}/school-years`);
  }

  saveSchoolYear(schoolId: number, body: unknown, id?: number): Observable<SchoolYear> {
    return id ? this.put(`/school-years/${id}`, body) : this.post(`/schools/${schoolId}/school-years`, body);
  }

  gradeLevels(): Observable<GradeLevel[]> {
    return this.get('/grade-levels');
  }

  classrooms(schoolId: number, schoolYearId?: number | null): Observable<Classroom[]> {
    return this.get(`/schools/${schoolId}/classrooms`, { schoolYearId });
  }

  saveClassroom(schoolId: number, body: unknown, id?: number): Observable<Classroom> {
    return id ? this.put(`/classrooms/${id}`, body) : this.post(`/schools/${schoolId}/classrooms`, body);
  }

  studentProfiles(schoolId: number, keyword?: string): Observable<StudentProfile[]> {
    return this.get(`/schools/${schoolId}/students`, { keyword });
  }

  saveStudentProfile(schoolId: number, body: unknown, id?: number): Observable<StudentProfile> {
    return id ? this.put(`/student-records/${id}`, body) : this.post(`/schools/${schoolId}/students`, body);
  }

  enroll(classroomId: number, studentId: number): Observable<StudentProfile> {
    return this.post(`/classrooms/${classroomId}/students`, { studentId });
  }

  transfer(studentId: number, classroomId: number): Observable<StudentProfile> {
    return this.post(`/student-records/${studentId}/transfer`, { classroomId });
  }

  studentTemplate(schoolId: number): Observable<Blob> {
    return this.blob(`/schools/${schoolId}/students/import-template`);
  }

  importStudents(schoolId: number, yearId: number, file: File): Observable<ImportResult> {
    return this.upload(`/schools/${schoolId}/school-years/${yearId}/students/import`, file);
  }

  classroomResults(classroomId: number): Observable<ClassroomExamResult[]> {
    return this.get(`/reports/classrooms/${classroomId}/exam-results`);
  }

  omrTemplatePreview(id: number): Observable<Blob> {
    return this.blob(`/omr/templates/${id}/preview`);
  }

  omrTemplateDraft(questionCount: number, choiceCount: number, style: string): Observable<Blob> {
    return this.http.get(`${apiUrl}/omr/templates/preview`, {
      params: { questionCount, choiceCount, style },
      responseType: 'blob',
    });
  }

  omrFormStyles(): Observable<OmrFormStyle[]> {
    return this.get('/omr/template-styles');
  }

  omrTemplates(): Observable<OmrTemplate[]> {
    return this.get('/omr/templates');
  }

  saveOmrTemplate(body: {
    code: string;
    version: number;
    questionCount: number;
    choiceCount: number;
    style: string;
    isActive: boolean;
  }): Observable<OmrTemplate> {
    return this.post('/omr/templates', body);
  }

  omrBatches(): Observable<OmrBatch[]> {
    return this.get('/omr/batches');
  }

  omrBatch(id: number): Observable<OmrBatch> {
    return this.get(`/omr/batches/${id}`);
  }

  createOmrBatch(body: { examId: number; templateId: number; sourceType: string }): Observable<OmrBatch> {
    return this.post('/omr/batches', body);
  }

  omrSheets(batchId: number, status: string | null, page: number, pageSize: number): Observable<OmrSheetPage> {
    return this.get(`/omr/batches/${batchId}/sheets`, { status, page, pageSize });
  }

  uploadOmrSheets(batchId: number, files: File[], sourceType: string): Observable<OmrUploadResult> {
    const body = new FormData();
    for (const file of files) body.append('files', file, file.name);
    body.append('sourceType', sourceType);
    return this.post(`/omr/batches/${batchId}/sheets`, body);
  }

  retryOmrSheet(batchId: number, sheetId: number): Observable<OmrSheet> {
    return this.post(`/omr/batches/${batchId}/sheets/${sheetId}/retry`, {});
  }

  omrSheet(id: number): Observable<OmrSheetDetail> {
    return this.get(`/omr/sheets/${id}`);
  }

  omrSheetFile(id: number): Observable<Blob> {
    return this.blob(`/omr/sheets/${id}/file`);
  }

  reviewOmrSheet(id: number, body: {
    studentCode: string | null;
    examCode: string | null;
    reviewNote: string | null;
    answers: { examQuestionId: number; selectedOptionIds: number[] }[];
  }): Observable<OmrSheetDetail> {
    return this.put(`/omr/sheets/${id}/review`, body);
  }

  gradeOmrBatch(id: number): Observable<OmrBatch> {
    return this.post(`/omr/batches/${id}/grade`, {});
  }

  approveOmrBatch(id: number): Observable<OmrBatch> {
    return this.post(`/omr/batches/${id}/approve`, {});
  }

  publishOmrBatch(id: number): Observable<OmrBatch> {
    return this.post(`/omr/batches/${id}/publish`, {});
  }

  omrResults(id: number): Observable<OmrResultRow[]> {
    return this.get(`/omr/batches/${id}/results`);
  }

  omrExport(id: number): Observable<Blob> {
    return this.blob(`/omr/batches/${id}/export`);
  }

  private get<T>(path: string, query?: Query): Observable<T> {
    return this.http.get<T>(`${apiUrl}${path}`, { params: this.params(query) });
  }

  private post<T>(path: string, body: unknown): Observable<T> {
    return this.http.post<T>(`${apiUrl}${path}`, body);
  }

  private put<T>(path: string, body: unknown): Observable<T> {
    return this.http.put<T>(`${apiUrl}${path}`, body);
  }

  private blob(path: string): Observable<Blob> {
    return this.http.get(`${apiUrl}${path}`, { responseType: 'blob' });
  }

  private upload<T>(path: string, file: File): Observable<T> {
    const body = new FormData();
    body.append('file', file);
    return this.http.post<T>(`${apiUrl}${path}`, body);
  }

  private params(query?: Query): HttpParams {
    let params = new HttpParams();
    if (!query) return params;
    for (const [key, value] of Object.entries(query)) {
      if (value === null || value === undefined || value === '') continue;
      params = params.set(key, String(value));
    }
    return params;
  }
}
