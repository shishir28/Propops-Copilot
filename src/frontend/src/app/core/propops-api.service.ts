import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CreateMaintenanceRequestPayload,
  DashboardOverview,
  IngestEmailPayload,
  IngestPhoneNotePayload,
  IngestSmsChatPayload,
  IntakeIngestionResult,
  IntakeSubmission,
  MaintenanceRequest
} from '../models/propops.models';

@Injectable({ providedIn: 'root' })
export class PropOpsApiService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = `${window.location.protocol}//${window.location.hostname}:8095/api`;

  getDashboardOverview(): Observable<DashboardOverview> {
    return this.http.get<DashboardOverview>(`${this.apiBaseUrl}/dashboard/overview`);
  }

  getRecentIntakeSubmissions(): Observable<IntakeSubmission[]> {
    return this.http.get<IntakeSubmission[]>(`${this.apiBaseUrl}/intakeConnectors/recent`);
  }

  createMaintenanceRequest(
    payload: CreateMaintenanceRequestPayload
  ): Observable<MaintenanceRequest> {
    return this.http.post<MaintenanceRequest>(`${this.apiBaseUrl}/maintenanceRequests`, payload);
  }

  ingestEmail(payload: IngestEmailPayload): Observable<IntakeIngestionResult> {
    return this.http.post<IntakeIngestionResult>(`${this.apiBaseUrl}/intakeConnectors/email`, payload);
  }

  ingestSmsChat(payload: IngestSmsChatPayload): Observable<IntakeIngestionResult> {
    return this.http.post<IntakeIngestionResult>(`${this.apiBaseUrl}/intakeConnectors/sms-chat`, payload);
  }

  ingestPhoneNote(payload: IngestPhoneNotePayload): Observable<IntakeIngestionResult> {
    return this.http.post<IntakeIngestionResult>(`${this.apiBaseUrl}/intakeConnectors/phone-note`, payload);
  }
}
