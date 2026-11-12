<script lang="ts">
  import { onMount } from 'svelte';
  import VacationList from '$lib/components/VacationList.svelte';
  import VacationFormDrawer from '$lib/components/VacationFormDrawer.svelte';
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

  // Drawer state
  let isDrawerOpen = $state(false);
  let editingVacation = $state<Vacation | null>(null);

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

  async function handleSubmit(data: {
    personId: number;
    startDate: string;
    endDate: string;
    status?: 'Pending' | 'Approved' | 'Rejected';
  }) {
    try {
      if (editingVacation) {
        await vacationsService.update(editingVacation.id, {
          ...data,
          status: data.status || 'Pending',
        });
      } else {
        await vacationsService.create(data);
      }
      await loadVacations();
    } catch (err) {
      error = 'Error al guardar la vacación';
      console.error(err);
      throw err;
    }
  }

  function handleEdit(vacation: Vacation) {
    editingVacation = vacation;
    isDrawerOpen = true;
  }

  async function handleDelete(id: number) {
    if (!confirm('¿Estás seguro de eliminar esta vacación?')) return;

    try {
      await vacationsService.delete(id);
      await loadVacations();
    } catch (err) {
      error = 'Error al eliminar la vacación';
      console.error(err);
    }
  }

  function openNewForm() {
    editingVacation = null;
    isDrawerOpen = true;
  }

  function closeDrawer() {
    isDrawerOpen = false;
    editingVacation = null;
  }
</script>

<svelte:head>
  <title>Vacaciones - Descansario</title>
</svelte:head>

<div class="container mx-auto px-4 py-8">
  <div class="mb-6">
    <h1 class="text-3xl font-bold text-gray-900 mb-2">Vacaciones</h1>
    <p class="text-gray-600">Gestiona las vacaciones del equipo</p>
  </div>

  {#if error}
    <div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-6">
      {error}
    </div>
  {/if}

  <div class="mb-4">
    <button
      onclick={openNewForm}
      class="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 font-medium"
      disabled={persons.length === 0}
    >
      + Nueva Vacación
    </button>
    {#if persons.length === 0}
      <p class="text-sm text-gray-500 mt-2">Primero debes crear al menos una persona</p>
    {/if}
  </div>

  {#if loading}
    <div class="bg-white p-8 rounded-lg shadow-md text-center">
      <p class="text-gray-500">Cargando...</p>
    </div>
  {:else}
    <VacationList {vacations} onEdit={handleEdit} onDelete={handleDelete} />
  {/if}
</div>

<!-- Drawer para editar/crear vacación -->
<VacationFormDrawer
  isOpen={isDrawerOpen}
  vacation={editingVacation}
  {persons}
  {holidays}
  onClose={closeDrawer}
  onSubmit={handleSubmit}
/>
