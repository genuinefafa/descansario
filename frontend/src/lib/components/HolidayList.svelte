<script lang="ts">
	import type { Holiday } from '$lib/types/holiday';
	import { format, parseISO } from 'date-fns';
	import { es } from 'date-fns/locale';

	interface Props {
		holidays: Holiday[];
		onEdit: (holiday: Holiday) => void;
		onDelete: (id: number) => void;
		onSync?: (year: number) => Promise<void>;
		isSyncing?: boolean;
	}

	let { holidays, onEdit, onDelete, onSync, isSyncing = false }: Props = $props();

	let selectedYear = $state(new Date().getFullYear());
	let showSyncDialog = $state(false);

	function formatDate(dateStr: string): string {
		try {
			return format(parseISO(dateStr), 'dd/MM/yyyy - EEEE', { locale: es });
		} catch {
			return dateStr;
		}
	}

	function getCountryName(country: string): string {
		return country === 'AR' ? 'Argentina' : 'España';
	}

	async function handleSync() {
		if (onSync) {
			await onSync(selectedYear);
			showSyncDialog = false;
		}
	}

	// Agrupar feriados por año
	let holidaysByYear = $derived(() => {
		const grouped = new Map<number, Holiday[]>();
		holidays.forEach((holiday) => {
			const year = parseISO(holiday.date).getFullYear();
			if (!grouped.has(year)) {
				grouped.set(year, []);
			}
			grouped.get(year)!.push(holiday);
		});
		// Ordenar por año descendente
		return new Map([...grouped.entries()].sort((a, b) => b[0] - a[0]));
	});
</script>

<div class="space-y-4">
	<!-- Botón de sincronización -->
	{#if onSync}
		<div class="flex justify-between items-center">
			<h2 class="text-xl font-semibold">Feriados</h2>
			<button
				onclick={() => (showSyncDialog = true)}
				disabled={isSyncing}
				class="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50 flex items-center gap-2"
			>
				{#if isSyncing}
					<span class="animate-spin">⟳</span>
					Sincronizando...
				{:else}
					🔄 Sincronizar Feriados
				{/if}
			</button>
		</div>
	{/if}

	<!-- Dialog de sincronización -->
	{#if showSyncDialog}
		<!-- svelte-ignore a11y_click_events_have_key_events -->
		<!-- svelte-ignore a11y_no_static_element_interactions -->
		<div
			class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50"
			onclick={(e) => e.target === e.currentTarget && (showSyncDialog = false)}
		>
			<div class="bg-white rounded-lg p-6 max-w-md w-full mx-4">
				<h3 class="text-lg font-semibold mb-4">Sincronizar Feriados</h3>
				<p class="text-sm text-gray-600 mb-4">
					Esto descargará los feriados oficiales de Argentina desde la API pública.
				</p>

				<div class="mb-4">
					<label for="sync-year" class="block text-sm font-medium text-gray-700 mb-1">
						Año a sincronizar
					</label>
					<input
						id="sync-year"
						type="number"
						bind:value={selectedYear}
						min="2000"
						max="2100"
						class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
					/>
				</div>

				<div class="flex gap-2 justify-end">
					<button
						onclick={() => (showSyncDialog = false)}
						disabled={isSyncing}
						class="px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50"
					>
						Cancelar
					</button>
					<button
						onclick={handleSync}
						disabled={isSyncing}
						class="px-4 py-2 text-white bg-green-600 rounded-md hover:bg-green-700 disabled:opacity-50"
					>
						{isSyncing ? 'Sincronizando...' : 'Sincronizar'}
					</button>
				</div>
			</div>
		</div>
	{/if}

	<!-- Lista de feriados -->
	<div class="bg-white shadow-md rounded-lg overflow-hidden">
		{#if holidays.length === 0}
			<div class="p-8 text-center text-gray-500">
				<p>No hay feriados registrados</p>
				<p class="text-sm mt-2">
					{onSync ? 'Usa el botón "Sincronizar" o agrega uno manualmente' : 'Comienza agregando un nuevo feriado'}
				</p>
			</div>
		{:else}
			{#each [...holidaysByYear()] as [year, yearHolidays] (year)}
				<div class="border-b border-gray-200 last:border-b-0">
					<div class="bg-gray-100 px-6 py-3">
						<h3 class="text-lg font-semibold text-gray-800">
							{year} <span class="text-sm text-gray-600">({yearHolidays.length} feriados)</span>
						</h3>
					</div>
					<table class="min-w-full divide-y divide-gray-200">
						<thead class="bg-gray-50">
							<tr>
								<th
									class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
								>
									Fecha
								</th>
								<th
									class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
								>
									Nombre
								</th>
								<th
									class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
								>
									País
								</th>
								<th
									class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
								>
									Región
								</th>
								<th
									class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider"
								>
									Acciones
								</th>
							</tr>
						</thead>
						<tbody class="bg-white divide-y divide-gray-200">
							{#each yearHolidays as holiday (holiday.id)}
								<tr class="hover:bg-gray-50">
									<td class="px-6 py-4 whitespace-nowrap">
										<div class="text-sm font-medium text-gray-900">{formatDate(holiday.date)}</div>
									</td>
									<td class="px-6 py-4">
										<div class="text-sm text-gray-900">{holiday.name}</div>
									</td>
									<td class="px-6 py-4 whitespace-nowrap">
										<div class="text-sm text-gray-500">{getCountryName(holiday.country)}</div>
									</td>
									<td class="px-6 py-4 whitespace-nowrap">
										<div class="text-sm text-gray-500">{holiday.region || '-'}</div>
									</td>
									<td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
										<button
											onclick={() => onEdit(holiday)}
											class="text-blue-600 hover:text-blue-900 mr-4"
										>
											Editar
										</button>
										<button
											onclick={() => onDelete(holiday.id)}
											class="text-red-600 hover:text-red-900"
										>
											Eliminar
										</button>
									</td>
								</tr>
							{/each}
						</tbody>
					</table>
				</div>
			{/each}
		{/if}
	</div>
</div>
