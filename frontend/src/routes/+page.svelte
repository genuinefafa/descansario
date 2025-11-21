<script lang="ts">
  import { onMount } from 'svelte';
  import { personsService } from '$lib/services/persons';
  import { vacationsService } from '$lib/services/vacations';
  import { holidaysService } from '$lib/services/holidays';
  import type { Person } from '$lib/types/person';
  import type { Vacation } from '$lib/types/vacation';
  import type { Holiday } from '$lib/types/holiday';

  let persons = $state<Person[]>([]);
  let vacations = $state<Vacation[]>([]);
  let holidays = $state<Holiday[]>([]);
  let loading = $state(true);

  onMount(async () => {
    await Promise.all([loadPersons(), loadVacations(), loadHolidays()]);
    loading = false;
  });

  async function loadPersons() {
    try {
      persons = await personsService.getAll();
    } catch (err) {
      console.error(err);
    }
  }

  async function loadVacations() {
    try {
      vacations = await vacationsService.getAll();
    } catch (err) {
      console.error(err);
    }
  }

  async function loadHolidays() {
    try {
      holidays = await holidaysService.getAll();
    } catch (err) {
      console.error(err);
    }
  }

  // Calculate stats
  const stats = $derived(() => {
    const today = new Date();
    const upcomingVacations = vacations.filter((v) => {
      const startDate = new Date(v.startDate);
      return startDate >= today && v.status === 'Approved';
    });

    const pendingVacations = vacations.filter((v) => v.status === 'Pending');

    return {
      totalPersons: persons.length,
      totalVacations: vacations.length,
      totalHolidays: holidays.length,
      upcomingVacations: upcomingVacations.length,
      pendingVacations: pendingVacations.length,
    };
  });
</script>

<svelte:head>
  <title>Dashboard - Descansario</title>
</svelte:head>

<div class="container mx-auto px-4 py-8">
  <div class="mb-8">
    <h1 class="text-4xl font-bold text-gray-900 mb-2">Dashboard</h1>
    <p class="text-lg text-gray-600">Bienvenido a Descansario - Gestión de vacaciones</p>
  </div>

  {#if loading}
    <div class="bg-white p-8 rounded-lg shadow-md text-center">
      <p class="text-gray-500">Cargando...</p>
    </div>
  {:else}
    <!-- Stats Cards -->
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
      <div class="bg-white rounded-lg shadow-md p-6 border-l-4 border-blue-500">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm font-medium text-gray-600">Personas</p>
            <p class="text-3xl font-bold text-gray-900">{stats().totalPersons}</p>
          </div>
          <div class="text-4xl">👥</div>
        </div>
        <a href="/persons" class="text-sm text-blue-600 hover:text-blue-700 mt-2 inline-block"
          >Ver todas →</a
        >
      </div>

      <div class="bg-white rounded-lg shadow-md p-6 border-l-4 border-green-500">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm font-medium text-gray-600">Vacaciones</p>
            <p class="text-3xl font-bold text-gray-900">{stats().totalVacations}</p>
          </div>
          <div class="text-4xl">🏖️</div>
        </div>
        <a href="/vacations" class="text-sm text-green-600 hover:text-green-700 mt-2 inline-block"
          >Ver todas →</a
        >
      </div>

      <div class="bg-white rounded-lg shadow-md p-6 border-l-4 border-purple-500">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm font-medium text-gray-600">Feriados</p>
            <p class="text-3xl font-bold text-gray-900">{stats().totalHolidays}</p>
          </div>
          <div class="text-4xl">📅</div>
        </div>
        <a href="/holidays" class="text-sm text-purple-600 hover:text-purple-700 mt-2 inline-block"
          >Ver todos →</a
        >
      </div>

      <div class="bg-white rounded-lg shadow-md p-6 border-l-4 border-yellow-500">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm font-medium text-gray-600">Vacaciones Próximas</p>
            <p class="text-3xl font-bold text-gray-900">{stats().upcomingVacations}</p>
          </div>
          <div class="text-4xl">🔜</div>
        </div>
        <a href="/calendar" class="text-sm text-yellow-600 hover:text-yellow-700 mt-2 inline-block"
          >Ver calendario →</a
        >
      </div>

      <div class="bg-white rounded-lg shadow-md p-6 border-l-4 border-orange-500">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm font-medium text-gray-600">Pendientes de Aprobación</p>
            <p class="text-3xl font-bold text-gray-900">{stats().pendingVacations}</p>
          </div>
          <div class="text-4xl">⏳</div>
        </div>
        <a href="/vacations" class="text-sm text-orange-600 hover:text-orange-700 mt-2 inline-block"
          >Revisar →</a
        >
      </div>

      <div class="bg-white rounded-lg shadow-md p-6 border-l-4 border-indigo-500">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm font-medium text-gray-600">Estadísticas</p>
            <p class="text-lg font-semibold text-gray-900">Ver reportes</p>
          </div>
          <div class="text-4xl">📊</div>
        </div>
        <a href="/stats" class="text-sm text-indigo-600 hover:text-indigo-700 mt-2 inline-block"
          >Ver estadísticas →</a
        >
      </div>
    </div>

    <!-- Quick Actions -->
    <div class="bg-white rounded-lg shadow-md p-6">
      <h2 class="text-xl font-bold text-gray-900 mb-4">Acciones Rápidas</h2>
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <a
          href="/persons"
          class="flex items-center gap-3 p-4 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
        >
          <div class="text-2xl">➕</div>
          <div>
            <p class="font-medium text-gray-900">Nueva Persona</p>
            <p class="text-sm text-gray-500">Agregar miembro al equipo</p>
          </div>
        </a>

        <a
          href="/vacations"
          class="flex items-center gap-3 p-4 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
        >
          <div class="text-2xl">🏖️</div>
          <div>
            <p class="font-medium text-gray-900">Nueva Vacación</p>
            <p class="text-sm text-gray-500">Solicitar días de descanso</p>
          </div>
        </a>

        <a
          href="/calendar"
          class="flex items-center gap-3 p-4 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
        >
          <div class="text-2xl">📆</div>
          <div>
            <p class="font-medium text-gray-900">Ver Calendario</p>
            <p class="text-sm text-gray-500">Visualizar todas las vacaciones</p>
          </div>
        </a>
      </div>
    </div>
  {/if}
</div>
