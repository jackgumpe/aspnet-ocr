import { Routes } from '@angular/router';
import { UploadPageComponent } from './features/upload/upload-page.component';
import { JobDashboardComponent } from './features/jobs/job-dashboard.component';
import { ResultViewerComponent } from './features/results/result-viewer.component';
import { ClientInventoryPlaceholderComponent } from './features/client-inventory/client-inventory-placeholder.component';
import { ClientHomeHostComponent } from './shell/client-home/client-home-host.component';

export const routes: Routes = [
  { path: '', component: ClientHomeHostComponent },
  { path: 'upload', component: UploadPageComponent },
  { path: 'jobs', component: JobDashboardComponent },
  { path: 'results/:id', component: ResultViewerComponent },
  { path: 'clients', component: ClientInventoryPlaceholderComponent },
  { path: '**', redirectTo: '' }
];
