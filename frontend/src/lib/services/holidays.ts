import { api } from './api';
import type {
  Holiday,
  CreateHolidayDto,
  UpdateHolidayDto,
  SyncHolidaysRequest,
  SyncHolidaysResponse,
  ImportHolidaysRequest,
  Country,
} from '$lib/types/holiday';

export const holidaysService = {
  // Listar todos los feriados
  async getAll(): Promise<Holiday[]> {
    return api.get<Holiday[]>('/api/holidays');
  },

  // Listar feriados de un año específico
  async getByYear(year: number, country?: Country): Promise<Holiday[]> {
    const params = country ? `?country=${country}` : '';
    return api.get<Holiday[]>(`/api/holidays/year/${year}${params}`);
  },

  // Obtener un feriado por ID
  async getById(id: number): Promise<Holiday> {
    return api.get<Holiday>(`/api/holidays/${id}`);
  },

  // Crear un nuevo feriado
  async create(holiday: CreateHolidayDto): Promise<Holiday> {
    return api.post<Holiday>('/api/holidays', holiday);
  },

  // Actualizar un feriado
  async update(id: number, holiday: UpdateHolidayDto): Promise<Holiday> {
    return api.put<Holiday>(`/api/holidays/${id}`, holiday);
  },

  // Eliminar un feriado
  async delete(id: number): Promise<void> {
    return api.delete(`/api/holidays/${id}`);
  },

  // Sincronizar feriados desde API externa
  async sync(request: SyncHolidaysRequest): Promise<SyncHolidaysResponse> {
    return api.post<SyncHolidaysResponse>('/api/holidays/sync', request);
  },

  // Importar feriados desde JSON
  async import(request: ImportHolidaysRequest): Promise<SyncHolidaysResponse> {
    return api.post<SyncHolidaysResponse>('/api/holidays/import', request);
  },

  // Eliminar feriados de un año específico (solo desarrollo)
  async deleteByYear(year: number): Promise<{ message: string; deletedCount: number }> {
    return api.delete(`/api/holidays/year/${year}`);
  },
};
