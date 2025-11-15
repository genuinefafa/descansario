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
  } from 'date-fns';
  import { es } from 'date-fns/locale';
  import type { Vacation } from '$lib/types/vacation';
  import type { Person } from '$lib/types/person';

  interface Props {
    vacations: Vacation[];
    persons: Person[];
  }

  let { vacations, persons }: Props = $props();

  let currentDate = $state(new Date());
  const monthStart = $derived(startOfMonth(currentDate));
  const monthEnd = $derived(endOfMonth(currentDate));
  const startDate = $derived(startOfWeek(monthStart, { weekStartsOn: 1 }));
  const endDate = $derived(endOfWeek(monthEnd, { weekStartsOn: 1 }));

  const calendarDays = $derived(() => {
    const days: Date[] = [];
    let day = startDate;
    while (day <= endDate) {
      days.push(day);
      day = addDays(day, 1);
    }
    return days;
  });

  function getVacationsForDay(day: Date): Vacation[] {
    return vacations.filter((vacation) => {
      const start = parseISO(vacation.startDate);
      const end = parseISO(vacation.endDate);
      return isWithinInterval(day, { start, end });
    });
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
      {#each ['Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb', 'Dom'] as day}
        <div class="p-2 text-center text-sm font-semibold text-gray-700">
          {day}
        </div>
      {/each}
    </div>

    <!-- Calendar days -->
    <div class="grid grid-cols-7">
      {#each calendarDays() as day}
        {@const dayVacations = getVacationsForDay(day)}
        {@const isToday = isSameDay(day, new Date())}
        {@const isCurrentMonth = isSameMonth(day, currentDate)}
        <div
          class="min-h-24 p-2 border-b border-r border-gray-200 {isCurrentMonth
            ? 'bg-white'
            : 'bg-gray-50'} {isToday ? 'ring-2 ring-blue-500' : ''}"
        >
          <div class="text-sm font-medium {isCurrentMonth ? 'text-gray-900' : 'text-gray-400'}">
            {format(day, 'd')}
          </div>
          <div class="mt-1 space-y-1">
            {#each dayVacations as vacation}
              {@const person = getPersonById(vacation.personId)}
              {#if person}
                <div
                  class="text-xs px-1 py-0.5 rounded text-white truncate {getPersonColor(
                    vacation.personId
                  )}"
                  title="{person.name} - {vacation.status}"
                >
                  {person.name.split(' ')[0]}
                </div>
              {/if}
            {/each}
          </div>
        </div>
      {/each}
    </div>
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
