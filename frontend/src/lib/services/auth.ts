/**
 * Servicio de autenticación para login, registro y gestión de tokens
 */

import { api } from './api';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  name: string;
}

export interface User {
  id: number;
  email: string;
  name: string;
  role: string;
  createdAt: string;
}

export interface LoginResponse {
  token: string;
  user: User;
  expiresAt: string;
}

const TOKEN_KEY = 'auth_token';
const USER_KEY = 'auth_user';

class AuthService {
  /**
   * Login de usuario
   */
  async login(email: string, password: string): Promise<LoginResponse> {
    const response = await api.post<LoginResponse>('/api/auth/login', {
      email,
      password,
    });

    // Guardar token y usuario en localStorage
    this.saveToken(response.token);
    this.saveUser(response.user);

    return response;
  }

  /**
   * Registro de nuevo usuario
   */
  async register(name: string, email: string, password: string): Promise<User> {
    const user = await api.post<User>('/api/auth/register', {
      name,
      email,
      password,
    });

    return user;
  }

  /**
   * Logout - limpiar token y usuario
   */
  logout(): void {
    this.clearToken();
    this.clearUser();

    // Redirigir a login si estamos en el navegador
    if (typeof window !== 'undefined') {
      window.location.href = '/login';
    }
  }

  /**
   * Obtener usuario actual del servidor (requiere token válido)
   */
  async getCurrentUser(): Promise<User | null> {
    try {
      if (!this.getToken()) {
        return null;
      }

      const user = await api.get<User>('/api/auth/me');
      this.saveUser(user);
      return user;
    } catch (error) {
      console.error('Error al obtener usuario actual:', error);
      this.logout();
      return null;
    }
  }

  /**
   * Guardar token en localStorage
   */
  private saveToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
  }

  /**
   * Obtener token de localStorage
   */
  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  /**
   * Limpiar token de localStorage
   */
  private clearToken(): void {
    localStorage.removeItem(TOKEN_KEY);
  }

  /**
   * Guardar usuario en localStorage
   */
  private saveUser(user: User): void {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  }

  /**
   * Obtener usuario de localStorage
   */
  getUser(): User | null {
    const userJson = localStorage.getItem(USER_KEY);
    if (!userJson) return null;

    try {
      return JSON.parse(userJson);
    } catch {
      return null;
    }
  }

  /**
   * Limpiar usuario de localStorage
   */
  private clearUser(): void {
    localStorage.removeItem(USER_KEY);
  }

  /**
   * Verificar si el usuario está autenticado
   */
  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  /**
   * Verificar si el usuario es admin
   */
  isAdmin(): boolean {
    const user = this.getUser();
    return user?.role === 'Admin';
  }
}

export const authService = new AuthService();
