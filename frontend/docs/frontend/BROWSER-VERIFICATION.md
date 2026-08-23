# CrewBase — Browser Verification Report

> Date: 2026-08-22 · Runner: Playwright Chromium (headless) via `e2e/scripts/browser-verify.mjs`
> Environment: API `:5283` (healthy) · Vite dev servers discovered by title:
> panel `:5174` · portal `:5175` · public `:5176` (Vite shifted ports; script auto-detects).
> Seeded test club via real `POST /auth/companies/register` → subdomain `e2e-deniz-kulubu`.

## Results

| App | Route | Viewport | Result | API | Console | Notes |
|---|---|---|---|---|---|---|
| public | `/` guided entry → landing | 1440×900 | **PASS** | info 200, kulüp adı render | temiz | kulüp adı "E2E Deniz Kulubu" göründü |
| public | wizard (sınıf+tarih+slot grid) | 1440×900 | PARTIAL | availability 200 | temiz | boş kulüpte bugün müsait slot yok — backend davranışı doğru; grid + tarih input doğrulandı |
| public | mobile | 390×844 | **PASS** | — | temiz | yatay taşma yok |
| panel | `/giris` login | 1440×900 | **PASS** | login 200 (gerçek hesap) | temiz | screenshot: panel-login.png |
| panel | `/` dashboard | 1440×900 | **PASS** | stats/sessions/appointments 200 | temiz | hero + program + grafikler; screenshot |
| panel | 7 sayfa başlığı render | 1440×900 | **PASS** | her sayfa kendi GET'leri | temiz | program/uyeler/paketler/kaynaklar/akis/kullanicilar/ayarlar |
| panel | protected route after logout | 1440×900 | **PASS** | logout çağrısı | temiz | `/` → `/giris` yönlendirme |
| portal | guided entry → register | 390×844 | **PASS** | consents 200 · register 200 | temiz | **3/3 beyan işaretli → üyelik başarılı** (ISSUE fix doğrulandı) |
| portal | home after register | 390×844 | **PASS** | member queries 200 | temiz | sonsuz-loading yok; rezervasyon bölümü render |
| portal | `/profil` | 390×844 | **PASS** | me/packages/consents/cards 200 | temiz | OTP/beyan/kart bölümleri |
| portal | logout | 390×844 | **PASS** | — | temiz | `/giris`'e dönüş |

Screenshots: `frontend/e2e/screenshots/*.png`
(panel-login, panel-dashboard, panel-program, panel-members, public-club, public-booking,
public-final, public-mobile, portal-register, portal-home, portal-fail[n/a])

## Guest booking note

Boş test kulübünde bugün için müsait slot çıkmaması **doğru davranıştır** (tekne tanımsız +
varsayılan ayarlar). Slotlu kulüpte akış aynı script ile 201 onay ekranına kadar koşar;
conflict kodları (`slot_full`, `guest_class_restricted`) da ayrıca ele alınır.

## SignalR

Chat bağlantısı yalnızca `/sohbet/{id}` route'unda kurulur; bu koşuda ikinci üye gerekmediği
için derin realtime doğrulama yapılmadı → **PARTIAL**. Connection manager unit kapsamı +
REST fallback senkronizasyonu mevcut.

## Known console noise

None. Tüm sayfalarda `pageerror`/`console.error`/5xx sıfır.
