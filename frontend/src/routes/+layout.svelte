<script lang="ts">
  import '../app.css';
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import { authStore } from '$lib/stores/authStore';

  let { children } = $props();

  // Inicializar auth store cuando la app carga
  onMount(() => {
    authStore.init();
  });

  // Check if we're on a public page (login/register)
  const isPublicPage = $derived(() => {
    const pathname = $page.url.pathname;
    return pathname === '/login' || pathname === '/register';
  });

  // Check if current route is active
  function isActive(path: string): boolean {
    return $page.url.pathname === path;
  }
</script>

{#if isPublicPage()}
  {@render children()}
{:else if $authStore.isAuthenticated}
  <div class="min-h-screen bg-gray-50">
    <!-- Navigation Bar -->
    <nav class="bg-white shadow-sm border-b border-gray-200">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between h-16">
          <!-- Logo and main navigation -->
          <div class="flex">
            <div class="flex-shrink-0 flex items-center">
              <a href="/" class="text-xl font-bold text-gray-900">🏖️ Descansario</a>
            </div>
            <div class="hidden sm:ml-6 sm:flex sm:space-x-8">
              <a
                href="/"
                class="inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium {isActive(
                  '/'
                )
                  ? 'border-blue-500 text-gray-900'
                  : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700'}"
              >
                Dashboard
              </a>
              <a
                href="/persons"
                class="inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium {isActive(
                  '/persons'
                )
                  ? 'border-blue-500 text-gray-900'
                  : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700'}"
              >
                Personas
              </a>
              <a
                href="/vacations"
                class="inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium {isActive(
                  '/vacations'
                )
                  ? 'border-blue-500 text-gray-900'
                  : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700'}"
              >
                Vacaciones
              </a>
              <a
                href="/holidays"
                class="inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium {isActive(
                  '/holidays'
                )
                  ? 'border-blue-500 text-gray-900'
                  : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700'}"
              >
                Feriados
              </a>
              <a
                href="/calendar"
                class="inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium {isActive(
                  '/calendar'
                )
                  ? 'border-blue-500 text-gray-900'
                  : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700'}"
              >
                Calendario
              </a>
              <a
                href="/stats"
                class="inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium {isActive(
                  '/stats'
                )
                  ? 'border-blue-500 text-gray-900'
                  : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700'}"
              >
                Estadísticas
              </a>
            </div>
          </div>

          <!-- User menu -->
          {#if $authStore.user}
            <div class="flex items-center gap-4">
              <div class="text-right">
                <p class="text-sm font-medium text-gray-900">{$authStore.user.name}</p>
                <div class="flex gap-1 justify-end">
                  {#if $authStore.user.role === 'Admin'}
                    <span
                      class="inline-block px-2 py-0.5 text-xs font-semibold text-blue-700 bg-blue-100 rounded"
                      >Admin</span
                    >
                  {/if}
                  {#if $authStore.user.personId}
                    <span
                      class="inline-block px-2 py-0.5 text-xs font-semibold text-green-700 bg-green-100 rounded"
                      title="Vinculado a persona"
                    >
                      ✓ {$authStore.user.personName}
                    </span>
                  {/if}
                </div>
              </div>
              <button
                onclick={() => authStore.logout()}
                class="px-3 py-1.5 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cerrar Sesión
              </button>
            </div>
          {/if}
        </div>
      </div>
    </nav>

    <!-- Page content -->
    {@render children()}
  </div>
{:else}
  <!-- Not authenticated, redirect handled by hooks -->
  {@render children()}
{/if}
