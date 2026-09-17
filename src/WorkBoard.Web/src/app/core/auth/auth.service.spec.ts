import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let auth: AuthService;
  let http: HttpTestingController;
  let router: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem('workboard.accessToken', 'access-token');
    localStorage.setItem('workboard.refreshToken', 'refresh-token');
    localStorage.setItem(
      'workboard.user',
      JSON.stringify({ id: 'user-1', email: 'ada@workboard.dev', displayName: 'Ada' }),
    );

    router = { navigate: vi.fn().mockResolvedValue(true) };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: router },
      ],
    });

    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('ends the local session without waiting for the logout endpoint', async () => {
    const signingOut = auth.logout();

    await signingOut;

    expect(auth.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('workboard.accessToken')).toBeNull();
    expect(localStorage.getItem('workboard.refreshToken')).toBeNull();
    expect(localStorage.getItem('workboard.user')).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/login'], { replaceUrl: true });

    // The server request is still made to revoke the token, but it is no longer on the critical
    // path for the UI. Reaching this assertion before flushing the request proves that behavior.
    http.expectOne('/api/auth/logout').flush(null, { status: 204, statusText: 'No Content' });
  });
});
