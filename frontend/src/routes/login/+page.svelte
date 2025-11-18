<script lang="ts">
  import { authStore } from '$lib/stores/authStore';
  import { goto } from '$app/navigation';

  let email = $state('');
  let password = $state('');
  let loading = $state(false);
  let error = $state('');

  // Cargar credenciales de admin para desarrollo
  $effect(() => {
    if (import.meta.env.DEV) {
      email = 'admin@descansario.com';
      password = 'admin123';
    }
  });

  async function handleLogin() {
    error = '';
    loading = true;

    try {
      const success = await authStore.login(email, password);

      if (success) {
        // Redirigir a la página principal
        goto('/');
      } else {
        error = $authStore.error || 'Credenciales inválidas';
      }
    } catch (err) {
      error = err instanceof Error ? err.message : 'Error al iniciar sesión';
    } finally {
      loading = false;
    }
  }

  function handleSubmit(event: Event) {
    event.preventDefault();
    handleLogin();
  }
</script>

<div class="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100">
  <div class="max-w-md w-full bg-white rounded-lg shadow-xl p-8">
    <!-- Logo / Title -->
    <div class="text-center mb-8">
      <h1 class="text-3xl font-bold text-gray-900">🏖️ Descansario</h1>
      <p class="text-gray-600 mt-2">Gestor de vacaciones</p>
    </div>

    <!-- Login Form -->
    <form on:submit={handleSubmit} class="space-y-6">
      {#if error}
        <div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
          {error}
        </div>
      {/if}

      <!-- Email -->
      <div>
        <label for="email" class="block text-sm font-medium text-gray-700 mb-2">
          Email
        </label>
        <input
          id="email"
          type="email"
          bind:value={email}
          required
          disabled={loading}
          class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 disabled:bg-gray-100 disabled:cursor-not-allowed"
          placeholder="admin@descansario.com"
        />
      </div>

      <!-- Password -->
      <div>
        <label for="password" class="block text-sm font-medium text-gray-700 mb-2">
          Contraseña
        </label>
        <input
          id="password"
          type="password"
          bind:value={password}
          required
          disabled={loading}
          class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 disabled:bg-gray-100 disabled:cursor-not-allowed"
          placeholder="••••••••"
        />
      </div>

      <!-- Submit Button -->
      <button
        type="submit"
        disabled={loading}
        class="w-full bg-blue-600 text-white py-3 px-4 rounded-lg font-medium hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
      >
        {#if loading}
          <span class="flex items-center justify-center">
            <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            Iniciando sesión...
          </span>
        {:else}
          Iniciar Sesión
        {/if}
      </button>
    </form>

    <!-- Dev Info -->
    {#if import.meta.env.DEV}
      <div class="mt-6 p-4 bg-yellow-50 border border-yellow-200 rounded-lg text-sm">
        <p class="font-semibold text-yellow-800 mb-1">🔧 Modo desarrollo</p>
        <p class="text-yellow-700">
          <strong>Email:</strong> admin@descansario.com<br />
          <strong>Password:</strong> admin123
        </p>
      </div>
    {/if}
  </div>
</div>
