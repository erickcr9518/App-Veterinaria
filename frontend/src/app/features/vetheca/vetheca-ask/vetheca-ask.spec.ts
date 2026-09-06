import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { VethecaAskResult } from '../../../core/models/vetheca.models';
import { VethecaService } from '../../../core/services/vetheca.service';
import { VethecaAsk } from './vetheca-ask';

describe('VethecaAsk', () => {
  it('shows the synthesis and sources when the search succeeds', async () => {
    const fixture = await createComponent(() => of(createResult()));
    askQuestion(fixture);

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Resumen de prueba');
    expect(text).toContain('Hallazgo de prueba');
    expect(text).toContain('Artículo de prueba');
    expect(text).toContain('PMID 12345678');
  });

  it('shows a fallback message when there is no synthesis yet', async () => {
    const fixture = await createComponent(() => of({ ...createResult(), synthesis: null }));
    askQuestion(fixture);

    expect(fixture.nativeElement.textContent).toContain('No se pudo generar un resumen esta vez');
  });

  it('flags when the evidence was insufficient', async () => {
    const result = createResult();
    result.synthesis!.evidenceSufficient = false;
    const fixture = await createComponent(() => of(result));
    askQuestion(fixture);

    expect(fixture.nativeElement.textContent).toContain('No se encontró evidencia suficiente');
  });

  it('shows an error message when the request fails', async () => {
    const fixture = await createComponent(() => throwError(() => new Error('boom')));
    askQuestion(fixture);

    expect(fixture.nativeElement.textContent).toContain('No se pudo completar la búsqueda');
  });

  function askQuestion(fixture: ComponentFixture<VethecaAsk>): void {
    fixture.componentInstance.form.controls.question.setValue('rehabilitacion temprana tras TPLO');
    fixture.componentInstance.ask();
    fixture.detectChanges();
  }

  async function createComponent(ask: () => ReturnType<VethecaService['ask']>): Promise<ComponentFixture<VethecaAsk>> {
    const vethecaService = { ask };

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
