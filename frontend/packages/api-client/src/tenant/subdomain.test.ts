import { describe, expect, it } from "vitest";
import { subdomainFromHost } from "./subdomain";

describe("subdomainFromHost (mirrors TenantResolver.FromHost)", () => {
  it("extracts from *.faturebase.com", () => {
    expect(subdomainFromHost("ornek-kulup.faturebase.com")).toBe("ornek-kulup");
    expect(subdomainFromHost("ornek.faturebase.com:8080")).toBe("ornek");
  });

  it("extracts from *.localhost (dev)", () => {
    expect(subdomainFromHost("kulup.localhost")).toBe("kulup");
  });

  it("returns null for bare hosts, www and multi-level", () => {
    expect(subdomainFromHost("localhost")).toBeNull();
    expect(subdomainFromHost("www.faturebase.com")).toBeNull();
    expect(subdomainFromHost("a.b.faturebase.com")).toBeNull();
    expect(subdomainFromHost("127.0.0.1")).toBeNull();
    expect(subdomainFromHost(null)).toBeNull();
    expect(subdomainFromHost("")).toBeNull();
  });
});
