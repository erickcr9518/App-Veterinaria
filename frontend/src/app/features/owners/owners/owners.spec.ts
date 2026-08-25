import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Observable, Subject, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { Owner } from '../../../core/models/clinical.models';
import { AuthService } from '../../../core/services/auth.service';
import { ClinicalService } from '../../../core/services/clinical.service';
import { Owners } from './owners';

describe('Owners', () => {
  it('shows a loading state while owners are being fetched', async () => {
    const owners$ = new Subject<Owner[]>();

    const fixture = await createComponent(['owners.read', 'owners.write'], createClinicalService({
      getOwners: () => owners$.asObservable(),
    }));

    expect(fixture.nativeElement.textContent).toContain('Cargando propietarios...');
  });

  it('shows the empty state when no owners exist', async () => {
    const fixture = await createComponent(['owners.read', 'owners.write'], createClinicalService({
      getOwners: () => of([]),
    }));

    expect(fixture.nativeElement.textContent).toContain('No hay propietarios registrados todavia.');
  });

  it('shows a permission-specific error for forbidden owner reads', async () => {
    const fixture = await createComponent(['owners.read'], createClinicalService({
      getOwners: () => throwError(() => new HttpErrorResponse({ status: 403 })),
    }));

    expect(fixture.nativeElement.textContent).toContain('No tienes permiso para ver propietarios.');
  });

  it('renders a read-only directory when the user cannot write owners', async () => {
    const fixture = await createComponent(['owners.read']);

    const text = fixture.nativeElement.textContent;
    const buttons = fixture.debugElement
      .queryAll(By.css('button'))
      .map((button) => button.nativeElement.textContent.trim());

    expect(text).toContain('Maria Fernandez');
    expect(text).not.toContain('Nuevo propietario');
    expect(buttons).not.toContain('Editar');
  });

  async function createComponent(
    permissions: string[],
    clinicalService = createClinicalService(),
  ): Promise<ComponentFixture<Owners>> {
    const authService = {
      hasPermission: vi.fn((code: string) => permissions.includes(code)),
    };

    await TestBed.configureTestingModule({
      imports: [Owners],
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: ClinicalService, useValue: clinicalService },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(Owners);
    fixture.detectChanges();
    return fixture;
  }

  function createClinicalService(overrides: Partial<Record<keyof ClinicalService, unknown>> = {}) {
    return {
      getOwners: vi.fn((): Observable<Owner[]> => of([
        {
          id: 'owner-1',
          fullName: 'Maria Fernandez',
          phone: '8888-0000',
          email: 'maria@example.test',
          patientCount: 1,
        },
      ])),
      createOwner: vi.fn(),
      updateOwner: vi.fn(),
      ...overrides,
    };
  }
});
