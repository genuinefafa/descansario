<script lang="ts">
  import { onMount } from 'svelte';
  import { calendarService } from '$lib/services/calendar';
  import type { CalendarSummary } from '$lib/types/calendar';

  interface Props {
    startDate: Date;
    endDate: Date;
  }

  let { startDate, endDate }: Props = $props();

  let summary = $state<CalendarSummary[]>([]);
  let loading = $state(true);
  let error = $state<string | null>(null);

  // Helper para convertir Date a ISO string
  function formatDateForApi(date: Date): string {
    return date.toISOString().split('T')[0];
  }

  // Helper para determinar el color según el porcentaje de uso
  function getUsageColor(used: number, available: number): string {
    if (available === 0) return 'text-gray-600';
    const percentage = (used / available) * 100;
    if (percentage < 50) return 'text-green-600';
    if (percentage < 80) return 'text-yellow-600';
    return 'text-red-600';
  }

  // Helper para determinar el color de la barra
  function getProgressBarColor(used: number, available: number): string {
    if (available === 0) return 'bg-gray-400';
    const percentage = (used / available) * 100;
    if (percentage < 50) return 'bg-green-600';
    if (percentage < 80) return 'bg-yellow-600';
    return 'bg-red-600';
  }

  // Helper para calcular el porcentaje
  function getProgressWidth(used: number, available: number): number {
    if (available === 0) return 0;
    return Math.min((used / available) * 100, 100);
  }

  // Cargar el resumen
  async function loadSummary() {
    loading = true;
    error = null;
    try {
      const startDateStr = formatDateForApi(startDate);
      const endDateStr = formatDateForApi(endDate);
      summary = await calendarService.getSummary(startDateStr, endDateStr);
    } catch (err) {
      error = err instanceof Error ? err.message : 'Error al cargar el resumen';
      console.error('Error loading calendar summary:', err);
    } finally {
      loading = false;
    }
  }

  // Cargar al montar y cuando cambien las fechas
  onMount(() => {
    loadSummary();
  });

  // Recargar cuando cambien las fechas (usando $effect)
  $effect(() => {
    // Acceder a las props para que el efecto se ejecute cuando cambien
    startDate;
    endDate;
    loadSummary();
  });
</script>

<div class="calendar-summary bg-white rounded-lg shadow-md p-6 mb-6">
  <h3 class="text-xl font-bold mb-4 text-gray-800">Resumen del Período</h3>

  {#if loading}
    <div class="flex items-center justify-center py-8">
      <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      <span class="ml-3 text-gray-600">Cargando resumen...</span>
    </div>
  {:else if error}
    <div class="bg-red-50 border border-red-200 rounded-lg p-4 text-red-700">
      <p class="font-semibold">Error al cargar el resumen</p>
      <p class="text-sm mt-1">{error}</p>
    </div>
  {:else if summary.length === 0}
    <div class="bg-blue-50 border border-blue-200 rounded-lg p-4 text-blue-700">
      <p class="text-sm">No hay vacaciones aprobadas en este período</p>
    </div>
  {:else}
    <div class="space-y-4">
      {#each summary as person (person.personId)}
        <div class="person-summary border-b border-gray-200 pb-4 last:border-b-0 last:pb-0">
          <div class="flex items-center justify-between mb-2">
            <span class="font-semibold text-gray-800">{person.personName}</span>
            <span
              class={`text-sm font-bold ${getUsageColor(person.workingDaysInRange, person.availableDays)}`}
            >
              {person.workingDaysInRange}
              {person.workingDaysInRange === 1 ? 'día' : 'días'}
            </span>
          </div>

          <!-- Barra de progreso -->
          <div class="w-full bg-gray-200 rounded-full h-3 overflow-hidden">
            <div
              class={`h-3 rounded-full transition-all duration-300 ${getProgressBarColor(person.workingDaysInRange, person.availableDays)}`}
              style="width: {getProgressWidth(person.workingDaysInRange, person.availableDays)}%"
            ></div>
          </div>

          <!-- Información adicional -->
          <div class="mt-1 flex justify-between text-xs text-gray-600">
            <span>
              {person.workingDaysInRange} de {person.availableDays} días anuales
            </span>
            <span>
              {getProgressWidth(person.workingDaysInRange, person.availableDays).toFixed(1)}%
            </span>
          </div>
        </div>
      {/each}
    </div>
  {/if}
</div>

<style>
  .animate-spin {
    animation: spin 1s linear infinite;
  }

  @keyframes spin {
    from {
      transform: rotate(0deg);
    }
    to {
      transform: rotate(360deg);
    }
  }
</style>
