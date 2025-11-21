<script lang="ts">
  interface Props {
    isOpen: boolean;
    onClose: () => void;
    title?: string;
    children: any;
  }

  let { isOpen, onClose, title, children }: Props = $props();

  // Manejar ESC para cerrar
  function handleKeydown(e: KeyboardEvent) {
    if (e.key === 'Escape' && isOpen) {
      onClose();
    }
  }

  // Prevenir scroll del body cuando el drawer está abierto
  $effect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }

    return () => {
      document.body.style.overflow = '';
    };
  });
</script>

<svelte:window onkeydown={handleKeydown} />

{#if isOpen}
  <!-- Backdrop overlay -->
  <!-- svelte-ignore a11y_click_events_have_key_events -->
  <!-- svelte-ignore a11y_no_static_element_interactions -->
  <div
    class="fixed inset-0 bg-black bg-opacity-50 z-40 transition-opacity duration-300"
    class:opacity-0={!isOpen}
    class:opacity-100={isOpen}
    onclick={onClose}
  ></div>

  <!-- Drawer panel -->
  <div
    class="fixed inset-y-0 right-0 z-50 w-full sm:w-11/12 md:w-3/4 lg:w-2/3 xl:w-1/2 max-w-2xl bg-white shadow-2xl transform transition-transform duration-300 ease-in-out overflow-y-auto"
    class:translate-x-full={!isOpen}
    class:translate-x-0={isOpen}
    role="dialog"
    aria-modal="true"
  >
    <!-- Header -->
    <div class="sticky top-0 bg-white border-b border-gray-200 px-6 py-4 flex items-center justify-between z-10">
      {#if title}
        <h2 class="text-xl font-semibold text-gray-900">{title}</h2>
      {:else}
        <div></div>
      {/if}
      <button
        onclick={onClose}
        class="text-gray-400 hover:text-gray-600 transition-colors"
        aria-label="Cerrar"
      >
        <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M6 18L18 6M6 6l12 12"
          />
        </svg>
      </button>
    </div>

    <!-- Content -->
    <div class="px-6 py-6">
      {@render children()}
    </div>
  </div>
{/if}

<style>
  /* Smooth transitions */
  .transition-transform {
    transition-property: transform;
    transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
  }
</style>
