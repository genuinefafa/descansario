export interface Person {
  id: number;
  name: string;
  email: string;
  availableDays: number;
}

export interface PersonFormData {
  name: string;
  email: string;
  availableDays: number;
}

export type CreatePersonDto = PersonFormData;
export type UpdatePersonDto = PersonFormData;
