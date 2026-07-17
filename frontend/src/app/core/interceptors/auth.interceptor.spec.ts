import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { apiUrl } from '../config/api.config';
import { AuthService } from '../services/auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        {
          provide: AuthService,
          useValue: { getToken: () => 'test-token' },
        },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
    TestBed.resetTestingModule();
  });

  it('adds a bearer token only to the configured API', () => {
    http.get(apiUrl('/projects')).subscribe();
    const apiRequest = httpTesting.expectOne(apiUrl('/projects'));
    expect(apiRequest.request.headers.get('Authorization')).toBe('Bearer test-token');
    apiRequest.flush({});

    http.get('https://untrusted.example/api/projects').subscribe();
    const externalRequest = httpTesting.expectOne('https://untrusted.example/api/projects');
    expect(externalRequest.request.headers.has('Authorization')).toBe(false);
    externalRequest.flush({});
  });
});
