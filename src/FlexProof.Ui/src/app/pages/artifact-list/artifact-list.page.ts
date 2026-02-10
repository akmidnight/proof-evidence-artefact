import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe, SlicePipe } from '@angular/common';
import { ArtifactService } from '../../services/artifact.service';
import { UsageRightArtifact } from '../../models/artifact.model';

@Component({
  selector: 'app-artifact-list',
  imports: [RouterLink, DatePipe, SlicePipe],
  templateUrl: './artifact-list.page.html',
})
export class ArtifactListPage implements OnInit {
  private readonly svc = inject(ArtifactService);
  readonly artifacts = signal<UsageRightArtifact[]>([]);

  ngOnInit(): void {
    this.svc.list().subscribe((data) => this.artifacts.set(data));
  }
}
