<script lang="ts">
  import type { StatsOverview } from '$lib/types/stats';

  interface Props {
    person: StatsOverview;
    onclick?: () => void;
  }

  let { person, onclick }: Props = $props();

  function getColorClass(percentage: number): string {
    if (percentage < 50) return 'bg-green-100 border-green-500';
    if (percentage < 80) return 'bg-yellow-100 border-yellow-500';
    return 'bg-red-100 border-red-500';
  }

  function getProgressBarColor(percentage: number): string {
    if (percentage < 50) return 'bg-green-600';
    if (percentage < 80) return 'bg-yellow-600';
    return 'bg-red-600';
  }
</script>

<div
  class="stats-card border-l-4 p-4 rounded-lg shadow hover:shadow-lg cursor-pointer transition {getColorClass(person.usagePercentage)}"
  onclick={onclick}
  role="button"
  tabindex="0"
  onkeydown={(e) => e.key === 'Enter' && onclick?.()}
>
  <h3 class="font-bold text-lg mb-2">{person.personName}</h3>

  <div class="stats-grid text-sm space-y-1">
    <div class="flex justify-between">
      <span class="text-gray-600">Disponible:</span>
      <span class="font-semibold">{person.available} días</span>
    </div>

    <div class="flex justify-between">
      <span class="text-gray-600">Usados:</span>
      <span class="font-semibold text-blue-600">{person.used} días</span>
    </div>

    <div class="flex justify-between">
      <span class="text-gray-600">Pendientes:</span>
      <span class="font-semibold text-yellow-600">{person.pending} días</span>
    </div>

    <div class="flex justify-between">
      <span class="text-gray-600">Restantes:</span>
      <span class="font-semibold text-green-600">{person.remaining} días</span>
    </div>
  </div>

  <!-- Barra de progreso -->
  <div class="mt-3">
    <div class="w-full bg-gray-200 rounded-full h-2">
      <div
        class="h-2 rounded-full transition-all {getProgressBarColor(person.usagePercentage)}"
        style="width: {Math.min(person.usagePercentage, 100)}%"
      ></div>
    </div>
    <p class="text-xs text-gray-500 mt-1 text-right">
      {person.usagePercentage.toFixed(1)}% utilizado
    </p>
  </div>
</div>
