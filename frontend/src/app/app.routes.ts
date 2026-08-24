import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { permissionGuard } from './core/guards/permission.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: '',
    loadComponent: () => import('./layout/shell/shell').then((m) => m.Shell),
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
      },
      {
        path: 'owners',
        loadComponent: () => import('./features/owners/owners/owners').then((m) => m.Owners),
        canActivate: [permissionGuard],
        data: { permission: 'owners.read' },
      },
      {
        path: 'patients',
        loadComponent: () => import('./features/patients/patients/patients').then((m) => m.Patients),
        canActivate: [permissionGuard],
        data: { permission: 'patients.read' },
      },
      {
        path: 'patients/:patientId/consultations',
        loadComponent: () =>
          import('./features/consultations/patient-consultations/patient-consultations').then((m) => m.PatientConsultations),
        canActivate: [permissionGuard],
        data: { permission: 'records.read.full' },
      },
      {
        path: 'patients/:patientId/consultations/new',
        loadComponent: () =>
          import('./features/consultations/consultation-form/consultation-form').then((m) => m.ConsultationForm),
        canActivate: [permissionGuard],
        data: { permission: 'consultations.write' },
      },
      {
        path: 'consultations/:id/edit',
        loadComponent: () =>
          import('./features/consultations/consultation-form/consultation-form').then((m) => m.ConsultationForm),
        canActivate: [permissionGuard],
        data: { permission: 'consultations.write' },
      },
      {
        path: 'consultations/:id',
        loadComponent: () =>
          import('./features/consultations/consultation-detail/consultation-detail').then((m) => m.ConsultationDetail),
        canActivate: [permissionGuard],
        data: { permission: 'records.read.full' },
      },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
    ],
  },
  { path: '**', redirectTo: '' },
];
