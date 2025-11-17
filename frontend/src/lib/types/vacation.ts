export interface Vacation {
  id: number;
  personId: number;
  personName: string;
  startDate: string; // ISO date string
  endDate: string; // ISO date string
  workingDaysCount: number;
  status: 'Pending' | 'Approved' | 'Rejected';
}

export interface CreateVacationDto {
  personId: number;
  startDate: string; // ISO date string
  endDate: string; // ISO date string
  status?: 'Pending' | 'Approved' | 'Rejected';
}

export interface UpdateVacationDto {
  personId: number;
  startDate: string; // ISO date string
  endDate: string; // ISO date string
  status: 'Pending' | 'Approved' | 'Rejected';
}
