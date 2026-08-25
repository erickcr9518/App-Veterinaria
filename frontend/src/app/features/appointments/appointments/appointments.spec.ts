import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { Observable, Subject, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { Appointment, Patient } from '../../../core/models/clinical.models';
import { AuthService } from '../../../core/services/auth.service';
import { ClinicalService } from '../../../core/services/clinical.service';
import { Appointments } from './appointments';

describe('Appointments', () => {
  it('shows a loading state while appointments are being fetched', async () => {
    const appointments$ = new Subject<Appointment[]>();

    const fixture = await createComponent(['appointments.read', 'appointments.write'], createClinicalService({
      getAppointments: () => appointments$.asObservable(),
    }));

    expect(fixture.nativeElement.textContent).toContain('Cargando agenda...');
  });

  it('shows an empty state when no appointments exist in the selected range', async () => {
    const fixture = await createComponent(['appointments.read', 'appointments.write'], createClinicalService({
      getAppointments: () => of([]),
    }));

    expect(fixture.nativeElement.textContent).toContain('No hay citas en este rango.');
  });

  it('shows an error state when appointments cannot be loaded', async () => {
    const fixture = await createComponent(['appointments.read'], createClinicalService({
      getAppointments: () => throwError(() => new HttpErrorResponse({ status: 500 })),
    }));

    expect(fixture.nativeElement.textContent).toContain('No se pudo cargar la agenda.');
  });

  it('hides edit and status actions when the user cannot write appointments', async () => {
    const fixture = await createComponent(['appointments.read']);

    const text = fixture.nativeElement.textContent;
    const buttons = fixture.debugElement
      .queryAll(By.css('button'))
      .map((button) => button.nativeElement.textContent.trim());

    expect(text).toContain('Firulais');
    expect(text).not.toContain('Nueva cita');
    expect(buttons).not.toContain('Editar');
    expect(buttons).not.toContain('Confirmar');
    expect(buttons).not.toContain('Completar');
    expect(buttons).not.toContain('Cancelar');
  });

  it('hides the patient record link when filtering by patient without full record access', async () => {
    const readonlyFixture = await createComponent(['appointments.read'], undefined, { patientId: 'patient-1' });

    expect(getAnchorHrefs(readonlyFixture)).not.toContain('/patients/patient-1/record');
    expect(getAnchorHrefs(readonlyFixture)).toContain('/appointments');
  });

  it('shows the patient record link when filtering by patient with full record access', async () => {
    const clinicalFixture = await createComponent(['appointments.read', 'records.read.full'], undefined, { patientId: 'patient-1' });

    expect(getAnchorHrefs(clinicalFixture)).toContain('/patients/patient-1/record');
  });

  async function createComponent(
    permissions: string[],
    clinicalService = createClinicalService(),
    queryParams: Record<string, string> = {},
  ): Promise<ComponentFixture<Appointments>> {
    const authService = {
      hasPermission: vi.fn((code: string) => permissions.includes(code)),
    };

    await TestBed.configureTestingModule({
      imports: [Appointments],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap(queryParams),
            },
          },
        },
        { provide: AuthService, useValue: authService },
        { provide: ClinicalService, useValue: clinicalService },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(Appointments);
    fixture.detectChanges();
    return fixture;
  }

  function createClinicalService(overrides: Partial<Record<keyof ClinicalService, unknown>> = {}) {
    return {
      getPatients: vi.fn((): Observable<Patient[]> => of([
        {
          id: 'patient-1',
          ownerId: 'owner-1',
          ownerName: 'Maria Fernandez',
          name: 'Firulais',
          species: 'Perro',
          breed: 'Mestizo',
          estimatedAge: '2 anos',
          sex: 'Macho',
          currentWeightKg: 19,
          status: 'Activo',
        },
      ])),
      getAppointments: vi.fn((): Observable<Appointment[]> => of([
        {
          id: 'appointment-1',
          patientId: 'patient-1',
          patientName: 'Firulais',
          ownerId: 'owner-1',
          ownerName: 'Maria Fernandez',
          assignedVeterinarianUserId: null,
          assignedVeterinarianName: null,
          startsAtUtc: '2026-08-25T15:00:00Z',
          endsAtUtc: '2026-08-25T15:30:00Z',
          visitType: 'Consulta',
          status: 'Scheduled',
          reason: 'Control de rutina',
          statusChanges: [],
        },
      ])),
      createAppointment: vi.fn(),
      updateAppointment: vi.fn(),
      changeAppointmentStatus: vi.fn(),
      ...overrides,
    };
  }

  function getAnchorHrefs(fixture: ComponentFixture<Appointments>): string[] {
    return fixture.debugElement
      .queryAll(By.css('a'))
      .map((anchor) => anchor.attributes['href'])
      .filter((href): href is string => Boolean(href));
  }
});
