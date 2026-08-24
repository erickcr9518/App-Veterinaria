export interface DashboardAppointment {
  id: string;
  patientId: string;
  patientName: string;
  startsAtUtc: string;
  endsAtUtc: string;
  visitType: string;
  status: string;
  reason: string;
}

export interface DashboardDraftItem {
  id: string;
  patientId: string;
  patientName: string;
  summary: string;
  dateUtc: string;
}

export interface DashboardRecentPatient {
  id: string;
  name: string;
  species: string;
  ownerName: string;
  createdAtUtc: string;
}

export interface DashboardSummary {
  todaysAppointmentsCount: number;
  upcomingAppointments: DashboardAppointment[];
  draftConsultations: DashboardDraftItem[];
  draftPrescriptions: DashboardDraftItem[];
  recentPatients: DashboardRecentPatient[];
}
