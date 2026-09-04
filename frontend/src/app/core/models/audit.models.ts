export interface AuditEntry {
  id: string;
  occurredAtUtc: string;
  entityType: string;
  entityId: string;
  action: string;
  summary: string;
  performedByName: string;
}
