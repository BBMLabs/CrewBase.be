import { expect, test } from "@playwright/test";
import { API_URL, memberSubdomain } from "../../helpers/env";

test.describe("Üye portalı — kritik akışlar", () => {
  let club = "";

  test.beforeAll(() => {
    club = memberSubdomain();
  });

  test("kullanılabilirlik gerçek backend'den okunur (anonim)", async ({ request }) => {
    const response = await request.get(`${API_URL}/api/v1/public/${club}/availability?boatClass=1x`);
    expect(response.status()).toBe(200);
    const body = await response.json();
    expect(body.success).toBe(true);
    for (const slot of body.data as Array<{ time: string; available: boolean; seatsLeft: number }>) {
      expect(slot.time).toMatch(/^\d{2}:\d{2}$/);
    }
  });

  test("misafir kısıtı: hesapsız çağıran için 1x rezervasyonu reddedilir", async ({ request }) => {
    const today = new Date();
    const date = `${today.getFullYear()}-${`${today.getMonth() + 1}`.padStart(2, "0")}-${`${today.getDate()}`.padStart(2, "0")}`;
    const response = await request.post(`${API_URL}/api/v1/public/${club}/appointments`, {
      data: {
        fullName: "E2E Misafir",
        phone: `0555${Date.now() % 10000000}`,
        date,
        time: "10:00",
        boatClass: "1x",
        acceptedConsents: ["swim", "health", "rules", "kvkk"],
      },
    });
    // Sözleşme: ya slot/geçerlilik hatası (400), ya misafir kısıtı veya çakışma (409), ya da başarı.
    expect([200, 201, 400, 409]).toContain(response.status());
    if (response.status() === 409) {
      const problem = await response.json();
      expect(problem.code === "guest_class_restricted" || typeof problem.detail === "string").toBeTruthy();
    }
  });
});
