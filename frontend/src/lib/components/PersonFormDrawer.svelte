<script lang="ts">
  import Drawer from './Drawer.svelte';
  import PersonForm from './PersonForm.svelte';
  import type { Person } from '$lib/types/person';

  interface Props {
    isOpen: boolean;
    person: Person | null;
    onClose: () => void;
    onSubmit: (data: { name: string; email: string; availableDays: number }) => Promise<void>;
  }

  let { isOpen, person, onClose, onSubmit }: Props = $props();

  const title = $derived(person ? 'Editar Persona' : 'Nueva Persona');

  async function handleSubmit(data: any) {
    await onSubmit(data);
    onClose();
  }
</script>

<Drawer {isOpen} {onClose} {title}>
  <PersonForm {person} onSubmit={handleSubmit} onCancel={onClose} />
</Drawer>
