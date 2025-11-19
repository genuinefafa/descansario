<script lang="ts">
  import { onMount } from 'svelte';
  import { calendarService } from '$lib/services/calendar';
  import type { CalendarSummary } from '$lib/types/calendar';
  import type { Vacation } from '$lib/types/vacation';
  import {
    parseISO,
    differenceInCalendarDays,
    format,
    max,
    min,
    startOfYear,
    endOfYear,
    subYears,
  } from 'date-fns';
  import { es } from 'date-fns/locale';

  interface Props {
    startDate: Date;
    endDate: Date;
    vacations: Vacation[]; // Para detectar cambios y refrescar
  }

  let { startDate, endDate, vacations }: Props = $props();

  let summary = $state<CalendarSummary[]>([]);
  let lastYearSummary = $state<CalendarSummary[]>([]);
  let loading = $state(true);
  let loadingLastYear = $state(false);
  let error = $state<string | null>(null);
  let expandedPersonIds = $state<Set<number>>(new Set());
  let showComparison = $state(false);

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

  // Cargar resumen del año anterior (mismo rango de fechas pero año anterior)
  async function loadLastYearSummary() {
    if (!showComparison) return;

    loadingLastYear = true;
    try {
      const lastYearStart = subYears(startDate, 1);
      const lastYearEnd = subYears(endDate, 1);

      const startDateStr = formatDateForApi(lastYearStart);
      const endDateStr = formatDateForApi(lastYearEnd);
      lastYearSummary = await calendarService.getSummary(startDateStr, endDateStr);
    } catch (err) {
      console.error('Error loading last year summary:', err);
      lastYearSummary = [];
    } finally {
      loadingLastYear = false;
    }
  }

  // Toggle comparativa
  function toggleComparison() {
    showComparison = !showComparison;
    if (showComparison && lastYearSummary.length === 0) {
      loadLastYearSummary();
    }
  }

  // Cargar al montar y cuando cambien las fechas
  onMount(() => {
    loadSummary();
  });

  // Recargar cuando cambien las fechas o las vacaciones (usando $effect)
  $effect(() => {
    // Acceder a las props para que el efecto se ejecute cuando cambien
    startDate;
    endDate;
    vacations; // Detectar cambios en vacaciones
    loadSummary();
    if (showComparison) {
      loadLastYearSummary();
    }
  });

  // Toggle expansión de persona
  function togglePersonExpansion(personId: number) {
    const newSet = new Set(expandedPersonIds);
    if (newSet.has(personId)) {
      newSet.delete(personId);
    } else {
      newSet.add(personId);
    }
    expandedPersonIds = newSet;
  }

  // Obtener vacaciones de una persona en el rango visible
  function getPersonVacationsInRange(personId: number): Vacation[] {
    return vacations.filter((v) => {
      if (v.personId !== personId) return false;
      if (v.status !== 'Approved') return false;

      const vacStart = parseISO(v.startDate);
      const vacEnd = parseISO(v.endDate);

      // Verificar intersección con el rango visible
      return vacStart <= endDate && vacEnd >= startDate;
    });
  }

  // Calcular días corridos para una vacación (con intersección del rango visible)
  function getCalendarDaysInRange(vacation: Vacation): {
    start: Date;
    end: Date;
    calendarDays: number;
  } {
    const vacStart = parseISO(vacation.startDate);
    const vacEnd = parseISO(vacation.endDate);

    // Calcular la intersección con el rango visible
    const effectiveStart = max([vacStart, startDate]);
    const effectiveEnd = min([vacEnd, endDate]);

    const calendarDays = differenceInCalendarDays(effectiveEnd, effectiveStart) + 1;

    return {
      start: effectiveStart,
      end: effectiveEnd,
      calendarDays,
    };
  }
</script>

<div class="calendar-summary bg-white rounded-lg shadow-md p-6 mb-6">
  <!-- Header con período y comparativa -->
  <div class="flex items-center justify-between mb-4">
    <div>
      <h3 class="text-xl font-bold text-gray-800">Resumen del Período</h3>
      <p class="text-sm text-gray-600 mt-0.5">
        {format(startDate, 'd MMM', { locale: es })} - {format(endDate, 'd MMM yyyy', {
          locale: es,
        })}
      </p>
    </div>
    <button
      onclick={toggleComparison}
      class="px-3 py-1.5 text-sm bg-gray-100 hover:bg-gray-200 rounded-md text-gray-700 font-medium transition-colors"
    >
      {showComparison ? '✓ ' : ''}Comparar con año anterior
    </button>
  </div>

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
        {@const isExpanded = expandedPersonIds.has(person.personId)}
        {@const personVacations = getPersonVacationsInRange(person.personId)}
        {@const lastYearPerson = showComparison
          ? lastYearSummary.find((p) => p.personId === person.personId)
          : null}
        <div class="person-summary border-b border-gray-200 pb-4 last:border-b-0 last:pb-0">
          <!-- Header clickeable -->
          <button
            onclick={() => togglePersonExpansion(person.personId)}
            class="w-full text-left hover:bg-gray-50 rounded-md p-2 -m-2 transition-colors"
          >
            <div class="flex items-center justify-between mb-2">
              <div class="flex items-center gap-2">
                <span class="text-gray-500 transition-transform {isExpanded ? 'rotate-90' : ''}"
                  >▶</span
                >
                <span class="font-semibold text-gray-800">{person.personName}</span>
              </div>
              <span
                class={`text-sm font-bold ${getUsageColor(person.workingDaysInRange, person.availableDays)}`}
              >
                {person.workingDaysInRange}
                {person.workingDaysInRange === 1 ? 'día hábil' : 'días hábiles'}
              </span>
            </div>

            <!-- Barra de progreso actual -->
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
          </button>

          <!-- Barra año anterior (justo debajo) -->
          {#if showComparison && lastYearPerson}
            <div class="mt-2 px-2">
              <div class="text-[10px] text-gray-500 mb-1">Año anterior (mismo período)</div>
              <div class="w-full bg-gray-200 rounded-full h-2 overflow-hidden opacity-60">
                <div
                  class={`h-2 rounded-full transition-all duration-300 ${getProgressBarColor(lastYearPerson.workingDaysInRange, lastYearPerson.availableDays)}`}
                  style="width: {getProgressWidth(
                    lastYearPerson.workingDaysInRange,
                    lastYearPerson.availableDays
                  )}%"
                ></div>
              </div>
              <div class="mt-0.5 text-[10px] text-gray-500">
                {lastYearPerson.workingDaysInRange}
                {lastYearPerson.workingDaysInRange === 1 ? 'día hábil' : 'días hábiles'} ({getProgressWidth(
                  lastYearPerson.workingDaysInRange,
                  lastYearPerson.availableDays
                ).toFixed(1)}%)
              </div>
            </div>
          {/if}

          <!-- Detalles expandibles -->
          {#if isExpanded && personVacations.length > 0}
            <div class="mt-3 ml-6 space-y-2">
              {#each personVacations as vacation}
                {@const range = getCalendarDaysInRange(vacation)}
                <div class="text-sm bg-gray-50 p-2 rounded border border-gray-200">
                  <div class="flex justify-between items-start">
                    <div>
                      <span class="font-medium">
                        {format(range.start, 'd MMM', { locale: es })} - {format(
                          range.end,
                          'd MMM yyyy',
                          { locale: es }
                        )}
                      </span>
                      <div class="text-xs text-gray-600 mt-0.5">
                        <span class="font-semibold">{range.calendarDays}</span>
                        {range.calendarDays === 1 ? 'día corrido' : 'días corridos'}
                        <span class="mx-1">•</span>
                        <span class="font-semibold">{vacation.workingDaysCount}</span>
                        {vacation.workingDaysCount === 1 ? 'día hábil' : 'días hábiles'}
                      </div>
                    </div>
                  </div>
                </div>
              {/each}
            </div>
          {/if}
        </div>
      {/each}
    </div>
  {/if}

  <!-- Loading indicator para comparativa -->
  {#if showComparison && loadingLastYear}
    <div class="mt-4 flex items-center justify-center py-2 bg-gray-50 rounded-lg">
      <div class="animate-spin rounded-full h-4 w-4 border-b-2 border-gray-400"></div>
      <span class="ml-2 text-xs text-gray-500">Cargando comparativa...</span>
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
