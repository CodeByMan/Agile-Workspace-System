import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { apiUrl } from '../config/api.config';
import { UsersService } from './users.service';

describe('UsersService', () => {
  let service: UsersService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [UsersService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(UsersService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
    TestBed.resetTestingModule();
  });

  it('sends bounded list filters and unwraps the API response', () => {
    let received: unknown;
    service
      .getAll({ pageNumber: 2, pageSize: 25, role: 'Developer', isActive: true })
      .subscribe((value) => (received = value));

    const request = httpTesting.expectOne((candidate) => candidate.url === apiUrl('/users'));
    expect(request.request.params.get('pageNumber')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('25');
    expect(request.request.params.get('role')).toBe('Developer');
    expect(request.request.params.get('isActive')).toBe('true');

    const result = { TotalCount: 0, PageNumber: 2, PageSize: 25, Items: [] };
    request.flush({ Success: true, Message: null, Data: result, Errors: null });
    expect(received).toEqual(result);
  });
});
