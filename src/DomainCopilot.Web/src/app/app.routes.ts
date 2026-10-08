import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'login', loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) },
  { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent), canActivate: [authGuard] },
  { path: 'screening/new', loadComponent: () => import('./features/screening-upload/screening-upload.component').then(m => m.ScreeningUploadComponent), canActivate: [authGuard], data: { roles: ['Admin', 'Recruiter'] } },
  { path: 'corpus', loadComponent: () => import('./features/corpus-ingestion/corpus-ingestion.component').then(m => m.CorpusIngestionComponent), canActivate: [authGuard], data: { roles: ['Admin'] } },
  { path: 'copilot', loadComponent: () => import('./features/copilot-chat/copilot-chat.component').then(m => m.CopilotChatComponent), canActivate: [authGuard] }
];
