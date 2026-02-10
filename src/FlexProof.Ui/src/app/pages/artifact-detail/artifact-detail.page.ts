import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DatePipe, SlicePipe } from '@angular/common';
import { ArtifactService } from '../../services/artifact.service';
import { UsageRightArtifact, AuditEntry, VerificationResult } from '../../models/artifact.model';

@Component({
  selector: 'app-artifact-detail',
  imports: [DatePipe, SlicePipe],
  templateUrl: './artifact-detail.page.html',
})
export class ArtifactDetailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly svc = inject(ArtifactService);

  readonly artifact = signal<UsageRightArtifact | null>(null);
  readonly auditTrail = signal<AuditEntry[]>([]);
  readonly verificationResult = signal<VerificationResult | null>(null);
  readonly revokeReason = signal('');

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.svc.get(id).subscribe((a) => this.artifact.set(a));
    this.svc.getAuditTrail(id).subscribe((t) => this.auditTrail.set(t));
  }

  verify(): void {
    const id = this.artifact()?.artifactId;
    if (!id) return;
    this.svc.verify(id).subscribe((r) => {
      this.verificationResult.set(r);
      this.svc.getAuditTrail(id).subscribe((t) => this.auditTrail.set(t));
    });
  }

  revoke(): void {
    const id = this.artifact()?.artifactId;
    const reason = this.revokeReason();
    if (!id || !reason) return;
    this.svc.revoke(id, reason).subscribe(() => {
      this.svc.get(id).subscribe((a) => this.artifact.set(a));
      this.svc.getAuditTrail(id).subscribe((t) => this.auditTrail.set(t));
    });
  }

  downloadArtifact(): void {
    const a = this.artifact();
    if (!a) return;
    const blob = new Blob([JSON.stringify(a, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `artifact-${a.artifactId.slice(0, 12)}.json`;
    link.click();
    URL.revokeObjectURL(url);
  }

  downloadVerificationReport(): void {
    const vr = this.verificationResult();
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
    const link = document.createElement('a');
    link.href = url;
    link.download = `verification-report-${vr.artifactId.slice(0, 12)}.json`;
    link.click();
    URL.revokeObjectURL(url);
  }
}
