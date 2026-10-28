import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './core/guards';
import { Shell } from './layout/shell';
import { AccountsPage } from './pages/accounts';
import { AttemptPage } from './pages/attempt';
import { AttemptsPage } from './pages/attempts';
import { ExamEditorPage } from './pages/exam-editor';
import { ExamGeneratePage } from './pages/exam-generate';
import { ExamsPage } from './pages/exams';
import { HomePage } from './pages/home';
import { LoginPage } from './pages/login';
import { MyExamsPage } from './pages/my-exams';
import { OmrBatchPage } from './pages/omr-batch';
import { OmrBatchesPage } from './pages/omr-batches';
import { OmrSheetPage } from './pages/omr-sheet';
import { OmrTemplatesPage } from './pages/omr-templates';
import { ProfilesPage } from './pages/profiles';
import { QuestionEditorPage } from './pages/question-editor';
import { QuestionsPage } from './pages/questions';
import { RolesPage } from './pages/roles';
import { SchoolsPage } from './pages/schools';
import { SubjectsPage } from './pages/subjects';
import { TopicsPage } from './pages/topics';

const staff = ['ADMIN', 'TEACHER'];
const learner = ['ADMIN', 'STUDENT'];

export const routes: Routes = [
  { path: 'login', component: LoginPage, canActivate: [guestGuard] },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: '', component: HomePage },
      { path: 'subjects', component: SubjectsPage, canActivate: [roleGuard], data: { roles: staff } },
      { path: 'topics', component: TopicsPage, canActivate: [roleGuard], data: { roles: staff } },
      { path: 'questions', component: QuestionsPage, canActivate: [roleGuard], data: { roles: staff } },
      { path: 'questions/new', component: QuestionEditorPage, canActivate: [roleGuard], data: { roles: staff } },
      { path: 'questions/:id', component: QuestionEditorPage, canActivate: [roleGuard], data: { roles: staff } },
      { path: 'exams', component: ExamsPage, canActivate: [roleGuard], data: { roles: staff } },
      { path: 'exams/generate', component: ExamGeneratePage, canActivate: [roleGuard], data: { roles: staff } },
      { path: 'exams/new', component: ExamEditorPage, canActivate: [roleGuard], data: { roles: staff } },
      { path: 'exams/:id', component: ExamEditorPage, canActivate: [roleGuard], data: { roles: staff } },
      { path: 'profiles', component: ProfilesPage, canActivate: [roleGuard], data: { roles: staff } },
      { path: 'omr', component: OmrBatchesPage, canActivate: [roleGuard], data: { roles: staff } },
      { path: 'omr/templates', component: OmrTemplatesPage, canActivate: [roleGuard], data: { roles: staff } },
      { path: 'omr/batches/:id', component: OmrBatchPage, canActivate: [roleGuard], data: { roles: staff } },
      { path: 'omr/sheets/:id', component: OmrSheetPage, canActivate: [roleGuard], data: { roles: staff } },
      { path: 'accounts', component: AccountsPage, canActivate: [roleGuard], data: { roles: ['ADMIN', 'SCHOOL_ADMIN'] } },
      { path: 'roles', component: RolesPage, canActivate: [roleGuard], data: { roles: ['ADMIN'] } },
      { path: 'schools', component: SchoolsPage, canActivate: [roleGuard], data: { roles: ['ADMIN', 'SCHOOL_ADMIN'] } },
      { path: 'my-exams', component: MyExamsPage, canActivate: [roleGuard], data: { roles: learner } },
      { path: 'attempts', component: AttemptsPage, canActivate: [roleGuard], data: { roles: learner } },
      { path: 'attempts/:id', component: AttemptPage, canActivate: [roleGuard], data: { roles: learner } },
    ],
  },
  { path: '**', redirectTo: '' },
];
