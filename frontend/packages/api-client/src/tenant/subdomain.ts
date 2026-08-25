/**
 * Subdomain helpers mirroring backend TenantResolver.FromHost (verified):
 * `xyz.faturebase.com` → "xyz"; dev browsers resolve `*.localhost` to 127.0.0.1.
 * Bare localhost / IP / "www" / multi-level → no subdomain.
 */
export const BASE_DOMAIN = "faturebase.com";

export function subdomainFromHost(host: string | null | undefined): string | null {
  if (!host) return null;
  const hostName = host.split(":")[0]?.toLowerCase() ?? "";
  if (hostName === "") return null;

  let candidate: string | null = null;
  if (hostName.endsWith(`.${BASE_DOMAIN}`)) {
    candidate = hostName.slice(0, -(BASE_DOMAIN.length + 1));
  } else if (hostName.endsWith(".localhost")) {
    candidate = hostName.slice(0, -".localhost".length);
  }

  if (candidate === null || candidate === "" || candidate.includes(".") || candidate === "www") {
    return null;
  }
  return candidate;
}

export function currentSubdomain(location: {
  readonly hostname: string;
} = globalThis.location): string | null {
  return subdomainFromHost(location.hostname);
}
