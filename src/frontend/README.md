# PropOps Copilot Frontend

This Angular application provides the first user-facing experience for PropOps Copilot.

## Available views

- **Overview:** operational KPIs and the recent maintenance queue
- **New request:** structured maintenance intake form

## Local development

```bash
npm install
npm start
```

The app runs on `http://localhost:4200` in local development and expects the API on `http://localhost:8095`.

## Portal sign-in

Seeded demo users:

- `manager@propops.local` / `PropOps!Manager1`
- `dispatcher@propops.local` / `PropOps!Dispatch1`
- `tenant@propops.local` / `PropOps!Tenant1`
- `owner@propops.local` / `PropOps!Owner1`
- `vendor@propops.local` / `PropOps!Vendor1`

Protected routes redirect unauthenticated users to the login page, and role-aware navigation keeps staff-only screens hidden from other user types.

## Build

```bash
npm run build
```

The production build output is used by the Docker image and served through Nginx.

## Tests

```bash
npm test
```
