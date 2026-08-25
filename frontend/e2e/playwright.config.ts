import { defineConfig } from "@playwright/test";

/**
 * CrewBase E2E — REAL BACKEND ONLY.
 * No mocks: browser → React → api-client → ASP.NET API → PostgreSQL.
 *
 * Environment:
 *   E2E_BASE_URL   frontend under test   (default http://localhost:5173 — company panel)
 *   E2E_API_URL    backend base URL      (default http://localhost:5283)
 *
 * The global-setup health probe FAILS FAST when the backend is not running —
 * we never silently fall back to stubs.
 */
const API_URL = process.env["E2E_API_URL"] ?? "http://localhost:5283";
const BASE_URL = process.env["E2E_BASE_URL"] ?? "http://localhost:5173";

export default defineConfig({
  testDir: "./specs",
  globalSetup: "./global-setup.ts",
  timeout: 30_000,
  expect: { timeout: 8_000 },
  fullyParallel: false,
  retries: process.env["CI"] === "true" ? 1 : 0,
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL: BASE_URL,
    trace: "retain-on-failure",
    locale: "tr-TR",
  },
  projects: [
    { name: "chromium", use: { browserName: "chromium" } },
  ],
});
