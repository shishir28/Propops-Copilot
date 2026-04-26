import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CreateMaintenanceRequestPayload,
  DashboardOverview,
  MaintenanceRequest
} from '../models/propops.models';

@Injectable({ providedIn: 'root' })
export class PropOpsApiService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = `${window.location.protocol}//${window.location.hostname}:8095/api`;

  getDashboardOverview(): Observable<DashboardOverview> {
    return this.http.get<DashboardOverview>(`${this.apiBaseUrl}/dashboard/overview`);
  }

  createMaintenanceRequest(
    payload: CreateMaintenanceRequestPayload
  ): Observable<MaintenanceRequest> {
    return this.http.post<MaintenanceRequest>(`${this.apiBaseUrl}/maintenanceRequests`, payload);
  }
}
