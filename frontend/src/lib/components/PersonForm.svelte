<script lang="ts">
	import type { Person } from '$lib/types/person';

	interface Props {
		person?: Person | null;
		onSubmit: (data: { name: string; email: string; availableDays: number }) => Promise<void>;
		onCancel: () => void;
	}

	let { person = null, onSubmit, onCancel }: Props = $props();

	let name = $state(person?.name || '');
	let email = $state(person?.email || '');
	let availableDays = $state(person?.availableDays || 20);
	let isSubmitting = $state(false);
	let error = $state('');

	async function handleSubmit(event: Event) {
		event.preventDefault();
		error = '';

		if (!name.trim() || !email.trim()) {
			error = 'Nombre y email son requeridos';
			return;
		}

		if (availableDays < 0) {
			error = 'Los días disponibles no pueden ser negativos';
			return;
		}

		try {
			isSubmitting = true;
			await onSubmit({ name, email, availableDays });
		} catch (err) {
			error = err instanceof Error ? err.message : 'Error al guardar la persona';
		} finally {
			isSubmitting = false;
		}
	}

	$effect(() => {
		if (person) {
			name = person.name;
			email = person.email;
			availableDays = person.availableDays;
		} else {
			name = '';
			email = '';
			availableDays = 20;
		}
	});
</script>

<div class="bg-white p-6 rounded-lg shadow-md">
	<h2 class="text-xl font-bold text-gray-900 mb-4">
		{person ? 'Editar Persona' : 'Nueva Persona'}
	</h2>

	<form onsubmit={handleSubmit} class="space-y-4">
		{#if error}
			<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
				{error}
			</div>
		{/if}

		<div>
			<label for="name" class="block text-sm font-medium text-gray-700 mb-1">
				Nombre
			</label>
			<input
				id="name"
				type="text"
				bind:value={name}
				required
				class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
				placeholder="Ej: Juan Pérez"
			/>
		</div>

		<div>
			<label for="email" class="block text-sm font-medium text-gray-700 mb-1">
				Email
			</label>
			<input
				id="email"
				type="email"
				bind:value={email}
				required
				class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
				placeholder="Ej: juan@example.com"
			/>
		</div>

		<div>
			<label for="availableDays" class="block text-sm font-medium text-gray-700 mb-1">
				Días disponibles
			</label>
			<input
				id="availableDays"
				type="number"
				bind:value={availableDays}
				min="0"
				required
				class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
			/>
		</div>

		<div class="flex gap-3 pt-4">
			<button
				type="submit"
				disabled={isSubmitting}
				class="flex-1 bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed"
			>
				{isSubmitting ? 'Guardando...' : person ? 'Actualizar' : 'Crear'}
			</button>
			<button
				type="button"
				onclick={onCancel}
				disabled={isSubmitting}
				class="flex-1 bg-gray-200 text-gray-700 py-2 px-4 rounded-md hover:bg-gray-300 disabled:bg-gray-100 disabled:cursor-not-allowed"
			>
				Cancelar
			</button>
		</div>
	</form>
</div>
