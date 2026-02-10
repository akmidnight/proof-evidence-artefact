export type ArtifactState = 'Draft' | 'Issued' | 'Revoked' | 'Superseded';
export type ClaimType = 'PeakWindowCompliance' | 'DemandChargeDeltaEstimate';
export type BaselineMode = 'HistoricalLookback' | 'CounterfactualModel';

export interface ClaimValue {
  type: ClaimType;
  metricName: string;
  value: number;
  unit: string;
  baselineRef?: string;
  computationVersion: string;
}

export interface RightsScope {
  counterpartyId: string;
  purpose: string;
  validFrom: string;
  validTo: string;
  constraints?: Record<string, string>;
}

export interface UsageRightArtifact {
  artifactId: string;
  schemaVersion: string;
  issuerId: string;
  createdAt: string;
  state: ArtifactState;
  periodStart: string;
  periodEnd: string;
  claim: ClaimValue;
  rights: RightsScope;
  dataCommitment?: string;
  signature?: string;
  signerPublicKey?: string;
  supersededBy?: string;
  revocationRef?: string;
}

export interface AuditEntry {
  entryId: string;
  artifactId: string;
  timestamp: string;
  eventType: string;
  actorId: string;
  detail?: string;
}

export interface VerificationCheck {
  checkName: string;
  passed: boolean;
  detail?: string;
}

export interface VerificationResult {
  isValid: boolean;
  verificationId: string;
  artifactId: string;
  verifiedAt: string;
  checks: VerificationCheck[];
  failureReasons: string[];
}

export interface IssueArtifactRequest {
  claimType: ClaimType;
  periodStart: string;
  periodEnd: string;
  baselineMode?: BaselineMode;
  lookbackStart?: string;
  counterpartyId: string;
  purpose: string;
  rightsValidFrom: string;
  rightsValidTo: string;
}
