import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthService } from '../core/auth.service';
import { REQUEST_CREATOR_ROLES, STAFF_ROLES } from '../core/portal-roles';
import { PortalRole } from '../models/propops.models';

@Component({
  selector: 'app-workspace-page',
  imports: [RouterLink],
  templateUrl: './workspace-page.component.html',
  styleUrl: './workspace-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WorkspacePageComponent {
  private readonly authService = inject(AuthService);

  protected readonly user = this.authService.user;
  protected readonly isStaff = computed(() =>
    STAFF_ROLES.includes((this.user()?.role as PortalRole | undefined) ?? 'Tenant')
  );
  protected readonly canCreateRequest = computed(() =>
    REQUEST_CREATOR_ROLES.includes((this.user()?.role as PortalRole | undefined) ?? 'Vendor')
  );
}
