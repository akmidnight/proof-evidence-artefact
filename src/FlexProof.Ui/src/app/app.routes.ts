import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'artifacts', pathMatch: 'full' },
  {
    path: 'artifacts',
    loadComponent: () =>
      import('./pages/artifact-list/artifact-list.page').then((m) => m.ArtifactListPage),
  },
  {
    path: 'artifacts/issue',
    loadComponent: () =>
      import('./pages/issue-artifact/issue-artifact.page').then((m) => m.IssueArtifactPage),
  },
  {
    path: 'artifacts/:id',
    loadComponent: () =>
      import('./pages/artifact-detail/artifact-detail.page').then((m) => m.ArtifactDetailPage),
  },
  {
    path: 'verify',
    loadComponent: () =>
      import('./pages/verify/verify.page').then((m) => m.VerifyPage),
  },
];
