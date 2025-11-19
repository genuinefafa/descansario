import { redirect } from '@sveltejs/kit';
import { browser } from '$app/environment';
import { authService } from '$lib/services/auth';

export const ssr = false; // Disable SSR for login page

export function load() {
  if (browser) {
    // Si ya está autenticado, redirigir a la app
    if (authService.isAuthenticated()) {
      throw redirect(302, '/');
    }
  }

  return {};
}
