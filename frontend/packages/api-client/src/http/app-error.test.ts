import { describe, expect, it } from "vitest";
import { AppError, errorFromResponse } from "./app-error";

function jsonResponse(status: number, body: unknown, contentType = "application/json"): Response {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": contentType } });
}

describe("errorFromResponse", () => {
  it("maps envelope failure shape", () => {
    const error = errorFromResponse(
      jsonResponse(404, { success: false, data: null, message: "Firma bulunamadı.", code: "company_not_found" }),
      { success: false, data: null, message: "Firma bulunamadı.", code: "company_not_found" },
      "fallback",
    );
    expect(error.kind).toBe("envelope");
    expect(error.code).toBe("company_not_found");
    expect(error.status).toBe(404);
    expect(error.serverMessage).toBe("Firma bulunamadı.");
  });

  it("maps ProblemDetails with field errors", () => {
    const body = {
      type: "https://errors.rowingclub.dev/validation_error",
      title: "Validation failed",
      status: 400,
      detail: "Doğrulama başarısız.",
      traceId: "abc123",
      code: "validation_error",
      errors: { fullName: ["Zorunludur."] },
    };
    const error = errorFromResponse(jsonResponse(400, body, "application/problem+json"), body, "fallback");
    expect(error.kind).toBe("problem");
    expect(error.code).toBe("validation_error");
    expect(error.traceId).toBe("abc123");
    expect(error.fieldErrors?.fullName).toEqual(["Zorunludur."]);
  });

  it("falls back to generic problem for non-JSON bodies", () => {
    const res = new Response("Böyle bir firma sitesi yok.", { status: 404 });
    const error = errorFromResponse(res, null, "fallback");
    expect(error.kind).toBe("problem");
    expect(error.status).toBe(404);
  });
});

describe("AppError flags", () => {
  it("detects unauthorized and forbidden", () => {
    const err = new AppError({ kind: "problem", message: "x", status: 401, code: "unauthorized" });
    expect(err.isUnauthorized).toBe(true);
    expect(err.isForbidden).toBe(false);
    const forbidden = new AppError({ kind: "problem", message: "x", status: 403, code: "forbidden" });
    expect(forbidden.isForbidden).toBe(true);
  });
});
