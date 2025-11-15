import { api } from './api';
import type { Person, CreatePersonDto, UpdatePersonDto } from '$lib/types/person';

export const personsService = {
	async getAll(): Promise<Person[]> {
		return api.get<Person[]>('/api/persons');
	},

	async getById(id: number): Promise<Person> {
		return api.get<Person>(`/api/persons/${id}`);
	},

	async create(data: CreatePersonDto): Promise<Person> {
		return api.post<Person>('/api/persons', data);
	},

	async update(id: number, data: UpdatePersonDto): Promise<Person> {
		return api.put<Person>(`/api/persons/${id}`, data);
	},

	async delete(id: number): Promise<void> {
		return api.delete<void>(`/api/persons/${id}`);
	}
};
