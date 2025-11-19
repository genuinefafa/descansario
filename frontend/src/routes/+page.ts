import { redirect } from '@sveltejs/kit';
import { browser } from '$app/environment';
import { authService } from '$lib/services/auth';

export const ssr = false; // Disable SSR for this page

export async function load() {
  if (browser) {
    // Si no está autenticado, redirigir a login
    if (!authService.isAuthenticated()) {
      throw redirect(302, '/login');
    }

    // Validar que el token siga siendo válido
    try {
      const user = await authService.getCurrentUser();
      if (!user) {
        throw redirect(302, '/login');
      }
      return { user };
    } catch (error) {
      console.error('Error validando autenticación:', error);
      throw redirect(302, '/login');
    }
  }

  return {};
}
