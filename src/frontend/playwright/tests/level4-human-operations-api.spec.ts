import { expect, test } from '@playwright/test';

import { apiBaseUrl, authHeaders, loginAsManager } from './helpers';

test.describe('Level 4 human-in-the-loop operations API', () => {
  test('reviews AI output and logs operational actions', async ({ request }) => {
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
    const inference = await inferResponse.json();

    const reviewResponse = await request.post(
      `${apiBaseUrl}/api/maintenanceRequests/${createdRequest.id}/operations/triage-review`,
      {
        headers: authHeaders(token),
        data: {
          aiOutput: inference.outputContract,
          guardrails: inference.guardrails,
          category: 'Plumbing',
          priority: 'Emergency',
          vendorType: 'Emergency Licensed Plumber',
          dispatchDecision: 'Dispatch immediately to the on-call plumber and confirm tenant access.',
          internalSummary: 'Emergency Plumbing issue at Harbour View Residences / 22A.',
          tenantResponseDraft: 'Thanks for the update. We are dispatching an emergency plumber now.'
        }
      }
    );
    expect(reviewResponse.status()).toBe(200);
    let detail = await reviewResponse.json();
    expect(detail.latestReview.status).toBe('Edited');
    expect(detail.request.priority).toBe('Emergency');
    expect(detail.request.status).toBe('InReview');

    const workOrderResponse = await request.post(
      `${apiBaseUrl}/api/maintenanceRequests/${createdRequest.id}/operations/actions/work-order`,
      {
        headers: authHeaders(token),
        data: {
          summary: detail.latestReview.finalInternalSummary
        }
      }
    );
    expect(workOrderResponse.status()).toBe(200);
    detail = await workOrderResponse.json();
    expect(detail.request.status).toBe('Scheduled');
    expect(detail.actions[0].actionType).toBe('WorkOrderCreated');
    expect(detail.actions[0].externalReference).toContain('WO-');

    const vendorResponse = await request.post(
      `${apiBaseUrl}/api/maintenanceRequests/${createdRequest.id}/operations/actions/vendor-assignment`,
      {
        headers: authHeaders(token),
        data: {
          vendorName: 'Emergency Licensed Plumber'
        }
      }
    );
    expect(vendorResponse.status()).toBe(200);

    const tenantResponse = await request.post(
      `${apiBaseUrl}/api/maintenanceRequests/${createdRequest.id}/operations/actions/tenant-notification`,
      {
        headers: authHeaders(token),
        data: {
          message: 'An emergency plumber has been assigned.'
        }
      }
    );
    expect(tenantResponse.status()).toBe(200);

    const noteResponse = await request.post(
      `${apiBaseUrl}/api/maintenanceRequests/${createdRequest.id}/operations/actions/internal-note`,
      {
        headers: authHeaders(token),
        data: {
          note: 'Tenant confirmed access after 6 PM.'
        }
      }
    );
    expect(noteResponse.status()).toBe(200);
    detail = await noteResponse.json();

    const actionTypes = detail.actions.map((action: { actionType: string }) => action.actionType);
    expect(actionTypes).toContain('WorkOrderCreated');
    expect(actionTypes).toContain('VendorAssigned');
    expect(actionTypes).toContain('TenantNotified');
    expect(actionTypes).toContain('InternalNoteLogged');

    const feedbackResponse = await request.post(
      `${apiBaseUrl}/api/maintenanceRequests/${createdRequest.id}/operations/resolution-feedback`,
      {
        headers: authHeaders(token),
        data: {
          finalResolution: 'Emergency plumber replaced the failed sink trap and confirmed the leak is resolved.',
          correctedCategory: 'Plumbing',
          correctedPriority: 'Emergency',
          finalTenantResponse: 'The emergency plumber has repaired the leak and confirmed the area is safe.',
          dispatchOutcome: 'Completed',
          resolutionNotes: 'Tenant confirmed completion after vendor visit.',
          excludeFromTraining: false,
          exclusionReason: ''
        }
      }
    );
    expect(feedbackResponse.status()).toBe(200);
    detail = await feedbackResponse.json();
    expect(detail.latestFeedback.dispatchOutcome).toBe('Completed');
    expect(detail.request.status).toBe('Completed');

    const candidatesResponse = await request.get(`${apiBaseUrl}/api/learning/dataset/candidates`, {
      headers: authHeaders(token)
    });
    expect(candidatesResponse.status()).toBe(200);
    const candidates = await candidatesResponse.json();
    expect(candidates.some((candidate: { maintenanceRequestId: string }) => candidate.maintenanceRequestId === createdRequest.id)).toBeTruthy();

    const exportResponse = await request.get(`${apiBaseUrl}/api/learning/dataset/export`, {
      headers: authHeaders(token)
    });
    expect(exportResponse.status()).toBe(200);
    const exportBody = await exportResponse.json();
    expect(exportBody.exampleCount).toBeGreaterThan(0);
  });
});
