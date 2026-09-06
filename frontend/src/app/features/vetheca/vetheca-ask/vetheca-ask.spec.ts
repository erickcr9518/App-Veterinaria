import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { VethecaAskResult, VethecaSavedSearchDetail, VethecaSavedSearchSummary } from '../../../core/models/vetheca.models';
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

  it('shows an error message when the request fails', async () => {
    const fixture = await createComponent({ ask: () => throwError(() => new Error('boom')) });
    askQuestion(fixture);

    expect(fixture.nativeElement.textContent).toContain('No se pudo completar la búsqueda');
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
        },
      ],
      synthesis: {
        modelUsed: 'claude-sonnet-5',
        evidenceSufficient: true,
        summary: 'Resumen de prueba.',
        keyFindings: ['Hallazgo de prueba'],
        clinicalApplicability: 'Aplicabilidad de prueba.',
        limitations: 'Limitaciones de prueba.',
        citations: [{ pmid: '12345678', claim: 'Afirmación de prueba' }],
      },
    };
  }
});
