# REST Kuralları

- Kaynak isimleri çoğul ve kebap değil düz İngilizce: `/appointments`, `/customers`, `/boats`,
  `/instructors`, `/packages`, `/sessions`, `/users`, `/settings`.
- Koleksiyon: `GET /api/v1/company/boats`; tekil oluşturma `POST` aynı yola; güncelleme
  `PUT /boats/{id}`; durum/aksiyon değişimi alt eylem POST'u: `/appointments/{id}/status`,
  `/customers/{id}/level`, `/sessions/{id}/assign`, `/users/{id}/role`.
- Filtre query string'te: `?date=2026-08-24`. Sayfalama gerekiyorsa `?page=&pageSize=`.
- Route parametreleri tiplenir: `{id:guid}`, `{companyId:guid}`.
- Version şimdilik URL'de sabit `v1`; kırıcı değişiklikte Asp.Versioning ApiVersionSet kullan
  (AuthEndpoints örneği).
- Silme yerine pasifleştirme tercih edilir (`IsActive=false` - Boat/Instructor/Package deseni).
- Public uçlarda kimlik yok; üye eşleşmesi telefon numarasıyla (blind index) yapılır.
