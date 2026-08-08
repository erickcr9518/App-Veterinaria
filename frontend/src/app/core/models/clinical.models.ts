export interface Owner {
  id: string;
  fullName: string;
  identificationNumber?: string | null;
  phone: string;
  email?: string | null;
  address?: string | null;
  alternateContact?: string | null;
  consentNotes?: string | null;
  patientCount: number;
}

export interface CreateOwnerRequest {
  fullName: string;
  identificationNumber?: string | null;
  phone: string;
  email?: string | null;
  address?: string | null;
  alternateContact?: string | null;
  consentNotes?: string | null;
}

export interface Patient {
  id: string;
  ownerId: string;
  ownerName: string;
  name: string;
  species: string;
  breed?: string | null;
  birthDate?: string | null;
  estimatedAge?: string | null;
  sex: string;
  reproductiveStatus?: string | null;
  color?: string | null;
  currentWeightKg?: number | null;
  microchipNumber?: string | null;
  photoUrl?: string | null;
  allergies?: string | null;
  chronicDiseases?: string | null;
  currentMedications?: string | null;
  vaccinationStatus?: string | null;
  dewormingStatus?: string | null;
  status: string;
}

export interface CreatePatientRequest {
  ownerId: string;
  name: string;
  species: string;
  breed?: string | null;
  birthDate?: string | null;
  estimatedAge?: string | null;
  sex: string;
  reproductiveStatus?: string | null;
  color?: string | null;
  currentWeightKg?: number | null;
  microchipNumber?: string | null;
  photoUrl?: string | null;
  allergies?: string | null;
  chronicDiseases?: string | null;
  currentMedications?: string | null;
  vaccinationStatus?: string | null;
  dewormingStatus?: string | null;
  status: string;
}
