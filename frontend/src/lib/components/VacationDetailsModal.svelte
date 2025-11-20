<script lang="ts">
  import type { Vacation } from '$lib/types/vacation';
  import { parseISO, differenceInCalendarDays, format } from 'date-fns';
  import { es } from 'date-fns/locale';

  interface Props {
    vacation: Vacation | null;
    onClose: () => void;
    onEdit?: (vacation: Vacation) => void;
  }

  let { vacation, onClose, onEdit }: Props = $props();

  // Calcular días corridos
  const calendarDays = $derived(() => {
    if (!vacation) return 0;
    const start = parseISO(vacation.startDate);
    const end = parseISO(vacation.endDate);
    return differenceInCalendarDays(end, start) + 1;
  });

  // Manejar click en backdrop para cerrar
  function handleBackdropClick(e: MouseEvent) {
    if (e.target === e.currentTarget) {
      onClose();
    }
  }

  // Manejar escape para cerrar
  function handleKeydown(e: KeyboardEvent) {
    if (e.key === 'Escape') {
      onClose();
    }
  }

  // Renderizar markdown simple (negrita, itálica, enlaces)
  function renderMarkdown(text: string | undefined): string {
    if (!text) return '';

    return text
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/\*(.+?)\*/g, '<em>$1</em>')
      .replace(
        /\[(.+?)\]\((.+?)\)/g,
        '<a href="$2" target="_blank" class="text-blue-600 hover:underline">$1</a>'
      )
      .replace(/\n/g, '<br>');
  }

  // Obtener color del estado
  function getStatusColor(status: string): string {
    switch (status) {
      case 'Approved':
        return 'bg-green-100 text-green-800';
      case 'Rejected':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-yellow-100 text-yellow-800';
    }
  }

  // Obtener texto del estado
  function getStatusText(status: string): string {
    switch (status) {
      case 'Approved':
        return 'Aprobada';
      case 'Rejected':
        return 'Rechazada';
      default:
        return 'Pendiente';
    }
  }
</script>

<svelte:window onkeydown={handleKeydown} />

{#if vacation}
  <!-- Modal backdrop -->
  <!-- svelte-ignore a11y_click_events_have_key_events -->
  <div
    class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4"
    onclick={handleBackdropClick}
    role="dialog"
    aria-modal="true"
    tabindex="-1"
  >
    <!-- Modal content -->
    <div class="bg-white rounded-lg shadow-xl max-w-2xl w-full max-h-[90vh] overflow-auto">
      <!-- Header -->
      <div class="flex items-start justify-between p-6 border-b border-gray-200">
        <div class="flex-1">
          <h2 class="text-2xl font-bold text-gray-900 mb-1">Detalles de Vacación</h2>
          <p class="text-sm text-gray-600">{vacation.personName}</p>
        </div>
        <button
          onclick={onClose}
          class="text-gray-400 hover:text-gray-600 transition-colors ml-4"
          aria-label="Cerrar"
        >
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M6 18L18 6M6 6l12 12"
            />
          </svg>
        </button>
      </div>

      <!-- Content -->
      <div class="p-6 space-y-6">
        <!-- Estado -->
        <div>
          <span
            class="inline-block px-3 py-1 rounded-full text-sm font-semibold {getStatusColor(
              vacation.status
            )}"
          >
            {getStatusText(vacation.status)}
          </span>
        </div>

        <!-- Fechas -->
        <div class="bg-gray-50 rounded-lg p-4">
          <h3 class="text-sm font-semibold text-gray-700 mb-2">Período</h3>
          <div class="space-y-1">
            <p class="text-lg font-medium text-gray-900">
              {format(parseISO(vacation.startDate), 'd MMM', { locale: es })} - {format(
                parseISO(vacation.endDate),
                'd MMM yyyy',
                { locale: es }
              )}
            </p>
            <div class="flex gap-4 text-sm text-gray-600">
              <span>
                <span class="font-semibold">{calendarDays()}</span>
                {calendarDays() === 1 ? 'día corrido' : 'días corridos'}
              </span>
              <span>•</span>
              <span>
                <span class="font-semibold">{vacation.workingDaysCount}</span>
                {vacation.workingDaysCount === 1 ? 'día hábil' : 'días hábiles'}
              </span>
            </div>
          </div>
        </div>

        <!-- Notas -->
        {#if vacation.notes}
          <div>
            <h3 class="text-sm font-semibold text-gray-700 mb-2">Notas</h3>
            <div class="prose prose-sm max-w-none">
              <!-- eslint-disable-next-line svelte/no-at-html-tags -->
              {@html renderMarkdown(vacation.notes)}
            </div>
          </div>
        {/if}
      </div>

      <!-- Footer -->
      <div class="flex items-center justify-end gap-3 p-6 border-t border-gray-200 bg-gray-50">
        <button
          onclick={onClose}
          class="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
        >
          Cerrar
        </button>
        {#if onEdit}
          <button
            onclick={() => onEdit && onEdit(vacation)}
            class="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            ✏️ Editar
          </button>
        {/if}
      </div>
    </div>
  </div>
{/if}

<style>
  /* Styles for prose content are applied inline via @html */
</style>
