import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ArtifactService } from '../../services/artifact.service';
import { ClaimType, BaselineMode, IssueArtifactRequest } from '../../models/artifact.model';

@Component({
  selector: 'app-issue-artifact',
  imports: [FormsModule],
  templateUrl: './issue-artifact.page.html',
})
export class IssueArtifactPage {
  private readonly svc = inject(ArtifactService);
  private readonly router = inject(Router);

  readonly claimType = signal<ClaimType>('PeakWindowCompliance');
  readonly periodStart = signal('2025-11-01');
  readonly periodEnd = signal('2025-11-30');
  readonly baselineMode = signal<BaselineMode>('HistoricalLookback');
  readonly lookbackStart = signal('2025-10-01');
  readonly counterpartyId = signal('');
  readonly purpose = signal('');
  readonly rightsValidFrom = signal(new Date().toISOString().slice(0, 10));
  readonly rightsValidTo = signal(
    new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10)
  );
  readonly submitting = signal(false);

  submit(): void {
    this.submitting.set(true);
    const req: IssueArtifactRequest = {
      claimType: this.claimType(),
      periodStart: new Date(this.periodStart()).toISOString(),
      periodEnd: new Date(this.periodEnd()).toISOString(),
      baselineMode: this.claimType() === 'DemandChargeDeltaEstimate' ? this.baselineMode() : undefined,
      lookbackStart:
        this.claimType() === 'DemandChargeDeltaEstimate'
          ? new Date(this.lookbackStart()).toISOString()
          : undefined,
      counterpartyId: this.counterpartyId(),
      purpose: this.purpose(),
      rightsValidFrom: new Date(this.rightsValidFrom()).toISOString(),
      rightsValidTo: new Date(this.rightsValidTo()).toISOString(),
    };

    this.svc.issue(req).subscribe({
      next: (artifact) => this.router.navigate(['/artifacts', artifact.artifactId]),
      error: () => this.submitting.set(false),
    });
  }
}
