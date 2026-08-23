import { expect, test } from "@playwright/test";
import { companyCredentials } from "../../helpers/env";

test.describe("Kimlik doğrulama (gerçek /auth uçları)", () => {
  test("hatalı parola → 401 + unauthorized kodu", async ({ request }) => {
    const response = await request.post("/api/v1/auth/login", {
      data: { email: "e2e-yok@crewbase.test", password: "yanlis-parola-1A!" },
    });
    expect(response.status()).toBe(401);
    const body = await response.json();
    expect(body.code ?? "").toBe("unauthorized");
  });

  test("başarılı login → dashboard görünür", async ({ page }) => {
    const credentials = companyCredentials();
    await page.goto("/giris");
    await page.getByLabel("E-posta").fill(credentials.email);
    await page.getByLabel("Parola").fill(credentials.password);
    await page.getByRole("button", { name: "Giriş yap" }).click();
    await expect(page).toHaveURL(/\/$/);
    await expect(page.getByRole("heading", { name: "Genel Bakış" })).toBeVisible();
  });

  test("çıkış → korumalı sayfa girişe yönlendirir", async ({ page }) => {
    const credentials = companyCredentials();
    await page.goto("/giris");
    await page.getByLabel("E-posta").fill(credentials.email);
    await page.getByLabel("Parola").fill(credentials.password);
    await page.getByRole("button", { name: "Giriş yap" }).click();
    await expect(page.getByRole("heading", { name: "Genel Bakış" })).toBeVisible();

    await page.getByRole("button", { name: "Çıkış yap" }).first().click();
    await page.getByRole("button", { name: "Çıkış yap" }).last().click();
    await expect(page).toHaveURL(/\/giris$/);

    await page.goto("/");
    await expect(page).toHaveURL(/\/giris$/);
  });
});
