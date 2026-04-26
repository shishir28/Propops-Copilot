import { expect, test } from '@playwright/test';

import { apiBaseUrl, authHeaders, loginAsManager } from './helpers';

test.describe('Level 1 intake and normalization API', () => {
  test('ingests email into a standardized payload and creates a maintenance request', async ({
    request
  }) => {
    const token = await loginAsManager(request);
    const sourceReference = `EMAIL-${Date.now()}`;

    const response = await request.post(`${apiBaseUrl}/api/intakeConnectors/email`, {
      headers: authHeaders(token),
      data: {
        submitterName: 'Ava Thompson',
        emailAddress: 'ava.thompson@example.com',
        subject: 'Leaking sink in 12B',
        messageBody:
          'Hi team, the kitchen sink is leaking heavily under the cabinet and water is spreading across the floor.',
        phoneNumber: '0412200100',
        propertyHint: '',
        unitHint: '',
        sourceReference,
        receivedAtUtc: '2026-04-25T11:30:00Z'
      }
    });

    expect(response.status()).toBe(201);

    const body = await response.json();

    expect(body.submission.standardizedPayload.channel).toBe('Email');
    expect(body.submission.standardizedPayload.sourceReference).toBe(sourceReference);
    expect(body.submission.standardizedPayload.propertyName).toBe('Harbour View Residences');
    expect(body.submission.standardizedPayload.unitNumber).toBe('12B');
    expect(body.submission.standardizedPayload.metadataMatched).toBe(true);
    expect(body.submission.standardizedPayload.isAfterHours).toBe(true);
    expect(body.submission.standardizedPayload.category).toBe('Plumbing');
    expect(body.submission.standardizedPayload.priority).toBe('High');
    expect(body.maintenanceRequest.referenceNumber).toMatch(/^MR-/);

    const queueResponse = await request.get(`${apiBaseUrl}/api/maintenanceRequests`, {
      headers: authHeaders(token)
    });
    expect(queueResponse.ok()).toBeTruthy();
    const requests = (await queueResponse.json()) as Array<{ referenceNumber: string }>;
    expect(
      requests.some((maintenanceRequest) => maintenanceRequest.referenceNumber === body.maintenanceRequest.referenceNumber)
    ).toBe(true);
  });

  test('ingests sms chat and classifies security emergencies', async ({ request }) => {
    const token = await loginAsManager(request);
    const sourceReference = `SMS-${Date.now()}`;

    const response = await request.post(`${apiBaseUrl}/api/intakeConnectors/sms-chat`, {
      headers: authHeaders(token),
      data: {
        submitterName: 'Mia Patel',
        phoneNumber: '0412200102',
        messageBody: 'Front door lock is jammed and I cannot secure the property tonight.',
        emailAddress: '',
        propertyHint: '',
        unitHint: '',
        sourceReference,
        receivedAtUtc: '2026-04-25T11:30:00Z'
      }
    });

    expect(response.status()).toBe(201);

    const body = await response.json();

    expect(body.submission.standardizedPayload.channel).toBe('SmsChat');
    expect(body.submission.standardizedPayload.propertyName).toBe('Elm Street Townhomes');
    expect(body.submission.standardizedPayload.unitNumber).toBe('3');
    expect(body.submission.standardizedPayload.category).toBe('Security');
    expect(body.submission.standardizedPayload.priority).toBe('Emergency');
    expect(body.submission.standardizedPayload.metadataMatched).toBe(true);
  });

  test('ingests phone notes into queue-ready maintenance requests', async ({ request }) => {
    const token = await loginAsManager(request);
    const sourceReference = `CALL-${Date.now()}`;

    const response = await request.post(`${apiBaseUrl}/api/intakeConnectors/phone-note`, {
      headers: authHeaders(token),
      data: {
        submitterName: 'Noah Williams',
        phoneNumber: '0412200103',
        emailAddress: 'noah.williams@example.com',
        note: 'Resident called to report the dishwasher stopped mid-cycle and left water in the base tray.',
        propertyHint: '',
        unitHint: '',
        sourceReference,
        receivedAtUtc: '2026-04-25T11:30:00Z'
      }
    });

    expect(response.status()).toBe(201);

    const body = await response.json();

    expect(body.submission.standardizedPayload.channel).toBe('PhoneNote');
    expect(body.submission.standardizedPayload.propertyName).toBe('Cityscape Lofts');
    expect(body.submission.standardizedPayload.unitNumber).toBe('19D');
    expect(body.submission.standardizedPayload.category).toBe('Appliances');
    expect(body.submission.standardizedPayload.priority).toBe('Normal');
    expect(body.maintenanceRequest.channel).toBe('PhoneNote');
  });

  test('rejects unknown contacts without property metadata', async ({ request }) => {
    const token = await loginAsManager(request);

    const response = await request.post(`${apiBaseUrl}/api/intakeConnectors/email`, {
      headers: authHeaders(token),
      data: {
        submitterName: 'Unknown Person',
        emailAddress: 'unknown.person@example.com',
        subject: 'Need help',
        messageBody: 'Something is broken in the apartment and I need a hand.',
        phoneNumber: '',
        propertyHint: '',
        unitHint: '',
        sourceReference: `EMAIL-FAIL-${Date.now()}`,
        receivedAtUtc: '2026-04-25T11:30:00Z'
      }
    });

    expect(response.status()).toBe(400);

    const body = await response.json();
    expect(body.title).toBe('Intake preprocessing failed');
    expect(body.detail).toContain('Unable to resolve property metadata');
  });
});
