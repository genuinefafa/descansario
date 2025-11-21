<script lang="ts">
  import {
    startOfMonth,
    endOfMonth,
    startOfWeek,
    endOfWeek,
    addDays,
    format,
    isSameMonth,
    isSameDay,
    addMonths,
    subMonths,
    parseISO,
    isWithinInterval,
    getDay,
    max,
    min,
    eachDayOfInterval,
  } from 'date-fns';
  import { es } from 'date-fns/locale';
  import { onMount } from 'svelte';
  import type { Vacation } from '$lib/types/vacation';
  import type { Person } from '$lib/types/person';
  import type { Holiday } from '$lib/types/holiday';
  import CalendarSummary from './CalendarSummary.svelte';
  import VacationTooltip from './VacationTooltip.svelte';

  interface Props {
    vacations: Vacation[];
    persons: Person[];
    holidays: Holiday[];
    onEditVacation?: (vacation: Vacation) => void;
  }

  interface VacationSegment {
    vacation: Vacation;
    person: Person;
    startCol: number; // 0-6 (lunes=0, domingo=6)
    span: number; // cuántas columnas ocupa
    weekStart: Date;
    row?: number; // fila asignada para evitar superposiciones
  }

  interface WeekRow {
    week: Date[];
    monthLabel?: { monthDate: Date; weeksInMonth: number };
  }

  let { vacations, persons, holidays, onEditVacation }: Props = $props();

  // Estado del tooltip
  let selectedVacation = $state<Vacation | null>(null);
  let tooltipPosition = $state<{ x: number; y: number } | null>(null);

  // Estado del sidebar de resumen
  let isSummaryOpen = $state(true);

  // Helper para encontrar feriado en una fecha
  function getHolidayForDate(date: Date): Holiday | undefined {
    const dateStr = format(date, 'yyyy-MM-dd');
    return holidays.find((h) => {
      const holidayDate = format(parseISO(h.date), 'yyyy-MM-dd');
      return holidayDate === dateStr;
    });
  }

  // Infinite scroll state
  const today = new Date();
  let months = $state<Date[]>([]);
  let monthsHistory = $state<Date[][]>([]); // Stack de estados anteriores
  let allWeeks = $derived(generateAllWeeks());
  let bottomSentinel: HTMLDivElement;

  // Calculate visible date range for CalendarSummary
  let visibleStartDate = $derived(() => {
    if (months.length === 0) return today;
    return startOfMonth(months[0]);
  });

  let visibleEndDate = $derived(() => {
    if (months.length === 0) return today;
    return endOfMonth(months[months.length - 1]);
  });

  // Initialize with current month onwards
  function initializeMonths() {
    const monthsList: Date[] = [];

    // From current month to 3 months in the future
    let currentMonth = startOfMonth(today);
    const endMonth = addMonths(today, 3);

    while (currentMonth <= endMonth) {
      monthsList.push(currentMonth);
      currentMonth = addMonths(currentMonth, 1);
    }

    months = monthsList;
  }

  // Detectar si ya tenemos cargado el inicio del año actual
  const hasCurrentYearStart = $derived(() => {
    if (months.length === 0) return false;
    const firstMonth = months[0];
    const currentYear = today.getFullYear();
    const januaryOfCurrentYear = new Date(currentYear, 0, 1);
    return firstMonth <= januaryOfCurrentYear;
  });

  // Cargar hasta el comienzo del año actual
  function loadToStartOfCurrentYear() {
    const firstMonth = months[0];
    const currentYear = today.getFullYear();
    const januaryOfCurrentYear = new Date(currentYear, 0, 1);

    // Si ya tenemos enero del año actual, no hacer nada
    if (firstMonth <= januaryOfCurrentYear) return;

    // Guardar estado actual en historial antes de modificar
    monthsHistory = [...monthsHistory, [...months]];

    const newMonths: Date[] = [];
    let currentMonth = startOfMonth(januaryOfCurrentYear);

    while (currentMonth < firstMonth) {
      newMonths.push(currentMonth);
      currentMonth = addMonths(currentMonth, 1);
    }

    months = [...newMonths, ...months];
  }

  // Cargar el año anterior completo
  function loadPreviousYear() {
    const firstMonth = months[0];
    const previousYear = new Date(firstMonth.getFullYear() - 1, 0, 1); // January of previous year

    // Guardar estado actual en historial antes de modificar
    monthsHistory = [...monthsHistory, [...months]];

    const newMonths: Date[] = [];
    let currentMonth = startOfMonth(previousYear);

    while (currentMonth < firstMonth) {
      newMonths.push(currentMonth);
      currentMonth = addMonths(currentMonth, 1);
    }

    months = [...newMonths, ...months];
  }

  // Función dinámica que carga según el contexto
  function loadPrevious() {
    if (hasCurrentYearStart()) {
      loadPreviousYear();
    } else {
      loadToStartOfCurrentYear();
    }
  }

  // Ocultar la última sección cargada (restaurar estado anterior)
  function hideLastLoaded() {
    if (monthsHistory.length === 0) return;

    // Restaurar el estado anterior del stack
    const previousState = monthsHistory[monthsHistory.length - 1];
    months = [...previousState];
    monthsHistory = monthsHistory.slice(0, -1);
  }

  // Generate all weeks from all months as a flat array
  function generateAllWeeks(): WeekRow[] {
    if (months.length === 0) return [];

    // Calculate the full date range
    const firstMonth = months[0];
    const lastMonth = months[months.length - 1];
    const rangeStart = startOfWeek(startOfMonth(firstMonth), { weekStartsOn: 0 });
    const rangeEnd = endOfWeek(endOfMonth(lastMonth), { weekStartsOn: 0 });

    // Generate all weeks continuously
    let currentWeekStart = rangeStart;
    const allWeeks: Date[][] = [];

    while (currentWeekStart <= rangeEnd) {
      const week: Date[] = [];
      for (let i = 0; i < 7; i++) {
        week.push(addDays(currentWeekStart, i));
      }
      allWeeks.push(week);
      currentWeekStart = addDays(currentWeekStart, 7);
    }

    // Initialize week rows
    const allWeekRows: WeekRow[] = allWeeks.map((week) => ({
      week,
      monthLabel: undefined,
    }));

    // Find weeks that contain day 1 of each month
    const firstDayWeekIndices: number[] = [];
    allWeeks.forEach((week, weekIndex) => {
      const hasFirstDay = week.some((day) => day.getDate() === 1);
      if (hasFirstDay) {
        // Find which month's day 1 this is
        const firstDayInWeek = week.find((day) => day.getDate() === 1);
        if (firstDayInWeek) {
          firstDayWeekIndices.push(weekIndex);

          // Calculate rowspan: from this week until the week before the next "day 1" week
          const nextFirstDayIndex =
            firstDayWeekIndices.length > 0
              ? allWeeks.findIndex((w, idx) => idx > weekIndex && w.some((d) => d.getDate() === 1))
              : -1;

          const weeksInMonth =
            nextFirstDayIndex === -1 ? allWeeks.length - weekIndex : nextFirstDayIndex - weekIndex;

          allWeekRows[weekIndex].monthLabel = {
            monthDate: startOfMonth(firstDayInWeek),
            weeksInMonth,
          };
        }
      }
    });

    return allWeekRows;
  }

  function isWeekend(day: Date): boolean {
    const dayOfWeek = getDay(day);
    return dayOfWeek === 0 || dayOfWeek === 6; // domingo o sábado
  }

  function getPersonById(id: number): Person | undefined {
    return persons.find((p) => p.id === id);
  }

  function getPersonColor(personId: number): string {
    const colors = [
      'bg-blue-500',
      'bg-green-500',
      'bg-purple-500',
      'bg-pink-500',
      'bg-yellow-500',
      'bg-indigo-500',
      'bg-red-500',
      'bg-teal-500',
    ];
    return colors[personId % colors.length];
  }

  function getPersonColorValue(personId: number): string {
    const colors = [
      '#3b82f6', // blue-500
      '#22c55e', // green-500
      '#a855f7', // purple-500
      '#ec4899', // pink-500
      '#eab308', // yellow-500
      '#6366f1', // indigo-500
      '#ef4444', // red-500
      '#14b8a6', // teal-500
    ];
    return colors[personId % colors.length];
  }

  // Asignar filas a segmentos para evitar superposiciones
  function assignRows(segments: VacationSegment[]): VacationSegment[] {
    const rows: { startCol: number; endCol: number }[][] = [];

    for (const segment of segments) {
      const endCol = segment.startCol + segment.span - 1;
      let assignedRow = -1;

      // Buscar la primera fila disponible
      for (let rowIdx = 0; rowIdx < rows.length; rowIdx++) {
        const rowSegments = rows[rowIdx];
        let hasConflict = false;

        for (const existing of rowSegments) {
          // Verificar si hay superposición
          if (!(endCol < existing.startCol || segment.startCol > existing.endCol)) {
            hasConflict = true;
            break;
          }
        }

        if (!hasConflict) {
          assignedRow = rowIdx;
          break;
        }
      }

      // Si no hay fila disponible, crear una nueva
      if (assignedRow === -1) {
        assignedRow = rows.length;
        rows.push([]);
      }

      rows[assignedRow].push({ startCol: segment.startCol, endCol });
      segment.row = assignedRow;
    }

    return segments;
  }

  // Calcular segmentos de vacaciones para una semana
  function getVacationSegments(week: Date[]): VacationSegment[] {
    const segments: VacationSegment[] = [];
    const weekStart = week[0];
    const weekEnd = week[6];

    for (const vacation of vacations) {
      const person = getPersonById(vacation.personId);
      if (!person) continue;

      const vacStart = parseISO(vacation.startDate);
      const vacEnd = parseISO(vacation.endDate);

      // Verificar si la vacación intersecta con esta semana
      if (vacStart > weekEnd || vacEnd < weekStart) continue;

      // Calcular el rango de días de la vacación en esta semana (solo días hábiles)
      const segmentStart = max([vacStart, weekStart]);
      const segmentEnd = min([vacEnd, weekEnd]);

      // Obtener todos los días del segmento
      const segmentDays = eachDayOfInterval({ start: segmentStart, end: segmentEnd });

      // Filtrar solo días laborables (excluir weekends Y feriados)
      const workDays = segmentDays.filter((day) => {
        if (isWeekend(day)) return false;
        const holiday = getHolidayForDate(day);
        return !holiday; // Excluir feriados
      });

      if (workDays.length === 0) continue;

      // Agrupar días consecutivos en subsegmentos
      let currentSubSegmentStart = workDays[0];
      let currentSubSegmentEnd = workDays[0];

      for (let i = 1; i < workDays.length; i++) {
        const prevDay = workDays[i - 1];
        const currentDay = workDays[i];

        // Calcular diferencia en días
        const diffInDays = Math.floor(
          (currentDay.getTime() - prevDay.getTime()) / (1000 * 60 * 60 * 24)
        );

        // Si son días consecutivos (diff = 1), continuar el segmento
        if (diffInDays === 1) {
          currentSubSegmentEnd = currentDay;
          continue;
        }

        // Si hay días en medio, verificar que TODOS sean weekends (no feriados)
        const daysBetween = eachDayOfInterval({
          start: addDays(prevDay, 1),
          end: addDays(currentDay, -1),
        });

        // Solo continuar el segmento si los días intermedios son SOLO weekends
        const allIntermediateAreWeekends = daysBetween.every((day) => isWeekend(day));

        if (allIntermediateAreWeekends) {
          // Continuar el segmento (solo hay weekends en medio)
          currentSubSegmentEnd = currentDay;
        } else {
          // Hay algo más que weekends en medio (probablemente un feriado)
          // Guardar el subsegmento actual y empezar uno nuevo
          const startCol = getDay(currentSubSegmentStart); // dom=0, lun=1, ..., sáb=6
          const endCol = getDay(currentSubSegmentEnd);
          const span = endCol - startCol + 1;

          segments.push({
            vacation,
            person,
            startCol,
            span,
            weekStart,
          });

          currentSubSegmentStart = currentDay;
          currentSubSegmentEnd = currentDay;
        }
      }

      // Agregar el último subsegmento
      const startCol = getDay(currentSubSegmentStart); // dom=0, lun=1, ..., sáb=6
      const endCol = getDay(currentSubSegmentEnd);
      const span = endCol - startCol + 1;

      segments.push({
        vacation,
        person,
        startCol,
        span,
        weekStart,
      });
    }

    return assignRows(segments);
  }

  // Add months to the end (auto-triggered)
  function loadNextMonths() {
    const lastMonth = months[months.length - 1];
    const twoYearsFromNow = addMonths(today, 24);

    // Limit to 2 years in the future
    if (lastMonth >= twoYearsFromNow) return;

    const newMonths: Date[] = [];
    for (let i = 1; i <= 6; i++) {
      const newMonth = addMonths(lastMonth, i);
      if (newMonth <= twoYearsFromNow) {
        newMonths.push(newMonth);
      }
    }
    months = [...months, ...newMonths];
  }

  function goToToday() {
    const anchor = getMonthAnchor(today);
    const element = document.getElementById(anchor);
    if (element) {
      element.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
  }

  // Abrir tooltip de vacación
  function openVacationTooltip(vacation: Vacation, event: MouseEvent) {
    selectedVacation = vacation;
    tooltipPosition = { x: event.clientX, y: event.clientY };
  }

  // Cerrar tooltip
  function closeVacationTooltip() {
    selectedVacation = null;
    tooltipPosition = null;
  }

  // Manejar edición desde el tooltip
  function handleEditFromTooltip(vacation: Vacation) {
    closeVacationTooltip();
    if (onEditVacation) {
      onEditVacation(vacation);
    }
  }

  // Generate month anchor ID (formato: mes-año, ej: "noviembre-2025")
  function getMonthAnchor(monthDate: Date): string {
    return format(monthDate, 'MMMM-yyyy', { locale: es }).toLowerCase();
  }

  // Setup infinite scroll (only for bottom)
  onMount(() => {
    initializeMonths();

    // Setup IntersectionObserver for infinite scroll forward
    const options = {
      root: null, // Use viewport instead of container
      rootMargin: '400px',
      threshold: 0,
    };

    const observer = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting && entry.target === bottomSentinel) {
          loadNextMonths();
        }
      });
    }, options);

    // Wait for next tick to ensure sentinels are rendered
    setTimeout(() => {
      if (bottomSentinel) observer.observe(bottomSentinel);
    }, 100);

    return () => {
      observer.disconnect();
    };
  });
</script>

<!-- Layout con calendario y sidebar -->
<div class="flex gap-4">
  <!-- Calendario principal -->
  <div class="flex-1 bg-white rounded-lg shadow-md p-6">
    <!-- Calendar Header -->
    <div class="flex items-center justify-between mb-6">
      <h2 class="text-2xl font-bold text-gray-900">Calendario de Vacaciones</h2>
      <div class="flex gap-2">
        <button
          onclick={() => (isSummaryOpen = !isSummaryOpen)}
          class="px-4 py-2 rounded-md font-medium transition-colors {isSummaryOpen
            ? 'bg-gray-100 hover:bg-gray-200 text-gray-700'
            : 'bg-blue-100 hover:bg-blue-200 text-blue-700'}"
          title={isSummaryOpen ? 'Ocultar resumen' : 'Mostrar resumen'}
        >
          {isSummaryOpen ? '📊 ←' : '→ 📊'}
        </button>
        <button
          onclick={goToToday}
          class="px-4 py-2 bg-blue-100 hover:bg-blue-200 rounded-md text-blue-700 font-medium"
        >
          Ir a Hoy
        </button>
      </div>
    </div>

  <!-- Legend -->
  <div class="mb-4 flex flex-wrap gap-3">
    {#each persons as person}
      <div class="flex items-center gap-2">
        <div class="w-4 h-4 rounded {getPersonColor(person.id)}"></div>
        <span class="text-sm text-gray-700">{person.name}</span>
      </div>
    {/each}
  </div>

  <!-- Load Previous Button (contextual) -->
  <div class="mb-4 space-y-2">
    <button
      onclick={loadPrevious}
      class="w-full px-4 py-2 bg-gray-100 hover:bg-gray-200 rounded-md text-gray-700 font-medium border border-gray-300"
    >
      {#if hasCurrentYearStart()}
        ↑ Cargar año anterior
      {:else}
        ↑ Cargar hasta comienzo de año
      {/if}
    </button>

    <!-- Hide Last Loaded Button (solo si hay historial) -->
    {#if monthsHistory.length > 0}
      <button
        onclick={hideLastLoaded}
        class="w-full px-4 py-2 bg-amber-50 hover:bg-amber-100 rounded-md text-amber-700 font-medium border border-amber-300"
      >
        ↓ Ocultar última sección cargada
      </button>
    {/if}
  </div>

  <!-- Calendar Container (using browser scroll) -->
  <div class="border border-gray-200 rounded-lg">
    <!-- Continuous Calendar Table -->
    <table class="w-full border-collapse">
      <!-- Day headers - shown once at the top -->
      <thead class="sticky top-0 bg-gray-50 z-10">
        <tr>
          {#each ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'] as day}
            <th
              class="p-2 text-center text-sm font-semibold text-gray-700 border-b border-gray-200"
            >
              {day}
            </th>
          {/each}
          <th
            class="p-2 text-center text-sm font-semibold text-gray-700 border-b border-l border-gray-200 w-20"
          >
            Mes
          </th>
        </tr>
      </thead>

      <tbody>
        {#each allWeeks as weekRow, weekIndex}
          {@const segments = getVacationSegments(weekRow.week)}
          <tr>
            <!-- Wrapper cell for the week -->
            <td colspan="7" class="p-0 relative">
              <div class="relative">
                <!-- Week days grid -->
                <div class="grid grid-cols-7">
                  {#each weekRow.week as day, dayIndex}
                    {@const isToday = isSameDay(day, today)}
                    {@const isWeekendDay = isWeekend(day)}
                    {@const isFirstOfMonth = day.getDate() === 1}
                    {@const dayMonth = startOfMonth(day)}
                    {@const isDayInVisibleMonths = months.some((m) => isSameMonth(m, dayMonth))}
                    {@const holiday = getHolidayForDate(day)}
                    <div
                      class="h-24 p-1 border-b border-r border-gray-200 {isFirstOfMonth
                        ? 'border-l-2 border-l-gray-900'
                        : ''} {isDayInVisibleMonths
                        ? holiday
                          ? 'bg-amber-50'
                          : isWeekendDay
                            ? 'bg-gray-100'
                            : 'bg-white'
                        : 'bg-gray-50'} {isToday ? 'ring-2 ring-inset ring-blue-500' : ''}"
                      title={holiday ? `Feriado: ${holiday.name}` : ''}
                    >
                      <div
                        class="text-xs {isFirstOfMonth
                          ? 'font-bold'
                          : 'font-medium'} {isDayInVisibleMonths
                          ? 'text-gray-900'
                          : 'text-gray-400'}"
                      >
                        {#if isFirstOfMonth}
                          {format(day, 'd MMM', { locale: es })}
                        {:else}
                          {format(day, 'd')}
                        {/if}
                      </div>
                      {#if holiday}
                        <div
                          class="text-[10px] text-red-600 font-semibold leading-tight mt-0.5"
                          title={holiday.name}
                        >
                          {holiday.name.length > 15
                            ? holiday.name.substring(0, 15) + '...'
                            : holiday.name}
                        </div>
                      {/if}
                    </div>
                  {/each}
                </div>

                <!-- Vacation bars overlay -->
                <div class="absolute top-0 left-0 right-0 bottom-0 pointer-events-none">
                  <div class="relative h-full">
                    {#each segments as segment}
                      {@const isPending = segment.vacation.status === 'Pending'}
                      {@const baseColor = getPersonColorValue(segment.person.id)}
                      {@const cellWidth = 100 / 7}
                      {@const leftPos = segment.startCol * cellWidth}
                      {@const width = segment.span * cellWidth}
                      {@const topPos = 20 + (segment.row ?? 0) * 20}
                      <button
                        onclick={(e) => openVacationTooltip(segment.vacation, e)}
                        class="pointer-events-auto absolute text-xs px-1 py-0.5 rounded text-white truncate text-left h-5 leading-tight {isPending
                          ? ''
                          : getPersonColor(
                              segment.person.id
                            )} cursor-pointer hover:opacity-90 transition-opacity"
                        style="left: {leftPos}%; width: calc({width}% - 2px); top: {topPos}px; {isPending
                          ? `background: repeating-linear-gradient(45deg, ${baseColor}, ${baseColor} 3px, rgba(60,60,60,0.3) 3px, rgba(60,60,60,0.3) 6px);`
                          : ''}"
                        title="{segment.person.name} - {segment.vacation
                          .status} (click para detalles)"
                      >
                        {segment.person.name.split(' ')[0]}
                      </button>
                    {/each}
                  </div>
                </div>
              </div>
            </td>

            <!-- Month label column -->
            {#if weekRow.monthLabel}
              {@const isCurrentMonth = isSameMonth(weekRow.monthLabel.monthDate, today)}
              {@const monthAnchor = getMonthAnchor(weekRow.monthLabel.monthDate)}
              <td
                id={monthAnchor}
                rowspan={weekRow.monthLabel.weeksInMonth}
                class="border-b border-l border-gray-200 bg-gray-50 text-center align-middle {isCurrentMonth
                  ? 'bg-blue-50'
                  : ''}"
              >
                <div
                  class="writing-mode-vertical text-sm font-bold {isCurrentMonth
                    ? 'text-blue-600'
                    : 'text-gray-700'} capitalize py-2"
                >
                  {format(weekRow.monthLabel.monthDate, 'MMMM yyyy', { locale: es })}
                </div>
              </td>
            {/if}
          </tr>
        {/each}
      </tbody>
    </table>

    <!-- Bottom sentinel for infinite scroll -->
    <div bind:this={bottomSentinel} class="h-1"></div>
  </div>
  </div>
  <!-- Fin del calendario principal -->

  <!-- Sidebar de resumen (colapsable) -->
  {#if isSummaryOpen}
    <div class="w-80 flex-shrink-0">
      <div class="sticky top-20">
        <CalendarSummary startDate={visibleStartDate()} endDate={visibleEndDate()} {vacations} />
      </div>
    </div>
  {/if}
</div>
<!-- Fin del layout flex -->

<!-- Tooltip de detalles de vacación -->
<VacationTooltip
  vacation={selectedVacation}
  position={tooltipPosition}
  onClose={closeVacationTooltip}
  onEdit={onEditVacation ? handleEditFromTooltip : undefined}
/>

<style>
  .writing-mode-vertical {
    writing-mode: vertical-rl;
    text-orientation: mixed;
  }
</style>
