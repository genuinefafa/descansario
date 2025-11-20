<script lang="ts">
  import { onMount } from 'svelte';
  import { statsService } from '$lib/services/stats';
  import type { PersonStats } from '$lib/types/stats';
  import { format, parseISO } from 'date-fns';
  import { es } from 'date-fns/locale';

  interface Props {
    personId: number;
    year: number;
  }

  let { personId, year }: Props = $props();

  let stats = $state<PersonStats | null>(null);
  let loading = $state(true);
  let error = $state<string | null>(null);

  async function loadStats() {
    try {
      loading = true;
      error = null;
      stats = await statsService.getPersonStats(personId, year);
    } catch (err) {
      error = err instanceof Error ? err.message : 'Error al cargar estadísticas';
      console.error('Error loading person stats:', err);
    } finally {
      loading = false;
    }
  }

  // Recargar cuando cambien personId o year
  $effect(() => {
    if (personId && year) {
      loadStats();
    }
  });

  function getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'Approved':
        return 'bg-green-100 text-green-800';
      case 'Pending':
        return 'bg-yellow-100 text-yellow-800';
      case 'Rejected':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  function getStatusLabel(status: string): string {
    switch (status) {
      case 'Approved':
        return 'Aprobado';
      case 'Pending':
        return 'Pendiente';
      case 'Rejected':
        return 'Rechazado';
      default:
        return status;
    }
  }

  function formatDate(dateString: string): string {
    try {
      return format(parseISO(dateString), 'dd/MM/yyyy', { locale: es });
    } catch {
      return dateString;
    }
  }
</script>

{#if loading}
  <div class="person-stats-detail bg-white p-6 rounded-lg shadow">
    <p class="text-gray-500">Cargando detalles...</p>
  </div>
{:else if error}
  <div class="person-stats-detail bg-red-50 border border-red-200 p-6 rounded-lg">
    <p class="text-red-600">Error: {error}</p>
  </div>
{:else if stats}
  <div class="person-stats-detail bg-white p-6 rounded-lg shadow">
    <h2 class="text-2xl font-bold mb-4">{stats.personName} - Estadísticas {stats.year}</h2>

    <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
      <div class="stat-box bg-gray-50 p-4 rounded">
        <p class="text-sm text-gray-600">Total Disponible</p>
        <p class="text-2xl font-bold">{stats.available}</p>
        <p class="text-xs text-gray-500">días</p>
      </div>

      <div class="stat-box bg-green-50 p-4 rounded">
        <p class="text-sm text-gray-600">Aprobados</p>
        <p class="text-2xl font-bold text-green-600">{stats.approved}</p>
        <p class="text-xs text-gray-500">
          {stats.available > 0 ? ((stats.approved / stats.available) * 100).toFixed(1) : 0}%
        </p>
      </div>

      <div class="stat-box bg-yellow-50 p-4 rounded">
        <p class="text-sm text-gray-600">Pendientes</p>
        <p class="text-2xl font-bold text-yellow-600">{stats.pending}</p>
        <p class="text-xs text-gray-500">
          {stats.available > 0 ? ((stats.pending / stats.available) * 100).toFixed(1) : 0}%
        </p>
      </div>

      <div class="stat-box bg-blue-50 p-4 rounded">
        <p class="text-sm text-gray-600">Restantes</p>
        <p class="text-2xl font-bold text-blue-600">{stats.remaining}</p>
        <p class="text-xs text-gray-500">
          {stats.available > 0 ? ((stats.remaining / stats.available) * 100).toFixed(1) : 0}%
        </p>
      </div>
    </div>

    {#if stats.rejected > 0}
      <div class="mb-6 bg-red-50 p-3 rounded">
        <p class="text-sm text-red-600">
          <span class="font-semibold">Rechazados:</span> {stats.rejected} días
        </p>
      </div>
    {/if}

    <div class="upcoming-vacations">
      <h3 class="text-lg font-semibold mb-3">Próximas Vacaciones</h3>

      {#if stats.upcomingVacations.length === 0}
        <p class="text-gray-500 text-sm">No hay vacaciones próximas programadas</p>
      {:else}
        <div class="space-y-2">
          {#each stats.upcomingVacations as vacation}
            <div class="vacation-item flex justify-between items-center bg-gray-50 p-3 rounded">
              <div>
                <p class="font-medium">
                  {formatDate(vacation.startDate)} - {formatDate(vacation.endDate)}
                </p>
                <p class="text-sm text-gray-600">{vacation.workingDaysCount} días hábiles</p>
              </div>
              <span class="px-3 py-1 rounded-full text-xs font-semibold {getStatusBadgeClass(vacation.status)}">
                {getStatusLabel(vacation.status)}
              </span>
            </div>
          {/each}
        </div>
      {/if}
    </div>
  </div>
{/if}
