<script lang="ts">
  import { onMount } from 'svelte';
  import { statsService } from '$lib/services/stats';
  import StatsCard from '$lib/components/StatsCard.svelte';
  import PersonStatsDetail from '$lib/components/PersonStatsDetail.svelte';
  import type { StatsOverview } from '$lib/types/stats';

  let overview = $state<StatsOverview[]>([]);
  let selectedPersonId = $state<number | null>(null);
  let year = $state(new Date().getFullYear());
  let loading = $state(true);
  let error = $state<string | null>(null);

  async function loadOverview() {
    try {
      loading = true;
      error = null;
      overview = await statsService.getOverview(year);
    } catch (err) {
      error = err instanceof Error ? err.message : 'Error al cargar estadísticas';
      console.error('Error loading stats overview:', err);
    } finally {
      loading = false;
    }
  }

  // Recargar cuando cambie el año
  $effect(() => {
    loadOverview();
  });

  function selectPerson(personId: number) {
    selectedPersonId = personId;
  }

  function closePerson() {
    selectedPersonId = null;
  }

  // Generar opciones de años (año actual y 2 años anteriores/posteriores)
  const currentYear = new Date().getFullYear();
  const yearOptions = Array.from({ length: 5 }, (_, i) => currentYear - 2 + i);
</script>

<div class="stats-page p-6 max-w-7xl mx-auto">
  <div class="header mb-6">
    <h1 class="text-3xl font-bold mb-2">📊 Estadísticas de Vacaciones {year}</h1>
    <p class="text-gray-600">Resumen del uso de días de vacaciones por persona</p>
  </div>

  <!-- Selector de año -->
  <div class="mb-6 flex items-center gap-4">
    <label for="year-select" class="font-medium text-gray-700">Año:</label>
    <select
      id="year-select"
      bind:value={year}
      class="select select-bordered w-32 px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
    >
      {#each yearOptions as yearOption}
        <option value={yearOption}>{yearOption}</option>
      {/each}
    </select>
  </div>

  {#if loading}
    <div class="loading bg-white p-8 rounded-lg shadow text-center">
      <p class="text-gray-500">Cargando estadísticas...</p>
    </div>
  {:else if error}
    <div class="error bg-red-50 border border-red-200 p-6 rounded-lg">
      <p class="text-red-600">Error: {error}</p>
      <button
        onclick={loadOverview}
        class="mt-4 px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700"
      >
        Reintentar
      </button>
    </div>
  {:else}
    <!-- Overview de todas las personas -->
    <div class="overview-grid grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-8">
      {#each overview as person}
        <StatsCard {person} onclick={() => selectPerson(person.personId)} />
      {/each}
    </div>

    {#if overview.length === 0}
      <div class="empty-state bg-blue-50 border border-blue-200 p-8 rounded-lg text-center">
        <p class="text-blue-600 text-lg">No hay datos de estadísticas para el año {year}</p>
        <p class="text-blue-500 text-sm mt-2">Prueba seleccionando otro año o agregando vacaciones</p>
      </div>
    {/if}

    <!-- Detalle de persona seleccionada -->
    {#if selectedPersonId}
      <div class="person-detail-container mb-8">
        <div class="flex justify-between items-center mb-4">
          <h2 class="text-2xl font-bold">Detalle de Estadísticas</h2>
          <button
            onclick={closePerson}
            class="px-4 py-2 bg-gray-200 text-gray-700 rounded hover:bg-gray-300 transition"
          >
            Cerrar
          </button>
        </div>
        <PersonStatsDetail personId={selectedPersonId} {year} />
      </div>
    {/if}
  {/if}
</div>

<style>
  .stats-page {
    min-height: calc(100vh - 4rem);
  }
</style>
