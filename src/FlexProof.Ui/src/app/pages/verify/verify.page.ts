import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ArtifactService } from '../../services/artifact.service';
import { VerificationResult } from '../../models/artifact.model';

@Component({
  selector: 'app-verify',
  imports: [FormsModule],
  templateUrl: './verify.page.html',
})
export class VerifyPage {
  private readonly svc = inject(ArtifactService);

  readonly artifactId = signal('');
  readonly result = signal<VerificationResult | null>(null);
  readonly error = signal('');
  readonly loading = signal(false);

  verify(): void {
    const id = this.artifactId().trim();
    if (!id) return;

    this.loading.set(true);
    this.error.set('');
    this.result.set(null);

    this.svc.verify(id).subscribe({
      next: (r) => {
        this.result.set(r);
        this.loading.set(false);
      },
      error: (e) => {
        this.error.set(e?.error?.error ?? 'Verification failed.');
        this.loading.set(false);
      },
    });
  }

  downloadReport(): void {
    const vr = this.result();
    if (!vr) return;

    const report = {
      title: 'FlexProof Verification Report',
      generatedAt: new Date().toISOString(),
      verificationId: vr.verificationId,
      artifactId: vr.artifactId,
      verifiedAt: vr.verifiedAt,
      overallResult: vr.isValid ? 'PASS' : 'FAIL',
      checks: vr.checks.map((c) => ({
        check: c.checkName,
        result: c.passed ? 'Pass' : 'Fail',
        detail: c.detail ?? null,
      })),
      failureReasons: vr.failureReasons,
    };

    const blob = new Blob([JSON.stringify(report, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `verification-report-${vr.artifactId.slice(0, 12)}.json`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
