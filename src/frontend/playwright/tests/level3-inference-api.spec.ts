import { expect, test } from '@playwright/test';

import { apiBaseUrl, authHeaders, loginAsManager } from './helpers';

test.describe('Level 3 baseline inference and guardrails API', () => {
  test('returns a baseline inference decision with guardrail metadata', async ({ request }) => {
    const token = await loginAsManager(request);

    const createResponse = await request.post(`${apiBaseUrl}/api/maintenanceRequests`, {
      headers: authHeaders(token),
      data: {
        submitterName: 'Jordan Blake',
        emailAddress: 'manager@propops.local',
        phoneNumber: '0412200100',
        propertyName: 'Harbour View Residences',
        unitNumber: '22A',
        description: 'Kitchen sink pipe is leaking heavily under the cabinet. Ref ' + Date.now(),
        category: 'Plumbing',
        priority: 'High',
        channel: 'Portal'
      }
    });

    expect(createResponse.status()).toBe(201);
    const createdRequest = await createResponse.json();

    const inferResponse = await request.post(`${apiBaseUrl}/api/ai/maintenance-triage/infer`, {
      headers: authHeaders(token),
      data: {
        maintenanceRequestId: createdRequest.id
      }
    });

    expect(inferResponse.status()).toBe(200);

    const body = await inferResponse.json();

    expect(body.rulesVersion).toBe('2026.04-level2');
    expect(body.outputContract.category).toBe('Plumbing');
    expect(body.outputContract.priority).toBe('High');
    expect(body.inferenceMetadata.providerMode).toBe('heuristic');
    expect(body.guardrails.schemaValid).toBe(true);
    expect(body.guardrails.requiresHumanReview).toBe(false);
    expect(body.guardrails.confidenceScore).toBeGreaterThanOrEqual(body.guardrails.confidenceThreshold);
  });

  test('falls back to human review when inference confidence is too low', async ({ request }) => {
    const token = await loginAsManager(request);

    const createResponse = await request.post(`${apiBaseUrl}/api/maintenanceRequests`, {
      headers: authHeaders(token),
      data: {
        submitterName: 'Jordan Blake',
        emailAddress: 'manager@propops.local',
        phoneNumber: '0412200100',
        propertyName: 'Harbour View Residences',
        unitNumber: '22A',
        description: 'Something is broken and there is a weird issue somewhere. Ref ' + Date.now(),
        category: 'General',
        priority: 'Normal',
        channel: 'Portal'
      }
    });

    expect(createResponse.status()).toBe(201);
    const createdRequest = await createResponse.json();

    const inferResponse = await request.post(`${apiBaseUrl}/api/ai/maintenance-triage/infer`, {
      headers: authHeaders(token),
      data: {
        maintenanceRequestId: createdRequest.id
      }
    });

    expect(inferResponse.status()).toBe(200);

    const body = await inferResponse.json();
    const issueCodes = body.guardrails.issues.map((issue: { code: string }) => issue.code);

    expect(body.guardrails.requiresHumanReview).toBe(true);
    expect(body.guardrails.fallbackApplied).toBe(true);
    expect(issueCodes).toContain('LOW_CONFIDENCE');
    expect(body.outputContract.dispatchDecision).toContain('staff triage review');
  });
});
