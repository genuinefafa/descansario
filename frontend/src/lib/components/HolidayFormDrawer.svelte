<script lang="ts">
  import Drawer from './Drawer.svelte';
  import HolidayForm from './HolidayForm.svelte';
  import type { Holiday } from '$lib/types/holiday';

  interface Props {
    isOpen: boolean;
    holiday: Holiday | null;
    onClose: () => void;
    onSubmit: (data: {
      date: string;
      name: string;
      country: 'AR' | 'ES';
      region?: string;
    }) => Promise<void>;
  }

  let { isOpen, holiday, onClose, onSubmit }: Props = $props();

  const title = $derived(holiday ? 'Editar Feriado' : 'Nuevo Feriado');

  async function handleSubmit(data: any) {
    await onSubmit(data);
    onClose();
  }
</script>

<Drawer {isOpen} {onClose} {title}>
  <HolidayForm {holiday} onSubmit={handleSubmit} onCancel={onClose} />
</Drawer>
