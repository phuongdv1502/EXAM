import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { blankToNull, saveBlob } from '../core/format';
import {
  Classroom,
  ClassroomExamResult,
  GradeLevel,
  ImportError,
  School,
  SchoolYear,
  StudentProfile,
} from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';

@Component({
  selector: 'app-schools',
  imports: [FormsModule, ...UI],
  templateUrl: './schools.html',
})
export class SchoolsPage extends Page implements OnInit {
  schools: School[] = [];
  years: SchoolYear[] = [];
  grades: GradeLevel[] = [];
  rooms: Classroom[] = [];
  students: StudentProfile[] = [];
  selected: School | null = null;
  tab: 'info' | 'years' | 'classes' | 'students' = 'info';

  schoolTabs(): { label: string; value: string; disabled?: boolean }[] {
    const locked = !this.selected;
    return [
      { label: 'Thông tin', value: 'info' },
      { label: 'Năm học', value: 'years', disabled: locked },
      { label: 'Lớp', value: 'classes', disabled: locked },
      { label: 'Học sinh', value: 'students', disabled: locked },
    ];
  }
  yearId: number | null = null;
  editingYearId: number | null = null;
  editingRoomId: number | null = null;
  editingStudentId: number | null = null;
  importErrors: ImportError[] = [];
  report: ClassroomExamResult[] = [];
  reportRoomId: number | null = null;

  code = '';
  name = '';
  address = '';
  phone = '';
  email = '';
  isActive = true;

  yearName = '';
  startDate = '';
  endDate = '';
  yearStatus = 'Planned';
  isCurrent = false;

  gradeLevelId: number | null = null;
  classCode = '';
  className = '';
  capacity: number | null = null;
  roomStatus = 'Active';

  studentCode = '';
  studentName = '';
  dateOfBirth = '';
  gender: string | null = null;
  studentEmail = '';
  studentPhone = '';
  studentAddress = '';
  studentUserId: number | null = null;

  ngOnInit(): void {
    this.watch(this.api.gradeLevels(), (items) => (this.grades = items));
    this.loadSchools();
  }

  loadSchools(): void {
    this.watch(this.api.schools(), (items) => (this.schools = items));
  }

  select(school: School): void {
    this.selected = school;
    this.code = school.code;
    this.name = school.name;
    this.address = school.address ?? '';
    this.phone = school.phone ?? '';
    this.email = school.email ?? '';
    this.isActive = school.isActive;
    this.report = [];
    this.loadYears();
    this.loadStudents();
  }

  resetSchool(): void {
    this.selected = null;
    this.code = '';
    this.name = '';
    this.address = '';
    this.phone = '';
    this.email = '';
    this.isActive = true;
    this.years = [];
    this.rooms = [];
    this.students = [];
  }

  saveSchool(): void {
    const body = {
      code: this.code.trim(),
      name: this.name.trim(),
      address: blankToNull(this.address),
      provinceCode: this.selected?.provinceCode ?? null,
      districtCode: this.selected?.districtCode ?? null,
      phone: blankToNull(this.phone),
      email: blankToNull(this.email),
      isActive: this.isActive,
    };
    this.loading = true;
    this.watch(this.api.saveSchool(body, this.selected?.id), (school) => {
      this.showOk(this.selected ? 'Đã cập nhật trường.' : 'Đã thêm trường.');
      this.select(school);
      this.loadSchools();
    });
  }

  loadYears(): void {
    if (!this.selected) return;
    this.watch(this.api.schoolYears(this.selected.id), (items) => {
      this.years = items;
      const current = items.find((item) => item.isCurrent) ?? items[0];
      this.yearId = current?.id ?? null;
      this.loadRooms();
    });
  }

  editYear(year: SchoolYear): void {
    this.editingYearId = year.id;
    this.yearName = year.name;
    this.startDate = year.startDate.slice(0, 10);
    this.endDate = year.endDate.slice(0, 10);
    this.yearStatus = year.status;
    this.isCurrent = year.isCurrent;
  }

  saveYear(): void {
    if (!this.selected) return;
    const body = {
      name: this.yearName.trim(),
      startDate: this.startDate,
      endDate: this.endDate,
      isCurrent: this.isCurrent,
      status: this.yearStatus,
    };
    this.loading = true;
    this.watch(this.api.saveSchoolYear(this.selected.id, body, this.editingYearId ?? undefined), () => {
      this.editingYearId = null;
      this.yearName = '';
      this.showOk('Đã lưu năm học.');
      this.loadYears();
    });
  }

  loadRooms(): void {
    if (!this.selected) return;
    this.watch(this.api.classrooms(this.selected.id, this.yearId), (items) => (this.rooms = items));
  }

  saveRoom(): void {
    if (!this.selected || !this.yearId || !this.gradeLevelId) {
      this.error = 'Chọn năm học và khối trước khi lưu lớp.';
      return;
    }
    const body = {
      schoolYearId: this.yearId,
      gradeLevelId: this.gradeLevelId,
      classCode: this.classCode.trim(),
      name: this.className.trim(),
      homeroomTeacherUserId: null,
      capacity: this.capacity ? Number(this.capacity) : null,
      status: this.roomStatus,
    };
    this.loading = true;
    this.watch(this.api.saveClassroom(this.selected.id, body, this.editingRoomId ?? undefined), () => {
      this.editingRoomId = null;
      this.classCode = '';
      this.className = '';
      this.capacity = null;
      this.showOk('Đã lưu lớp.');
      this.loadRooms();
    });
  }

  editRoom(room: Classroom): void {
    this.editingRoomId = room.id;
    this.yearId = room.schoolYearId;
    this.gradeLevelId = room.gradeLevelId;
    this.classCode = room.classCode;
    this.className = room.name;
    this.capacity = room.capacity;
    this.roomStatus = room.status;
  }

  loadStudents(): void {
    if (!this.selected) return;
    this.watch(this.api.studentProfiles(this.selected.id), (items) => (this.students = items));
  }

  saveStudent(): void {
    if (!this.selected) return;
    const body = {
      studentCode: this.studentCode.trim(),
      fullName: this.studentName.trim(),
      dateOfBirth: this.dateOfBirth || null,
      gender: this.gender || null,
      email: blankToNull(this.studentEmail),
      phone: blankToNull(this.studentPhone),
      address: blankToNull(this.studentAddress),
      userId: this.studentUserId,
      status: 'Active',
    };
    this.loading = true;
    this.watch(this.api.saveStudentProfile(this.selected.id, body, this.editingStudentId ?? undefined), () => {
      this.editingStudentId = null;
      this.studentCode = '';
      this.studentName = '';
      this.dateOfBirth = '';
      this.gender = '';
      this.studentEmail = '';
      this.studentPhone = '';
      this.studentAddress = '';
      this.studentUserId = null;
      this.showOk('Đã lưu hồ sơ học sinh.');
      this.loadStudents();
    });
  }

  editStudent(student: StudentProfile): void {
    this.editingStudentId = student.id;
    this.studentCode = student.studentCode;
    this.studentName = student.fullName;
    this.dateOfBirth = student.dateOfBirth?.slice(0, 10) ?? '';
    this.gender = student.gender ?? '';
    this.studentEmail = student.email ?? '';
    this.studentPhone = student.phone ?? '';
    this.studentAddress = student.address ?? '';
    this.studentUserId = student.userId;
  }

  enroll(student: StudentProfile, value: Event | number | string | null): void {
    const classroomId = value instanceof Event ? Number((value.target as HTMLSelectElement).value) : Number(value);
    if (!classroomId) return;
    const request = student.classroomId
      ? this.api.transfer(student.id, classroomId)
      : this.api.enroll(classroomId, student.id);
    this.watch(request, () => {
      this.showOk(student.classroomId ? 'Đã chuyển lớp.' : 'Đã xếp lớp.');
      this.loadStudents();
      this.loadRooms();
    });
  }

  downloadTemplate(): void {
    if (!this.selected) return;
    this.watch(this.api.studentTemplate(this.selected.id), (blob) => saveBlob(blob, 'mau-hoc-sinh.xlsx'));
  }

  importFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file || !this.selected || !this.yearId) {
      this.error = 'Chọn năm học trước khi nhập Excel.';
      return;
    }
    this.watch(this.api.importStudents(this.selected.id, this.yearId, file), (result) => {
      this.importErrors = result.errors;
      this.showOk(`Đã nhập ${result.imported} học sinh.`);
      this.loadStudents();
      this.loadRooms();
    });
  }

  showReport(room: Classroom): void {
    this.reportRoomId = room.id;
    this.watch(this.api.classroomResults(room.id), (items) => (this.report = items));
  }
}
