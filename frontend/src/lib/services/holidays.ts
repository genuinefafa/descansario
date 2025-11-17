import { apiClient } from './api';
import type {
	Holiday,
	CreateHolidayDto,
	UpdateHolidayDto,
	SyncHolidaysRequest,
	SyncHolidaysResponse,
	Country
} from '$lib/types/holiday';

export const holidaysService = {
	// Listar todos los feriados
	async getAll(): Promise<Holiday[]> {
		return apiClient.get<Holiday[]>('/holidays');
	},

	// Listar feriados de un año específico
	async getByYear(year: number, country?: Country): Promise<Holiday[]> {
		const params = country ? `?country=${country}` : '';
		return apiClient.get<Holiday[]>(`/holidays/year/${year}${params}`);
	},

	// Obtener un feriado por ID
	async getById(id: number): Promise<Holiday> {
		return apiClient.get<Holiday>(`/holidays/${id}`);
	},

	// Crear un nuevo feriado
	async create(holiday: CreateHolidayDto): Promise<Holiday> {
		return apiClient.post<Holiday>('/holidays', holiday);
	},

	// Actualizar un feriado
	async update(id: number, holiday: UpdateHolidayDto): Promise<Holiday> {
		return apiClient.put<Holiday>(`/holidays/${id}`, holiday);
	},

	// Eliminar un feriado
	async delete(id: number): Promise<void> {
		return apiClient.delete(`/holidays/${id}`);
	},

	// Sincronizar feriados desde API externa
	async sync(request: SyncHolidaysRequest): Promise<SyncHolidaysResponse> {
		return apiClient.post<SyncHolidaysResponse>('/holidays/sync', request);
	}
};
