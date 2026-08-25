import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { Owner, Patient } from '../../../core/models/clinical.models';
import { AuthService } from '../../../core/services/auth.service';
import { ClinicalService } from '../../../core/services/clinical.service';
import { Patients } from './patients';

describe('Patients', () => {
  it('shows front-desk actions but hides full-record actions for reception', async () => {
    const fixture = await createComponent(['patients.read', 'patients.write', 'appointments.read']);

    const text = fixture.nativeElement.textContent;
    const hrefs = getAnchorHrefs(fixture);

    expect(text).toContain('Nuevo paciente');
    expect(text).toContain('Agendar');
    expect(text).toContain('Editar');
    expect(text).not.toContain('Expediente');
    expect(text).not.toContain('Consultas');
    expect(text).not.toContain('Recetas');
    expect(hrefs).toContain('/appointments?patientId=patient-1');
    expect(hrefs.some((href) => href.includes('/record'))).toBe(false);
    expect(hrefs.some((href) => href.includes('/consultations'))).toBe(false);
    expect(hrefs.some((href) => href.includes('/prescriptions'))).toBe(false);
  });

  it('shows clinical record actions for users with full record permission', async () => {
    const fixture = await createComponent(['patients.read', 'patients.write', 'appointments.read', 'records.read.full']);

    const text = fixture.nativeElement.textContent;
    const hrefs = getAnchorHrefs(fixture);

    expect(text).toContain('Expediente');
    expect(text).toContain('Consultas');
    expect(text).toContain('Recetas');
    expect(hrefs).toContain('/patients/patient-1/record');
    expect(hrefs).toContain('/patients/patient-1/consultations');
    expect(hrefs).toContain('/patients/patient-1/prescriptions');
  });

  it('renders a read-only patient list when the user cannot write patients', async () => {
    const clinicalService = createClinicalService();
    const fixture = await createComponent(['patients.read', 'appointments.read', 'records.read.full'], clinicalService);

    const text = fixture.nativeElement.textContent;

    expect(text).not.toContain('Nuevo paciente');
    expect(text).not.toContain('Editar');
    expect(text).toContain('Firulais');
    expect(clinicalService.getOwners).not.toHaveBeenCalled();
  });

  async function createComponent(
    permissions: string[],
    clinicalService = createClinicalService(),
  ): Promise<ComponentFixture<Patients>> {
    const authService = {
      hasPermission: vi.fn((code: string) => permissions.includes(code)),
    };

    await TestBed.configureTestingModule({
      imports: [Patients],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService },
        { provide: ClinicalService, useValue: clinicalService },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(Patients);
    fixture.detectChanges();
    return fixture;
  }

  function createClinicalService() {
    return {
      getOwners: vi.fn(() => of<Owner[]>([
        {
          id: 'owner-1',
          fullName: 'Maria Fernandez',
          phone: '8888-0000',
          patientCount: 1,
        },
      ])),
      getPatients: vi.fn(() => of<Patient[]>([
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
      createPatient: vi.fn(),
      updatePatient: vi.fn(),
    };
  }

  function getAnchorHrefs(fixture: ComponentFixture<Patients>): string[] {
    return fixture.debugElement
      .queryAll(By.css('a'))
      .map((anchor) => anchor.attributes['href'])
      .filter((href): href is string => Boolean(href));
  }
});
