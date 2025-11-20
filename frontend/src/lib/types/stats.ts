export interface VacationInYear {
  id: number;
  startDate: string;
  endDate: string;
  workingDaysCount: number; // Total de días de la vacación
  workingDaysInYear: number; // Días que caen en el año analizado
  effectiveStartInYear: string; // Fecha inicio efectiva en el año
  effectiveEndInYear: string; // Fecha fin efectiva en el año
  status: string;
  notes: string | null;
  spansMultipleYears: boolean; // Si cruza múltiples años
}

export interface PersonStats {
  personId: number;
  personName: string;
  year: number;
  available: number;
  approved: number;
  pending: number;
  rejected: number;
  remaining: number;
  vacationsInYear: VacationInYear[];
}

export interface StatsOverview {
  personId: number;
  personName: string;
  available: number;
  used: number;
  pending: number;
  remaining: number;
  usagePercentage: number;
}
