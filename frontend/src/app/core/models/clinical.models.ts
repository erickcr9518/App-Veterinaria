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

export interface ConsultationSummary {
  id: string;
  patientId: string;
  consultationDateUtc: string;
  reasonForVisit: string;
  veterinarianName: string;
  status: string;
  followUpDate?: string | null;
}

export interface ConsultationAmendment {
  id: string;
  reason: string;
  previousValuesJson: string;
  createdAtUtc: string;
  createdByName: string;
}

export interface ConsultationDetail {
  id: string;
  patientId: string;
  patientName: string;
  veterinarianUserId: string;
  veterinarianName: string;
  consultationDateUtc: string;
  reasonForVisit: string;
  historyOfPresentIllness?: string | null;
  physicalExamFindings?: string | null;
  temperatureCelsius?: number | null;
  heartRateBpm?: number | null;
  respiratoryRateRpm?: number | null;
  weightKg?: number | null;
  diagnosticPlan?: string | null;
  treatment?: string | null;
  recommendations?: string | null;
  followUpDate?: string | null;
  status: 'Draft' | 'Finalized';
  finalizedAtUtc?: string | null;
  finalizedByName?: string | null;
  subjective?: string | null;
  objective?: string | null;
  assessment?: string | null;
  plan?: string | null;
  amendments: ConsultationAmendment[];
}

export interface ConsultationFormValue {
  reasonForVisit: string;
  historyOfPresentIllness?: string | null;
  physicalExamFindings?: string | null;
  temperatureCelsius?: number | null;
  heartRateBpm?: number | null;
  respiratoryRateRpm?: number | null;
  weightKg?: number | null;
  diagnosticPlan?: string | null;
  treatment?: string | null;
  recommendations?: string | null;
  followUpDate?: string | null;
  subjective?: string | null;
  objective?: string | null;
  assessment?: string | null;
  plan?: string | null;
}

export type CreateConsultationRequest = ConsultationFormValue & { patientId: string };

export type UpdateConsultationRequest = ConsultationFormValue;

export type AmendConsultationRequest = Omit<ConsultationFormValue, 'weightKg'> & { reason: string };
