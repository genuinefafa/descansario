<script lang="ts">
  import MarkdownRenderer from './MarkdownRenderer.svelte';

  interface Props {
    value: string;
    onchange?: (value: string) => void;
    placeholder?: string;
    rows?: number;
    id?: string;
  }

  let { value = $bindable(''), placeholder = '', rows = 4, id }: Props = $props();

  let activeTab = $state<'source' | 'preview'>('source');

  function handleInput(event: Event) {
    const target = event.target as HTMLTextAreaElement;
    value = target.value;
  }
</script>

<div class="markdown-editor">
  <!-- Tabs -->
  <div class="flex border-b border-gray-300">
    <button
      type="button"
      class="px-4 py-2 text-sm font-medium border-b-2 transition-colors {activeTab === 'source'
        ? 'border-blue-500 text-blue-600'
        : 'border-transparent text-gray-600 hover:text-gray-900'}"
      onclick={() => (activeTab = 'source')}
    >
      📝 Source
    </button>
    <button
      type="button"
      class="px-4 py-2 text-sm font-medium border-b-2 transition-colors {activeTab === 'preview'
        ? 'border-blue-500 text-blue-600'
        : 'border-transparent text-gray-600 hover:text-gray-900'}"
      onclick={() => (activeTab = 'preview')}
    >
      👁️ Preview
    </button>
  </div>

  <!-- Content -->
  <div class="border border-gray-300 border-t-0 rounded-b-md">
    {#if activeTab === 'source'}
      <textarea
        {id}
        {value}
        oninput={handleInput}
        {rows}
        {placeholder}
        class="w-full px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 font-mono text-sm rounded-b-md"
      ></textarea>
    {:else}
      <div class="px-3 py-2 min-h-[100px] bg-gray-50 rounded-b-md">
        {#if value.trim()}
          <MarkdownRenderer content={value} />
        {:else}
          <p class="text-gray-400 italic text-sm">Nada que previsualizar</p>
        {/if}
      </div>
    {/if}
  </div>

  <!-- Helper text -->
  <div class="mt-1 text-xs text-gray-500">
    Soporte Markdown: <strong>**negrita**</strong>, <em>*itálica*</em>, [enlaces](url), listas,
    código, etc.
  </div>
</div>

<style>
  textarea {
    resize: vertical;
  }
</style>
