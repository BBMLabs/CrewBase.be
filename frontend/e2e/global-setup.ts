import { API_URL, BASE_URL, requireBackend } from "./helpers/env";

export default async function globalSetup(): Promise<void> {
  // Fail fast with a clear message when infrastructure is missing.
  await requireBackend(API_URL);
  console.info(`E2E hedefi → frontend: ${BASE_URL} · api: ${API_URL}`);
}
