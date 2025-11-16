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

  interface Props {
    vacations: Vacation[];
    persons: Person[];
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
    monthDate: Date;
    isFirstWeekOfMonth: boolean;
    weeksInMonth?: number;
  }

  let { vacations, persons }: Props = $props();

  // Infinite scroll state
  const today = new Date();
  let months = $state<Date[]>([]);
  let allWeeks = $derived(generateAllWeeks());
  let scrollContainer: HTMLDivElement;
  let currentWeekRef: HTMLTableRowElement | null = null;
  let topSentinel: HTMLDivElement;
  let bottomSentinel: HTMLDivElement;
  let hasScrolledToToday = false;

  // Initialize with current month and buffer
  function initializeMonths() {
    const monthsList: Date[] = [];
    // 2 months before, current month, 3 months after
    for (let i = -2; i <= 3; i++) {
      monthsList.push(addMonths(startOfMonth(today), i));
    }
    months = monthsList;
  }

  // Generate all weeks from all months as a flat array
  function generateAllWeeks(): WeekRow[] {
    if (months.length === 0) return [];

    const allWeekRows: WeekRow[] = [];

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

    // Now assign each week to a month and detect first week of month
    for (const monthDate of months) {
      const monthStart = startOfMonth(monthDate);
      const monthWeeks: Date[][] = [];

      // Find all weeks that contain days from this month
      for (const week of allWeeks) {
        const hasMonthDays = week.some((day) => isSameMonth(day, monthDate));
        if (hasMonthDays) {
          monthWeeks.push(week);
        }
      }

      // Mark the first week that contains the 1st day of the month
      const firstWeekIndex = monthWeeks.findIndex((week) =>
        week.some((day) => isSameMonth(day, monthDate) && day.getDate() === 1)
      );

      // Add week rows only for the first occurrence
      monthWeeks.forEach((week, index) => {
        // Only add if this week hasn't been added yet (check by week start date)
        const weekStart = week[0];
        const alreadyAdded = allWeekRows.some((wr) => wr.week[0].getTime() === weekStart.getTime());

        if (!alreadyAdded) {
          allWeekRows.push({
            week,
            monthDate,
            isFirstWeekOfMonth: index === firstWeekIndex,
            weeksInMonth: index === firstWeekIndex ? monthWeeks.length : undefined,
          });
        }
      });
    }

    return allWeekRows;
  }

  // Action to capture the current week ref
  function captureCurrentWeekRef(node: HTMLTableRowElement, containsToday: boolean) {
    if (containsToday && !currentWeekRef) {
      currentWeekRef = node;
    }
    return {
      destroy() {},
    };
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

      // Filtrar solo días laborables
      const workDays = segmentDays.filter((day) => !isWeekend(day));

      if (workDays.length === 0) continue;

      // Agrupar días consecutivos en subsegmentos
      let currentSubSegmentStart = workDays[0];
      let currentSubSegmentEnd = workDays[0];

      for (let i = 1; i < workDays.length; i++) {
        const prevDay = workDays[i - 1];
        const currentDay = workDays[i];

        // Si el día actual es consecutivo al anterior (considerando que puede haber fin de semana en medio)
        const diffDays = Math.floor(
          (currentDay.getTime() - prevDay.getTime()) / (1000 * 60 * 60 * 24)
        );

        if (diffDays <= 3) {
          // 3 días máximo (viernes -> lunes)
          currentSubSegmentEnd = currentDay;
        } else {
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

  // Add months to the beginning
  function loadPreviousMonths() {
    const firstMonth = months[0];
    const twoYearsAgo = subMonths(today, 24);

    // Limit to 2 years in the past
    if (firstMonth <= twoYearsAgo) return;

    const newMonths: Date[] = [];
    for (let i = 3; i > 0; i--) {
      const newMonth = subMonths(firstMonth, i);
      if (newMonth >= twoYearsAgo) {
        newMonths.push(newMonth);
      }
    }
    months = [...newMonths, ...months];
  }

  // Add months to the end
  function loadNextMonths() {
    const lastMonth = months[months.length - 1];
    const twoYearsFromNow = addMonths(today, 24);

    // Limit to 2 years in the future
    if (lastMonth >= twoYearsFromNow) return;

    const newMonths: Date[] = [];
    for (let i = 1; i <= 3; i++) {
      const newMonth = addMonths(lastMonth, i);
      if (newMonth <= twoYearsFromNow) {
        newMonths.push(newMonth);
      }
    }
    months = [...months, ...newMonths];
  }

  function goToToday() {
    if (currentWeekRef) {
      currentWeekRef.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }

  // Setup infinite scroll
  onMount(() => {
    initializeMonths();

    // Setup IntersectionObserver for infinite scroll
    const options = {
      root: scrollContainer,
      rootMargin: '400px',
      threshold: 0,
    };

    const observer = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          if (entry.target === topSentinel) {
            loadPreviousMonths();
          } else if (entry.target === bottomSentinel) {
            loadNextMonths();
          }
        }
      });
    }, options);

    // Wait for next tick to ensure sentinels are rendered
    setTimeout(() => {
      if (topSentinel) observer.observe(topSentinel);
      if (bottomSentinel) observer.observe(bottomSentinel);

      // Scroll to current week
      if (currentWeekRef && !hasScrolledToToday) {
        currentWeekRef.scrollIntoView({ behavior: 'instant', block: 'center' });
        hasScrolledToToday = true;
      }
    }, 100);

    return () => {
      observer.disconnect();
    };
  });
</script>

<div class="bg-white rounded-lg shadow-md p-6">
  <!-- Calendar Header -->
  <div class="flex items-center justify-between mb-6">
    <h2 class="text-2xl font-bold text-gray-900">Calendario de Vacaciones</h2>
    <div class="flex gap-2">
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

  <!-- Scrollable Calendar Container -->
  <div bind:this={scrollContainer} class="overflow-y-auto max-h-[70vh] border border-gray-200 rounded-lg">
    <!-- Top sentinel for infinite scroll -->
    <div bind:this={topSentinel} class="h-1"></div>

    <!-- Continuous Calendar Table -->
    <table class="w-full border-collapse">
      <!-- Day headers - shown once at the top -->
      <thead class="sticky top-0 bg-gray-50 z-10">
        <tr>
          {#each ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'] as day}
            <th class="p-2 text-center text-sm font-semibold text-gray-700 border-b border-gray-200">
              {day}
            </th>
          {/each}
          <th class="p-2 text-center text-sm font-semibold text-gray-700 border-b border-l border-gray-200 w-20">
            Mes
          </th>
        </tr>
      </thead>

      <tbody>
        {#each allWeeks as weekRow, weekIndex}
          {@const segments = getVacationSegments(weekRow.week)}
          {@const containsToday = weekRow.week.some((day) => isSameDay(day, today))}
          <tr use:captureCurrentWeekRef={containsToday}>
            <!-- Wrapper cell for the week -->
            <td colspan="7" class="p-0 relative">
              <div class="relative">
                <!-- Week days grid -->
                <div class="grid grid-cols-7">
                  {#each weekRow.week as day, dayIndex}
                    {@const isToday = isSameDay(day, today)}
                    {@const isWeekendDay = isWeekend(day)}
                    {@const isFirstOfMonth = day.getDate() === 1}
                    {@const isDayInCurrentMonth = isSameMonth(day, weekRow.monthDate)}
                    <div
                      class="h-24 p-1 border-b border-r border-gray-200 {isDayInCurrentMonth
                        ? isWeekendDay
                          ? 'bg-gray-100'
                          : 'bg-white'
                        : 'bg-gray-50'} {isToday ? 'ring-2 ring-inset ring-blue-500' : ''}"
                    >
                      <div class="text-xs font-medium {isDayInCurrentMonth ? 'text-gray-900' : 'text-gray-400'}">
                        {#if isFirstOfMonth}
                          {format(day, 'd MMM', { locale: es })}
                        {:else}
                          {format(day, 'd')}
                        {/if}
                      </div>
                    </div>
                  {/each}
                </div>

                <!-- Vacation bars overlay -->
                <div class="absolute top-0 left-0 right-0 bottom-0 pointer-events-none">
                  <div class="grid grid-cols-7 h-full gap-0 p-1 pt-5">
                    {#each segments as segment}
                      {@const isPending = segment.vacation.status === 'Pending'}
                      {@const baseColor = getPersonColorValue(segment.person.id)}
                      <div
                        class="pointer-events-auto text-xs px-1 py-0.5 rounded text-white truncate {isPending
                          ? ''
                          : getPersonColor(segment.person.id)}"
                        style="grid-column: {segment.startCol + 1} / span {segment.span}; grid-row: {(segment.row ?? 0) + 1}; {isPending
                          ? `background: repeating-linear-gradient(45deg, ${baseColor}, ${baseColor} 3px, rgba(60,60,60,0.3) 3px, rgba(60,60,60,0.3) 6px);`
                          : ''}"
                        title="{segment.person.name} - {segment.vacation.status}"
                      >
                        {segment.person.name.split(' ')[0]}
                      </div>
                    {/each}
                  </div>
                </div>
              </div>
            </td>

            <!-- Month label column (only on first week of month) -->
            {#if weekRow.isFirstWeekOfMonth}
              {@const isCurrentMonth = isSameMonth(weekRow.monthDate, today)}
              <td
                rowspan={weekRow.weeksInMonth}
                class="border-b border-l border-gray-200 bg-gray-50 text-center align-middle {isCurrentMonth
                  ? 'bg-blue-50'
                  : ''}"
              >
                <div class="writing-mode-vertical text-sm font-bold {isCurrentMonth ? 'text-blue-600' : 'text-gray-700'} capitalize py-2">
                  {format(weekRow.monthDate, 'MMMM yyyy', { locale: es })}
                </div>
              </td>
            {:else}
              <!-- Empty cell to maintain 8-column structure when no month label -->
              <td class="hidden"></td>
            {/if}
          </tr>
        {/each}
      </tbody>
    </table>

    <!-- Bottom sentinel for infinite scroll -->
    <div bind:this={bottomSentinel} class="h-1"></div>
  </div>
</div>

<style>
  .writing-mode-vertical {
    writing-mode: vertical-rl;
    text-orientation: mixed;
  }
</style>
