<script lang="ts">
	import { onMount } from 'svelte';
	import PersonList from '$lib/components/PersonList.svelte';
	import PersonForm from '$lib/components/PersonForm.svelte';
	import { personsService } from '$lib/services/persons';
	import type { Person } from '$lib/types/person';

	let persons = $state<Person[]>([]);
	let showForm = $state(false);
	let editingPerson = $state<Person | null>(null);
	let loading = $state(true);
	let error = $state('');

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
			closeForm();
		} catch (err) {
			error = 'Error al guardar la persona';
			console.error(err);
		}
	}

	function handleEdit(person: Person) {
		editingPerson = person;
		showForm = true;
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
		showForm = true;
	}

	function closeForm() {
		showForm = false;
		editingPerson = null;
	}
</script>

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

		<!-- Form o Lista -->
		<div class="mb-6">
			{#if showForm}
				<PersonForm person={editingPerson} onSubmit={handleSubmit} onCancel={closeForm} />
			{:else}
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
			{/if}
		</div>
	</div>
</div>
