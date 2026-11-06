# Banking API – Prueba Técnica Backend (.NET 10)

API REST para una plataforma de banca digital: perfiles de clientes, cuentas bancarias,
depósitos, retiros, saldo e historial de transacciones.

**Stack:** .NET 10 · ASP.NET Core Web API (controladores) · Entity Framework Core 10 · SQLite · xUnit

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- No requiere instalar ninguna base de datos (SQLite se crea sola).

## Cómo ejecutar

```bash
# 1. Restaurar y compilar
dotnet build

# 2. Ejecutar las pruebas unitarias
dotnet test

# 3. Levantar la API (http://localhost:5080)
dotnet run --project src/Banking.Api
```

Al arrancar, la API crea automáticamente el archivo `banking.db` (SQLite) con sus tablas
y el índice único sobre `AccountNumber`. En modo Development el contrato OpenAPI está en
`http://localhost:5080/openapi/v1.json`.

El archivo `src/Banking.Api/Banking.Api.http` contiene peticiones listas para probar
(VS Code con REST Client, Visual Studio o Rider).

## Endpoints

| Método | Ruta | Descripción | Éxito | Errores |
|---|---|---|---|---|
| POST | `/api/customers` | Crear perfil de cliente | 201 | 400 |
| GET | `/api/customers/{id}` | Consultar cliente | 200 | 404 |
| POST | `/api/accounts` | Crear cuenta (cliente + saldo inicial) | 201 | 400, 404 |
| GET | `/api/accounts/{accountNumber}/balance` | Consultar saldo | 200 | 404 |
| POST | `/api/accounts/{accountNumber}/deposits` | Registrar depósito | 201 | 400, 404 |
| POST | `/api/accounts/{accountNumber}/withdrawals` | Registrar retiro | 201 | 400 (sin fondos), 404 |
| GET | `/api/accounts/{accountNumber}/transactions` | Historial cronológico | 200 | 404 |

### Ejemplos

```bash
# Crear cliente
curl -X POST http://localhost:5080/api/customers -H "Content-Type: application/json" -d '{
  "fullName": "María Fernanda López",
  "dateOfBirth": "1995-03-14",
  "gender": "Female",
  "monthlyIncome": 1500.50
}'

# Crear cuenta
curl -X POST http://localhost:5080/api/accounts -H "Content-Type: application/json" -d '{
  "customerId": "<ID_DEL_CLIENTE>",
  "initialBalance": 100
}'

# Retirar
curl -X POST http://localhost:5080/api/accounts/ACC-20261105-0427/withdrawals \
  -H "Content-Type: application/json" -d '{ "amount": 20 }'
```

`gender` acepta: `Male`, `Female`, `Other`, `PreferNotToSay`.

### Formato de errores (RFC 7807)

```json
{
  "title": "Fondos insuficientes",
  "status": 400,
  "detail": "Fondos insuficientes para realizar el retiro.",
  "code": "insufficient_funds",
  "traceId": "0HN7..."
}
```

| Excepción de negocio | HTTP | `code` |
|---|---|---|
| `DomainValidationException` | 400 | `validation_error` |
| `InsufficientFundsException` | 400 | `insufficient_funds` |
| `NotFoundException` | 404 | `not_found` |
| `ConflictException` | 409 | `conflict` |
| Cualquier otra (inesperada) | 500 | `internal_error` (sin detalles internos) |

## Estructura

```
src/
  Banking.Domain          Entidades, reglas de negocio y excepciones (sin dependencias)
  Banking.Application     Servicios de aplicación, DTOs e interfaces (puertos)
  Banking.Infrastructure  EF Core + SQLite, repositorios, generador de números de cuenta
  Banking.Api             Controladores delgados, manejador global de excepciones, Program.cs
tests/
  Banking.Tests           Pruebas unitarias (dominio, servicios y generador)
```

Dependencias: `Api → Application → Domain` y `Infrastructure → Application → Domain`.
Los controladores **nunca** tocan el `DbContext`: solo usan `ICustomerService` / `IAccountService`.

Las decisiones de diseño y su justificación están en [`DECISIONES_DE_DISENO.md`](DECISIONES_DE_DISENO.md).
