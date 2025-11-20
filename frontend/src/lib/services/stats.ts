import { api } from './api';
import type { PersonStats, StatsOverview } from '$lib/types/stats';

export const statsService = {
  async getPersonStats(personId: number, year: number): Promise<PersonStats> {
    return api.get<PersonStats>(`/api/persons/${personId}/stats?year=${year}`);
  },

  async getOverview(year: number): Promise<StatsOverview[]> {
    return api.get<StatsOverview[]>(`/api/stats/overview?year=${year}`);
  },
};
