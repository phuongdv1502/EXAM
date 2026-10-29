import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Classroom, ExamDrawSeat, ExamSummary, School, Subject } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';

@Component({
  selector: 'app-exams',
  imports: [FormsModule, RouterLink, ...UI],
  templateUrl: './exams.html',
})
export class ExamsPage extends Page implements OnInit {
  items: ExamSummary[] = [];
  subjects: Subject[] = [];
  listSubjectId: number | null = null;
  schools: School[] = [];
  rooms: Classroom[] = [];
  drawOpen = false;
  drawSchoolId: number | null = null;
  drawRoomId: number | null = null;
  drawSubjectId: number | null = null;
  drawExamIds: number[] = [];
  seats: ExamDrawSeat[] = [];

  ngOnInit(): void {
    this.watch(this.api.teachingSubjects(), (items) => (this.subjects = items));
    this.watch(this.api.exams(), (items) => (this.items = items));
  }

  shown(): ExamSummary[] {
    const allowed = new Set(this.subjects.map((item) => item.id));
    return this.items.filter((item) => allowed.has(item.subjectId) && (!this.listSubjectId || item.subjectId === this.listSubjectId));
  }

  drawChoices(): ExamSummary[] {
    if (!this.drawSubjectId) return [];
    const allowed = new Set(this.subjects.map((item) => item.id));
    return this.items.filter((item) =>
      item.status === 'Published'
      && item.subjectId === this.drawSubjectId
      && allowed.has(item.subjectId));
  }

  drawEmptyText(): string {
    if (!this.subjects.length) return 'Chưa được phân môn giảng dạy.';
    if (!this.drawSubjectId) return 'Chọn môn để hiện các đề đang mở.';
    return 'Chưa có đề đang mở thuộc môn này.';
  }

  emptyText(): string {
    if (!this.subjects.length) return 'Chưa được phân môn giảng dạy.';
    if (this.listSubjectId) return 'Chưa có đề thuộc môn này.';
    return 'Chưa có đề thi.';
  }

  openDraw(): void {
    this.seats = [];
    this.drawExamIds = [];
    this.drawSubjectId = null;
    this.drawOpen = true;
    if (!this.schools.length)
      this.watch(this.api.schools(), (items) => (this.schools = items), () => (this.schools = []));
  }

  closeDraw(): void {
    this.drawOpen = false;
  }

  loadRooms(): void {
    this.drawRoomId = null;
    this.rooms = [];
    if (!this.drawSchoolId) return;
    this.watch(this.api.classrooms(this.drawSchoolId), (items) => (this.rooms = items));
  }

  onDrawSubject(): void {
    const allowed = new Set(this.drawChoices().map((item) => item.id));
    this.drawExamIds = this.drawExamIds.filter((id) => allowed.has(id));
  }

  toggleExam(id: number, checked: boolean): void {
    this.drawExamIds = checked
      ? [...this.drawExamIds, id]
      : this.drawExamIds.filter((item) => item !== id);
  }

  async draw(): Promise<void> {
    if (!this.drawRoomId || this.drawExamIds.length < 2) {
      this.fail('Chọn một lớp và ít nhất hai đề đã phát hành.');
      return;
    }
    const accepted = await this.confirm('Mỗi học sinh trong lớp nhận một đề. Đề đang giao cả lớp trong danh sách này sẽ chuyển thành chỗ của từng học sinh.');
    if (!accepted) return;
    this.watch(this.api.drawExams({ classroomId: this.drawRoomId, examIds: this.drawExamIds }), (result) => {
      this.seats = result.seats;
      this.showOk(`Đã chia ${result.seats.length} học sinh trong ${result.classroomName}.`);
    });
  }
}
