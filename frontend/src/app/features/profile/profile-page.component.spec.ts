import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ProfileService } from '../../core/services/profile.service';
import { ProfilePageComponent } from './profile-page.component';

const profileServiceStub = {
  getMyProfile: vi.fn(() =>
    of({
      Id: 'user-1',
      UserName: 'developer',
      Email: 'developer@example.test',
      FullName: 'Developer User',
      JobTitle: 'Junior Developer',
      PhoneNumber: '+92 300 0000000',
      Role: 'Developer',
    }),
  ),
  updateMyProfile: vi.fn(),
  changePassword: vi.fn(),
};

describe('ProfilePageComponent password form', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProfilePageComponent],
      providers: [{ provide: ProfileService, useValue: profileServiceStub }],
    })
      .overrideComponent(ProfilePageComponent, { set: { template: '' } })
      .compileComponents();
  });

  afterEach(() => TestBed.resetTestingModule());

  it('enforces the backend length and complexity policy', () => {
    const component = TestBed.createComponent(ProfilePageComponent).componentInstance;
    const control = component.passwordForm.controls.newPassword;

    control.setValue('Aa!1');
    expect(control.hasError('minlength')).toBe(true);

    control.setValue('lowercase!123');
    expect(control.hasError('pattern')).toBe(true);

    control.setValue('Strong!Pass123');
    expect(control.valid).toBe(true);
  });

  it('blocks submission when confirmation does not match', () => {
    const component = TestBed.createComponent(ProfilePageComponent).componentInstance;
    component.passwordForm.setValue({
      currentPassword: 'Current!Pass123',
      newPassword: 'Strong!Pass123',
      confirmPassword: 'Different!Pass123',
    });

    component.changePassword();

    expect(component.passwordMessage()).toBe('Password confirmation does not match.');
    expect(profileServiceStub.changePassword).not.toHaveBeenCalled();
  });
});
