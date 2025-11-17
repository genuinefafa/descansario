<script lang="ts">
  import type { Vacation } from '$lib/types/vacation';
  import { format } from 'date-fns';
  import { es } from 'date-fns/locale';

  interface Props {
    vacations: Vacation[];
    onEdit: (vacation: Vacation) => void;
    onDelete: (id: number) => void;
  }

  let { vacations, onEdit, onDelete }: Props = $props();

  // Filtro "desde" - predeterminado hoy
  let filterFromDate = $state(new Date().toISOString().split('T')[0]);

  // Vacaciones filtradas y ordenadas
  let filteredVacations = $derived(() => {
    const fromDate = new Date(filterFromDate);
    return vacations
      .filter(v => new Date(v.endDate) >= fromDate)
      .sort((a, b) => new Date(a.startDate).getTime() - new Date(b.startDate).getTime());
  });

  function formatDate(dateString: string): string {
    return format(new Date(dateString), 'dd/MM/yyyy', { locale: es });
  }

  function getStatusBadge(status: string): string {
    const badges = {
      Pending: 'bg-yellow-100 text-yellow-800',
      Approved: 'bg-green-100 text-green-800',
      Rejected: 'bg-red-100 text-red-800',
    };
    return badges[status as keyof typeof badges] || 'bg-gray-100 text-gray-800';
  }

  function getStatusText(status: string): string {
    const texts = {
      Pending: 'Pendiente',
      Approved: 'Aprobado',
      Rejected: 'Rechazado',
    };
    return texts[status as keyof typeof texts] || status;
  }
</script>

<div class="bg-white shadow-md rounded-lg overflow-hidden">
  <div class="p-4 border-b border-gray-200">
    <label for="filterFrom" class="block text-sm font-medium text-gray-700 mb-2">
      Mostrar vacaciones activas desde:
    </label>
    <input
      id="filterFrom"
      type="date"
      bind:value={filterFromDate}
      class="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
    />
    <span class="ml-3 text-sm text-gray-500">
      ({filteredVacations().length} de {vacations.length} vacaciones)
    </span>
  </div>

  {#if filteredVacations().length === 0}
    <div class="p-8 text-center text-gray-500">
      <p>No hay vacaciones registradas</p>
      <p class="text-sm mt-2">Comienza agregando una nueva vacación</p>
    </div>
  {:else}
    <table class="min-w-full divide-y divide-gray-200">
      <thead class="bg-gray-50">
        <tr>
          <th
            class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
          >
            Persona
          </th>
          <th
            class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
          >
            Fecha Inicio
          </th>
          <th
            class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
          >
            Fecha Fin
          </th>
          <th
            class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
          >
            Días Hábiles
          </th>
          <th
            class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
          >
            Estado
          </th>
          <th
            class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider"
          >
            Acciones
          </th>
        </tr>
      </thead>
      <tbody class="bg-white divide-y divide-gray-200">
        {#each filteredVacations() as vacation (vacation.id)}
          <tr class="hover:bg-gray-50">
            <td class="px-6 py-4 whitespace-nowrap">
              <div class="text-sm font-medium text-gray-900">{vacation.personName}</div>
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <div class="text-sm text-gray-900">{formatDate(vacation.startDate)}</div>
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <div class="text-sm text-gray-900">{formatDate(vacation.endDate)}</div>
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <div class="text-sm text-gray-900">{vacation.workingDaysCount} días</div>
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <span
                class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full {getStatusBadge(
                  vacation.status
                )}"
              >
                {getStatusText(vacation.status)}
              </span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
              <button
                onclick={() => onEdit(vacation)}
                class="text-blue-600 hover:text-blue-900 mr-4"
              >
                Editar
              </button>
              <button onclick={() => onDelete(vacation.id)} class="text-red-600 hover:text-red-900">
                Eliminar
              </button>
            </td>
          </tr>
        {/each}
      </tbody>
    </table>
  {/if}
</div>
