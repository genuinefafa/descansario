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

  let { vacations, persons }: Props = $props();

  let currentDate = $state(new Date());
  const monthStart = $derived(startOfMonth(currentDate));
  const monthEnd = $derived(endOfMonth(currentDate));
  const calendarStart = $derived(startOfWeek(monthStart, { weekStartsOn: 0 }));
  const calendarEnd = $derived(endOfWeek(monthEnd, { weekStartsOn: 0 }));

  // Organizar días en semanas
  const calendarWeeks = $derived(() => {
    const weeks: Date[][] = [];
    let currentWeekStart = calendarStart;

    while (currentWeekStart <= calendarEnd) {
      const week: Date[] = [];
      for (let i = 0; i < 7; i++) {
        week.push(addDays(currentWeekStart, i));
      }
      weeks.push(week);
      currentWeekStart = addDays(currentWeekStart, 7);
    }

    return weeks;
  });

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

  function previousMonth() {
    currentDate = subMonths(currentDate, 1);
  }

  function nextMonth() {
    currentDate = addMonths(currentDate, 1);
  }

  function goToToday() {
    currentDate = new Date();
  }
</script>

<div class="bg-white rounded-lg shadow-md p-6">
  <!-- Calendar Header -->
  <div class="flex items-center justify-between mb-6">
    <h2 class="text-2xl font-bold text-gray-900">
      {format(currentDate, 'MMMM yyyy', { locale: es })}
    </h2>
    <div class="flex gap-2">
      <button
        onclick={previousMonth}
        class="px-3 py-1 bg-gray-100 hover:bg-gray-200 rounded-md text-gray-700 font-medium"
      >
        ← Anterior
      </button>
      <button
        onclick={goToToday}
        class="px-3 py-1 bg-blue-100 hover:bg-blue-200 rounded-md text-blue-700 font-medium"
      >
        Hoy
      </button>
      <button
        onclick={nextMonth}
        class="px-3 py-1 bg-gray-100 hover:bg-gray-200 rounded-md text-gray-700 font-medium"
      >
        Siguiente →
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

  <!-- Calendar Grid -->
  <div class="border border-gray-200 rounded-lg overflow-hidden">
    <!-- Day headers -->
    <div class="grid grid-cols-7 bg-gray-50 border-b border-gray-200">
      {#each ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'] as day}
        <div class="p-2 text-center text-sm font-semibold text-gray-700">
          {day}
        </div>
      {/each}
    </div>

    <!-- Calendar weeks -->
    {#each calendarWeeks() as week}
      {@const segments = getVacationSegments(week)}
      <div class="relative">
        <!-- Days row -->
        <div class="grid grid-cols-7">
          {#each week as day}
            {@const isToday = isSameDay(day, new Date())}
            {@const isCurrentMonth = isSameMonth(day, currentDate)}
            {@const isWeekendDay = isWeekend(day)}
            <div
              class="h-24 p-2 border-b border-r border-gray-200 {isCurrentMonth
                ? isWeekendDay
                  ? 'bg-gray-100'
                  : 'bg-white'
                : 'bg-gray-50'} {isToday ? 'ring-2 ring-inset ring-blue-500' : ''}"
            >
              <div class="text-sm font-medium {isCurrentMonth ? 'text-gray-900' : 'text-gray-400'}">
                {format(day, 'd')}
              </div>
            </div>
          {/each}
        </div>

        <!-- Vacation bars overlay -->
        <div class="absolute top-0 left-0 right-0 bottom-0 pointer-events-none">
          <div class="grid grid-cols-7 h-full gap-0 p-2">
            {#each segments as segment}
              {@const isPending = segment.vacation.status === 'Pending'}
              {@const baseColor = getPersonColorValue(segment.person.id)}
              <div
                class="pointer-events-auto text-xs px-2 py-1 rounded text-white truncate {isPending
                  ? ''
                  : getPersonColor(segment.person.id)} mt-1"
                style="grid-column: {segment.startCol +
                  1} / span {segment.span}; grid-row: {(segment.row ?? 0) + 1}; {isPending
                  ? `background: repeating-linear-gradient(45deg, ${baseColor}, ${baseColor} 6px, rgba(255,255,255,0.4) 6px, rgba(255,255,255,0.4) 12px);`
                  : ''}"
                title="{segment.person.name} - {segment.vacation.status}"
              >
                {segment.person.name.split(' ')[0]}
              </div>
            {/each}
          </div>
        </div>
      </div>
    {/each}
  </div>

  <!-- Summary -->
  <div class="mt-4 text-sm text-gray-600">
    {#if vacations.length > 0}
      <p>
        Total de vacaciones en {format(currentDate, 'MMMM', { locale: es })}: {vacations.filter(
          (v) => {
            const start = parseISO(v.startDate);
            const end = parseISO(v.endDate);
            return (
              isWithinInterval(monthStart, { start, end }) ||
              isWithinInterval(monthEnd, { start, end }) ||
              (start <= monthStart && end >= monthEnd)
            );
          }
        ).length}
      </p>
    {:else}
      <p>No hay vacaciones registradas para este mes</p>
    {/if}
  </div>
</div>
