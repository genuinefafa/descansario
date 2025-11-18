import { redirect } from '@sveltejs/kit';

// Redirigir la raíz a /login
// La app principal está en /(app)/+page.svelte (protegida)
export function load() {
  throw redirect(302, '/login');
}
