import { DatePipe } from '@angular/common';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { CurrentUser } from '../../core/models/auth.models';
import { DashboardSummary } from '../../core/models/dashboard.models';
import { AuthService } from '../../core/services/auth.service';
import { DashboardService } from '../../core/services/dashboard.service';
import { Dashboard } from './dashboard';

describe('Dashboard', () => {
  it('does not link recent patients to the full record without records.read.full', async () => {
    const fixture = await createComponent(['patients.read'], createUser({ role: 'Recepcion' }));

    const recordLinks = getAnchorHrefs(fixture).filter((href) => href.includes('/record'));

    expect(recordLinks).toEqual([]);
    expect(fixture.nativeElement.textContent).toContain('Firulais');
  });

  it('links recent patients to the full record when the user can read full records', async () => {
    const fixture = await createComponent(['patients.read', 'records.read.full'], createUser({ role: 'Veterinario' }));

    const recordLinks = getAnchorHrefs(fixture).filter((href) => href.includes('/record'));

    expect(recordLinks).toContain('/patients/patient-1/record');
  });

  it('renders platform copy for users without an assigned clinic', async () => {
    const fixture = await createComponent(['patients.read'], createUser({
      clinicId: null,
      clinicName: null,
      role: 'SuperAdministrador',
    }));

    const text = fixture.nativeElement.textContent;

    expect(text).toContain('Este es el panel de plataforma.');
    expect(text).not.toContain('panel de .');
  });

  async function createComponent(permissions: string[], user: CurrentUser): Promise<ComponentFixture<Dashboard>> {
    const currentUser = signal<CurrentUser | null>(user);
    const authService = {
      currentUser: currentUser.asReadonly(),
      hasPermission: (code: string) => permissions.includes(code),
    };
    const dashboardService = {
      getSummary: () => of(createSummary()),
    };

    await TestBed.configureTestingModule({
      imports: [Dashboard],
      providers: [
        DatePipe,
        provideRouter([]),
        { provide: AuthService, useValue: authService },
        { provide: DashboardService, useValue: dashboardService },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(Dashboard);
    fixture.detectChanges();
    return fixture;
  }

  function createUser(overrides: Partial<CurrentUser> = {}): CurrentUser {
    return {
      userId: 'user-1',
      email: 'user@vetplatform.test',
      fullName: 'QA User',
      clinicId: 'clinic-1',
      clinicName: 'Clinica Demo',
      role: 'Administrador',
      permissions: [],
      ...overrides,
    };
  }

  function createSummary(): DashboardSummary {
    return {
      todaysAppointmentsCount: 0,
      upcomingAppointments: [],
      draftConsultations: [],
      draftPrescriptions: [],
      recentPatients: [
        {
          id: 'patient-1',
          name: 'Firulais',
          species: 'Perro',
          ownerName: 'Maria Fernandez',
          createdAtUtc: '2026-08-24T18:00:00Z',
        },
      ],
    };
  }

  function getAnchorHrefs(fixture: ComponentFixture<Dashboard>): string[] {
    return fixture.debugElement
      .queryAll(By.css('a'))
      .map((anchor) => anchor.attributes['href'])
      .filter((href): href is string => Boolean(href));
  }
});
