import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  UsageRightArtifact,
  AuditEntry,
  VerificationResult,
  IssueArtifactRequest,
} from '../models/artifact.model';

@Injectable({ providedIn: 'root' })
export class ArtifactService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/artifacts';

  list(): Observable<UsageRightArtifact[]> {
    return this.http.get<UsageRightArtifact[]>(this.baseUrl);
  }

  get(id: string): Observable<UsageRightArtifact> {
    return this.http.get<UsageRightArtifact>(`${this.baseUrl}/${id}`);
  }

  issue(request: IssueArtifactRequest): Observable<UsageRightArtifact> {
    return this.http.post<UsageRightArtifact>(`${this.baseUrl}/issue`, request);
  }

  verify(artifactId: string): Observable<VerificationResult> {
    return this.http.post<VerificationResult>(`${this.baseUrl}/verify`, { artifactId });
  }

  revoke(artifactId: string, reason: string): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/revoke`, { artifactId, reason });
  }

  getAuditTrail(artifactId: string): Observable<AuditEntry[]> {
    return this.http.get<AuditEntry[]>(`${this.baseUrl}/${artifactId}/audit`);
  }
}
