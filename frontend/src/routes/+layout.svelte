<script lang="ts">
  import '../app.css';
  import { onMount } from 'svelte';
  import { authStore } from '$lib/stores/authStore';

  let { children } = $props();

  // Inicializar auth store cuando la app carga
  onMount(() => {
    authStore.init();
  });
</script>

{@render children()}
