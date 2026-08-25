import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { CurrentUser } from '../../../core/models/auth.models';
import { UserSummary } from '../../../core/models/user.models';
import { AuthService } from '../../../core/services/auth.service';
import { ClinicsService } from '../../../core/services/clinics.service';
import { UsersService } from '../../../core/services/users.service';
import { Users } from './users';

describe('Users', () => {
  it('lists the clinic staff and hides the role select option for platform admin', async () => {
    const fixture = await createComponent(createUser());

    const text = fixture.nativeElement.textContent;
    const roleOptions = fixture.debugElement
      .queryAll(By.css('select[formcontrolname="role"] option'))
      .map((option) => option.nativeElement.textContent.trim());

    expect(text).toContain('Dra. Ana Rojas');
    expect(roleOptions).not.toContain('Superadministrador (plataforma)');
  });

  it('does not show a deactivate button for the signed-in user own row', async () => {
    const fixture = await createComponent(createUser({ userId: 'user-1' }));

    const rows = fixture.debugElement.queryAll(By.css('.row'));
    const selfRow = rows.find((row) => row.nativeElement.textContent.includes('(tu)'));
    const otherRow = rows.find((row) => !row.nativeElement.textContent.includes('(tu)'));

    expect(selfRow?.query(By.css('button'))).toBeFalsy();
    expect(otherRow?.query(By.css('button'))?.nativeElement.textContent.trim()).toBe('Desactivar');
  });

  it('requires selecting a clinic before showing staff for a platform administrator', async () => {
    const fixture = await createComponent(createUser({ clinicId: null, clinicName: null, role: 'SuperAdministrador' }));

    const text = fixture.nativeElement.textContent;
    const roleOptions = fixture.debugElement
      .queryAll(By.css('select[formcontrolname="role"] option'))
      .map((option) => option.nativeElement.textContent.trim());

    expect(text).toContain('Selecciona una clinica para ver su personal.');
    expect(text).not.toContain('Dra. Ana Rojas');
    expect(roleOptions).toContain('Superadministrador (plataforma)');
  });

  async function createComponent(user: CurrentUser): Promise<ComponentFixture<Users>> {
    const currentUser = signal<CurrentUser | null>(user);
    const authService = { currentUser: currentUser.asReadonly() };
    const usersService = {
      getUsers: () => of(createStaff()),
      createUser: () => of('new-user-id'),
      setUserActive: () => of(undefined),
    };
    const clinicsService = {
      getClinics: () => of([{ id: 'clinic-1', name: 'Clinica Demo', timeZone: 'UTC', isActive: true }]),
    };

    await TestBed.configureTestingModule({
      imports: [Users],
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: UsersService, useValue: usersService },
        { provide: ClinicsService, useValue: clinicsService },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(Users);
    fixture.detectChanges();
    return fixture;
  }

  function createUser(overrides: Partial<CurrentUser> = {}): CurrentUser {
    return {
      userId: 'user-1',
      email: 'admin@vetplatform.test',
      fullName: 'Admin Demo',
      clinicId: 'clinic-1',
      clinicName: 'Clinica Demo',
      role: 'Administrador',
      permissions: [],
      ...overrides,
    };
  }

  function createStaff(): UserSummary[] {
    return [
      { userId: 'user-1', email: 'admin@vetplatform.test', fullName: 'Admin Demo', role: 'Administrador', isActive: true },
      { userId: 'user-2', email: 'vet@vetplatform.test', fullName: 'Dra. Ana Rojas', role: 'Veterinario', isActive: true },
    ];
  }
});
