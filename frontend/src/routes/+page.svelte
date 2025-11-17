<script lang="ts">
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import PersonList from '$lib/components/PersonList.svelte';
  import PersonForm from '$lib/components/PersonForm.svelte';
  import VacationList from '$lib/components/VacationList.svelte';
  import VacationForm from '$lib/components/VacationForm.svelte';
  import VacationCalendar from '$lib/components/VacationCalendar.svelte';
  import HolidayList from '$lib/components/HolidayList.svelte';
  import HolidayForm from '$lib/components/HolidayForm.svelte';
  import { personsService } from '$lib/services/persons';
  import { vacationsService } from '$lib/services/vacations';
  import { holidaysService } from '$lib/services/holidays';
  import type { Person } from '$lib/types/person';
  import type { Vacation } from '$lib/types/vacation';
  import type { Holiday } from '$lib/types/holiday';

  type Tab = 'persons' | 'vacations' | 'holidays' | 'calendar';

  // Initialize tab from URL (runs once on mount)
  function getInitialTab(): Tab {
    if (typeof window !== 'undefined') {
      const urlParams = new URLSearchParams(window.location.search);
      return (urlParams.get('tab') as Tab) || 'persons';
    }
    return 'persons';
  }

  let activeTab = $state<Tab>(getInitialTab());

  // Sync activeTab with URL changes
  $effect(() => {
    const tab = ($page.url.searchParams.get('tab') as Tab) || 'persons';
    if (tab !== activeTab) {
      activeTab = tab;
    }
  });

  // Page title based on active tab
  const pageTitle = $derived(() => {
    switch (activeTab) {
      case 'persons':
        return 'Personas - Descansario';
      case 'vacations':
        return 'Vacaciones - Descansario';
      case 'holidays':
        return 'Feriados - Descansario';
      case 'calendar':
        return 'Calendario - Descansario';
      default:
        return 'Descansario - Gestión de Vacaciones';
    }
  });

  // Persons state
  let persons = $state<Person[]>([]);
  let showPersonForm = $state(false);
  let editingPerson = $state<Person | null>(null);

  // Vacations state
  let vacations = $state<Vacation[]>([]);
  let showVacationForm = $state(false);
  let editingVacation = $state<Vacation | null>(null);

  // Holidays state
  let holidays = $state<Holiday[]>([]);
  let showHolidayForm = $state(false);
  let editingHoliday = $state<Holiday | null>(null);
  let isSyncing = $state(false);

  // Common state
  let loading = $state(true);
  let error = $state('');

  onMount(async () => {
    await Promise.all([loadPersons(), loadVacations(), loadHolidays()]);
  });

  async function loadPersons() {
    try {
      loading = true;
      error = '';
      persons = await personsService.getAll();
    } catch (err) {
      error = 'Error al cargar las personas. Verifica que el backend esté corriendo.';
      console.error(err);
    } finally {
      loading = false;
    }
  }

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

  async function loadHolidays() {
    try {
      loading = true;
      error = '';
      holidays = await holidaysService.getAll();
    } catch (err) {
      error = 'Error al cargar los feriados. Verifica que el backend esté corriendo.';
      console.error(err);
    } finally {
      loading = false;
    }
  }

  // Persons handlers
  async function handlePersonSubmit(data: { name: string; email: string; availableDays: number }) {
    try {
      if (editingPerson) {
        await personsService.update(editingPerson.id, data);
      } else {
        await personsService.create(data);
      }
      await loadPersons();
      closePersonForm();
    } catch (err) {
      error = 'Error al guardar la persona';
      console.error(err);
    }
  }

  function handlePersonEdit(person: Person) {
    editingPerson = person;
    showPersonForm = true;
  }

  async function handlePersonDelete(id: number) {
    if (!confirm('¿Estás seguro de eliminar esta persona?')) return;

    try {
      await personsService.delete(id);
      await loadPersons();
    } catch (err) {
      error = 'Error al eliminar la persona';
      console.error(err);
    }
  }

  function openNewPersonForm() {
    editingPerson = null;
    showPersonForm = true;
  }

  function closePersonForm() {
    showPersonForm = false;
    editingPerson = null;
  }

  // Vacations handlers
  async function handleVacationSubmit(data: {
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
      closeVacationForm();
    } catch (err) {
      error = 'Error al guardar la vacación';
      console.error(err);
    }
  }

  function handleVacationEdit(vacation: Vacation) {
    editingVacation = vacation;
    showVacationForm = true;
  }

  async function handleVacationDelete(id: number) {
    if (!confirm('¿Estás seguro de eliminar esta vacación?')) return;

    try {
      await vacationsService.delete(id);
      await loadVacations();
    } catch (err) {
      error = 'Error al eliminar la vacación';
      console.error(err);
    }
  }

  function openNewVacationForm() {
    editingVacation = null;
    showVacationForm = true;
  }

  function closeVacationForm() {
    showVacationForm = false;
    editingVacation = null;
  }

  // Holidays handlers
  async function handleHolidaySubmit(data: {
    date: string;
    name: string;
    country: 'AR' | 'ES';
    region?: string;
  }) {
    try {
      if (editingHoliday) {
        await holidaysService.update(editingHoliday.id, data);
      } else {
        await holidaysService.create(data);
      }
      await loadHolidays();
      closeHolidayForm();
    } catch (err) {
      error = 'Error al guardar el feriado';
      console.error(err);
    }
  }

  function handleHolidayEdit(holiday: Holiday) {
    editingHoliday = holiday;
    showHolidayForm = true;
  }

  async function handleHolidayDelete(id: number) {
    if (!confirm('¿Estás seguro de eliminar este feriado?')) return;

    try {
      await holidaysService.delete(id);
      await loadHolidays();
    } catch (err) {
      error = 'Error al eliminar el feriado';
      console.error(err);
    }
  }

  async function handleHolidaySync(year: number) {
    try {
      isSyncing = true;
      error = '';
      const response = await holidaysService.sync({ year, country: 'AR' });
      await loadHolidays();

      // Mostrar mensaje de éxito
      const message = `Sincronización completada: ${response.added} agregados, ${response.updated} actualizados (${response.total} total)`;
      alert(message);
    } catch (err) {
      error = 'Error al sincronizar feriados';
      console.error(err);
    } finally {
      isSyncing = false;
    }
  }

  async function handleHolidayImport(jsonContent: string) {
    try {
      isSyncing = true; // Reusar el mismo flag para simplificar
      error = '';
      const response = await holidaysService.import({ jsonContent, country: 'AR' });
      await loadHolidays();

      // Mostrar mensaje de éxito
      const message = `Importación completada: ${response.added} agregados, ${response.updated} actualizados (${response.total} total)`;
      alert(message);
    } catch (err) {
      error = 'Error al importar feriados';
      console.error(err);
      throw err; // Re-throw para que el componente maneje el error
    } finally {
      isSyncing = false;
    }
  }

  async function handleHolidayDeleteByYear(year: number) {
    try {
      isSyncing = true;
      error = '';
      const response = await holidaysService.deleteByYear(year);
      await loadHolidays();

      // Mostrar mensaje de éxito
      alert(`${response.message}`);
    } catch (err) {
      error = `Error al eliminar feriados del año ${year}`;
      console.error(err);
    } finally {
      isSyncing = false;
    }
  }

  function openNewHolidayForm() {
    editingHoliday = null;
    showHolidayForm = true;
  }

  function closeHolidayForm() {
    showHolidayForm = false;
    editingHoliday = null;
  }

  function changeTab(tab: Tab) {
    activeTab = tab;
    goto(`/?tab=${tab}`, { replaceState: true });
  }
</script>

<svelte:head>
  <title>{pageTitle()}</title>
</svelte:head>

<div class="min-h-screen bg-gray-50 py-8">
  <div class="max-w-6xl mx-auto px-4">
    <!-- Header -->
    <div class="mb-8">
      <h1 class="text-4xl font-bold text-gray-900 mb-2">🏖️ Descansario</h1>
      <p class="text-lg text-gray-600">Gestión de vacaciones para tu equipo</p>
    </div>

    {#if error}
      <div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-6">
        {error}
      </div>
    {/if}

    <!-- Tabs -->
    <div class="mb-6 border-b border-gray-200">
      <nav class="-mb-px flex space-x-8">
        <button
          onclick={() => changeTab('persons')}
          class="py-4 px-1 border-b-2 font-medium text-sm {activeTab === 'persons'
            ? 'border-blue-500 text-blue-600'
            : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'}"
        >
          Personas
        </button>
        <button
          onclick={() => changeTab('vacations')}
          class="py-4 px-1 border-b-2 font-medium text-sm {activeTab === 'vacations'
            ? 'border-blue-500 text-blue-600'
            : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'}"
        >
          Vacaciones
        </button>
        <button
          onclick={() => changeTab('holidays')}
          class="py-4 px-1 border-b-2 font-medium text-sm {activeTab === 'holidays'
            ? 'border-blue-500 text-blue-600'
            : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'}"
        >
          Feriados
        </button>
        <button
          onclick={() => changeTab('calendar')}
          class="py-4 px-1 border-b-2 font-medium text-sm {activeTab === 'calendar'
            ? 'border-blue-500 text-blue-600'
            : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'}"
        >
          Calendario
        </button>
      </nav>
    </div>

    <!-- Content -->
    <div class="mb-6">
      {#if activeTab === 'persons'}
        {#if showPersonForm}
          <PersonForm
            person={editingPerson}
            onSubmit={handlePersonSubmit}
            onCancel={closePersonForm}
          />
        {:else}
          <div class="mb-4">
            <button
              onclick={openNewPersonForm}
              class="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 font-medium"
            >
              + Nueva Persona
            </button>
          </div>

          {#if loading}
            <div class="bg-white p-8 rounded-lg shadow-md text-center">
              <p class="text-gray-500">Cargando...</p>
            </div>
          {:else}
            <PersonList {persons} onEdit={handlePersonEdit} onDelete={handlePersonDelete} />
          {/if}
        {/if}
      {:else if activeTab === 'vacations'}
        {#if showVacationForm}
          <VacationForm
            vacation={editingVacation}
            {persons}
            {holidays}
            onSubmit={handleVacationSubmit}
            onCancel={closeVacationForm}
          />
        {:else}
          <div class="mb-4">
            <button
              onclick={openNewVacationForm}
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
            <VacationList {vacations} onEdit={handleVacationEdit} onDelete={handleVacationDelete} />
          {/if}
        {/if}
      {:else if activeTab === 'holidays'}
        {#if showHolidayForm}
          <HolidayForm
            holiday={editingHoliday}
            onSubmit={handleHolidaySubmit}
            onCancel={closeHolidayForm}
          />
        {:else}
          <div class="mb-4">
            <button
              onclick={openNewHolidayForm}
              class="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 font-medium"
            >
              + Nuevo Feriado
            </button>
          </div>

          {#if loading}
            <div class="bg-white p-8 rounded-lg shadow-md text-center">
              <p class="text-gray-500">Cargando...</p>
            </div>
          {:else}
            <HolidayList
              {holidays}
              onEdit={handleHolidayEdit}
              onDelete={handleHolidayDelete}
              onSync={handleHolidaySync}
              onImport={handleHolidayImport}
              onDeleteByYear={handleHolidayDeleteByYear}
              {isSyncing}
            />
          {/if}
        {/if}
      {:else if activeTab === 'calendar'}
        {#if loading}
          <div class="bg-white p-8 rounded-lg shadow-md text-center">
            <p class="text-gray-500">Cargando...</p>
          </div>
        {:else}
          <VacationCalendar {vacations} {persons} {holidays} />
        {/if}
      {/if}
    </div>
  </div>
</div>
