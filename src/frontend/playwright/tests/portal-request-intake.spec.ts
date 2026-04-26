import { expect, test } from '@playwright/test';

import { apiBaseUrl, authHeaders, loginAsManager } from './helpers';

test('manager can create a maintenance request from the portal', async ({ page, request }) => {
  await page.goto('/login');

  await page.getByRole('button', { name: 'Sign in to portal' }).click();
  await expect(page).toHaveURL(/\/(workspace|overview)$/);

  await page.getByRole('link', { name: 'New request' }).click();
  await expect(page).toHaveURL(/\/intake$/);

  const uniqueSuffix = Date.now();

  await page.getByLabel('Resident name').fill('Jordan Blake');
  await page.getByLabel('Email address').fill('manager@propops.local');
  await page.getByLabel('Phone number').fill('0412200100');
  await page.getByLabel('Property').fill('Harbour View Residences');
  await page.getByLabel('Unit').fill('22A');
  await page
    .getByLabel('Description')
    .fill(`Bathroom tap is leaking constantly and water is pooling near the vanity. Ref ${uniqueSuffix}.`);

  await page.getByRole('button', { name: 'Create maintenance request' }).click();

  const successMessage = page.locator('.form-message--success');
  await expect(successMessage).toBeVisible();

  const referenceNumber = await successMessage.locator('strong').innerText();
  expect(referenceNumber).toMatch(/^MR-/);

  const token = await loginAsManager(request);
  const queueResponse = await request.get(`${apiBaseUrl}/api/maintenanceRequests`, {
    headers: authHeaders(token)
  });

  expect(queueResponse.ok()).toBeTruthy();

  const requests = (await queueResponse.json()) as Array<{ referenceNumber: string; description: string }>;
  expect(
    requests.some(
      (maintenanceRequest) =>
        maintenanceRequest.referenceNumber === referenceNumber &&
        maintenanceRequest.description.includes(`Ref ${uniqueSuffix}`)
    )
  ).toBe(true);
});
