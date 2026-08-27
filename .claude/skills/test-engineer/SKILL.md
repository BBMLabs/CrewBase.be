---
name: test-engineer
description: Test yazarken/güncellerken kullanılır. Beş test projesinin rolleri, desenleri ve hangi değişikliğin hangi testi kırdığı.
---

# Test Engineer

## Test projeleri

| Proje | Ne test eder | Araçlar |
|---|---|---|
| UnitTests | Handler + Domain davranışı, mock repo'larla | xUnit, NSubstitute, FluentAssertions v7 |
| ArchitectureTests | Katman bağımlılık kuralları | NetArchTest |
| IntegrationTests | Repository'ler gerçek Postgres'e karşı | fixture: `PostgresIdentityFixture` |
| FunctionalTests | HTTP uçları `WebApplicationFactory<Program>` ile | in-memory host, test RSA anahtarları |
| SecurityTests | 401/403/400 sözleşmeleri, SameCompany handler | endpoint envanter listeleri |

Çalıştırma: `dotnet test RowingClub.sln`.

## Desenler

- Handler testi: repo'lar `Substitute.For<>`, handler ctor'una ver, `Handle` çağır,
  FluentAssertions ile doğrula. `DomainException` kodu `.Which.ErrorCode.Should().Be("...")`.
- Ortak entity kurulumları için factory helper (örn. `CompanyTestFactory.Create`).
- Endpoint eklendi/kaldırıldı → GÜNCELLE: `SecurityTests/AuthEndpointSecurityTests`
  (ProtectedEndpoints / PublicEndpointsWithInvalidPayload listeleri) ve
  `FunctionalTests/AuthEndpointFunctionalTests` (OpenAPI envanteri).
- Handler ctor imzası değişti → ilgili unit testlere yeni substitute parametresi ekle.
- Test adları `Snake_case_davranış_cümlesi` (mevcut stile uy).

## Neyi test et

- Domain kural ihlalleri (her `DomainException` dalı için bir test).
- Gruplama/kapasite gibi algoritmik mantık (seans doldurma, slot hesaplama).
- Yetki sınırları: yanlış rol → 401/403; firma sınırı ihlali → forbidden.
- Mutlu yol + en az bir sınır durumu; framework'ü (EF, MediatR) değil KENDİ mantığını test et.
