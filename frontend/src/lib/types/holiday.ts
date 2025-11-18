export type Country = 'AR' | 'ES';

export interface Holiday {
	id: number;
	date: string; // ISO date string
	name: string;
	country: Country;
	region?: string;
}

export interface CreateHolidayDto {
	date: string;
	name: string;
	country: Country;
	region?: string;
}

export interface UpdateHolidayDto {
	date: string;
	name: string;
	country: Country;
	region?: string;
}

export interface SyncHolidaysRequest {
	year: number;
	country: Country;
}

export interface SyncHolidaysResponse {
	added: number;
	updated: number;
	total: number;
	holidays: string[];
}

export interface ImportHolidaysRequest {
	jsonContent: string;
	country: Country;
}
