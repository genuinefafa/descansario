import { browser } from '$app/environment';
import { goto } from '$app/navigation';
import { authService } from '$lib/services/auth';

export const ssr = false; // Deshabilitar SSR para rutas protegidas

export async function load() {
  if (browser) {
    // Verificar si el usuario está autenticado
    if (!authService.isAuthenticated()) {
      // No hay token, redirigir a login
      goto('/login');
      return {};
    }

    // Verificar que el token siga siendo válido
    try {
      const user = await authService.getCurrentUser();

      if (!user) {
        // Token inválido, redirigir a login
        goto('/login');
        return {};
      }

      return { user };
    } catch (error) {
      // Error al validar token, redirigir a login
      console.error('Error al validar autenticación:', error);
      goto('/login');
      return {};
    }
  }

  return {};
}
