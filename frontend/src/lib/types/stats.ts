export interface UpcomingVacation {
  id: number;
  startDate: string;
  endDate: string;
  workingDaysCount: number;
  status: string;
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
  upcomingVacations: UpcomingVacation[];
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
