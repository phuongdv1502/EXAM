import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { blankToNull, saveBlob } from '../core/format';
import {
  Classroom,
  ClassroomExamResult,
  GradeLevel,
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
  tab: 'years' | 'classes' | 'students' = 'classes';
  drawer: 'school' | 'year' | 'room' | 'student' | null = null;
  schoolIsNew = false;

  readonly schoolTabs = [
    { label: 'Lớp', value: 'classes' },
    { label: 'Học sinh', value: 'students' },
    { label: 'Năm học', value: 'years' },
  ];
  yearId: number | null = null;
  editingYearId: number | null = null;
  editingRoomId: number | null = null;
  editingStudentId: number | null = null;
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
    this.watch(this.api.schools(), (items) => {
      this.schools = items;
      if (!this.selected && items[0]) {
        this.select(items[0]);
        return;
      }
      this.selected = items.find((item) => item.id === this.selected?.id) ?? null;
    });
  }

  pickSchool(id: number | null): void {
    const school = this.schools.find((item) => item.id === id);
    if (school) this.select(school);
  }

  selectedYear(): SchoolYear | undefined {
    return this.years.find((item) => item.id === this.yearId);
  }

  changeYear(id: number | null): void {
    this.yearId = id;
    this.report = [];
    this.reportRoomId = null;
    this.loadRooms();
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

  protected override closeForm(): void {
    this.drawer = null;
    super.closeForm();
  }

  openNewSchool(): void {
    this.schoolIsNew = true;
    this.code = '';
    this.name = '';
    this.address = '';
    this.phone = '';
    this.email = '';
    this.isActive = true;
    this.openDrawer('school');
  }

  openEditSchool(): void {
    if (!this.selected) return;
    this.schoolIsNew = false;
    this.code = this.selected.code;
    this.name = this.selected.name;
    this.address = this.selected.address ?? '';
    this.phone = this.selected.phone ?? '';
    this.email = this.selected.email ?? '';
    this.isActive = this.selected.isActive;
    this.openDrawer('school');
  }

  openNewYear(): void {
    this.editingYearId = null;
    this.yearName = '';
    this.startDate = '';
    this.endDate = '';
    this.yearStatus = 'Planned';
    this.isCurrent = false;
    this.openDrawer('year');
  }

  openNewRoom(): void {
    this.editingRoomId = null;
    this.gradeLevelId = null;
    this.classCode = '';
    this.className = '';
    this.capacity = null;
    this.roomStatus = 'Active';
    this.openDrawer('room');
  }

  openNewStudent(): void {
    this.editingStudentId = null;
    this.studentCode = '';
    this.studentName = '';
    this.dateOfBirth = '';
    this.gender = null;
    this.studentEmail = '';
    this.studentPhone = '';
    this.studentAddress = '';
    this.studentUserId = null;
    this.openDrawer('student');
  }

  drawerTitle(): string {
    if (this.drawer === 'school') return this.schoolIsNew ? 'Trường mới' : 'Sửa trường';
    if (this.drawer === 'year') return this.editingYearId ? 'Sửa năm học' : 'Năm học mới';
    if (this.drawer === 'room') return this.editingRoomId ? 'Sửa lớp' : 'Lớp mới';
    return this.editingStudentId ? 'Sửa hồ sơ' : 'Hồ sơ mới';
  }

  private openDrawer(kind: 'school' | 'year' | 'room' | 'student'): void {
    this.drawer = kind;
    this.openForm();
  }

  saveSchool(): void {
    const body = {
      code: this.code.trim(),
      name: this.name.trim(),
      address: blankToNull(this.address),
      provinceCode: this.schoolIsNew ? null : this.selected?.provinceCode ?? null,
      districtCode: this.schoolIsNew ? null : this.selected?.districtCode ?? null,
      phone: blankToNull(this.phone),
      email: blankToNull(this.email),
      isActive: this.isActive,
    };
    this.loading = true;
    const id = this.schoolIsNew ? undefined : this.selected?.id;
    this.watch(this.api.saveSchool(body, id), (school) => {
      this.showOk(id ? 'Đã cập nhật trường.' : 'Đã thêm trường.');
      this.closeForm();
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
    this.openDrawer('year');
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
      this.closeForm();
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
      this.fail('Chọn năm học và khối trước khi lưu lớp.');
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
      this.closeForm();
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
    this.openDrawer('room');
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
      this.closeForm();
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
    this.openDrawer('student');
  }

  enroll(student: StudentProfile, value: Event | number | string | null): void {
    const classroomId = value instanceof Event ? Number((value.target as HTMLSelectElement).value) : Number(value);
    if (!classroomId || classroomId === student.classroomId) return;
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
      this.fail('Chọn năm học trước khi nhập Excel.');
      return;
    }
    this.watch(this.api.importStudents(this.selected.id, this.yearId, file), (result) => {
      this.showOk(`Đã nhập ${result.imported} học sinh.`);
      if (result.errors.length) {
        this.warn('Một số dòng không nhập được', result.errors.map((item) => `Dòng ${item.row}: ${item.message}`).join('\n'));
      }
      this.loadStudents();
      this.loadRooms();
    });
  }

  showReport(room: Classroom): void {
    if (this.reportRoomId === room.id) {
      this.reportRoomId = null;
      this.report = [];
      return;
    }
    this.reportRoomId = room.id;
    this.watch(this.api.classroomResults(room.id), (items) => (this.report = items));
  }

  reportRoom(): Classroom | undefined {
    return this.rooms.find((item) => item.id === this.reportRoomId);
  }

  inYear(student: StudentProfile): boolean {
    return this.rooms.some((room) => room.id === student.classroomId);
  }
}
