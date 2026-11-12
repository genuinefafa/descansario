<script lang="ts">
  import '../app.css';
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import { authStore } from '$lib/stores/authStore';

  let { children } = $props();

  // Mobile menu state
  let isMobileMenuOpen = $state(false);

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

  // Close mobile menu when route changes
  $effect(() => {
    $page.url.pathname;
    isMobileMenuOpen = false;
  });
</script>

{#if isPublicPage()}
  {@render children()}
{:else if $authStore.isAuthenticated}
  <div class="min-h-screen bg-gray-50">
    <!-- Navigation Bar -->
    <nav class="bg-white shadow-sm border-b border-gray-200 sticky top-0 z-50">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between h-16">
          <!-- Logo and main navigation -->
          <div class="flex">
            <div class="flex-shrink-0 flex items-center">
              <a href="/" class="text-xl font-bold text-gray-900">🏖️ Descansario</a>
            </div>
            <!-- Desktop Navigation -->
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

          <!-- Right side: User menu + Mobile menu button -->
          <div class="flex items-center gap-2">
            <!-- User info (hidden on mobile) -->
            {#if $authStore.user}
              <div class="hidden sm:flex items-center gap-4">
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
                    {:else}
                      <span
                        class="inline-block px-2 py-0.5 text-xs font-semibold text-gray-700 bg-gray-100 rounded"
                        title="No vinculado a persona"
                      >
                        Sin persona
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

            <!-- Mobile menu button -->
            <button
              onclick={() => (isMobileMenuOpen = !isMobileMenuOpen)}
              class="sm:hidden inline-flex items-center justify-center p-2 rounded-md text-gray-700 hover:text-gray-900 hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-inset focus:ring-blue-500"
              aria-label="Menú"
            >
              {#if isMobileMenuOpen}
                <!-- X icon -->
                <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M6 18L18 6M6 6l12 12"
                  />
                </svg>
              {:else}
                <!-- Hamburger icon -->
                <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M4 6h16M4 12h16M4 18h16"
                  />
                </svg>
              {/if}
            </button>
          </div>
        </div>
      </div>

      <!-- Mobile menu dropdown -->
      {#if isMobileMenuOpen}
        <div class="sm:hidden border-t border-gray-200 bg-white">
          <div class="px-2 pt-2 pb-3 space-y-1">
            <a
              href="/"
              class="block px-3 py-2 rounded-md text-base font-medium {isActive('/')
                ? 'bg-blue-50 text-blue-700'
                : 'text-gray-700 hover:bg-gray-50 hover:text-gray-900'}"
            >
              Dashboard
            </a>
            <a
              href="/persons"
              class="block px-3 py-2 rounded-md text-base font-medium {isActive('/persons')
                ? 'bg-blue-50 text-blue-700'
                : 'text-gray-700 hover:bg-gray-50 hover:text-gray-900'}"
            >
              Personas
            </a>
            <a
              href="/vacations"
              class="block px-3 py-2 rounded-md text-base font-medium {isActive('/vacations')
                ? 'bg-blue-50 text-blue-700'
                : 'text-gray-700 hover:bg-gray-50 hover:text-gray-900'}"
            >
              Vacaciones
            </a>
            <a
              href="/holidays"
              class="block px-3 py-2 rounded-md text-base font-medium {isActive('/holidays')
                ? 'bg-blue-50 text-blue-700'
                : 'text-gray-700 hover:bg-gray-50 hover:text-gray-900'}"
            >
              Feriados
            </a>
            <a
              href="/calendar"
              class="block px-3 py-2 rounded-md text-base font-medium {isActive('/calendar')
                ? 'bg-blue-50 text-blue-700'
                : 'text-gray-700 hover:bg-gray-50 hover:text-gray-900'}"
            >
              Calendario
            </a>
            <a
              href="/stats"
              class="block px-3 py-2 rounded-md text-base font-medium {isActive('/stats')
                ? 'bg-blue-50 text-blue-700'
                : 'text-gray-700 hover:bg-gray-50 hover:text-gray-900'}"
            >
              Estadísticas
            </a>
          </div>

          <!-- User section in mobile menu -->
          {#if $authStore.user}
            <div class="border-t border-gray-200 px-2 pt-4 pb-3">
              <div class="px-3 mb-3">
                <div class="text-base font-medium text-gray-800">{$authStore.user.name}</div>
                <div class="text-sm text-gray-500">{$authStore.user.email}</div>
                <div class="flex gap-1 mt-2">
                  {#if $authStore.user.role === 'Admin'}
                    <span
                      class="inline-block px-2 py-0.5 text-xs font-semibold text-blue-700 bg-blue-100 rounded"
                      >Admin</span
                    >
                  {/if}
                  {#if $authStore.user.personId}
                    <span
                      class="inline-block px-2 py-0.5 text-xs font-semibold text-green-700 bg-green-100 rounded"
                    >
                      ✓ {$authStore.user.personName}
                    </span>
                  {:else}
                    <span
                      class="inline-block px-2 py-0.5 text-xs font-semibold text-gray-700 bg-gray-100 rounded"
                    >
                      Sin persona
                    </span>
                  {/if}
                </div>
              </div>
              <button
                onclick={() => authStore.logout()}
                class="w-full text-left px-3 py-2 rounded-md text-base font-medium text-red-700 hover:bg-red-50"
              >
                Cerrar Sesión
              </button>
            </div>
          {/if}
        </div>
      {/if}
    </nav>

    <!-- Page content -->
    {@render children()}
  </div>
{:else}
  <!-- Not authenticated, redirect handled by hooks -->
  {@render children()}
{/if}
