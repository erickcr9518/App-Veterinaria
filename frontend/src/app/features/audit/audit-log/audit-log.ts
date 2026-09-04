import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { AuditService } from '../../../core/services/audit.service';
import { AuditEntry } from '../../../core/models/audit.models';

interface AuditDayGroup {
  dateKey: string;
  entries: AuditEntry[];
}

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './audit-log.html',
  styleUrl: './audit-log.scss',
})
export class AuditLog implements OnInit {
  private readonly auditService = inject(AuditService);

  readonly entries = signal<AuditEntry[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly hasEntries = computed(() => this.entries().length > 0);

  readonly groupedByDay = computed<AuditDayGroup[]>(() => {
    const groups = new Map<string, AuditEntry[]>();
    for (const entry of this.entries()) {
      const dateKey = entry.occurredAtUtc.slice(0, 10);
      const group = groups.get(dateKey);
      if (group) {
        group.push(entry);
      } else {
        groups.set(dateKey, [entry]);
      }
    }
    return Array.from(groups.entries()).map(([dateKey, entries]) => ({ dateKey, entries }));
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.auditService.getAuditLog().subscribe({
      next: (entries) => {
        this.entries.set(entries);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudo cargar la bitácora de auditoría.');
        this.isLoading.set(false);
      },
    });
  }
}
