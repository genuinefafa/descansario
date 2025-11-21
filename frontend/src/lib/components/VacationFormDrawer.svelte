<script lang="ts">
  import Drawer from './Drawer.svelte';
  import VacationForm from './VacationForm.svelte';
  import type { Vacation } from '$lib/types/vacation';
  import type { Person } from '$lib/types/person';
  import type { Holiday } from '$lib/types/holiday';

  interface Props {
    isOpen: boolean;
    vacation: Vacation | null;
    persons: Person[];
    holidays: Holiday[];
    onClose: () => void;
    onSubmit: (data: {
      personId: number;
      startDate: string;
      endDate: string;
      status?: 'Pending' | 'Approved' | 'Rejected';
    }) => Promise<void>;
  }

  let { isOpen, vacation, persons, holidays, onClose, onSubmit }: Props = $props();

  const title = $derived(vacation ? 'Editar Vacación' : 'Nueva Vacación');

  async function handleSubmit(data: any) {
    await onSubmit(data);
    onClose();
  }
</script>

<Drawer {isOpen} {onClose} {title}>
  <VacationForm {vacation} {persons} {holidays} onSubmit={handleSubmit} onCancel={onClose} />
</Drawer>
