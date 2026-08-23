import { chromium } from "@playwright/test";

const browser = await chromium.launch();
const context = await browser.newContext({ viewport: { width: 390, height: 844 } });
const page = await context.newPage();
await page.goto("http://localhost:5175/", { waitUntil: "domcontentloaded" });
await page.waitForURL(/\/giris/);

// guided club entry if present
const clubInput = page.getByLabel("Kulüp alt alan adı");
if (await clubInput.count()) {
  await clubInput.fill("e2e-deniz-kulubu");
  await page.getByRole("button", { name: "Git" }).click();
}
await page.getByRole("link", { name: "Üye olun" }).click();
await page.waitForURL(/\/kayit/);
await page.waitForTimeout(1500); // allow consents query

const total = await page.locator("fieldset input[type=checkbox]").count();
const scoped = await page.locator('fieldset:has(legend:text-is("Beyanlar")) input[type="checkbox"]').count();
const legends = await page.locator("fieldset legend").allTextContents();
const labels = await page.locator('fieldset:has(legend:text-is("Beyanlar")) label span').allTextContents();
console.log(JSON.stringify({ total, scoped, legends, labelSample: labels.slice(0, 8) }, null, 2));
await browser.close();
