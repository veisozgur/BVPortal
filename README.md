# BVPortal

BV Kırtasiye için okul ve ofis müşterilerine yönelik online teklif, SMS doğrulama, bildirim ve CRM platformu.

## Sprint 1 kapsamı

- .NET 9 ve Visual Studio solution
- Clean Architecture katmanları
- ASP.NET Core Web API
- SQL Server ve EF Core migration altyapısı
- Kullanıcı kaydı, giriş ve telefon doğrulama
- SMS OTP üretme ve doğrulama
- JWT access token ve refresh token rotasyonu
- Global hata yönetimi ve audit log
- Swagger JWT desteği
- Docker Compose ile SQL Server 2022
- GitHub Actions restore ve build kontrolü
- Geliştirme SMS göndericisi ve NetGSM üretim adaptörü

## Gereksinimler

- .NET SDK 9
- Docker Desktop veya SQL Server 2022
- Visual Studio 2022 17.12 veya üzeri

## Çalıştırma

1. `docker compose up -d`
2. `dotnet restore BVPortal.sln`
3. `dotnet ef database update --project src/BV.Persistence --startup-project src/BV.Api`
4. `dotnet run --project src/BV.Api`

Swagger arayüzü uygulama adresinin `/swagger` yolundadır. Sağlık kontrolü `/health` yolundadır.

## Yapılandırma

Üretim ortamına ait SQL Server, JWT ve NetGSM bilgileri repoya yazılmamalıdır. Bu değerler environment variable, kullanıcı sırları veya güvenli secret store üzerinden verilmelidir.

NetGSM devre dışıyken OTP kodları gerçek SMS yerine uygulama loguna yazılır. Üretimde NetGSM ayarları etkinleştirilmelidir.

## Temel API uçları

- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/send-otp`
- `POST /api/v1/auth/verify-otp`
- `POST /api/v1/auth/verify-phone`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `GET /health`

## Güvenlik

Şifreler PBKDF2-SHA256 ve rastgele salt ile saklanır. OTP ve refresh token değerleri açık metin olarak veritabanında tutulmaz.
