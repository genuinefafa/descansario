<script lang="ts">
  import { onMount } from 'svelte';
  import HolidayList from '$lib/components/HolidayList.svelte';
  import HolidayForm from '$lib/components/HolidayForm.svelte';
  import { holidaysService } from '$lib/services/holidays';
  import type { Holiday } from '$lib/types/holiday';

  let holidays = $state<Holiday[]>([]);
  let showForm = $state(false);
  let editingHoliday = $state<Holiday | null>(null);
  let isSyncing = $state(false);
  let loading = $state(true);
  let error = $state('');

  onMount(async () => {
    await loadHolidays();
  });

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

  async function handleSubmit(data: {
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
      closeForm();
    } catch (err) {
      error = 'Error al guardar el feriado';
      console.error(err);
    }
  }

  function handleEdit(holiday: Holiday) {
    editingHoliday = holiday;
    showForm = true;
  }

  async function handleDelete(id: number) {
    if (!confirm('¿Estás seguro de eliminar este feriado?')) return;

    try {
      await holidaysService.delete(id);
      await loadHolidays();
    } catch (err) {
      error = 'Error al eliminar el feriado';
      console.error(err);
    }
  }

  async function handleSync(year: number) {
    try {
      isSyncing = true;
      error = '';
      const response = await holidaysService.sync({ year, country: 'AR' });
      await loadHolidays();

      const message = `Sincronización completada: ${response.added} agregados, ${response.updated} actualizados (${response.total} total)`;
      alert(message);
    } catch (err) {
      error = 'Error al sincronizar feriados';
      console.error(err);
    } finally {
      isSyncing = false;
    }
  }

  async function handleImport(jsonContent: string) {
    try {
      isSyncing = true;
      error = '';
      const response = await holidaysService.import({ jsonContent, country: 'AR' });
      await loadHolidays();

      const message = `Importación completada: ${response.added} agregados, ${response.updated} actualizados (${response.total} total)`;
      alert(message);
    } catch (err) {
      error = 'Error al importar feriados';
      console.error(err);
      throw err;
    } finally {
      isSyncing = false;
    }
  }

  async function handleDeleteByYear(year: number) {
    try {
      isSyncing = true;
      error = '';
      const response = await holidaysService.deleteByYear(year);
      await loadHolidays();

      alert(`${response.message}`);
    } catch (err) {
      error = `Error al eliminar feriados del año ${year}`;
      console.error(err);
    } finally {
      isSyncing = false;
    }
  }

  function openNewForm() {
    editingHoliday = null;
    showForm = true;
  }

  function closeForm() {
    showForm = false;
    editingHoliday = null;
  }
</script>

<svelte:head>
  <title>Feriados - Descansario</title>
</svelte:head>

<div class="container mx-auto px-4 py-8">
  <div class="mb-6">
    <h1 class="text-3xl font-bold text-gray-900 mb-2">Feriados</h1>
    <p class="text-gray-600">Gestiona los feriados del calendario</p>
  </div>

  {#if error}
    <div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-6">
      {error}
    </div>
  {/if}

  {#if showForm}
    <HolidayForm holiday={editingHoliday} onSubmit={handleSubmit} onCancel={closeForm} />
  {:else}
    <div class="mb-4">
      <button
        onclick={openNewForm}
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
        onEdit={handleEdit}
        onDelete={handleDelete}
        onSync={handleSync}
        onImport={handleImport}
        onDeleteByYear={handleDeleteByYear}
        {isSyncing}
      />
    {/if}
  {/if}
</div>
