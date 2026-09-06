export interface VethecaArticle {
  pmid: string;
  title: string;
  authors: string;
  journal: string | null;
  year: string | null;
  abstractText: string | null;
  url: string;
}

export interface VethecaCitation {
  pmid: string;
  claim: string;
}

export interface VethecaSynthesis {
  evidenceSufficient: boolean;
  summary: string;
  keyFindings: string[];
  clinicalApplicability: string | null;
  limitations: string | null;
  citations: VethecaCitation[];
}

export interface VethecaAskResult {
  articles: VethecaArticle[];
  synthesis: VethecaSynthesis | null;
}
