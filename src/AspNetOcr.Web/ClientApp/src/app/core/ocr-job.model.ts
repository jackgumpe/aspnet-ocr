export type OcrJobStatus = 'queued' | 'processing' | 'complete' | 'failed';

export interface OcrFieldResult {
  name: string;
  value: string;
  confidence: {
    value: number;
  };
  pageNumber: number;
  sourceRule: string;
  required: boolean;
}

export interface OcrJobResult {
  text: string;
  fields: OcrFieldResult[];
  confidence: number;
  characterErrorRate: number | null;
  wordErrorRate: number | null;
  rawJson: string;
}

export interface OcrJob {
  id: string;
  sourceFileName: string;
  status: OcrJobStatus;
  confidence: number | null;
  pagesProcessed: number;
  totalPages: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  completedAtUtc: string | null;
  error: string | null;
  result: OcrJobResult | null;
}
