import { api } from './api';
import type { Vacation, CreateVacationDto, UpdateVacationDto } from '$lib/types/vacation';

export const vacationsService = {
  async getAll(): Promise<Vacation[]> {
    return api.get<Vacation[]>('/api/vacations');
  },

  async getByPerson(personId: number): Promise<Vacation[]> {
    return api.get<Vacation[]>(`/api/vacations/person/${personId}`);
  },

  async getById(id: number): Promise<Vacation> {
    return api.get<Vacation>(`/api/vacations/${id}`);
  },

  async create(data: CreateVacationDto): Promise<Vacation> {
    return api.post<Vacation>('/api/vacations', data);
  },

  async update(id: number, data: UpdateVacationDto): Promise<Vacation> {
    return api.put<Vacation>(`/api/vacations/${id}`, data);
  },

  async delete(id: number): Promise<void> {
    return api.delete<void>(`/api/vacations/${id}`);
  },

  async getOverlapping(startDate: string, endDate: string): Promise<Vacation[]> {
    return api.get<Vacation[]>(`/api/vacations/overlap?startDate=${startDate}&endDate=${endDate}`);
  },

  async calculateWorkingDays(startDate: string, endDate: string): Promise<number> {
    const response = await api.get<{ workingDays: number }>(
      `/api/vacations/working-days?startDate=${startDate}&endDate=${endDate}`
    );
    return response.workingDays;
  },
};
