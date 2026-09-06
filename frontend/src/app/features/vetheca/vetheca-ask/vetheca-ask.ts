import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { VethecaService } from '../../../core/services/vetheca.service';
import { VethecaAskResult } from '../../../core/models/vetheca.models';

@Component({
  selector: 'app-vetheca-ask',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './vetheca-ask.html',
  styleUrl: './vetheca-ask.scss',
})
export class VethecaAsk {
  private readonly fb = inject(FormBuilder);
  private readonly vethecaService = inject(VethecaService);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly result = signal<VethecaAskResult | null>(null);

  readonly form = this.fb.group({
    question: ['', [Validators.required, Validators.maxLength(500)]],
  });

  ask(): void {
    if (this.form.invalid || this.isLoading()) {
      return;
    }

    const question = this.form.value.question!.trim();
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.result.set(null);

    this.vethecaService.ask(question).subscribe({
      next: (result) => {
        this.result.set(result);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudo completar la búsqueda. Intentá de nuevo en unos minutos.');
        this.isLoading.set(false);
      },
    });
  }
}
