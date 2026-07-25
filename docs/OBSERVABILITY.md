# Gözlemlenebilirlik (Observability)

RowingClub, Serilog (structured logging), OpenTelemetry (trace), Prometheus (metric) ve Grafana (dashboard) üzerine kurulu bir gözlemlenebilirlik stratejisi izler.

## 1. Log Formatı

Serilog ile **structured logging** kullanılır (JSON formatında sink'e yazılır). Her log kaydında bulunması zorunlu alanlar:

| Alan | Açıklama |
|---|---|
| Timestamp | UTC zaman damgası |
| LogLevel | Trace/Debug/Information/Warning/Error/Fatal |
| Message | Log mesajı |
| CorrelationId | İstek boyunca yayılan korelasyon kimliği (bkz. §5) |
| TraceId | OpenTelemetry trace kimliği |
| UserId | İşlemi yapan kullanıcı (varsa) |
| ClubId | Aktif tenant (varsa) |
| Endpoint | İsteğin route'u |
| HTTP method | GET/POST/PATCH/DELETE vb. |
| Status code | Response HTTP durum kodu |
| Duration | İşlem süresi (ms) |
| Event name | Yapısal olay adı (ör. `AppointmentCreated`, `LoginFailed`) |

**Hassas veri hiçbir koşulda loglanmaz.** Parola, token, secret, tam kişisel veri (telefon, adres, kimlik no vb.) loglara yazılmaz; gerekiyorsa maskelenmiş biçimde yazılır. Bu kural log enricher/filter seviyesinde merkezi olarak uygulanır, her handler'da ayrı ayrı hatırlanmaya bırakılmaz.

## 2. Metric Listesi

Prometheus formatında en az aşağıdaki metrikler üretilir:

- Request sayısı (endpoint, method, status code kırılımında)
- Request süresi (histogram)
- Hata oranı
- Login başarısızlıkları
- Randevu oluşturma sayısı
- Randevu iptal sayısı
- Ders doluluk oranı
- Outbox pending count
- Outbox retry count
- PostgreSQL erişim süreleri
- Background job başarısızlıkları

Identity implementasyonu tamamlandıkça login/refresh/logout ile ilgili metrikler ilk olarak devreye girecektir; Scheduling/Packages metrikleri ilgili modüller implemente edildiğinde eklenecektir.

## 3. Trace Akışı

OpenTelemetry ile aşağıdaki span'ler izlenir:

```
API request
 └─ MediatR handler (Command/Query)
     ├─ PostgreSQL query (henüz otomatik span'lenmiyor - aşağıdaki nota bakın)
     ├─ Redis (cache/idempotency/rate limit/lock)
     ├─ Outbox processing (background worker span'i, ayrı trace kökü olabilir)
     └─ Notification provider (e-posta/SMS/push adaptörü)
```

**Not — PostgreSQL/EF Core span'leri henüz yok:** `OpenTelemetrySetup.AddRowingClubOpenTelemetry` şu an yalnızca ASP.NET Core ve `HttpClient` instrumentation'ını ekliyor; Npgsql/EF Core için henüz bir OpenTelemetry instrumentation paketi wire edilmedi, bu yüzden Postgres sorgu span'leri izlenmiyor (bkz. `RowingClub.BuildingBlocks.Observability/Telemetry/OpenTelemetrySetup.cs` içindeki kod yorumu). Uygun bir paket (ör. Npgsql'in kendi OpenTelemetry desteği) değerlendirilip kabul edildiğinde bu boşluk kapatılacaktır.

Trace'ler `OTEL_EXPORTER_OTLP_ENDPOINT` üzerinden bir OTLP collector'a export edilir. Her span, ilgili `CorrelationId` ve tenant (`ClubId`) bilgisini attribute olarak taşır.

## 4. Grafana Dashboard'ları

Provision edilmesi öngörülen dashboard'lar (`deploy/grafana/` altında, ilgili modüller implemente edildikçe doldurulacak):

| Dashboard | Kapsam |
|---|---|
| API genel görünüm | Toplam istek, status code dağılımı, throughput |
| Hata ve latency | Hata oranı, p50/p95/p99 latency, endpoint bazlı yavaşlıklar |
| Database | PostgreSQL sorgu süreleri, bağlantı havuzu durumu |
| Outbox | Pending/retry/dead-letter sayıları, işleme gecikmesi (lag) |
| Authentication güvenliği | Login başarısızlıkları, hesap kilitlemeleri, refresh token reuse tespitleri |
| Scheduling operasyonları | Randevu oluşturma/iptal oranı, doluluk oranı, bekleme listesi büyüklüğü |
| Notification delivery | Gönderim başarı/başarısızlık oranı, provider bazlı gecikme |

## 5. Correlation ID Yayılımı

- Her istek bir `X-Correlation-Id` header'ı taşıyabilir; istemci göndermezse sunucu bir tane üretir.
- Correlation id; middleware seviyesinde request scope'una eklenir, tüm log kayıtlarına, trace span attribute'larına ve outbox mesajlarına taşınır.
- Response header'ında da `X-Correlation-Id` geri döndürülür, böylece istemci/destek ekibi bir isteği uçtan uca izleyebilir.
- Background worker'lar (outbox processing, notification gönderimi gibi) orijinal isteğin correlation id'sini event payload'ı üzerinden taşımaya devam eder.

## 6. Alert Kuralları

Alert kuralları, ilgili metrikler üretilmeye başladıkça Prometheus/Alertmanager tarafında tanımlanacaktır. Öncelikli adaylar: yüksek hata oranı, p99 latency eşik aşımı, outbox lag'in belirli bir süreyi aşması, login başarısızlık oranındaki ani artış (brute-force işareti), background job ardışık başarısızlıkları. Kesin eşik değerleri, üretim trafiği gözlemlendikçe kalibre edilecektir.
