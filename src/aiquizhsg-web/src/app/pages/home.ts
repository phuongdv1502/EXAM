import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SessionStore } from '../core/auth.service';
import { UI } from '../core/ui';

interface Card {
  title: string;
  text: string;
  link: string;
  roles: string[];
}

@Component({
  selector: 'app-home',
  imports: [RouterLink, ...UI],
  templateUrl: './home.html',
})
export class HomePage {
  readonly store = inject(SessionStore);

  private readonly cards: Card[] = [
    { title: 'Ngân hàng câu hỏi', text: 'Soạn, gửi duyệt và phê duyệt câu trắc nghiệm.', link: '/questions', roles: ['ADMIN', 'TEACHER'] },
    { title: 'Đề thi', text: 'Lập đề thủ công hoặc sinh theo ma trận độ khó.', link: '/exams', roles: ['ADMIN', 'TEACHER'] },
    { title: 'Chấm phiếu OMR', text: 'Tải phiếu trắc nghiệm, đối soát và ghi điểm vào lượt làm bài.', link: '/omr', roles: ['ADMIN', 'TEACHER'] },
    { title: 'Trường và lớp', text: 'Năm học, lớp, hồ sơ học sinh và kết quả.', link: '/schools', roles: ['ADMIN', 'SCHOOL_ADMIN'] },
    { title: 'Tài khoản', text: 'Tạo giáo viên, quản trị trường, học sinh và gán vai trò.', link: '/accounts', roles: ['ADMIN', 'SCHOOL_ADMIN'] },
    { title: 'Phân quyền', text: 'Chọn quyền cho từng vai trò. Có hiệu lực với mọi người mang vai trò đó.', link: '/roles', roles: ['ADMIN'] },
    { title: 'Đề của tôi', text: 'Xem đề đang mở và vào làm bài.', link: '/my-exams', roles: ['ADMIN', 'STUDENT'] },
    { title: 'Lượt làm bài', text: 'Bài đang làm dở và các lượt đã nộp.', link: '/attempts', roles: ['ADMIN', 'STUDENT'] },
  ];

  visible(): Card[] {
    return this.cards.filter((card) => this.store.hasRole(...card.roles));
  }
}
