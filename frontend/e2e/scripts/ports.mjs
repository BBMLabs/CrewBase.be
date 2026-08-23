import { chromium } from "@playwright/test";

/** Discover which localhost port serves which CrewBase app by document title. */
export async function discoverPorts(launchedBrowser) {
  const browser = launchedBrowser ?? (await chromium.launch());
  const ownsBrowser = launchedBrowser === undefined;
  const map = {};
  for (const port of [5173, 5174, 5175, 5176, 5177, 5178]) {
    const context = await browser.newContext();
    const page = await context.newPage();
    try {
      await page.goto(`http://localhost:${port}/`, { waitUntil: "domcontentloaded", timeout: 6000 });
      const title = await page.title();
      if (title.includes("Kulüp Paneli")) map.panel = `http://localhost:${port}`;
      else if (title.includes("Üye Portalı")) map.portal = `http://localhost:${port}`;
      else if (title.includes("Rezervasyon")) map.public = `http://localhost:${port}`;
    } catch {
      /* port boş */
    }
    await context.close();
  }
  if (ownsBrowser) await browser.close();
  return map;
}
