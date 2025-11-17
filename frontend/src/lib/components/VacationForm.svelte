<script lang="ts">
  import type { Vacation } from '$lib/types/vacation';
  import type { Person } from '$lib/types/person';
  import type { Holiday } from '$lib/types/holiday';
  import { vacationsService } from '$lib/services/vacations';
  import { parseISO, isWithinInterval, eachDayOfInterval, isWeekend } from 'date-fns';

  interface Props {
    vacation?: Vacation | null;
    persons: Person[];
    holidays: Holiday[];
    onSubmit: (data: {
      personId: number;
      startDate: string;
      endDate: string;
      status?: 'Pending' | 'Approved' | 'Rejected';
    }) => Promise<void>;
    onCancel: () => void;
  }

  let { vacation = null, persons, holidays, onSubmit, onCancel }: Props = $props();

  let personId = $state(vacation?.personId || (persons[0]?.id ?? 0));
  let startDate = $state(vacation?.startDate.split('T')[0] || '');
  let endDate = $state(vacation?.endDate.split('T')[0] || '');
  let status = $state<'Pending' | 'Approved' | 'Rejected'>(vacation?.status || 'Pending');
  let notes = $state(vacation?.notes || '');
  let isSubmitting = $state(false);
  let error = $state('');
  let overlappingVacations = $state<Vacation[]>([]);

  // Calcular feriados y días hábiles en el rango seleccionado
  let holidaysInRange = $derived(() => {
    if (!startDate || !endDate) return [];

    const start = parseISO(startDate + 'T00:00:00');
    const end = parseISO(endDate + 'T23:59:59');

    return holidays.filter(h => {
      const holidayDate = parseISO(h.date);
      return holidayDate >= start && holidayDate <= end;
    });
  });

  // Calcular días hábiles estimados (excluye weekends y feriados)
  let estimatedWorkingDays = $derived(() => {
    if (!startDate || !endDate) return 0;

    const start = parseISO(startDate + 'T00:00:00');
    const end = parseISO(endDate + 'T23:59:59');
    const allDays = eachDayOfInterval({ start, end });

    // Contar días que NO son weekend ni feriados
    return allDays.filter(day => {
      if (isWeekend(day)) return false;

      const dayStr = day.toISOString().split('T')[0];
      const isHoliday = holidays.some(h => {
        const holidayStr = parseISO(h.date).toISOString().split('T')[0];
        return holidayStr === dayStr;
      });

      return !isHoliday;
    }).length;
  });

  async function handleSubmit(event: Event) {
    event.preventDefault();
    error = '';

    if (!personId || !startDate || !endDate) {
      error = 'Todos los campos son requeridos';
      return;
    }

    if (new Date(startDate) > new Date(endDate)) {
      error = 'La fecha de inicio debe ser anterior o igual a la fecha de fin';
      return;
    }

    try {
      isSubmitting = true;
      const submitData: any = {
        personId,
        startDate: new Date(startDate).toISOString(),
        endDate: new Date(endDate).toISOString(),
        status: status,
        notes: notes || undefined,
      };

      await onSubmit(submitData);
    } catch (err) {
      error = err instanceof Error ? err.message : 'Error al guardar la vacación';
    } finally {
      isSubmitting = false;
    }
  }

  $effect(() => {
    if (vacation) {
      personId = vacation.personId;
      startDate = vacation.startDate.split('T')[0];
      endDate = vacation.endDate.split('T')[0];
      status = vacation.status;
      notes = vacation.notes || '';
    } else {
      personId = persons[0]?.id ?? 0;
      startDate = '';
      endDate = '';
      status = 'Pending';
      notes = '';
    }
  });

  // Auto-seleccionar endDate cuando cambia startDate
  $effect(() => {
    if (startDate && (!endDate || endDate < startDate)) {
      const start = new Date(startDate);
      start.setDate(start.getDate() + 1);
      endDate = start.toISOString().split('T')[0];
    }
  });

  // Verificar solapamiento de vacaciones
  $effect(() => {
    if (startDate && endDate) {
      vacationsService.getOverlapping(startDate, endDate)
        .then(vacations => {
          // Filtrar la vacación actual si estamos editando
          overlappingVacations = vacations.filter(v => v.id !== vacation?.id);
        })
        .catch(err => {
          console.error('Error checking overlapping vacations:', err);
        });
    } else {
      overlappingVacations = [];
    }
  });
</script>

<div class="bg-white p-6 rounded-lg shadow-md">
  <h2 class="text-xl font-bold text-gray-900 mb-4">
    {vacation ? 'Editar Vacación' : 'Nueva Vacación'}
  </h2>

  <form onsubmit={handleSubmit} class="space-y-4">
    {#if error}
      <div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
        {error}
      </div>
    {/if}

    <div>
      <label for="personId" class="block text-sm font-medium text-gray-700 mb-1"> Persona </label>
      <select
        id="personId"
        bind:value={personId}
        required
        class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
      >
        {#each persons as person (person.id)}
          <option value={person.id}>{person.name}</option>
        {/each}
      </select>
    </div>

    <div>
      <label for="startDate" class="block text-sm font-medium text-gray-700 mb-1">
        Fecha de Inicio
      </label>
      <input
        id="startDate"
        type="date"
        bind:value={startDate}
        required
        class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
      />
    </div>

    <div>
      <label for="endDate" class="block text-sm font-medium text-gray-700 mb-1">
        Fecha de Fin
      </label>
      <input
        id="endDate"
        type="date"
        bind:value={endDate}
        required
        class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
      />
    </div>

    {#if startDate && endDate}
      <div class="bg-green-50 border border-green-200 px-4 py-3 rounded">
        <p class="font-semibold text-sm text-green-900 mb-1">
          Días hábiles estimados: <span class="text-lg">{estimatedWorkingDays()}</span>
        </p>
        <p class="text-xs text-green-700">
          (excluye fines de semana y feriados)
        </p>
      </div>
    {/if}

    {#if holidaysInRange().length > 0}
      <div class="bg-yellow-50 border border-yellow-200 text-yellow-800 px-4 py-3 rounded">
        <p class="font-medium text-sm mb-1">
          Feriados en este período ({holidaysInRange().length}):
        </p>
        <ul class="text-sm space-y-1">
          {#each holidaysInRange() as holiday}
            <li>
              • {new Date(holiday.date).toLocaleDateString('es-AR')} - {holiday.name}
            </li>
          {/each}
        </ul>
      </div>
    {/if}

    {#if overlappingVacations.length > 0}
      <div class="bg-blue-50 border border-blue-200 text-blue-800 px-4 py-3 rounded">
        <p class="font-medium text-sm mb-1">Vacaciones en este período:</p>
        <ul class="text-sm space-y-1">
          {#each overlappingVacations as overlap}
            <li>
              • {overlap.personName} ({new Date(overlap.startDate).toLocaleDateString('es-AR')} - {new Date(overlap.endDate).toLocaleDateString('es-AR')})
            </li>
          {/each}
        </ul>
      </div>
    {/if}

    <div>
      <label for="status" class="block text-sm font-medium text-gray-700 mb-1"> Estado </label>
      <select
        id="status"
        bind:value={status}
        class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
      >
        <option value="Pending">Pendiente</option>
        <option value="Approved">Aprobado</option>
        <option value="Rejected">Rechazado</option>
      </select>
    </div>

    <div>
      <label for="notes" class="block text-sm font-medium text-gray-700 mb-1">
        Notas <span class="text-gray-500 text-xs font-normal">(soporte Markdown)</span>
      </label>
      <textarea
        id="notes"
        bind:value={notes}
        rows="4"
        placeholder="Agrega notas adicionales..."
        class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 font-mono text-sm"
      ></textarea>
    </div>

    <div class="flex gap-3 pt-4">
      <button
        type="submit"
        disabled={isSubmitting}
        class="flex-1 bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed"
      >
        {isSubmitting ? 'Guardando...' : vacation ? 'Actualizar' : 'Crear'}
      </button>
      <button
        type="button"
        onclick={onCancel}
        disabled={isSubmitting}
        class="flex-1 bg-gray-200 text-gray-700 py-2 px-4 rounded-md hover:bg-gray-300 disabled:bg-gray-100 disabled:cursor-not-allowed"
      >
        Cancelar
      </button>
    </div>
  </form>
</div>
