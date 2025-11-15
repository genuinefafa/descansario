export interface Person {
	id: number;
	name: string;
	email: string;
	availableDays: number;
}

export interface CreatePersonDto {
	name: string;
	email: string;
	availableDays?: number;
}

export interface UpdatePersonDto {
	name: string;
	email: string;
	availableDays: number;
}
