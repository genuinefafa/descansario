<script lang="ts">
	import type { Person } from '$lib/types/person';

	interface Props {
		persons: Person[];
		onEdit: (person: Person) => void;
		onDelete: (id: number) => void;
	}

	let { persons, onEdit, onDelete }: Props = $props();
</script>

<div class="bg-white shadow-md rounded-lg overflow-hidden">
	{#if persons.length === 0}
		<div class="p-8 text-center text-gray-500">
			<p>No hay personas registradas</p>
			<p class="text-sm mt-2">Comienza agregando una nueva persona</p>
		</div>
	{:else}
		<table class="min-w-full divide-y divide-gray-200">
			<thead class="bg-gray-50">
				<tr>
					<th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
						Nombre
					</th>
					<th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
						Email
					</th>
					<th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
						Días disponibles
					</th>
					<th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
						Acciones
					</th>
				</tr>
			</thead>
			<tbody class="bg-white divide-y divide-gray-200">
				{#each persons as person (person.id)}
					<tr class="hover:bg-gray-50">
						<td class="px-6 py-4 whitespace-nowrap">
							<div class="text-sm font-medium text-gray-900">{person.name}</div>
						</td>
						<td class="px-6 py-4 whitespace-nowrap">
							<div class="text-sm text-gray-500">{person.email}</div>
						</td>
						<td class="px-6 py-4 whitespace-nowrap">
							<div class="text-sm text-gray-900">{person.availableDays} días</div>
						</td>
						<td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
							<button
								onclick={() => onEdit(person)}
								class="text-blue-600 hover:text-blue-900 mr-4"
							>
								Editar
							</button>
							<button
								onclick={() => onDelete(person.id)}
								class="text-red-600 hover:text-red-900"
							>
								Eliminar
							</button>
						</td>
					</tr>
				{/each}
			</tbody>
		</table>
	{/if}
</div>
