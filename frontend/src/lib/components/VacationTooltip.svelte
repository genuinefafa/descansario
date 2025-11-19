<script lang="ts">
  import type { Vacation } from '$lib/types/vacation';
  import { parseISO, differenceInCalendarDays, format } from 'date-fns';
  import { es } from 'date-fns/locale';

  interface Props {
    vacation: Vacation | null;
    position: { x: number; y: number } | null;
    onClose: () => void;
    onEdit?: (vacation: Vacation) => void;
  }

  let { vacation, position, onClose, onEdit }: Props = $props();

  // Calcular días corridos
  const calendarDays = $derived(() => {
    if (!vacation) return 0;
    const start = parseISO(vacation.startDate);
    const end = parseISO(vacation.endDate);
    return differenceInCalendarDays(end, start) + 1;
  });

  // Renderizar markdown simple (negrita, itálica, enlaces)
  function renderMarkdown(text: string | undefined): string {
    if (!text) return '';

    return text
      .replace(/\*\*(.+?)\*\*/g, '<strong class="font-semibold">$1</strong>')
      .replace(/\*(.+?)\*/g, '<em class="italic">$1</em>')
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

  // Manejar escape para cerrar
  function handleKeydown(e: KeyboardEvent) {
    if (e.key === 'Escape') {
      onClose();
    }
  }

  // Calcular posición del tooltip
  const tooltipStyle = $derived(() => {
    if (!position) return '';

    // Ajustar para que no se salga de la pantalla
    const maxWidth = 400;
    let left = position.x;
    let top = position.y + 10; // 10px debajo del cursor

    // Si está muy a la derecha, moverlo a la izquierda
    if (left + maxWidth > window.innerWidth - 20) {
      left = window.innerWidth - maxWidth - 20;
    }

    // Si está muy arriba, moverlo abajo
    if (top < 20) {
      top = 20;
    }

    return `left: ${left}px; top: ${top}px;`;
  });
</script>

<svelte:window onkeydown={handleKeydown} />

{#if vacation && position}
  <!-- Backdrop invisible para cerrar al hacer click fuera -->
  <!-- svelte-ignore a11y_click_events_have_key_events -->
  <div
    class="fixed inset-0 z-40"
    onclick={onClose}
    role="button"
    tabindex="-1"
    aria-label="Cerrar tooltip"
  ></div>

  <!-- Tooltip/Popover -->
  <div
    class="fixed z-50 bg-white rounded-lg shadow-2xl border border-gray-200 max-w-md"
    style={tooltipStyle()}
    role="dialog"
    aria-modal="false"
  >
    <!-- Header compacto -->
    <div class="flex items-start justify-between p-4 border-b border-gray-200 bg-gray-50">
      <div class="flex-1 min-w-0">
        <h3 class="font-bold text-gray-900 truncate">{vacation.personName}</h3>
        <p class="text-xs text-gray-600 mt-0.5">
          {format(parseISO(vacation.startDate), 'd MMM', { locale: es })} - {format(
            parseISO(vacation.endDate),
            'd MMM yyyy',
            { locale: es }
          )}
        </p>
      </div>
      <button
        onclick={onClose}
        class="text-gray-400 hover:text-gray-600 ml-2 flex-shrink-0"
        aria-label="Cerrar"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
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
    <div class="p-4 space-y-3">
      <!-- Estado -->
      <div class="flex items-center gap-2">
        <span
          class="inline-block px-2 py-1 rounded-md text-xs font-semibold {getStatusColor(
            vacation.status
          )}"
        >
          {getStatusText(vacation.status)}
        </span>
        <span class="text-xs text-gray-600">
          <span class="font-semibold">{calendarDays()}</span>
          {calendarDays() === 1 ? 'día corrido' : 'días corridos'} •
          <span class="font-semibold">{vacation.workingDaysCount}</span>
          {vacation.workingDaysCount === 1 ? 'día hábil' : 'días hábiles'}
        </span>
      </div>

      <!-- Notas -->
      {#if vacation.notes}
        <div class="text-sm text-gray-700 border-t border-gray-100 pt-3">
          <p class="text-xs font-semibold text-gray-500 mb-1">Notas:</p>
          <div class="prose prose-sm max-w-none">
            <!-- eslint-disable-next-line svelte/no-at-html-tags -->
            {@html renderMarkdown(vacation.notes)}
          </div>
        </div>
      {/if}

      <!-- Botón editar -->
      {#if onEdit}
        <div class="pt-2 border-t border-gray-100">
          <button
            onclick={() => {
              onEdit && onEdit(vacation);
              onClose();
            }}
            class="w-full px-3 py-1.5 text-sm font-medium text-blue-700 bg-blue-50 hover:bg-blue-100 rounded-md transition-colors"
          >
            ✏️ Editar
          </button>
        </div>
      {/if}
    </div>
  </div>
{/if}

<style>
  /* Estilos para el contenido markdown */
</style>
