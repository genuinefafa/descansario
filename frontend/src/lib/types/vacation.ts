export type VacationStatus = 'Pending' | 'Approved' | 'Rejected';

export interface Vacation {
  id: number;
  personId: number;
  personName: string;
  startDate: string; // ISO date string
  endDate: string; // ISO date string
  workingDaysCount: number;
  status: VacationStatus;
}

export interface VacationFormData {
  personId: number;
  startDate: string; // ISO date string
  endDate: string; // ISO date string
  status?: VacationStatus;
}

export type CreateVacationDto = VacationFormData;
export type UpdateVacationDto = VacationFormData;
