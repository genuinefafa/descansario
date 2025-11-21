<script lang="ts">
  import { onMount } from 'svelte';
  import PersonList from '$lib/components/PersonList.svelte';
  import PersonFormDrawer from '$lib/components/PersonFormDrawer.svelte';
  import { personsService } from '$lib/services/persons';
  import type { Person } from '$lib/types/person';

  let persons = $state<Person[]>([]);
  let loading = $state(true);
  let error = $state('');

  // Drawer state
  let isDrawerOpen = $state(false);
  let editingPerson = $state<Person | null>(null);

  onMount(async () => {
    await loadPersons();
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

  async function handleSubmit(data: { name: string; email: string; availableDays: number }) {
    try {
      if (editingPerson) {
        await personsService.update(editingPerson.id, data);
      } else {
        await personsService.create(data);
      }
      await loadPersons();
    } catch (err) {
      error = 'Error al guardar la persona';
      console.error(err);
      throw err;
    }
  }

  function handleEdit(person: Person) {
    editingPerson = person;
    isDrawerOpen = true;
  }

  async function handleDelete(id: number) {
    if (!confirm('¿Estás seguro de eliminar esta persona?')) return;

    try {
      await personsService.delete(id);
      await loadPersons();
    } catch (err) {
      error = 'Error al eliminar la persona';
      console.error(err);
    }
  }

  function openNewForm() {
    editingPerson = null;
    isDrawerOpen = true;
  }

  function closeDrawer() {
    isDrawerOpen = false;
    editingPerson = null;
  }
</script>

<svelte:head>
  <title>Personas - Descansario</title>
</svelte:head>

<div class="container mx-auto px-4 py-8">
  <div class="mb-6">
    <h1 class="text-3xl font-bold text-gray-900 mb-2">Personas</h1>
    <p class="text-gray-600">Gestiona las personas del equipo</p>
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
    >
      + Nueva Persona
    </button>
  </div>

  {#if loading}
    <div class="bg-white p-8 rounded-lg shadow-md text-center">
      <p class="text-gray-500">Cargando...</p>
    </div>
  {:else}
    <PersonList {persons} onEdit={handleEdit} onDelete={handleDelete} />
  {/if}
</div>

<!-- Drawer para editar/crear persona -->
<PersonFormDrawer
  isOpen={isDrawerOpen}
  person={editingPerson}
  onClose={closeDrawer}
  onSubmit={handleSubmit}
/>
