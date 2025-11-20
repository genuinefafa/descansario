<script lang="ts">
  import { authStore } from '$lib/stores/authStore';
  import { goto } from '$app/navigation';

  let name = $state('');
  let email = $state('');
  let password = $state('');
  let confirmPassword = $state('');
  let loading = $state(false);
  let error = $state('');

  async function handleRegister() {
    error = '';

    // Validaciones básicas
    if (!name || !email || !password) {
      error = 'Todos los campos son obligatorios';
      return;
    }

    if (password.length < 6) {
      error = 'La contraseña debe tener al menos 6 caracteres';
      return;
    }

    if (password !== confirmPassword) {
      error = 'Las contraseñas no coinciden';
      return;
    }

    loading = true;

    try {
      const success = await authStore.register(name, email, password);

      if (success) {
        // Redirigir a la página principal después del registro exitoso
        goto('/');
      } else {
        error = $authStore.error || 'Error al registrarse. Verifique que el email no exista.';
      }
    } catch (err) {
      error = err instanceof Error ? err.message : 'Error al registrarse';
    } finally {
      loading = false;
    }
  }

  function handleSubmit(event: Event) {
    event.preventDefault();
    handleRegister();
  }
</script>

<div
  class="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100"
>
  <div class="max-w-md w-full bg-white rounded-lg shadow-xl p-8">
    <!-- Logo / Title -->
    <div class="text-center mb-8">
      <h1 class="text-3xl font-bold text-gray-900">🏖️ Descansario</h1>
      <p class="text-gray-600 mt-2">Crear nueva cuenta</p>
    </div>

    <!-- Register Form -->
    <form onsubmit={handleSubmit} class="space-y-5">
      {#if error}
        <div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
          {error}
        </div>
      {/if}

      <!-- Name -->
      <div>
        <label for="name" class="block text-sm font-medium text-gray-700 mb-2">
          Nombre completo
        </label>
        <input
          id="name"
          type="text"
          bind:value={name}
          required
          disabled={loading}
          class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 disabled:bg-gray-100 disabled:cursor-not-allowed"
          placeholder="Juan Pérez"
        />
      </div>

      <!-- Email -->
      <div>
        <label for="email" class="block text-sm font-medium text-gray-700 mb-2"> Email </label>
        <input
          id="email"
          type="email"
          bind:value={email}
          required
          disabled={loading}
          class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 disabled:bg-gray-100 disabled:cursor-not-allowed"
          placeholder="juan@ejemplo.com"
        />
        <p class="mt-1 text-xs text-gray-500">
          Si tu email coincide con una persona del sistema, se vinculará automáticamente.
        </p>
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
          minlength="6"
          class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 disabled:bg-gray-100 disabled:cursor-not-allowed"
          placeholder="••••••••"
        />
        <p class="mt-1 text-xs text-gray-500">Mínimo 6 caracteres</p>
      </div>

      <!-- Confirm Password -->
      <div>
        <label for="confirmPassword" class="block text-sm font-medium text-gray-700 mb-2">
          Confirmar contraseña
        </label>
        <input
          id="confirmPassword"
          type="password"
          bind:value={confirmPassword}
          required
          disabled={loading}
          minlength="6"
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
            <svg
              class="animate-spin -ml-1 mr-3 h-5 w-5 text-white"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
            >
              <circle
                class="opacity-25"
                cx="12"
                cy="12"
                r="10"
                stroke="currentColor"
                stroke-width="4"
              ></circle>
              <path
                class="opacity-75"
                fill="currentColor"
                d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
              ></path>
            </svg>
            Registrando...
          </span>
        {:else}
          Crear cuenta
        {/if}
      </button>
    </form>

    <!-- Link to login -->
    <div class="mt-6 text-center">
      <p class="text-sm text-gray-600">
        ¿Ya tenés cuenta?
        <a href="/login" class="font-medium text-blue-600 hover:text-blue-500"> Iniciá sesión </a>
      </p>
    </div>

    <!-- Info about auto-linking -->
    <div class="mt-6 p-4 bg-blue-50 border border-blue-200 rounded-lg text-sm">
      <p class="font-semibold text-blue-800 mb-1">ℹ️ Vinculación automática</p>
      <p class="text-blue-700">
        Si tu email coincide con una persona del sistema, tu cuenta se vinculará automáticamente y
        podrás gestionar tus vacaciones.
      </p>
    </div>
  </div>
</div>
