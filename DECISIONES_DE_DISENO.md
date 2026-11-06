# Decisiones de diseño

Documento que explica las decisiones técnicas tomadas en la solución y el porqué de cada una.

## 1. Arquitectura: Clean Architecture en 4 proyectos

| Capa | Responsabilidad | Depende de |
|---|---|---|
| **Domain** | Entidades (`Customer`, `Account`, `Transaction`), reglas de negocio, excepciones de negocio | Nada |
| **Application** | Casos de uso (`CustomerService`, `AccountService`), DTOs, interfaces (repositorios, generador, unidad de trabajo) | Domain |
| **Infrastructure** | EF Core/SQLite, repositorios, generador de números de cuenta | Application |
| **Api** | Controladores, manejo global de errores, composición de dependencias | Application, Infrastructure |

**Por qué:** la regla de dependencia apunta hacia el dominio. La lógica bancaria no sabe nada de HTTP ni de SQLite,
por lo que se puede probar sin infraestructura y se puede cambiar la base de datos o el transporte sin tocar el negocio.
Se separó en proyectos distintos (y no solo en carpetas) para que el compilador **impida** violar las capas:
por ejemplo, `Application` no puede referenciar EF Core.

## 2. Controladores desacoplados de la persistencia

Los controladores solo reciben la petición HTTP, llaman a una interfaz de servicio de aplicación y devuelven un DTO.
No inyectan `DbContext` ni repositorios. El acceso a datos se esconde detrás de `IAccountRepository`,
`ICustomerRepository` e `IUnitOfWork` (definidas en Application, implementadas en Infrastructure).

Además, **nunca se exponen entidades**: se devuelven DTOs (`record`), así el contrato de la API es independiente del modelo de datos.

## 3. Modelo de dominio rico (las reglas viven en el dominio)

`Account` es el agregado raíz y es la única que puede cambiar su saldo mediante `Deposit()` y `Withdraw()`:

- El monto debe ser mayor que cero y con máximo 2 decimales.
- Un retiro mayor al saldo lanza `InsufficientFundsException` **antes** de modificar nada, así que la cuenta
  nunca queda en un estado inconsistente ni se registra una transacción fallida.
- Los setters son privados: ninguna otra capa puede saltarse las reglas.

Cada `Transaction` es inmutable y guarda `BalanceAfter` (**saldo histórico**), como pide el enunciado. Esto permite mostrar
el historial sin recalcular y deja una traza auditable.

Reglas adicionales en `Customer` que el enunciado no especifica, pero que son razonables en banca:
el nombre es obligatorio, la fecha de nacimiento no puede ser futura, el cliente debe ser **mayor de 18 años**
y los ingresos no pueden ser negativos. (Es una constante, `Customer.MinimumAge`, fácil de ajustar.)

## 4. Generador de números de cuenta (`ACC-YYYYMMDD-XXXX`)

- Prefijo constante `ACC-`, fecha UTC de creación y **4 dígitos aleatorios** con ceros a la izquierda (`:D4`), longitud total de 17 caracteres.
- Depende de `TimeProvider` (abstracción nativa de .NET) y de un `Random` opcional → pruebas **deterministas**
  (fecha controlada y semilla fija), sin flakiness.

**Punto clave – la unicidad:** con solo 4 dígitos hay 10 000 combinaciones por día, y la paradoja del cumpleaños
hace que las colisiones aparezcan con pocas cuentas. Por eso la unicidad se garantiza en **dos niveles**, y no se confía
solo en el azar:

1. **Lógica (`AccountService`)**: antes de usar un número verifica si existe y reintenta hasta 10 veces; si se agota, lanza `ConflictException` (409).
2. **Física (BD)**: índice único `UX_Accounts_AccountNumber` sobre `AccountNumber`, como exige el enunciado. Es la red de seguridad final ante condiciones de carrera.

Limitación conocida: el formato impone un tope de 10 000 cuentas por día. Es una restricción del enunciado, no del diseño;
si cambiara el formato, solo habría que cambiar la implementación de `IAccountNumberGenerator`.

## 5. Manejo global de excepciones (sin try-catch en controladores)

- El dominio lanza excepciones de negocio tipadas que heredan de `BusinessException`
  (`NotFoundException`, `InsufficientFundsException`, `DomainValidationException`, `ConflictException`).
- `GlobalExceptionHandler` implementa `IExceptionHandler` (mecanismo moderno de ASP.NET Core) y traduce cada tipo a su código HTTP.
  **El dominio no conoce HTTP**; el mapeo vive solo en la capa Api.
- Las respuestas siguen **RFC 7807 (`ProblemDetails`)**, con un `code` estable para clientes y un `traceId` para soporte.
- Errores inesperados → 500 con mensaje genérico; el detalle real solo va al log. **Nunca se filtran stack traces ni mensajes internos.**
- La validación de forma de las peticiones (campos requeridos, rangos) la hace `[ApiController]` con DataAnnotations y responde 400 automáticamente.

## 6. Persistencia (SQLite + EF Core)

- Configuración con `IEntityTypeConfiguration<T>` (Fluent API) en clases separadas, en lugar de atributos en el dominio, para mantener las entidades limpias.
- `AccountNumber`: longitud 17, requerido, **índice único**.
- Claves `Guid` generadas por la aplicación (`ValueGeneratedNever`), lo que permite que el dominio asigne su propio identificador.
- Enums guardados como texto (legibles en la BD y robustos ante reordenamientos del enum).
- Fechas en **UTC**, reinterpretadas como `DateTimeKind.Utc` al leer.
- **Concurrencia optimista:** `Account.Version` es un token de concurrencia. Si dos operaciones modifican la misma cuenta al mismo tiempo
  (p. ej. dos retiros simultáneos), la segunda falla y se traduce a 409 en lugar de generar un saldo incorrecto (*lost update*).
- La BD se crea con `EnsureCreated()` al iniciar para que el evaluador ejecute el proyecto sin pasos extra.
  En producción se usarían **migraciones** de EF Core.

## 7. Pruebas unitarias (xUnit)

| Área | Qué se valida |
|---|---|
| `AccountNumberGeneratorTests` | Longitud, prefijo, fecha de hoy, formato exacto, ceros a la izquierda, unicidad básica, uso del reloj |
| `AccountTests` (dominio) | Depósito, retiro con fondos, retiro sin fondos (rechazo limpio sin efectos), retiro total, montos inválidos, saldo histórico |
| `AccountServiceTests` | Crear cuenta, cliente inexistente, reintento ante colisión de número, saldo, depósitos, retiros, historial ordenado, cuenta inexistente |
| `CustomerServiceTests` | Alta válida, fecha futura, menor de edad, cliente inexistente |

Los servicios se prueban con **repositorios en memoria hechos a mano** (sin librerías de mocking): son simples, legibles y
no añaden dependencias. El tiempo y el azar se controlan con `TimeProvider` y `Random` con semilla.

## 8. Principios SOLID aplicados

- **S**: cada clase tiene una única razón de cambio (servicios de aplicación, repositorios, generador, manejador de errores).
- **O**: añadir un nuevo tipo de error HTTP o un nuevo generador de números no obliga a modificar los servicios existentes.
- **L**: las implementaciones (repositorios EF vs. en memoria de pruebas) son intercambiables sin romper a los consumidores.
- **I**: interfaces pequeñas y específicas (`IAccountRepository`, `ICustomerRepository`, `IUnitOfWork`, `IAccountNumberGenerator`).
- **D**: Application depende de abstracciones; Infrastructure las implementa; todo se compone con inyección de dependencias en `Program.cs`.

## 9. Otras decisiones

- **Códigos HTTP:** depósitos/retiros devuelven `201 Created` con el movimiento creado; fondos insuficientes → `400` (según el enunciado);
  cuenta o cliente inexistente → `404`.
- **Dinero con `decimal`**, nunca `double`/`float`, para evitar errores de redondeo.
- **Número de cuenta en la URL** (`/api/accounts/{accountNumber}/...`) porque es el identificador de negocio que usa el cliente; se normaliza (trim + mayúsculas).
- **`TimeProvider`** en lugar de `DateTime.UtcNow` directo, para poder probar el comportamiento temporal.
- Async con `CancellationToken` de extremo a extremo.

## 10. Mejoras futuras (fuera del alcance de la prueba)

- Autenticación/autorización (JWT) y que un cliente solo opere sus propias cuentas.
- Migraciones de EF Core, y un motor de BD de servidor (PostgreSQL/SQL Server) para producción.
- Idempotencia en depósitos/retiros (cabecera `Idempotency-Key`) para evitar duplicados por reintentos.
- Paginación del historial y filtros por rango de fechas.
- Transferencias entre cuentas (atómicas), pruebas de integración con `WebApplicationFactory`, logging estructurado y health checks.
