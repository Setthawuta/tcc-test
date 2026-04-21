import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, of, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { API } from '../constants/api-endpoints';
import { STORAGE_KEYS } from '../constants/storage-keys';
import {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  User,
} from '../models/auth.models';
import { isTokenExpired } from '../utils/jwt.util';
import { StorageService } from './storage.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storage = inject(StorageService);
  private readonly apiUrl = environment.apiUrl;

  private readonly _currentUser = signal<User | null>(null);
  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._currentUser() !== null);

  getToken(): string | null {
    return this.storage.get(STORAGE_KEYS.authToken);
  }

  getRefreshToken(): string | null {
    return this.storage.get(STORAGE_KEYS.refreshToken);
  }

  hasValidToken(): boolean {
    const token = this.getToken();
    return !!token && !isTokenExpired(token);
  }

  hasRefreshToken(): boolean {
    return !!this.getRefreshToken();
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}${API.auth.login}`, request).pipe(
      tap((response) => this.applyAuthResponse(response)),
    );
  }

  register(request: RegisterRequest): Observable<User> {
    return this.http.post<User>(`${this.apiUrl}${API.auth.register}`, request);
  }

  refresh(): Observable<AuthResponse> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available'));
    }
    return this.http
      .post<AuthResponse>(`${this.apiUrl}${API.auth.refresh}`, { refreshToken })
      .pipe(tap((response) => this.applyAuthResponse(response)));
  }

  loadCurrentUser(): Observable<User | null> {
    return this.http.get<User>(`${this.apiUrl}${API.auth.me}`).pipe(
      tap((user) => this._currentUser.set(user)),
      catchError((err) => {
        this.clearSession();
        return throwError(() => err);
      }),
    );
  }

  logout(): Observable<void> {
    const refreshToken = this.getRefreshToken();
    this.clearSession();
    if (!refreshToken) return of(undefined);
    return this.http
      .post<void>(`${this.apiUrl}${API.auth.logout}`, { refreshToken })
      .pipe(catchError(() => of(undefined)));
  }

  clearSession(): void {
    this.storage.remove(STORAGE_KEYS.authToken);
    this.storage.remove(STORAGE_KEYS.refreshToken);
    this._currentUser.set(null);
  }

  private applyAuthResponse(response: AuthResponse): void {
    this.storage.set(STORAGE_KEYS.authToken, response.accessToken);
    this.storage.set(STORAGE_KEYS.refreshToken, response.refreshToken);
    this._currentUser.set(response.user);
  }
}
