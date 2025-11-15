<script lang="ts">
	import { onMount } from 'svelte';
	import PersonList from '$lib/components/PersonList.svelte';
	import PersonForm from '$lib/components/PersonForm.svelte';
	import VacationList from '$lib/components/VacationList.svelte';
	import VacationForm from '$lib/components/VacationForm.svelte';
	import { personsService } from '$lib/services/persons';
	import { vacationsService } from '$lib/services/vacations';
	import type { Person } from '$lib/types/person';
	import type { Vacation } from '$lib/types/vacation';

	type Tab = 'persons' | 'vacations';

	let activeTab = $state<Tab>('persons');

	// Persons state
	let persons = $state<Person[]>([]);
	let showPersonForm = $state(false);
	let editingPerson = $state<Person | null>(null);

	// Vacations state
	let vacations = $state<Vacation[]>([]);
	let showVacationForm = $state(false);
	let editingVacation = $state<Vacation | null>(null);

	// Common state
	let loading = $state(true);
	let error = $state('');

	onMount(async () => {
		await Promise.all([loadPersons(), loadVacations()]);
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
	async function handleVacationSubmit(data: { personId: number; startDate: string; endDate: string; status?: string }) {
		try {
			if (editingVacation) {
				await vacationsService.update(editingVacation.id, { ...data, status: data.status || 'Pending' });
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
</script>

<div class="min-h-screen bg-gray-50 py-8">
	<div class="max-w-6xl mx-auto px-4">
		<!-- Header -->
		<div class="mb-8">
			<h1 class="text-4xl font-bold text-gray-900 mb-2">
				🏖️ Descansario
			</h1>
			<p class="text-lg text-gray-600">
				Gestión de vacaciones para tu equipo
			</p>
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
					onclick={() => activeTab = 'persons'}
					class="py-4 px-1 border-b-2 font-medium text-sm {activeTab === 'persons' ? 'border-blue-500 text-blue-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'}"
				>
					Personas
				</button>
				<button
					onclick={() => activeTab = 'vacations'}
					class="py-4 px-1 border-b-2 font-medium text-sm {activeTab === 'vacations' ? 'border-blue-500 text-blue-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'}"
				>
					Vacaciones
				</button>
			</nav>
		</div>

		<!-- Content -->
		<div class="mb-6">
			{#if activeTab === 'persons'}
				{#if showPersonForm}
					<PersonForm person={editingPerson} onSubmit={handlePersonSubmit} onCancel={closePersonForm} />
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
						<PersonList persons={persons} onEdit={handlePersonEdit} onDelete={handlePersonDelete} />
					{/if}
				{/if}
			{:else if activeTab === 'vacations'}
				{#if showVacationForm}
					<VacationForm vacation={editingVacation} persons={persons} onSubmit={handleVacationSubmit} onCancel={closeVacationForm} />
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
						<VacationList vacations={vacations} onEdit={handleVacationEdit} onDelete={handleVacationDelete} />
					{/if}
				{/if}
			{/if}
		</div>
	</div>
</div>
