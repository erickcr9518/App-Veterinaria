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
  it('lists the clinic staff and hides the platform role option for clinic admins', async () => {
    const fixture = await createComponent(createUser());

    const text = fixture.nativeElement.textContent;
    const roleOptions = getRoleOptionLabels(fixture);

    expect(text).toContain('Dra. Ana Rojas');
    expect(roleOptions).not.toContain('Superadministrador (plataforma)');
  });

  it('does not show a deactivate button for the signed-in user own row', async () => {
    const fixture = await createComponent(createUser({ userId: 'user-1' }));

    const rows = fixture.debugElement.queryAll(By.css('.row'));
    const selfRow = rows.find((row) => row.nativeElement.textContent.includes('(tu)'));
    const otherRow = rows.find((row) => !row.nativeElement.textContent.includes('(tu)'));

    expect(selfRow?.nativeElement.textContent).not.toContain('Editar roles');
    expect(selfRow?.nativeElement.textContent).not.toContain('Desactivar');
    expect(otherRow?.nativeElement.textContent).toContain('Editar roles');
    expect(otherRow?.nativeElement.textContent).toContain('Desactivar');
  });

  it('shows platform accounts by default and clinic staff after selecting a clinic for a platform administrator', async () => {
    const fixture = await createComponent(createUser({
      clinicId: null,
      clinicName: null,
      role: 'SuperAdministrador',
      roles: ['SuperAdministrador'],
    }));

    let text = fixture.nativeElement.textContent;
    const roleOptions = getRoleOptionLabels(fixture);

    expect(text).toContain('Cuentas de plataforma');
    expect(text).toContain('Root Admin');
    expect(text).not.toContain('Dra. Ana Rojas');
    expect(roleOptions).toContain('Superadministrador (plataforma)');

    const scopeSelect = fixture.debugElement.query(By.css('header select')).nativeElement as HTMLSelectElement;
    scopeSelect.value = 'clinic-1';
    scopeSelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    text = fixture.nativeElement.textContent;
    expect(text).toContain('Personal de la clinica');
    expect(text).toContain('Dra. Ana Rojas');
    expect(text).not.toContain('Root Admin');
  });

  it('submits multiple selected clinic roles as a role list', async () => {
    let payload: unknown;
    const fixture = await createComponent(createUser(), {
      createUser: (request: unknown) => {
        payload = request;
        return of('new-user-id');
      },
    });

    fixture.componentInstance.form.patchValue({
      fullName: 'Dra. Encargada',
      email: 'encargada@vetplatform.test',
      password: 'Password123!',
    });

    const veterinarian = findRoleOption(fixture, 'Veterinario');
    const administrator = findRoleOption(fixture, 'Administrador');
    expect(veterinarian.checked).toBe(true);

    administrator.checked = true;
    administrator.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    fixture.componentInstance.submit();

    expect(payload).toEqual({
      fullName: 'Dra. Encargada',
      email: 'encargada@vetplatform.test',
      password: 'Password123!',
      role: 'Administrador',
      roles: ['Administrador', 'Veterinario'],
      clinicId: null,
    });
  });

  it('keeps the platform role mutually exclusive in the form', async () => {
    const fixture = await createComponent(createUser({
      clinicId: null,
      clinicName: null,
      role: 'SuperAdministrador',
      roles: ['SuperAdministrador'],
    }));

    const administrator = findRoleOption(fixture, 'Administrador');
    const platform = findRoleOption(fixture, 'Superadministrador (plataforma)');

    administrator.checked = true;
    administrator.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(fixture.componentInstance.form.controls.roles.value).toContain('Administrador');

    platform.checked = true;
    platform.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(fixture.componentInstance.form.controls.roles.value).toEqual(['SuperAdministrador']);
  });

  it('updates roles for an existing clinic staff account', async () => {
    let payload: unknown;
    const fixture = await createComponent(createUser(), {
      updateUserRoles: (_userId: string, request: unknown) => {
        payload = request;
        return of(undefined);
      },
    });

    const staffRow = fixture.debugElement
      .queryAll(By.css('.row'))
      .find((row) => row.nativeElement.textContent.includes('Dra. Ana Rojas'));

    expect(staffRow).toBeTruthy();
    staffRow!.queryAll(By.css('button'))
      .find((button) => button.nativeElement.textContent.includes('Editar roles'))!
      .nativeElement.click();
    fixture.detectChanges();

    const administrator = findEditableRoleOption(fixture, 'Administrador');
    administrator.checked = true;
    administrator.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    fixture.debugElement.queryAll(By.css('.role-editor button'))
      .find((button) => button.nativeElement.textContent.includes('Guardar roles'))!
      .nativeElement.click();

    expect(payload).toEqual({
      role: 'Administrador',
      roles: ['Administrador', 'Veterinario'],
    });
  });

  it('does not offer role editing for platform accounts', async () => {
    const fixture = await createComponent(createUser({
      clinicId: null,
      clinicName: null,
      role: 'SuperAdministrador',
      roles: ['SuperAdministrador'],
    }));

    expect(fixture.nativeElement.textContent).not.toContain('Editar roles');
  });

  async function createComponent(
    user: CurrentUser,
    usersServiceOverrides: Partial<UsersService> = {},
  ): Promise<ComponentFixture<Users>> {
    const currentUser = signal<CurrentUser | null>(user);
    const authService = { currentUser: currentUser.asReadonly() };
    const usersService = {
      getUsers: (clinicId?: string | null) => of(createStaffForScope(user, clinicId)),
      createUser: () => of('new-user-id'),
      setUserActive: () => of(undefined),
      updateUserRoles: () => of(undefined),
      ...usersServiceOverrides,
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
      roles: ['Administrador'],
      permissions: [],
      ...overrides,
    };
  }

  function createStaffForScope(user: CurrentUser, clinicId?: string | null): UserSummary[] {
    if (user.roles.includes('SuperAdministrador') && !user.clinicId && !clinicId) {
      return [
        { userId: 'user-1', email: 'root@vetplatform.test', fullName: 'Root Admin', role: 'SuperAdministrador', roles: ['SuperAdministrador'], isActive: true },
        { userId: 'user-3', email: 'platform@vetplatform.test', fullName: 'Platform Operator', role: 'SuperAdministrador', roles: ['SuperAdministrador'], isActive: true },
      ];
    }

    return [
      { userId: 'user-1', email: 'admin@vetplatform.test', fullName: 'Admin Demo', role: 'Administrador', roles: ['Administrador', 'Veterinario'], isActive: true },
      { userId: 'user-2', email: 'vet@vetplatform.test', fullName: 'Dra. Ana Rojas', role: 'Veterinario', roles: ['Veterinario'], isActive: true },
    ];
  }

  function getRoleOptionLabels(fixture: ComponentFixture<Users>): string[] {
    return fixture.debugElement
      .queryAll(By.css('.role-option span'))
      .map((option) => option.nativeElement.textContent.trim());
  }

  function findRoleOption(fixture: ComponentFixture<Users>, label: string): HTMLInputElement {
    const option = fixture.debugElement
      .queryAll(By.css('.role-option'))
      .find((element) => element.nativeElement.textContent.includes(label));

    expect(option).toBeTruthy();
    return option!.query(By.css('input')).nativeElement as HTMLInputElement;
  }

  function findEditableRoleOption(fixture: ComponentFixture<Users>, label: string): HTMLInputElement {
    const option = fixture.debugElement
      .queryAll(By.css('.role-editor .role-option'))
      .find((element) => element.nativeElement.textContent.includes(label));

    expect(option).toBeTruthy();
    return option!.query(By.css('input')).nativeElement as HTMLInputElement;
  }
});
