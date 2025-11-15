<script lang="ts">
	import type { Vacation } from '$lib/types/vacation';
	import type { Person } from '$lib/types/person';

	interface Props {
		vacation?: Vacation | null;
		persons: Person[];
		onSubmit: (data: { personId: number; startDate: string; endDate: string; status?: string }) => Promise<void>;
		onCancel: () => void;
	}

	let { vacation = null, persons, onSubmit, onCancel }: Props = $props();

	let personId = $state(vacation?.personId || (persons[0]?.id ?? 0));
	let startDate = $state(vacation?.startDate.split('T')[0] || '');
	let endDate = $state(vacation?.endDate.split('T')[0] || '');
	let status = $state<'Pending' | 'Approved' | 'Rejected'>(vacation?.status || 'Pending');
	let isSubmitting = $state(false);
	let error = $state('');

	async function handleSubmit(event: Event) {
		event.preventDefault();
		error = '';

		if (!personId || !startDate || !endDate) {
			error = 'Todos los campos son requeridos';
			return;
		}

		if (new Date(startDate) > new Date(endDate)) {
			error = 'La fecha de inicio debe ser anterior o igual a la fecha de fin';
			return;
		}

		try {
			isSubmitting = true;
			const submitData: any = {
				personId,
				startDate: new Date(startDate).toISOString(),
				endDate: new Date(endDate).toISOString()
			};

			// Solo incluir status al editar
			if (vacation) {
				submitData.status = status;
			}

			await onSubmit(submitData);
		} catch (err) {
			error = err instanceof Error ? err.message : 'Error al guardar la vacación';
		} finally {
			isSubmitting = false;
		}
	}

	$effect(() => {
		if (vacation) {
			personId = vacation.personId;
			startDate = vacation.startDate.split('T')[0];
			endDate = vacation.endDate.split('T')[0];
			status = vacation.status;
		} else {
			personId = persons[0]?.id ?? 0;
			startDate = '';
			endDate = '';
			status = 'Pending';
		}
	});
</script>

<div class="bg-white p-6 rounded-lg shadow-md">
	<h2 class="text-xl font-bold text-gray-900 mb-4">
		{vacation ? 'Editar Vacación' : 'Nueva Vacación'}
	</h2>

	<form onsubmit={handleSubmit} class="space-y-4">
		{#if error}
			<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
				{error}
			</div>
		{/if}

		<div>
			<label for="personId" class="block text-sm font-medium text-gray-700 mb-1">
				Persona
			</label>
			<select
				id="personId"
				bind:value={personId}
				required
				class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
			>
				{#each persons as person (person.id)}
					<option value={person.id}>{person.name}</option>
				{/each}
			</select>
		</div>

		<div>
			<label for="startDate" class="block text-sm font-medium text-gray-700 mb-1">
				Fecha de Inicio
			</label>
			<input
				id="startDate"
				type="date"
				bind:value={startDate}
				required
				class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
			/>
		</div>

		<div>
			<label for="endDate" class="block text-sm font-medium text-gray-700 mb-1">
				Fecha de Fin
			</label>
			<input
				id="endDate"
				type="date"
				bind:value={endDate}
				required
				class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
			/>
		</div>

		{#if vacation}
			<div>
				<label for="status" class="block text-sm font-medium text-gray-700 mb-1">
					Estado
				</label>
				<select
					id="status"
					bind:value={status}
					class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
				>
					<option value="Pending">Pendiente</option>
					<option value="Approved">Aprobado</option>
					<option value="Rejected">Rechazado</option>
				</select>
			</div>
		{/if}

		<div class="flex gap-3 pt-4">
			<button
				type="submit"
				disabled={isSubmitting}
				class="flex-1 bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed"
			>
				{isSubmitting ? 'Guardando...' : vacation ? 'Actualizar' : 'Crear'}
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
