import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { PropOpsApiService } from './propops-api.service';

describe('PropOpsApiService', () => {
  const apiBaseUrl = `${window.location.protocol}//${window.location.hostname}:8095/api`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
  });

  it('requests the dashboard overview from the API', () => {
    const service = TestBed.inject(PropOpsApiService);
    const http = TestBed.inject(HttpTestingController);

    service.getDashboardOverview().subscribe();

    const request = http.expectOne(`${apiBaseUrl}/dashboard/overview`);
    expect(request.request.method).toBe('GET');
    request.flush({});
  });

  it('requests recent intake submissions from the API', () => {
    const service = TestBed.inject(PropOpsApiService);
    const http = TestBed.inject(HttpTestingController);

    service.getRecentIntakeSubmissions().subscribe();

    const request = http.expectOne(`${apiBaseUrl}/intakeConnectors/recent`);
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('posts new maintenance requests to the API', () => {
    const service = TestBed.inject(PropOpsApiService);
    const http = TestBed.inject(HttpTestingController);
    const payload = {
      submitterName: 'Jordan Blake',
      emailAddress: 'manager@propops.local',
      phoneNumber: '0412200100',
      propertyName: 'Harbour View Residences',
      unitNumber: '22A',
      description: 'Bathroom tap is leaking.',
      category: 'Plumbing' as const,
      priority: 'High' as const,
      channel: 'Portal' as const
    };

    service.createMaintenanceRequest(payload).subscribe();

    const request = http.expectOne(`${apiBaseUrl}/maintenanceRequests`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(payload);
    request.flush({});
  });

  it('requests maintenance operations detail from the API', () => {
    const service = TestBed.inject(PropOpsApiService);
    const http = TestBed.inject(HttpTestingController);

    service.getMaintenanceOperationsDetail('request-1').subscribe();

    const request = http.expectOne(`${apiBaseUrl}/maintenanceRequests/request-1/operations`);
    expect(request.request.method).toBe('GET');
    request.flush({});
  });

  it('posts maintenance triage inference requests to the API', () => {
    const service = TestBed.inject(PropOpsApiService);
    const http = TestBed.inject(HttpTestingController);

    service.inferMaintenanceTriage('request-1').subscribe();

    const request = http.expectOne(`${apiBaseUrl}/ai/maintenance-triage/infer`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ maintenanceRequestId: 'request-1' });
    request.flush({});
  });

  it('posts reviewed triage output to the operations API', () => {
    const service = TestBed.inject(PropOpsApiService);
    const http = TestBed.inject(HttpTestingController);
    const output = {
      category: 'Plumbing' as const,
      priority: 'High' as const,
      vendorType: 'Licensed Plumber',
      dispatchDecision: 'Create an urgent work order.',
      internalSummary: 'High plumbing issue.',
      tenantResponseDraft: 'A plumber is being assigned.'
    };
    const guardrails = {
      schemaValid: true,
      policyPassed: true,
      emergencyKeywordCheckPassed: true,
      confidenceScore: 0.84,
      confidenceThreshold: 0.68,
      requiresHumanReview: false,
      fallbackApplied: false,
      issues: []
    };

    service
      .submitMaintenanceTriageReview('request-1', {
        aiOutput: output,
        guardrails,
        category: 'Plumbing',
        priority: 'High',
        vendorType: 'Licensed Plumber',
        dispatchDecision: 'Create an urgent work order.',
        internalSummary: 'High plumbing issue.',
        tenantResponseDraft: 'A plumber is being assigned.'
      })
      .subscribe();

    const request = http.expectOne(`${apiBaseUrl}/maintenanceRequests/request-1/operations/triage-review`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body.aiOutput).toEqual(output);
    request.flush({});
  });

  it('posts operational actions to the API', () => {
    const service = TestBed.inject(PropOpsApiService);
    const http = TestBed.inject(HttpTestingController);

    service.createWorkOrder('request-1', 'Create work order.').subscribe();
    service.assignVendor('request-1', 'Licensed Plumber').subscribe();
    service.notifyTenant('request-1', 'Vendor assigned.').subscribe();
    service.logInternalNote('request-1', 'Tenant prefers morning access.').subscribe();

    const workOrder = http.expectOne(`${apiBaseUrl}/maintenanceRequests/request-1/operations/actions/work-order`);
    expect(workOrder.request.body).toEqual({ summary: 'Create work order.' });
    workOrder.flush({});

    const vendor = http.expectOne(`${apiBaseUrl}/maintenanceRequests/request-1/operations/actions/vendor-assignment`);
    expect(vendor.request.body).toEqual({ vendorName: 'Licensed Plumber' });
    vendor.flush({});

    const tenant = http.expectOne(`${apiBaseUrl}/maintenanceRequests/request-1/operations/actions/tenant-notification`);
    expect(tenant.request.body).toEqual({ message: 'Vendor assigned.' });
    tenant.flush({});

    const note = http.expectOne(`${apiBaseUrl}/maintenanceRequests/request-1/operations/actions/internal-note`);
    expect(note.request.body).toEqual({ note: 'Tenant prefers morning access.' });
    note.flush({});
  });

  it('posts resolution feedback and requests dataset endpoints', () => {
    const service = TestBed.inject(PropOpsApiService);
    const http = TestBed.inject(HttpTestingController);
    const payload = {
      finalResolution: 'Licensed plumber repaired the sink leak.',
      correctedCategory: 'Plumbing' as const,
      correctedPriority: 'High' as const,
      finalTenantResponse: 'The leak has been repaired.',
      dispatchOutcome: 'Completed' as const,
      resolutionNotes: 'Tenant confirmed completion.',
      excludeFromTraining: false,
      exclusionReason: ''
    };

    service.submitResolutionFeedback('request-1', payload).subscribe();
    service.getFineTuningCandidates().subscribe();
    service.exportFineTuningDataset().subscribe();

    const feedback = http.expectOne(`${apiBaseUrl}/maintenanceRequests/request-1/operations/resolution-feedback`);
    expect(feedback.request.method).toBe('POST');
    expect(feedback.request.body).toEqual(payload);
    feedback.flush({});

    const candidates = http.expectOne(`${apiBaseUrl}/learning/dataset/candidates`);
    expect(candidates.request.method).toBe('GET');
    candidates.flush([]);

    const exportRequest = http.expectOne(`${apiBaseUrl}/learning/dataset/export`);
    expect(exportRequest.request.method).toBe('GET');
    exportRequest.flush({});
  });

  it('posts email intake payloads to the API', () => {
    const service = TestBed.inject(PropOpsApiService);
    const http = TestBed.inject(HttpTestingController);
    const payload = {
      submitterName: 'Ava Thompson',
      emailAddress: 'ava.thompson@example.com',
      subject: 'Leaking sink',
      messageBody: 'Sink leaking under cabinet.',
      phoneNumber: '0412200100',
      propertyHint: '',
      unitHint: '',
      sourceReference: 'EMAIL-1',
      receivedAtUtc: null
    };

    service.ingestEmail(payload).subscribe();

    const request = http.expectOne(`${apiBaseUrl}/intakeConnectors/email`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(payload);
    request.flush({});
  });

  it('posts SMS/chat intake payloads to the API', () => {
    const service = TestBed.inject(PropOpsApiService);
    const http = TestBed.inject(HttpTestingController);
    const payload = {
      submitterName: 'Mia Patel',
      phoneNumber: '0412200102',
      messageBody: 'Door lock is jammed.',
      emailAddress: '',
      propertyHint: '',
      unitHint: '',
      sourceReference: 'SMS-1',
      receivedAtUtc: null
    };

    service.ingestSmsChat(payload).subscribe();

    const request = http.expectOne(`${apiBaseUrl}/intakeConnectors/sms-chat`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(payload);
    request.flush({});
  });

  it('posts phone-note intake payloads to the API', () => {
    const service = TestBed.inject(PropOpsApiService);
    const http = TestBed.inject(HttpTestingController);
    const payload = {
      submitterName: 'Noah Williams',
      phoneNumber: '0412200103',
      emailAddress: 'noah.williams@example.com',
      note: 'Dishwasher stopped mid-cycle.',
      propertyHint: '',
      unitHint: '',
      sourceReference: 'CALL-1',
      receivedAtUtc: null
    };

    service.ingestPhoneNote(payload).subscribe();

    const request = http.expectOne(`${apiBaseUrl}/intakeConnectors/phone-note`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(payload);
    request.flush({});
  });
});
