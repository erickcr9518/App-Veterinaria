import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { permissionGuard } from './core/guards/permission.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./features/auth/forgot-password/forgot-password').then((m) => m.ForgotPassword),
  },
  {
    path: 'reset-password',
    loadComponent: () => import('./features/auth/reset-password/reset-password').then((m) => m.ResetPassword),
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
        path: 'patients/:patientId/record',
        loadComponent: () => import('./features/patients/patient-record/patient-record').then((m) => m.PatientRecord),
        canActivate: [permissionGuard],
        data: { permission: 'records.read.full' },
      },
      {
        path: 'appointments',
        loadComponent: () => import('./features/appointments/appointments/appointments').then((m) => m.Appointments),
        canActivate: [permissionGuard],
        data: { permission: 'appointments.read' },
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
      {
        path: 'patients/:patientId/prescriptions',
        loadComponent: () =>
          import('./features/prescriptions/patient-prescriptions/patient-prescriptions').then((m) => m.PatientPrescriptions),
        canActivate: [permissionGuard],
        data: { permission: 'records.read.full' },
      },
      {
        path: 'consultations/:consultationId/prescriptions/new',
        loadComponent: () =>
          import('./features/prescriptions/prescription-form/prescription-form').then((m) => m.PrescriptionForm),
        canActivate: [permissionGuard],
        data: { permission: 'prescriptions.write' },
      },
      {
        path: 'prescriptions/:id/edit',
        loadComponent: () =>
          import('./features/prescriptions/prescription-form/prescription-form').then((m) => m.PrescriptionForm),
        canActivate: [permissionGuard],
        data: { permission: 'prescriptions.write' },
      },
      {
        path: 'prescriptions/:id',
        loadComponent: () =>
          import('./features/prescriptions/prescription-detail/prescription-detail').then((m) => m.PrescriptionDetail),
        canActivate: [permissionGuard],
        data: { permission: 'records.read.full' },
      },
      {
        path: 'users',
        loadComponent: () => import('./features/users/users/users').then((m) => m.Users),
        canActivate: [permissionGuard],
        data: { permission: 'users.manage' },
      },
      {
        path: 'audit',
        loadComponent: () => import('./features/audit/audit-log/audit-log').then((m) => m.AuditLog),
        canActivate: [permissionGuard],
        data: { permission: ['audit.read.all', 'audit.read.own'] },
      },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
    ],
  },
  { path: '**', redirectTo: '' },
];
