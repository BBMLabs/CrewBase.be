import { chromium } from "@playwright/test";
import { discoverPorts } from "./ports.mjs";

const browser = await chromium.launch();
const PORTS = await discoverPorts(browser);
console.log("PORTS:", JSON.stringify(PORTS));

const context = await browser.newContext({ viewport: { width: 390, height: 844 } });
const page = await context.newPage();
page.on("console", (m) => { if (m.type() === "error") console.log("CONSOLE_ERR:", m.text().slice(0, 150)); });

await page.goto(`${PORTS.portal}/`, { waitUntil: "domcontentloaded" });
await page.waitForURL(/\/giris/);

// Guided club entry yalnızca bağlam yokken görünür
const clubInput = page.getByLabel("Kulüp alt alan adı");
if (await clubInput.count()) {
  await clubInput.fill("e2e-deniz-kulubu");
  await page.getByRole("button", { name: "Git" }).click();
} else {
  console.log("kulüp bağlamı zaten çözümlü (host/persisted)");
}
await page.getByRole("link", { name: "Üye olun" }).click();
await page.waitForURL(/\/kayit/);
await page.waitForTimeout(1200);

const stamp = Date.now() % 1000000;
await page.getByLabel("Ad Soyad").fill("E2E Test Üyesi");
await page.getByLabel("Telefon").fill(`0555${stamp}`);
await page.locator('input[type="email"]').first().fill(`uye${stamp}@crewbase.test`);
await page.locator('input[type="password"]').fill("UyeParola-2026!");

const boxes = page.locator('fieldset:has(legend:text-is("Beyanlar")) input[type="checkbox"]');
const n = await boxes.count();
console.log("checkbox sayısı:", n);
for (let i = 0; i < n; i += 1) {
  const box = boxes.nth(i);
  if (!(await box.isChecked())) await box.check();
}
const states = [];
for (let i = 0; i < n; i += 1) states.push(await boxes.nth(i).isChecked());
console.log("işaretli durumları:", states);

await page.getByRole("button", { name: "Üye ol" }).click();
await page.waitForTimeout(2500);
console.log("URL:", page.url());
const alerts = await page.locator('[role="alert"]').allTextContents().catch(() => []);
console.log("ALERTS:", JSON.stringify(alerts));
const onHome = await page.locator('section[aria-label="Tarih ve tekne sınıfı"]').count();
console.log("home section:", onHome);
await page.screenshot({ path: "e2e/screenshots/debug-register.png", fullPage: true });
await context.close();
await browser.close();
