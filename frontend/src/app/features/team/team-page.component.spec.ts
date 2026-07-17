import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { UsersService } from '../../core/services/users.service';
import { UserSummary } from '../../core/models/user.models';
import { TeamPageComponent } from './team-page.component';

const currentUser = signal({
  Id: 'admin-1',
  UserName: 'admin',
  Email: 'admin@example.test',
  FullName: 'Admin User',
  Role: 'Admin',
  Token: 'test-token',
});

const authServiceStub = { currentUser };
const usersServiceStub = {
  getAll: vi.fn(() => of({ TotalCount: 0, PageNumber: 1, PageSize: 100, Items: [] })),
  update: vi.fn(() => of(null)),
  toggleStatus: vi.fn(() => of(null)),
};

const teamMember: UserSummary = {
  Id: 'developer-1',
  UserName: 'developer',
  Email: 'developer@example.test',
  FullName: 'Developer User',
  JobTitle: 'Junior Developer',
  PhoneNumber: '+92 300 0000000',
  Role: 'Developer',
  IsActive: true,
  AssignedTasks: 3,
  CompletedTasks: 1,
  CreatedAt: '2026-07-16T00:00:00Z',
};

describe('TeamPageComponent editor', () => {
  beforeEach(async () => {
    currentUser.set({
      Id: 'admin-1',
      UserName: 'admin',
      Email: 'admin@example.test',
      FullName: 'Admin User',
      Role: 'Admin',
      Token: 'test-token',
    });
    vi.clearAllMocks();

    await TestBed.configureTestingModule({
      imports: [TeamPageComponent],
      providers: [
        { provide: AuthService, useValue: authServiceStub },
        { provide: UsersService, useValue: usersServiceStub },
      ],
    })
      .overrideComponent(TeamPageComponent, { set: { template: '' } })
      .compileComponents();
  });

  afterEach(() => TestBed.resetTestingModule());

  it('loads all editable fields without erasing the phone number', () => {
    const component = TestBed.createComponent(TeamPageComponent).componentInstance;

    component.select(teamMember);

    expect(component.form.getRawValue()).toEqual({
      fullName: 'Developer User',
      email: 'developer@example.test',
      role: 'Developer',
      jobTitle: 'Junior Developer',
      phoneNumber: '+92 300 0000000',
    });
  });

  it('prevents a user from changing their own role', () => {
    currentUser.set({
      Id: teamMember.Id,
      UserName: teamMember.UserName,
      Email: teamMember.Email,
      FullName: teamMember.FullName,
      Role: 'Developer',
      Token: 'test-token',
    });
    const component = TestBed.createComponent(TeamPageComponent).componentInstance;

    component.select(teamMember);
    component.form.controls.role.setValue('Admin');
    component.submit();

    expect(component.error()).toBe('You cannot change your own role.');
    expect(usersServiceStub.update).not.toHaveBeenCalled();
  });
});
