import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import {
  VethecaAskResult,
  VethecaLibraryDocument,
  VethecaSavedSearchDetail,
  VethecaSavedSearchSummary,
} from '../../../core/models/vetheca.models';
import { VethecaService } from '../../../core/services/vetheca.service';
import { VethecaAsk } from './vetheca-ask';

describe('VethecaAsk', () => {
  it('shows the synthesis and sources when the search succeeds', async () => {
    const fixture = await createComponent({ ask: () => of(createResult()) });
    askQuestion(fixture);

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Resumen de prueba');
    expect(text).toContain('Hallazgo de prueba');
    expect(text).toContain('Artículo de prueba');
    expect(text).toContain('PMID 12345678');
  });

  it('shows a fallback message when there is no synthesis yet', async () => {
    const fixture = await createComponent({ ask: () => of({ ...createResult(), synthesis: null }) });
    askQuestion(fixture);

    expect(fixture.nativeElement.textContent).toContain('No se pudo generar un resumen esta vez');
  });

  it('flags when the evidence was insufficient', async () => {
    const result = createResult();
    result.synthesis!.evidenceSufficient = false;
    const fixture = await createComponent({ ask: () => of(result) });
    askQuestion(fixture);

    expect(fixture.nativeElement.textContent).toContain('No se encontró evidencia suficiente');
  });

  it('shows the study type of each source, or "no confirmado" when missing', async () => {
    const result = createResult();
    result.articles[0].studyType = null;
    const fixture = await createComponent({ ask: () => of(result) });
    askQuestion(fixture);

    expect(fixture.nativeElement.textContent).toContain('Tipo de estudio: no confirmado');
  });

  it('shows a verified badge when the citation excerpt matches the abstract', async () => {
    const fixture = await createComponent({ ask: () => of(createResult()) });
    askQuestion(fixture);
    expect(fixture.nativeElement.textContent).toContain('texto verificado');
  });

  it('shows a warning when the citation excerpt could not be verified', async () => {
    const unverified = createResult();
    unverified.synthesis!.citations[0].quoteVerified = false;
    const fixture = await createComponent({ ask: () => of(unverified) });
    askQuestion(fixture);
    expect(fixture.nativeElement.textContent).toContain('no se pudo verificar el texto exacto');
  });

  it('lets the user rate a response as helpful', async () => {
    const submitFeedback = vi.fn(() => of(undefined));
    const fixture = await createComponent({ ask: () => of(createResult()), submitFeedback });
    askQuestion(fixture);

    fixture.componentInstance.rateHelpful(true);
    fixture.detectChanges();

    expect(submitFeedback).toHaveBeenCalledWith('log-1', true, null);
    expect(fixture.nativeElement.textContent).toContain('Gracias por tu calificación');
  });

  it('asks for a note before submitting a "not helpful" rating', async () => {
    const submitFeedback = vi.fn(() => of(undefined));
    const fixture = await createComponent({ ask: () => of(createResult()), submitFeedback });
    askQuestion(fixture);

    fixture.componentInstance.rateHelpful(false);
    fixture.detectChanges();
    expect(submitFeedback).not.toHaveBeenCalled();
    expect(fixture.componentInstance.showFeedbackNote()).toBe(true);

    fixture.componentInstance.feedbackForm.controls.note.setValue('No encontró nada relevante');
    fixture.componentInstance.submitFeedbackNote();
    fixture.detectChanges();

    expect(submitFeedback).toHaveBeenCalledWith('log-1', false, 'No encontró nada relevante');
  });

  it('shows an error message when the request fails', async () => {
    const fixture = await createComponent({ ask: () => throwError(() => new Error('boom')) });
    askQuestion(fixture);

    expect(fixture.nativeElement.textContent).toContain('No se pudo completar la búsqueda');
  });

  it('shows how many questions are left this month when the account has a limit', async () => {
    const fixture = await createComponent({
      getQuota: () => of({ monthlyLimit: 100, usedThisMonth: 40, remaining: 60, resetsAtUtc: '2026-10-01T00:00:00Z' }),
    });

    fixture.componentInstance.form.controls.question.setValue('una pregunta');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Preguntas disponibles este mes: 60 de 100');
    expect(fixture.nativeElement.querySelector('.ask-form button[type="submit"]').disabled).toBe(false);
  });

  it('shows nothing about a quota for accounts without a limit', async () => {
    const fixture = await createComponent({});

    expect(fixture.nativeElement.querySelector('.quota')).toBeNull();
  });

  it('blocks asking and explains why when the monthly limit is used up', async () => {
    const ask = vi.fn(() => of(createResult()));
    const fixture = await createComponent({
      ask,
      getQuota: () => of({ monthlyLimit: 100, usedThisMonth: 100, remaining: 0, resetsAtUtc: '2026-10-01T00:00:00Z' }),
    });
    askQuestion(fixture);

    expect(fixture.nativeElement.textContent).toContain('Llegaste al límite de preguntas de este mes (100)');
    expect(fixture.nativeElement.querySelector('.ask-form button[type="submit"]').disabled).toBe(true);
    expect(ask).not.toHaveBeenCalled();
  });

  it('explains a monthly-limit rejection from the server differently from the too-fast rate limit', async () => {
    const fixture = await createComponent({
      ask: () =>
        throwError(
          () =>
            new HttpErrorResponse({
              status: 429,
              error: { code: 'vetheca_quota_exceeded', resetsAtUtc: '2026-10-01T00:00:00Z' },
            }),
        ),
    });
    askQuestion(fixture);

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Alcanzaste el límite de preguntas de este mes');
    expect(text).not.toContain('muchas preguntas en poco tiempo');
  });

  it('shows a friendly rate-limit message when asking too many questions too fast', async () => {
    const fixture = await createComponent({
      ask: () => throwError(() => new HttpErrorResponse({ status: 429 })),
    });
    askQuestion(fixture);

    expect(fixture.nativeElement.textContent).toContain('Hiciste muchas preguntas en poco tiempo');
  });

  it('lets the user save a search and shows the saved badge afterwards', async () => {
    const saveSearch = vi.fn(() => of(undefined));
    const fixture = await createComponent({ ask: () => of(createResult()), saveSearch });
    askQuestion(fixture);

    fixture.componentInstance.saveForm.controls.title.setValue('Mi título');
    fixture.componentInstance.saveCurrent();
    fixture.detectChanges();

    expect(saveSearch).toHaveBeenCalledWith('log-1', 'Mi título');
    expect(fixture.nativeElement.textContent).toContain('Guardada como "Mi título"');
  });

  it('shows a badge and page reference for a citation from the clinic library', async () => {
    const result = createResult();
    result.synthesis!.citations = [
      {
        source: 'Library',
        pmid: null,
        libraryDocumentId: 'doc-1',
        libraryDocumentTitle: 'Manual de dosis felinas',
        libraryPageNumber: 3,
        claim: 'Afirmación de la biblioteca',
        supportingExcerpt: 'extracto de la biblioteca',
        quoteVerified: true,
      },
    ];
    const fixture = await createComponent({ ask: () => of(result) });
    askQuestion(fixture);

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('De tu biblioteca');
    expect(text).toContain('Manual de dosis felinas, página 3');
  });

  it('shows uploaded library documents and lets the user delete one', async () => {
    const documents: VethecaLibraryDocument[] = [
      { id: 'doc-1', title: 'Manual de dosis felinas', fileName: 'dosis.pdf', pageCount: 12, uploadedAtUtc: '2026-09-10T00:00:00Z' },
    ];
    const deleteLibraryDocument = vi.fn(() => of(undefined));
    const fixture = await createComponent({
      ask: () => of(createResult()),
      getLibraryDocuments: () => of(documents),
      deleteLibraryDocument,
    });

    fixture.componentInstance.toggleLibraryPanel();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Manual de dosis felinas');

    fixture.componentInstance.deleteLibraryDocument('doc-1');
    fixture.detectChanges();

    expect(deleteLibraryDocument).toHaveBeenCalledWith('doc-1');
    expect(fixture.componentInstance.libraryDocuments()).toEqual([]);
  });

  it('shows previously saved searches and opens one on click', async () => {
    const savedSummary: VethecaSavedSearchSummary = {
      id: 'log-2',
      question: 'pregunta guardada',
      title: 'Título guardado',
      articleCount: 2,
      evidenceSufficient: true,
      createdAtUtc: '2026-09-05T00:00:00Z',
    };
    const savedDetail: VethecaSavedSearchDetail = {
      ...createResult(),
      id: 'log-2',
      question: 'pregunta guardada',
      title: 'Título guardado',
      createdAtUtc: '2026-09-05T00:00:00Z',
    };
    const getSavedSearchById = vi.fn(() => of(savedDetail));

    const fixture = await createComponent({
      ask: () => of(createResult()),
      getSavedSearches: () => of([savedSummary]),
      getSavedSearchById,
    });

    expect(fixture.nativeElement.textContent).toContain('Título guardado');

    fixture.componentInstance.openSaved('log-2');
    fixture.detectChanges();

    expect(getSavedSearchById).toHaveBeenCalledWith('log-2');
    expect(fixture.nativeElement.textContent).toContain('Guardada como "Título guardado"');
  });

  function askQuestion(fixture: ComponentFixture<VethecaAsk>): void {
    fixture.componentInstance.form.controls.question.setValue('rehabilitacion temprana tras TPLO');
    fixture.componentInstance.ask();
    fixture.detectChanges();
  }

  async function createComponent(overrides: Partial<VethecaService>): Promise<ComponentFixture<VethecaAsk>> {
    const vethecaService: Partial<VethecaService> = {
      getSavedSearches: () => of([]),
      getLibraryDocuments: () => of([]),
      getQuota: () => of({ monthlyLimit: null, usedThisMonth: 0, remaining: null, resetsAtUtc: '2026-10-01T00:00:00Z' }),
      ...overrides,
    };

    await TestBed.configureTestingModule({
      imports: [VethecaAsk],
      providers: [{ provide: VethecaService, useValue: vethecaService }],
    }).compileComponents();

    const fixture = TestBed.createComponent(VethecaAsk);
    fixture.detectChanges();
    return fixture;
  }

  function createResult(): VethecaAskResult {
    return {
      id: 'log-1',
      articles: [
        {
          pmid: '12345678',
          title: 'Artículo de prueba',
          authors: 'Smith J',
          journal: 'Veterinary Surgery',
          year: '2023',
          abstractText: 'Abstract de prueba.',
          url: 'https://pubmed.ncbi.nlm.nih.gov/12345678/',
          studyType: 'Randomized Controlled Trial',
        },
      ],
      synthesis: {
        modelUsed: 'claude-sonnet-5',
        evidenceSufficient: true,
        summary: 'Resumen de prueba.',
        keyFindings: ['Hallazgo de prueba'],
        clinicalApplicability: 'Aplicabilidad de prueba.',
        limitations: 'Limitaciones de prueba.',
        citations: [
          {
            source: 'PubMed',
            pmid: '12345678',
            libraryDocumentId: null,
            libraryDocumentTitle: null,
            libraryPageNumber: null,
            claim: 'Afirmación de prueba',
            supportingExcerpt: 'extracto textual',
            quoteVerified: true,
          },
        ],
      },
    };
  }
});
