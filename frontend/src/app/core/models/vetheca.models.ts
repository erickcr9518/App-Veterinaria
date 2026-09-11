export interface VethecaArticle {
  pmid: string;
  title: string;
  authors: string;
  journal: string | null;
  year: string | null;
  abstractText: string | null;
  url: string;
  studyType: string | null;
}

export type VethecaCitationSource = 'PubMed' | 'Library';

export interface VethecaCitation {
  source: VethecaCitationSource;
  pmid: string | null;
  libraryDocumentId: string | null;
  libraryDocumentTitle: string | null;
  libraryPageNumber: number | null;
  claim: string;
  supportingExcerpt: string | null;
  quoteVerified: boolean;
}

export interface VethecaSynthesis {
  modelUsed: string;
  evidenceSufficient: boolean;
  summary: string;
  keyFindings: string[];
  clinicalApplicability: string | null;
  limitations: string | null;
  citations: VethecaCitation[];
}

export interface VethecaAskResult {
  id: string;
  articles: VethecaArticle[];
  synthesis: VethecaSynthesis | null;
}

export interface VethecaSavedSearchSummary {
  id: string;
  question: string;
  title: string | null;
  articleCount: number;
  evidenceSufficient: boolean | null;
  createdAtUtc: string;
}

export interface VethecaSavedSearchDetail {
  id: string;
  question: string;
  title: string | null;
  createdAtUtc: string;
  articles: VethecaArticle[];
  synthesis: VethecaSynthesis | null;
}

export interface VethecaLibraryDocument {
  id: string;
  title: string;
  fileName: string;
  pageCount: number;
  uploadedAtUtc: string;
}
