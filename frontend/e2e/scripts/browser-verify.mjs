/**
 * Real-browser runtime verification for all three CrewBase apps.
 * Drives actual dev servers + real backend. Captures console errors, failed
 * requests and screenshots. Prints a JSON result summary at the end.
 *
 * Prereqs: API :5283 healthy · Vite :5173/:5174/:5175 running · seeded club
 * (env SEED_SUBDOMAIN/SEED_ADMIN_EMAIL/SEED_ADMIN_PASSWORD or defaults below).
 */
import { chromium } from "@playwright/test";
import { mkdirSync } from "node:fs";

const SHOTS = "e2e/screenshots";
mkdirSync(SHOTS, { recursive: true });

const SUB = process.env.SEED_SUBDOMAIN ?? "e2e-deniz-kulubu";
const ADMIN_EMAIL = process.env.SEED_ADMIN_EMAIL ?? "e2e-admin@crewbase.test";
const ADMIN_PASSWORD = process.env.SEED_ADMIN_PASSWORD ?? "E2eParola-2026!x";

const results = [];
let PORTS = {};


/** Discover which port serves which app by document title (Vite may shift ports). */
async function discoverPorts(browser) {
  const map = {};
  for (const port of [5173, 5174, 5175, 5176, 5177]) {
    const { context, page } = await newPage(browser);
    try {
      await page.goto(`http://localhost:${port}/`, { waitUntil: "domcontentloaded", timeout: 8000 });
      const title = await page.title();
      if (title.includes("Kulüp Paneli")) map.panel = `http://localhost:${port}`;
      else if (title.includes("Üye Portalı")) map.portal = `http://localhost:${port}`;
      else if (title.includes("Rezervasyon")) map.public = `http://localhost:${port}`;
    } catch {
      /* nothing on this port */
    }
    await context.close();
  }
  if (!map.panel || !map.portal || !map.public) {
    throw new Error(`Uygulama portları keşfedilemedi: ${JSON.stringify(map)} — dev serverlar çalışıyor mu?`);
  }
  console.error(`ports → ${JSON.stringify(map)}`);
  return map;
}

function note(app, route, result, details = "") {
  results.push({ app, route, result, details });
  const icon = result === "PASS" ? "✅" : result === "PARTIAL" ? "🟡" : "❌";
  console.error(`${icon} [${app}] ${route} ${result} ${details}`);
}

function attachCollector(page, bucket) {
  page.on("pageerror", (err) => bucket.push(`pageerror: ${err.message}`));
  page.on("console", (msg) => {
    if (msg.type() === "error") {
      const text = msg.text();
      if (text.includes("favicon") || text.includes("[vite]")) return;
      bucket.push(`console.error: ${text.slice(0, 200)}`);
    }
  });
  page.on("response", (res) => {
    if (res.status() >= 500) bucket.push(`${res.request().method()} ${res.url()} → ${res.status()}`);
  });
}

async function newPage(browser, { viewport = { width: 1440, height: 900 } } = {}) {
  const context = await browser.newContext({ viewport, locale: "tr-TR" });
  const page = await context.newPage();
  const errors = [];
  attachCollector(page, errors);
  return { context, page, errors };
}

/* ── PUBLIC SITE ─────────────────────────────────────────── */
async function verifyPublic(browser) {
  const { context, page, errors } = await newPage(browser);
  try {
    await page.goto(`${PORTS.public}/`, { waitUntil: "domcontentloaded" });

    // Guided club entry (fresh context → no persisted club)
    await page.getByLabel("Kulüp alt alan adı").fill(SUB);
    await page.getByRole("button", { name: "Git" }).click();
    await expectVisible(page, "h1", 10_000);
    const clubName = await page.locator("h1").first().textContent();
    note("public", "/", clubName?.includes("E2E") ? "PASS" : "PARTIAL", `kulüp adı: ${clubName}`);
    await page.screenshot({ path: `${SHOTS}/public-club.png`, fullPage: true });

    // Wizard
    await page.getByRole("link", { name: /Rezervasyona başla/ }).click();
    await page.getByRole("button", { name: "4x" }).click();
    const slotGrid = page.locator('fieldset[aria-label="Tekne sınıfı"]');
    await slotGrid.waitFor();
    const dateInput = page.locator('input[type="date"][aria-label="Rezervasyon tarihi"]');
    if (await dateInput.count()) await dateInput.fill(await todayIso());
    const enabledSlots = page.locator('div.grid button:not([disabled])');
    const slotCount = await enabledSlots.count();
    if (slotCount > 0) {
      await enabledSlots.first().click();
      await page.getByRole("button", { name: "Devam et" }).click();
      await page.getByLabel("Ad Soyad").fill("E2E Misafir Kürekçi");
      await page.getByLabel("Telefon").fill(`0555${Date.now() % 10000000}`);
      // Check every consent checkbox in the wizard
      const boxes = page.locator('fieldset legend:text("Beyanlar") ~ * input[type="checkbox"], fieldset:has(legend:text-is("Beyanlar")) input[type="checkbox"]');
      const boxCount = await boxes.count();
      for (let i = 0; i < boxCount; i += 1) {
        const box = boxes.nth(i);
        if (!(await box.isChecked())) await box.check();
      }
      await page.screenshot({ path: `${SHOTS}/public-booking.png` });
      await page.getByRole("button", { name: "Rezervasyonu tamamla" }).click();
      const confirmed = await page.getByText("Randevunuz alındı!").waitFor({ timeout: 8000 }).then(() => true).catch(() => false);
      const alertText = confirmed ? "" : (await page.locator('[role="alert"]').first().textContent({ timeout: 3000 }).catch(() => ""));
      note("public", "guest booking", confirmed ? "PASS" : "PARTIAL", confirmed ? "201 onay ekranı" : `handled sonucu: ${alertText.trim().slice(0, 120)}`);
    } else {
      note("public", "guest booking", "PARTIAL", "bugün müsait slot yok — grid render doğrulandı");
    }
    await page.screenshot({ path: `${SHOTS}/public-final.png` });
    reportErrors("public", errors);

    // Mobile viewport
    await context.close();
    const mob = await newPage(browser, { viewport: { width: 390, height: 844 } });
    await mob.page.goto(`${PORTS.public}/`, { waitUntil: "domcontentloaded" });
    await mob.page.waitForTimeout(800); // persisted club → landing
    await mob.page.screenshot({ path: `${SHOTS}/public-mobile.png`, fullPage: true });
    const overflow = await mob.page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 2);
    note("public", "mobile 390px", overflow ? "PARTIAL" : "PASS", overflow ? "yatay taşma var" : "taşma yok");
    reportErrors("public-mobile", mob.errors);
    await mob.context.close();
  } catch (err) {
    note("public", "flow", "FAIL", String(err).slice(0, 200));
    await context.close();
  }
}

/* ── COMPANY PANEL ───────────────────────────────────────── */
async function verifyPanel(browser) {
  const { context, page, errors } = await newPage(browser);
  try {
    await page.goto(`${PORTS.panel}/giris`, { waitUntil: "domcontentloaded" });
    await page.screenshot({ path: `${SHOTS}/panel-login.png` });

    await page.getByLabel("E-posta").fill(ADMIN_EMAIL);
    await page.getByLabel("Parola").fill(ADMIN_PASSWORD);
    await page.getByRole("button", { name: "Giriş yap" }).click();

    await expectVisible(page, 'h1:text("Genel Bakış")');
    note("panel", "/giris → auth", "PASS");
    await page.screenshot({ path: `${SHOTS}/panel-dashboard.png`, fullPage: true });

    const pages = [
      ["/program", "Gün Programı"],
      ["/uyeler", "Üyeler"],
      ["/paketler", "Paketler"],
      ["/kaynaklar", "Kaynaklar"],
      ["/akis", "Kulüp Akışı"],
      ["/kullanicilar", "Kullanıcılar"],
      ["/ayarlar", "Ayarlar"],
    ];
    for (const [path, title] of pages) {
      await page.goto(`${PORTS.panel}${path}`, { waitUntil: "domcontentloaded" });
      await expectVisible(page, `h1:text("${title}")`);
    }
    note("panel", "tüm sayfalar render", "PASS");

    await page.goto(`${PORTS.panel}/program`, { waitUntil: "domcontentloaded" });
    await page.waitForTimeout(600);
    await page.screenshot({ path: `${SHOTS}/panel-program.png`, fullPage: true });
    await page.goto(`${PORTS.panel}/uyeler`, { waitUntil: "domcontentloaded" });
    await page.waitForTimeout(600);
    await page.screenshot({ path: `${SHOTS}/panel-members.png`, fullPage: true });

    // Protected-route behaviour after logout
    await page.getByRole("button", { name: "Çıkış yap" }).first().click();
    await page.getByRole("dialog").getByRole("button", { name: "Çıkış yap" }).click();
    await page.waitForURL(/\/giris$/);
    await page.goto(`${PORTS.panel}/`);
    await page.waitForURL(/\/giris$/);
    note("panel", "protected route after logout", "PASS");
    reportErrors("panel", errors);
    await context.close();
  } catch (err) {
    note("panel", "flow", "FAIL", String(err).slice(0, 200));
    await context.close();
  }
}

/* ── MEMBER PORTAL ───────────────────────────────────────── */
async function verifyPortal(browser) {
  const { context, page, errors } = await newPage(browser, { viewport: { width: 390, height: 844 } });
  try {
    // Guided club entry (fresh context)
    await page.goto(`${PORTS.portal}/`, { waitUntil: "domcontentloaded" });
    await page.waitForURL(/\/giris/);
    await page.getByLabel("Kulüp alt alan adı").fill(SUB);
    await page.getByRole("button", { name: "Git" }).click();

    // Register a brand-new member through the UI (validates consent fix end-to-end)
    await page.getByRole("link", { name: "Üye olun" }).click();
    await page.waitForURL(/\/kayit/);
    await page.waitForTimeout(1200); // consents catalog render

    const stamp = Date.now() % 1000000;
    await page.getByLabel("Ad Soyad").fill("E2E Test Üyesi");
    await page.getByLabel("Telefon").fill(`0555${stamp}`);
    await page.locator('input[type="email"]').first().fill(`uye${stamp}@crewbase.test`);
    await page.locator('input[type="password"]').fill("UyeParola-2026!");

    const consentBoxes = page.locator('fieldset:has(legend:text-is("Beyanlar")) input[type="checkbox"]');
    const count = await consentBoxes.count();
    for (let i = 0; i < count; i += 1) {
      const box = consentBoxes.nth(i);
      if (!(await box.isChecked())) await box.check();
    }
    const checkedStates = await consentBoxes.evaluateAll((els) => els.map((e) => e.checked));
    await page.screenshot({ path: `${SHOTS}/portal-register.png`, fullPage: true });
    await page.getByRole("button", { name: "Üye ol" }).click();

    // Either land on home or surface the exact blocking reason
    let landed = true;
    try {
      await expectVisible(page, 'section[aria-label="Tarih ve tekne sınıfı"]', 12_000);
    } catch {
      landed = false;
      const alerts = await page.locator('[role="alert"]').allTextContents().catch(() => []);
      note(
        "portal",
        "register diagnostics",
        "PARTIAL",
        `url=${page.url()} checked=[${checkedStates}] alerts=${JSON.stringify(alerts).slice(0, 260)}`,
      );
      throw new Error(`Kayıt sonrası ana sayfaya geçilemedi. Alerts: ${alerts.join(" | ").slice(0, 200)}`);
    }
    note("portal", "register → home", landed ? "PASS" : "PARTIAL", `beyan checkbox: ${count} işaretli [${checkedStates}]`);
    await page.screenshot({ path: `${SHOTS}/portal-home.png`, fullPage: true });

    // Profile renders real data
    await page.getByRole("navigation").last().getByRole("link", { name: "Profil" }).click();
    await expectVisible(page, 'h1:text("Profilim")');
    note("portal", "/profil", "PASS");

    // Logout returns to login
    await page.getByRole("button", { name: "Çıkış" }).click();
    await page.waitForURL(/\/giris/);
    note("portal", "logout", "PASS");
    reportErrors("portal", errors);
    await context.close();
  } catch (err) {
    note("portal", "flow", "FAIL", String(err).slice(0, 300));
    try { await page.screenshot({ path: `${SHOTS}/portal-fail.png` }); } catch {}
    await context.close();
  }
}

/* ── helpers ─────────────────────────────────────────────── */
async function expectVisible(page, selector, timeout = 10_000) {
  await page.locator(selector).first().waitFor({ state: "visible", timeout });
}

function todayIso() {
  const d = new Date();
  return `${d.getFullYear()}-${`${d.getMonth() + 1}`.padStart(2, "0")}-${`${d.getDate()}`.padStart(2, "0")}`;
}

function reportErrors(app, errors) {
  if (errors.length > 0) {
    note(app, "console/network", "PARTIAL", `${errors.length} sorun → ${errors.slice(0, 3).join(" | ")}`);
  } else {
    note(app, "console/network", "PASS", "temiz");
  }
}

/* ── run ─────────────────────────────────────────────────── */
const browser = await chromium.launch();
try {
  PORTS = await discoverPorts(browser);
  await verifyPublic(browser);
  await verifyPanel(browser);
  await verifyPortal(browser);
} finally {
  await browser.close();
  console.log(JSON.stringify(results, null, 2));
}
