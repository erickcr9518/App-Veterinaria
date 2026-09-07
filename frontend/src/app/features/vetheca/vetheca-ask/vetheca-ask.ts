import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { VethecaService } from '../../../core/services/vetheca.service';
import { VethecaArticle, VethecaSavedSearchSummary, VethecaSynthesis } from '../../../core/models/vetheca.models';

interface DisplayedResult {
  id: string;
  question: string;
  articles: VethecaArticle[];
  synthesis: VethecaSynthesis | null;
  isSaved: boolean;
  title: string | null;
  feedbackGiven: boolean | null;
}

@Component({
  selector: 'app-vetheca-ask',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './vetheca-ask.html',
  styleUrl: './vetheca-ask.scss',
})
export class VethecaAsk implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly vethecaService = inject(VethecaService);

  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly isSendingFeedback = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly result = signal<DisplayedResult | null>(null);
  readonly savedSearches = signal<VethecaSavedSearchSummary[]>([]);
  readonly showFeedbackNote = signal(false);

  readonly form = this.fb.group({
    question: ['', [Validators.required, Validators.maxLength(500)]],
  });

  readonly saveForm = this.fb.group({
    title: ['', [Validators.maxLength(200)]],
  });

  readonly feedbackForm = this.fb.group({
    note: ['', [Validators.maxLength(1000)]],
  });

  ngOnInit(): void {
    this.loadSavedSearches();
  }

  ask(): void {
    if (this.form.invalid || this.isLoading()) {
      return;
    }

    const question = this.form.value.question!.trim();
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.result.set(null);
    this.saveForm.reset();
    this.resetFeedbackUi();

    this.vethecaService.ask(question).subscribe({
      next: (response) => {
        this.result.set({
          id: response.id,
          question,
          articles: response.articles,
          synthesis: response.synthesis,
          isSaved: false,
          title: null,
          feedbackGiven: null,
        });
        this.isLoading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage.set(
          error.status === 429
            ? 'Hiciste muchas preguntas en poco tiempo. Esperá unos minutos antes de volver a preguntar.'
            : 'No se pudo completar la búsqueda. Intentá de nuevo en unos minutos.'
        );
        this.isLoading.set(false);
      },
    });
  }

  saveCurrent(): void {
    const current = this.result();
    if (!current || this.isSaving()) {
      return;
    }

    const title = this.saveForm.value.title?.trim() || null;
    this.isSaving.set(true);
    this.vethecaService.saveSearch(current.id, title).subscribe({
      next: () => {
        this.result.set({ ...current, isSaved: true, title });
        this.isSaving.set(false);
        this.loadSavedSearches();
      },
      error: () => {
        this.errorMessage.set('No se pudo guardar la consulta.');
        this.isSaving.set(false);
      },
    });
  }

  unsaveCurrent(): void {
    const current = this.result();
    if (!current || this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    this.vethecaService.unsaveSearch(current.id).subscribe({
      next: () => {
        this.result.set({ ...current, isSaved: false, title: null });
        this.isSaving.set(false);
        this.loadSavedSearches();
      },
      error: () => {
        this.errorMessage.set('No se pudo quitar la consulta de guardadas.');
        this.isSaving.set(false);
      },
    });
  }

  openSaved(id: string): void {
    if (this.isLoading()) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.resetFeedbackUi();

    this.vethecaService.getSavedSearchById(id).subscribe({
      next: (detail) => {
        this.form.patchValue({ question: detail.question });
        this.result.set({
          id: detail.id,
          question: detail.question,
          articles: detail.articles,
          synthesis: detail.synthesis,
          isSaved: true,
          title: detail.title,
          feedbackGiven: null,
        });
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudo abrir esa búsqueda guardada.');
        this.isLoading.set(false);
      },
    });
  }

  rateHelpful(helpful: boolean): void {
    if (!helpful) {
      // Ask for a short note on what went wrong before actually submitting -
      // more useful signal than a bare thumbs-down.
      this.showFeedbackNote.set(true);
      return;
    }

    this.sendFeedback(true, null);
  }

  submitFeedbackNote(): void {
    const note = this.feedbackForm.value.note?.trim() || null;
    this.sendFeedback(false, note);
  }

  private sendFeedback(helpful: boolean, note: string | null): void {
    const current = this.result();
    if (!current || this.isSendingFeedback()) {
      return;
    }

    this.isSendingFeedback.set(true);
    this.vethecaService.submitFeedback(current.id, helpful, note).subscribe({
      next: () => {
        this.result.set({ ...current, feedbackGiven: helpful });
        this.showFeedbackNote.set(false);
        this.isSendingFeedback.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudo enviar la calificación.');
        this.isSendingFeedback.set(false);
      },
    });
  }

  private resetFeedbackUi(): void {
    this.showFeedbackNote.set(false);
    this.feedbackForm.reset();
  }

  private loadSavedSearches(): void {
    this.vethecaService.getSavedSearches().subscribe({
      next: (searches) => this.savedSearches.set(searches),
      error: () => {
        // Non-critical for the main flow - just leave the saved list empty.
      },
    });
  }
}
