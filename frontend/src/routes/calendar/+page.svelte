<script lang="ts">
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import VacationCalendar from '$lib/components/VacationCalendar.svelte';
  import { vacationsService } from '$lib/services/vacations';
  import { personsService } from '$lib/services/persons';
  import { holidaysService } from '$lib/services/holidays';
  import type { Vacation } from '$lib/types/vacation';
  import type { Person } from '$lib/types/person';
  import type { Holiday } from '$lib/types/holiday';

  let vacations = $state<Vacation[]>([]);
  let persons = $state<Person[]>([]);
  let holidays = $state<Holiday[]>([]);
  let loading = $state(true);
  let error = $state('');

  onMount(async () => {
    await Promise.all([loadVacations(), loadPersons(), loadHolidays()]);
  });

  async function loadVacations() {
    try {
      loading = true;
      error = '';
      vacations = await vacationsService.getAll();
    } catch (err) {
      error = 'Error al cargar las vacaciones. Verifica que el backend esté corriendo.';
      console.error(err);
    } finally {
      loading = false;
    }
  }

  async function loadPersons() {
    try {
      persons = await personsService.getAll();
    } catch (err) {
      console.error(err);
    }
  }

  async function loadHolidays() {
    try {
      holidays = await holidaysService.getAll();
    } catch (err) {
      console.error(err);
    }
  }

  function handleEditVacation(vacation: Vacation) {
    // Navigate to vacations page with highlight param
    goto(`/vacations?highlight=${vacation.id}`);
  }
</script>

<svelte:head>
  <title>Calendario - Descansario</title>
</svelte:head>

<div class="container mx-auto px-4 py-8">
  {#if error}
    <div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-6">
      {error}
    </div>
  {/if}

  {#if loading}
    <div class="bg-white p-8 rounded-lg shadow-md text-center">
      <p class="text-gray-500">Cargando...</p>
    </div>
  {:else}
    <VacationCalendar {vacations} {persons} {holidays} onEditVacation={handleEditVacation} />
  {/if}
</div>
