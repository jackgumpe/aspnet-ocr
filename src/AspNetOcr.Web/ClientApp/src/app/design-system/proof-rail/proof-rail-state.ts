export const PROOF_RAIL_STATE_ORDER = [
  'RESULT_CREATED',
  'EVIDENCE_WRITTEN',
  'INGESTED',
  'QUERYABLE',
  'LIVE_VERIFIED'
] as const;

export type ProofRailStateCode = typeof PROOF_RAIL_STATE_ORDER[number];

export type ProofRailReviewStatus = 'NOT_SELECTED' | 'SELECTED' | 'REVIEW_SPOT_CHECKED' | 'REVIEW_FAILED';

export interface ProofRailStateDefinition {
  readonly code: ProofRailStateCode;
  readonly label: string;
  readonly accessibleMeaning: string;
  readonly proofMetadata: string;
  readonly integrityBehavior: string;
}

export const LIVE_VERIFIED_DEFINITION =
  'Consumer-path retrieval returned the exact evidence marker and source-to-ingest hash verification passed.';

export const PROOF_RAIL_STATE_DEFINITIONS: readonly ProofRailStateDefinition[] = [
  {
    code: 'RESULT_CREATED',
    label: 'Result created',
    accessibleMeaning: 'OCR result object exists for this job.',
    proofMetadata: 'job_id, source_file_name, created_at',
    integrityBehavior: 'No evidence claim is trusted yet.'
  },
  {
    code: 'EVIDENCE_WRITTEN',
    label: 'Evidence written',
    accessibleMeaning: 'Durable evidence artifact was written for this result.',
    proofMetadata: 'evidence_path, evidence_sha256',
    integrityBehavior: 'Hash is captured before ingest.'
  },
  {
    code: 'INGESTED',
    label: 'Ingested',
    accessibleMeaning: 'Evidence artifact entered the indexable corpus.',
    proofMetadata: 'ingested_artifact_path, ingested_sha256',
    integrityBehavior: 'Source and ingested hashes must match.'
  },
  {
    code: 'QUERYABLE',
    label: 'Queryable',
    accessibleMeaning: 'Consumer retriever can return the evidence artifact.',
    proofMetadata: 'consumer_backend, exact_hit_count, top_hit_source_path',
    integrityBehavior: 'Preload-only or self-referential hits do not satisfy this state.'
  },
  {
    code: 'LIVE_VERIFIED',
    label: 'Live verified',
    accessibleMeaning: LIVE_VERIFIED_DEFINITION,
    proofMetadata: 'consumer_verification_latency_seconds, hash_verified',
    integrityBehavior: 'Visible proof is valid only when query and hash checks both pass.'
  }
];
