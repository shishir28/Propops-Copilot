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
  FineTuningDatasetExport,
  FineTuningExampleCandidate,
  MaintenanceOperationsDetail,
  MaintenanceRequest,
  MaintenanceTriageInferenceResult,
  SubmitMaintenanceResolutionFeedbackPayload,
  SubmitMaintenanceTriageReviewPayload
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

  getMaintenanceOperationsDetail(maintenanceRequestId: string): Observable<MaintenanceOperationsDetail> {
    return this.http.get<MaintenanceOperationsDetail>(
      `${this.apiBaseUrl}/maintenanceRequests/${maintenanceRequestId}/operations`
    );
  }

  inferMaintenanceTriage(maintenanceRequestId: string): Observable<MaintenanceTriageInferenceResult> {
    return this.http.post<MaintenanceTriageInferenceResult>(
      `${this.apiBaseUrl}/ai/maintenance-triage/infer`,
      { maintenanceRequestId }
    );
  }

  submitMaintenanceTriageReview(
    maintenanceRequestId: string,
    payload: SubmitMaintenanceTriageReviewPayload
  ): Observable<MaintenanceOperationsDetail> {
    return this.http.post<MaintenanceOperationsDetail>(
      `${this.apiBaseUrl}/maintenanceRequests/${maintenanceRequestId}/operations/triage-review`,
      payload
    );
  }

  createWorkOrder(maintenanceRequestId: string, summary: string): Observable<MaintenanceOperationsDetail> {
    return this.http.post<MaintenanceOperationsDetail>(
      `${this.apiBaseUrl}/maintenanceRequests/${maintenanceRequestId}/operations/actions/work-order`,
      { summary }
    );
  }

  assignVendor(maintenanceRequestId: string, vendorName: string): Observable<MaintenanceOperationsDetail> {
    return this.http.post<MaintenanceOperationsDetail>(
      `${this.apiBaseUrl}/maintenanceRequests/${maintenanceRequestId}/operations/actions/vendor-assignment`,
      { vendorName }
    );
  }

  notifyTenant(maintenanceRequestId: string, message: string): Observable<MaintenanceOperationsDetail> {
    return this.http.post<MaintenanceOperationsDetail>(
      `${this.apiBaseUrl}/maintenanceRequests/${maintenanceRequestId}/operations/actions/tenant-notification`,
      { message }
    );
  }

  logInternalNote(maintenanceRequestId: string, note: string): Observable<MaintenanceOperationsDetail> {
    return this.http.post<MaintenanceOperationsDetail>(
      `${this.apiBaseUrl}/maintenanceRequests/${maintenanceRequestId}/operations/actions/internal-note`,
      { note }
    );
  }

  submitResolutionFeedback(
    maintenanceRequestId: string,
    payload: SubmitMaintenanceResolutionFeedbackPayload
  ): Observable<MaintenanceOperationsDetail> {
    return this.http.post<MaintenanceOperationsDetail>(
      `${this.apiBaseUrl}/maintenanceRequests/${maintenanceRequestId}/operations/resolution-feedback`,
      payload
    );
  }

  getFineTuningCandidates(): Observable<FineTuningExampleCandidate[]> {
    return this.http.get<FineTuningExampleCandidate[]>(`${this.apiBaseUrl}/learning/dataset/candidates`);
  }

  exportFineTuningDataset(): Observable<FineTuningDatasetExport> {
    return this.http.get<FineTuningDatasetExport>(`${this.apiBaseUrl}/learning/dataset/export`);
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
