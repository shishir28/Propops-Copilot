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
