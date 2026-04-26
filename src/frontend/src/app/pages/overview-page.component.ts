import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { PropOpsApiService } from '../core/propops-api.service';
import { DashboardOverview, MaintenanceRequest } from '../models/propops.models';

@Component({
  selector: 'app-overview-page',
  imports: [CommonModule, RouterLink],
  templateUrl: './overview-page.component.html',
  styleUrl: './overview-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OverviewPageComponent implements OnInit {
  private readonly api = inject(PropOpsApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly overview = signal<DashboardOverview | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.api
      .getDashboardOverview()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (overview) => {
          this.overview.set(overview);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Unable to load the operations overview right now.');
          this.loading.set(false);
        }
      });
  }

  protected trackByReference(_: number, request: MaintenanceRequest): string {
    return request.referenceNumber;
  }
}
