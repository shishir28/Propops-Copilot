import { expect, test } from '@playwright/test';

import { apiBaseUrl, authHeaders, loginAsManager } from './helpers';

test.describe('Level 2 rules and knowledge API', () => {
  test('returns AI contracts for maintenance triage', async ({ request }) => {
    const token = await loginAsManager(request);

    const response = await request.get(`${apiBaseUrl}/api/ai/maintenance-triage/contracts`, {
      headers: authHeaders(token)
    });

    expect(response.status()).toBe(200);

    const body = await response.json();

    expect(body.rulesVersion).toBe('2026.04-level2');
    expect(body.inputContractSchema.properties.request_id.type).toBe('string');
    expect(body.outputContractSchema.properties.vendor_type.type).toBe('string');
    expect(body.outputContractTemplate.dispatchDecision).toContain('Queue for standard triage');
  });

  test('prepares retrieved rules, property notes, and triage contract template for an emergency request', async ({
    request
  }) => {
    const token = await loginAsManager(request);

    const createResponse = await request.post(`${apiBaseUrl}/api/maintenanceRequests`, {
      headers: authHeaders(token),
      data: {
        submitterName: 'Jordan Blake',
        emailAddress: 'manager@propops.local',
        phoneNumber: '0412200100',
        propertyName: 'Harbour View Residences',
        unitNumber: '22A',
        description: 'Front door lock is jammed and the apartment cannot secure properly. Ref ' + Date.now(),
        category: 'Security',
        priority: 'Emergency',
        channel: 'Portal'
      }
    });

    expect(createResponse.status()).toBe(201);
    const createdRequest = await createResponse.json();

    const prepareResponse = await request.post(`${apiBaseUrl}/api/ai/maintenance-triage/prepare`, {
      headers: authHeaders(token),
      data: {
        maintenanceRequestId: createdRequest.id
      }
    });

    expect(prepareResponse.status()).toBe(200);

    const body = await prepareResponse.json();
    const sourceTypes = body.knowledgeItems.map((item: { sourceType: string }) => item.sourceType);

    expect(body.rulesVersion).toBe('2026.04-level2');
    expect(body.inputContract.referenceNumber).toBe(createdRequest.referenceNumber);
    expect(body.outputContractTemplate.vendorType).toBe('Emergency Locksmith / Security Contractor');
    expect(body.outputContractTemplate.dispatchDecision).toContain('Dispatch immediately');
    expect(sourceTypes).toEqual(
      expect.arrayContaining(['MaintenanceSop', 'VendorRule', 'PropertyNote', 'EmergencyPolicy'])
    );
  });
});
