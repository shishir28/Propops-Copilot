import { expect, type APIRequestContext } from '@playwright/test';

export const apiBaseUrl = process.env.PLAYWRIGHT_API_URL ?? 'http://127.0.0.1:8095';

export async function loginAsManager(request: APIRequestContext): Promise<string> {
  const response = await request.post(`${apiBaseUrl}/api/auth/login`, {
    data: {
      email: 'manager@propops.local',
      password: 'PropOps!Manager1'
    }
  });

  expect(response.ok()).toBeTruthy();

  const body = (await response.json()) as { accessToken: string };
  return body.accessToken;
}

export function authHeaders(token: string): Record<string, string> {
  return {
    Authorization: `Bearer ${token}`
  };
}
