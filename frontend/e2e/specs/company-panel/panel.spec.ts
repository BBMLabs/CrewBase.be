import { expect, test } from "@playwright/test";
import { companyCredentials, loginCompany } from "../../helpers/env";

test.describe("Firma paneli — kritik akışlar", () => {
  let accessToken: string | undefined;

  test.beforeAll(async () => {
    const tokens = await loginCompany(companyCredentials());
    accessToken = tokens.accessToken;
  });

  test("dashboard gerçek istatistikleri gösterir", async ({ page }) => {
    test.skip(accessToken === undefined, "CompanyAdmin girişi başarısız");
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Genel Bakış" })).toBeVisible();
    await expect(page.getByText("Bugünkü randevu")).toBeVisible();
  });

  test("üye oluşturma → listede görünür (backend'de üye silme ucu yoktur; test verisi kalır)", async ({ page }) => {
    test.skip(accessToken === undefined, "CompanyAdmin girişi başarısız");
    await page.goto("/uyeler");
    const unique = `E2E Üye ${Date.now()}`;
    await page.getByRole("button", { name: "Üye ekle" }).click();
    await page.getByLabel("Ad Soyad").fill(unique);
    await page.getByLabel("Telefon").fill(`0555${Math.floor(1000000 + Math.random() * 8999999)}`);
    await page.getByRole("button", { name: "Kaydet", exact: true }).click();
    await expect(page.getByRole("cell", { name: unique })).toBeVisible();
  });

  test("gün programı tarih kapsamlı sorgu döner", async ({ request }) => {
    test.skip(accessToken === undefined, "CompanyAdmin girişi başarısız");
    const today = new Date();
    const date = `${today.getFullYear()}-${`${today.getMonth() + 1}`.padStart(2, "0")}-${`${today.getDate()}`.padStart(2, "0")}`;
    const response = await request.get(`/api/v1/company/appointments?date=${date}`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    expect(response.status()).toBe(200);
    const body = await response.json();
    expect(body.success).toBe(true);
    expect(Array.isArray(body.data)).toBe(true);
  });
});
