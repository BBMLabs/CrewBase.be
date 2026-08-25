import { expect, test } from "@playwright/test";
import { memberSubdomain } from "../../helpers/env";

test.describe("Genel site — misafir deneyimi", () => {
  let club = "";

  test.beforeAll(() => {
    club = memberSubdomain();
  });

  test("kulüp sayfası bilgi + rezervasyon sihirbazı gösterir", async ({ page }) => {
    await page.goto(`/?club=${club}`);
    await expect(page.getByRole("heading", { level: 1 })).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText("Slot seç")).toBeVisible();
  });

  test("bilinmeyen kulüp → açık hata durumu", async ({ page }) => {
    await page.goto("/?club=e2e-olmayan-kulup");
    await expect(page.getByRole("alert").first()).toBeVisible({ timeout: 10_000 });
  });
});
