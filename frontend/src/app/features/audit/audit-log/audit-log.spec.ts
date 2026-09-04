import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AuditEntry } from '../../../core/models/audit.models';
import { AuditService } from '../../../core/services/audit.service';
import { AuditLog } from './audit-log';

describe('AuditLog', () => {
  it('shows the empty state when there is no activity', async () => {
    const fixture = await createComponent(() => of([]));

    expect(fixture.nativeElement.textContent).toContain('No hay actividad registrada en este período.');
  });

  it('groups entries by day and renders their details', async () => {
    const fixture = await createComponent(() => of(createEntries()));

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Paciente registrado');
    expect(text).toContain('Firulais (Perro)');
    expect(text).toContain('Dra. Ana Rojas');
  });

  it('shows an error message when the request fails', async () => {
    const fixture = await createComponent(() => throwError(() => new Error('boom')));

    expect(fixture.nativeElement.textContent).toContain('No se pudo cargar la bitácora de auditoría.');
  });

  async function createComponent(getAuditLog: () => ReturnType<AuditService['getAuditLog']>): Promise<ComponentFixture<AuditLog>> {
    const auditService = { getAuditLog };

    await TestBed.configureTestingModule({
      imports: [AuditLog],
      providers: [{ provide: AuditService, useValue: auditService }],
    }).compileComponents();

    const fixture = TestBed.createComponent(AuditLog);
    fixture.detectChanges();
    return fixture;
  }

  function createEntries(): AuditEntry[] {
    return [
      {
        id: 'entry-1',
        occurredAtUtc: '2026-08-29T10:00:00Z',
        entityType: 'Patient',
        entityId: 'patient-1',
        action: 'Paciente registrado',
        summary: 'Firulais (Perro)',
        performedByName: 'Dra. Ana Rojas',
      },
    ];
  }
});
