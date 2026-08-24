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

export type AppointmentStatus = 'Scheduled' | 'Confirmed' | 'Cancelled' | 'Completed' | 'NoShow';

export interface AppointmentStatusChange {
  id: string;
  fromStatus?: AppointmentStatus | null;
  toStatus: AppointmentStatus;
  reason?: string | null;
  changedAtUtc: string;
  changedByName: string;
}

export interface Appointment {
  id: string;
  patientId: string;
  patientName: string;
  ownerId: string;
  ownerName: string;
  assignedVeterinarianUserId?: string | null;
  assignedVeterinarianName?: string | null;
  startsAtUtc: string;
  endsAtUtc: string;
  visitType: string;
  status: AppointmentStatus;
  reason: string;
  notes?: string | null;
  reminderSentAtUtc?: string | null;
  reminderChannel?: string | null;
  reminderNotes?: string | null;
  statusChanges: AppointmentStatusChange[];
}

export interface AppointmentRequest {
  patientId: string;
  assignedVeterinarianUserId?: string | null;
  startsAtUtc: string;
  endsAtUtc: string;
  visitType: string;
  reason: string;
  notes?: string | null;
  reminderChannel?: string | null;
  reminderNotes?: string | null;
}

export interface PrescriptionItem {
  id: string;
  productName: string;
  concentration?: string | null;
  presentation?: string | null;
  quantity: string;
  route: string;
  frequency: string;
  duration: string;
  instructions?: string | null;
}

export interface PrescriptionItemInput {
  productName: string;
  concentration?: string | null;
  presentation?: string | null;
  quantity: string;
  route: string;
  frequency: string;
  duration: string;
  instructions?: string | null;
}

export interface PrescriptionSummary {
  id: string;
  consultationId: string;
  patientId: string;
  issuedAtUtc: string;
  veterinarianName: string;
  status: 'Draft' | 'Finalized';
  productNames: string[];
}

export interface PrescriptionDetail {
  id: string;
  consultationId: string;
  patientId: string;
  patientName: string;
  veterinarianName: string;
  issuedAtUtc: string;
  weightKgAtPrescription?: number | null;
  generalInstructions?: string | null;
  warnings?: string | null;
  status: 'Draft' | 'Finalized';
  finalizedAtUtc?: string | null;
  finalizedByName?: string | null;
  items: PrescriptionItem[];
}

export interface PrescriptionFormValue {
  weightKgAtPrescription?: number | null;
  generalInstructions?: string | null;
  warnings?: string | null;
  items: PrescriptionItemInput[];
}

export type CreatePrescriptionRequest = PrescriptionFormValue & { consultationId: string };

export type UpdatePrescriptionRequest = PrescriptionFormValue;
