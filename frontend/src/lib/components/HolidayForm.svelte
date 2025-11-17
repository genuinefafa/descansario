<script lang="ts">
	import type { Holiday, Country } from '$lib/types/holiday';
	import { format } from 'date-fns';

	interface Props {
		holiday?: Holiday | null;
		onSubmit: (data: { date: string; name: string; country: Country; region?: string }) => Promise<void>;
		onCancel: () => void;
	}

	let { holiday = null, onSubmit, onCancel }: Props = $props();

	let date = $state(holiday?.date.split('T')[0] || format(new Date(), 'yyyy-MM-dd'));
	let name = $state(holiday?.name || '');
	let country = $state<Country>(holiday?.country || 'AR');
	let region = $state(holiday?.region || '');
	let isSubmitting = $state(false);
	let error = $state('');

	async function handleSubmit(event: Event) {
		event.preventDefault();
		error = '';

		if (!date || !name.trim()) {
			error = 'Fecha y nombre son requeridos';
			return;
		}

		try {
			isSubmitting = true;
			await onSubmit({
				date,
				name,
				country,
				region: region.trim() || undefined
			});
		} catch (err) {
			error = err instanceof Error ? err.message : 'Error al guardar el feriado';
		} finally {
			isSubmitting = false;
		}
	}

	$effect(() => {
		if (holiday) {
			date = holiday.date.split('T')[0];
			name = holiday.name;
			country = holiday.country;
			region = holiday.region || '';
		} else {
			date = format(new Date(), 'yyyy-MM-dd');
			name = '';
			country = 'AR';
			region = '';
		}
	});
</script>

<form onsubmit={handleSubmit} class="space-y-4 bg-gray-50 p-4 rounded-lg border border-gray-200">
	<h3 class="text-lg font-semibold">{holiday ? 'Editar Feriado' : 'Nuevo Feriado'}</h3>

	{#if error}
		<div class="bg-red-50 text-red-700 p-3 rounded border border-red-200">
			{error}
		</div>
	{/if}

	<div class="grid grid-cols-1 md:grid-cols-2 gap-4">
		<div>
			<label for="date" class="block text-sm font-medium text-gray-700 mb-1">
				Fecha <span class="text-red-500">*</span>
			</label>
			<input
				id="date"
				type="date"
				bind:value={date}
				required
				autofocus
				class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
			/>
		</div>

		<div>
			<label for="name" class="block text-sm font-medium text-gray-700 mb-1">
				Nombre <span class="text-red-500">*</span>
			</label>
			<input
				id="name"
				type="text"
				bind:value={name}
				placeholder="Ej: Año Nuevo"
				required
				class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
			/>
		</div>

		<div>
			<label for="country" class="block text-sm font-medium text-gray-700 mb-1">
				País <span class="text-red-500">*</span>
			</label>
			<select
				id="country"
				bind:value={country}
				class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
			>
				<option value="AR">Argentina</option>
				<option value="ES">España</option>
			</select>
		</div>

		<div>
			<label for="region" class="block text-sm font-medium text-gray-700 mb-1">
				Región (opcional)
			</label>
			<input
				id="region"
				type="text"
				bind:value={region}
				placeholder="Ej: Buenos Aires, Madrid"
				class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
			/>
		</div>
	</div>

	<div class="flex gap-2 justify-end">
		<button
			type="button"
			onclick={onCancel}
			disabled={isSubmitting}
			class="px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50"
		>
			Cancelar
		</button>
		<button
			type="submit"
			disabled={isSubmitting}
			class="px-4 py-2 text-white bg-blue-600 rounded-md hover:bg-blue-700 disabled:opacity-50"
		>
			{isSubmitting ? 'Guardando...' : 'Guardar'}
		</button>
	</div>
</form>
