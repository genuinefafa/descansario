/**
 * Svelte store para gestionar el estado de autenticación
 */

import { writable } from 'svelte/store';
import type { User } from '$lib/services/auth';
import { authService } from '$lib/services/auth';

export interface AuthState {
  user: User | null;
  loading: boolean;
  error: string | null;
  isAuthenticated: boolean;
}

const initialState: AuthState = {
  user: null,
  loading: true,
  error: null,
  isAuthenticated: false,
};

function createAuthStore() {
  const { subscribe, set, update } = writable<AuthState>(initialState);

  return {
    subscribe,

    /**
     * Inicializar el store verificando si hay un usuario autenticado
     */
    async init() {
      update((state) => ({ ...state, loading: true, error: null }));

      try {
        const user = authService.getUser();

        if (user && authService.isAuthenticated()) {
          // Verificar que el token siga siendo válido
          const currentUser = await authService.getCurrentUser();
          set({ user: currentUser, loading: false, error: null, isAuthenticated: true });
        } else {
          set({ user: null, loading: false, error: null, isAuthenticated: false });
        }
      } catch (error) {
        console.error('Error al inicializar auth store:', error);
        set({ user: null, loading: false, error: null, isAuthenticated: false });
      }
    },

    /**
     * Login de usuario
     */
    async login(email: string, password: string) {
      update((state) => ({ ...state, loading: true, error: null }));

      try {
        const response = await authService.login(email, password);
        set({ user: response.user, loading: false, error: null, isAuthenticated: true });
        return true;
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Error al iniciar sesión';
        set({ user: null, loading: false, error: errorMessage, isAuthenticated: false });
        return false;
      }
    },

    /**
     * Registro de nuevo usuario
     */
    async register(name: string, email: string, password: string) {
      update((state) => ({ ...state, loading: true, error: null }));

      try {
        await authService.register(name, email, password);
        // Después de registrarse, hacer login automáticamente
        return await this.login(email, password);
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Error al registrarse';
        set({ user: null, loading: false, error: errorMessage, isAuthenticated: false });
        return false;
      }
    },

    /**
     * Logout de usuario
     */
    logout() {
      authService.logout();
      set({ user: null, loading: false, error: null, isAuthenticated: false });
    },

    /**
     * Limpiar error
     */
    clearError() {
      update((state) => ({ ...state, error: null }));
    },
  };
}

export const authStore = createAuthStore();
