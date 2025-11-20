import { api } from './api';
import type { CalendarSummary } from '$lib/types/calendar';

export const calendarService = {
  async getSummary(startDate: string, endDate: string): Promise<CalendarSummary[]> {
    return api.get<CalendarSummary[]>(
      `/api/calendar/summary?startDate=${startDate}&endDate=${endDate}`
    );
  },
};
