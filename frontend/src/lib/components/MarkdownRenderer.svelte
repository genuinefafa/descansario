<script lang="ts">
  import { marked } from 'marked';
  import DOMPurify from 'isomorphic-dompurify';

  interface Props {
    content: string;
    class?: string;
  }

  let { content, class: className = '' }: Props = $props();

  // Configurar marked para mejor seguridad y renderizado
  marked.setOptions({
    breaks: true, // Convertir \n en <br>
    gfm: true, // GitHub Flavored Markdown
  });

  // Renderizar markdown de forma segura
  const html = $derived(() => {
    if (!content) return '';
    const rawHtml = marked.parse(content) as string;
    return DOMPurify.sanitize(rawHtml);
  });
</script>

<div class="markdown-content {className}">
  <!-- eslint-disable-next-line svelte/no-at-html-tags -->
  {@html html()}
</div>

<style>
  .markdown-content :global(p) {
    margin-bottom: 0.5em;
  }

  .markdown-content :global(p:last-child) {
    margin-bottom: 0;
  }

  .markdown-content :global(strong) {
    font-weight: 600;
  }

  .markdown-content :global(em) {
    font-style: italic;
  }

  .markdown-content :global(a) {
    color: rgb(37, 99, 235); /* blue-600 */
    text-decoration: underline;
  }

  .markdown-content :global(a:hover) {
    color: rgb(29, 78, 216); /* blue-700 */
  }

  .markdown-content :global(ul),
  .markdown-content :global(ol) {
    margin-left: 1.5em;
    margin-bottom: 0.5em;
  }

  .markdown-content :global(li) {
    margin-bottom: 0.25em;
  }

  .markdown-content :global(h1),
  .markdown-content :global(h2),
  .markdown-content :global(h3),
  .markdown-content :global(h4),
  .markdown-content :global(h5),
  .markdown-content :global(h6) {
    font-weight: 600;
    margin-top: 0.75em;
    margin-bottom: 0.5em;
  }

  .markdown-content :global(h1) {
    font-size: 1.5em;
  }
  .markdown-content :global(h2) {
    font-size: 1.3em;
  }
  .markdown-content :global(h3) {
    font-size: 1.1em;
  }

  .markdown-content :global(code) {
    background-color: rgb(243, 244, 246); /* gray-100 */
    padding: 0.125rem 0.25rem;
    border-radius: 0.25rem;
    font-family: ui-monospace, monospace;
    font-size: 0.875em;
  }

  .markdown-content :global(pre) {
    background-color: rgb(243, 244, 246); /* gray-100 */
    padding: 0.75rem;
    border-radius: 0.375rem;
    overflow-x: auto;
    margin-bottom: 0.5em;
  }

  .markdown-content :global(pre code) {
    background-color: transparent;
    padding: 0;
  }

  .markdown-content :global(blockquote) {
    border-left: 4px solid rgb(229, 231, 235); /* gray-200 */
    padding-left: 1rem;
    margin-left: 0;
    margin-bottom: 0.5em;
    color: rgb(107, 114, 128); /* gray-500 */
  }
</style>
