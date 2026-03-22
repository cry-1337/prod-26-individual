# REPOSITORY MAP

Карта репозитория LOTTY A/B Platform — как ориентироваться в коде.

---

## Точки входа

- **Program.cs** (`src/LottyAB/LottyAB.Api/Program.cs`) — конфигурация приложения, DI, middleware, JWT, Health checks
- **docker-compose.yml** (корень репозитория) — запуск PostgreSQL + API
- **DbInitializer.cs** (`src/LottyAB/LottyAB.Infrastructure/Persistence/DbInitializer.cs`) — seed данных (admin user, event types, metrics)

---

## Структура репозитория

```
lame84712/
├── docker-compose.yml          # Оркестрация PostgreSQL + API
├── Dockerfile                  # Образ для LottyAB.Api
├── README.md                   # Описание проекта
├── docs/                       # Вся документация
└── src/LottyAB/                # Исходный код
    ├── LottyAB.sln             # Solution файл
    ├── LottyAB.Domain/         # Бизнес-модели и правила
    ├── LottyAB.Contracts/      # DTO для API
    ├── LottyAB.Application/    # Бизнес-логика (CQRS handlers)
    ├── LottyAB.Infrastructure/ # EF Core, репозитории, Background Services
    ├── LottyAB.Api/            # REST API контроллеры
    └── LottyAB.Tests/          # Интеграционные тесты
```

---

## Структура src/LottyAB (Clean Architecture)

### LottyAB.Domain (слой 1 - бизнес-модели)

```
LottyAB.Domain/
├── Entities/
│   ├── FeatureFlagEntity.cs    # Флаг с default значением
│   ├── ExperimentEntity.cs     # Эксперимент с вариантами и таргетингом
│   ├── VariantEntity.cs        # Вариант с весом и значением
│   ├── DecisionEntity.cs       # Решение (какой вариант выдан subject)
│   ├── EventEntity.cs          # Событие (exposure/conversion/click)
│   ├── UserEntity.cs           # Пользователь с ролью
│   ├── ReviewEntity.cs         # Ревью эксперимента (approve/reject)
│   ├── MetricDefinitionEntity.cs  # Определение метрики
│   ├── EventTypeEntity.cs      # Тип события с полями
│   └── GuardrailTriggerHistoryEntity.cs  # История срабатывания guardrails
└── Enums/
    ├── EExperimentStatus.cs    # Draft, InReview, Approved, Running, Paused, Completed
    ├── EReviewOutcome.cs       # Approve, Reject
    ├── EGuardrailAction.cs     # Pause, Rollback
    └── EUserRole.cs            # Admin, Experimenter, Approver, Viewer
```

### LottyAB.Contracts (слой 2 - DTO)

```
LottyAB.Contracts/
├── Request/
│   ├── CreateFeatureFlagRequest.cs
│   ├── CreateExperimentRequest.cs
│   ├── SendEventsRequest.cs
│   └── ... (все request DTO)
└── Responses/
    ├── DecisionResponse.cs     # Ответ /api/decide
    ├── LoginResponse.cs        # JWT токен
    ├── ExperimentReportResponse.cs
    └── ... (все response DTO)
```

### LottyAB.Application (слой 3 - бизнес-логика)

```
LottyAB.Application/
├── Commands/                   # Write операции
│   ├── DecideCommand.cs        # Получить вариант для subject
│   ├── CreateExperimentCommand.cs
│   ├── SendEventsCommand.cs
│   └── ... (все команды)
├── Queries/                    # Read операции
│   ├── GetExperimentReportQuery.cs
│   ├── GetFeatureFlagQuery.cs
│   └── ... (все запросы)
├── Handlers/                   # Обработчики команд/запросов
│   ├── DecideHandler.cs        # КРИТИЧНО - основная логика decide
│   ├── SendEventsHandler.cs    # Сохранение событий
│   ├── GetExperimentReportHandler.cs
│   └── ... (все handlers)
├── Services/                   # Доменные сервисы
│   ├── HashVariantSelector.cs  # Выбор варианта по хешу от subjectId
│   ├── TargetingEvaluator.cs   # Проверка таргетинга (DSL парсер)
│   └── ValueTypeConverter.cs   # Конвертация типов (Boolean/String/Number/JSON)
├── Validators/                 # FluentValidation
│   ├── DecideCommandValidator.cs
│   └── ... (все валидаторы)
├── Behaviors/
│   └── ValidationBehavior.cs   # MediatR pipeline для валидации
├── Exceptions/
│   ├── ValidationException.cs
│   ├── NotFoundException.cs
│   └── ForbiddenException.cs
└── Interfaces/
    ├── IApplicationDbContext.cs
    ├── IHashVariantSelector.cs
    ├── ITargetingEvaluator.cs
    └── ... (все интерфейсы)
```

### LottyAB.Infrastructure (слой 4 - инфраструктура)

```
LottyAB.Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs  # EF Core DbContext
│   ├── DbInitializer.cs        # Seed данных (admin, event types, metrics)
│   └── Configurations/         # EF Core Entity Configurations
├── Services/
│   ├── EventAttributionService.cs      # Background: атрибуция exposure → conversion
│   ├── GuardrailMonitoringService.cs   # Background: проверка guardrails каждые 60 сек
│   ├── JwtTokenService.cs      # Генерация JWT токенов
│   └── PasswordHasher.cs       # BCrypt хеширование паролей
├── Migrations/                 # EF Core миграции
└── Interfaces/
    └── ... (интерфейсы для Infrastructure)
```

### LottyAB.Api (слой 5 - REST API)

```
LottyAB.Api/
├── Controllers/
│   ├── DecideController.cs     # POST /api/decide
│   ├── EventsController.cs     # POST /api/events
│   ├── ExperimentsController.cs
│   ├── ReportsController.cs    # GET /api/experiments/{id}/report
│   ├── GuardrailsController.cs
│   ├── FeatureFlagsController.cs
│   ├── AuthController.cs       # POST /api/auth/login
│   └── UsersController.cs
├── Middleware/
│   └── GlobalExceptionHandler.cs  # Обработка исключений
├── Program.cs                  # Точка входа, DI, JWT, Health checks
└── appsettings.json            # Конфигурация (ConnectionStrings, JWT)
```

### LottyAB.Tests (слой 6 - тесты)

```
LottyAB.Tests/
├── BaseTestFactory.cs          # WebApplicationFactory с SQLite In-Memory
├── SmokeTests.cs               # Health, Ready, Login
├── DecisionTests.cs            # /api/decide: default, variant, таргетинг, веса
├── ExperimentTests.cs          # Lifecycle: Draft → InReview → Approved → Running
├── EventTests.cs               # /api/events: валидация, дедупликация, атрибуция
├── ReportTests.cs              # /api/experiments/{id}/report
├── GuardrailTests.cs           # Срабатывание guardrails, pause/rollback
└── FullExperimentFlowTests.cs  # Сквозной happy-path
```

---

## Критичный поток: Decide

**Endpoint:** `POST /api/decide`

**Путь выполнения:**

1. **DecideController.cs:12**
   - Принимает `DecideCommand` (FeatureFlagKey, SubjectId, SubjectAttributes)
   - Отправляет команду через MediatR: `mediator.Send(decideRequest)`

2. **ValidationBehavior.cs** (MediatR pipeline)
   - Запускает `DecideCommandValidator`
   - Проверяет: FeatureFlagKey не пустой, SubjectId не пустой

3. **DecideHandler.cs:18** (основная логика)
   - Загружает FeatureFlag из БД с Include(Experiments).ThenInclude(Variants)
   - Проверяет существование флага → если нет, выбрасывает NotFoundException
   - Ищет активный эксперимент (Status == Running)
   - **Если нет активного эксперимента:**
     - → вызывает `CreateDefaultDecision()` (строка 32)
     - → сохраняет Decision с IsDefault=true
     - → возвращает default значение флага
   - Проверяет таргетинг через `ITargetingEvaluator.EvaluateRule()`
   - **Если таргетинг не пройден:**
     - → вызывает `CreateDefaultDecision()` (строка 32)
   - Выбирает вариант через `IHashVariantSelector.SelectVariant(subjectId, experiment)`
   - **Если вариант выбран:**
     - → вызывает `CreateVariantDecision()` (строка 41)
     - → сохраняет Decision с ExperimentId, VariantId, IsDefault=false
     - → возвращает значение варианта
   - Сохраняет Decision в БД через `dbContext.SaveChangesAsync()`

4. **Возврат DecisionResponse**
   - `DecisionId` (Guid)
   - `VariantKey` (название варианта или ключ флага)
   - `VariantValue` (строковое значение)
   - `TypedValue` (типизированное значение: bool/string/number/json)
   - `IsDefault` (true/false)
   - `FeatureFlagKey`
   - `ExperimentId`, `VariantId` (если не default)

**Ключевые файлы:**
- `LottyAB.Api/Controllers/DecideController.cs` - endpoint
- `LottyAB.Application/Handlers/DecideHandler.cs` - вся логика
- `LottyAB.Application/Services/HashVariantSelector.cs` - выбор варианта по весам
- `LottyAB.Application/Services/TargetingEvaluator.cs` - проверка targeting DSL
- `LottyAB.Application/Validators/DecideCommandValidator.cs` - валидация

---

## Критичный поток: SendEvents

**Endpoint:** `POST /api/events`

**Путь выполнения:**

1. **EventsController → SendEventsHandler**
   - Валидация событий (типы полей, обязательные поля)
   - Дедупликация по EventId
   - Сохранение в таблицу Events

2. **EventAttributionService (Background Service, каждые 30 сек)**
   - Ищет неатрибутированные события (AttributedToExperimentId == null)
   - Связывает exposure → conversion через DecisionId
   - Обновляет Events.AttributedToExperimentId

**Ключевые файлы:**
- `LottyAB.Api/Controllers/EventsController.cs`
- `LottyAB.Application/Handlers/SendEventsHandler.cs`
- `LottyAB.Infrastructure/Services/EventAttributionService.cs`

---

## Критичный поток: GetExperimentReport

**Endpoint:** `GET /api/experiments/{id}/report?startDate=...&endDate=...`

**Путь выполнения:**

1. **ReportsController → GetExperimentReportHandler**
   - Загружает эксперимент с вариантами
   - Фильтрует события по периоду (startDate, endDate)
   - Подсчитывает экспозиции и конверсии для каждого варианта
   - Рассчитывает метрики (conversion_rate = conversions / exposures)
   - Возвращает отчёт в разрезе вариантов

**Ключевые файлы:**
- `LottyAB.Api/Controllers/ReportsController.cs`
- `LottyAB.Application/Handlers/GetExperimentReportHandler.cs`

---

## Критичный поток: Guardrails

**Background Service:** `GuardrailMonitoringService` (запускается каждые 60 сек)

**Путь выполнения:**

1. Находит все активные эксперименты (Status == Running) с guardrails
2. Для каждого эксперимента:
   - Получает текущие метрики из GetExperimentReportHandler
   - Сравнивает с порогом (Threshold)
   - Если превышен → выполняет действие (Pause/Rollback)
   - Сохраняет в GuardrailTriggerHistory

**Ключевые файлы:**
- `LottyAB.Infrastructure/Services/GuardrailMonitoringService.cs`

---

## Слои и зависимости (Clean Architecture)

```
        Api
         ↓
    Application  <-  Infrastructure
         ↓
      Domain  <-  Contracts
```

**Правила зависимостей:**
- **Domain** - не зависит ни от кого (чистые модели)
- **Contracts** - зависит от Domain (DTO ссылаются на Enums)
- **Application** - зависит от Domain и Contracts (handlers, services)
- **Infrastructure** - зависит от Application (реализует интерфейсы)
- **Api** - зависит от всех (точка входа)

---

## Конвенции именования

### Entities (Domain)
- Суффикс `Entity`: `FeatureFlagEntity`, `ExperimentEntity`, `DecisionEntity`
- Primary key: `Id` (Guid)
- Foreign keys: `<Entity>Id` (например, `FeatureFlagId`, `ExperimentId`)
- Timestamps: `CreatedAt`, `UpdatedAt`

### Commands/Queries (Application)
- Суффикс `Command` для write: `DecideCommand`, `CreateExperimentCommand`
- Суффикс `Query` для read: `GetExperimentReportQuery`
- Реализуют `IRequest<TResponse>`

### Handlers (Application)
- Суффикс `Handler`: `DecideHandler`, `SendEventsHandler`
- Реализуют `IRequestHandler<TRequest, TResponse>`

### Controllers (Api)
- Суффикс `Controller`: `DecideController`, `ExperimentsController`
- Route: `[Route("api/...")]`
- Методы: `[HttpPost]`, `[HttpGet]`, `[HttpPut]`

### Tests
- Суффикс `Tests`: `DecisionTests`, `EventTests`
- Методы: `<Method>_<Scenario>_<ExpectedResult>`
  - Пример: `Decide_WithNonExistentFeatureFlag_ReturnsNotFound`

---

## Где искать реализацию критериев (B1-B10)

### B1: Запуск и воспроизводимость
- `docs/RUNBOOK.md` - инструкции запуска
- `docker-compose.yml` - оркестрация
- `Dockerfile` - сборка образа

### B2: Feature Flags
- `LottyAB.Application/Handlers/DecideHandler.cs` - логика default/variant
- `LottyAB.Application/Services/TargetingEvaluator.cs` - таргетинг DSL
- `LottyAB.Application/Services/HashVariantSelector.cs` - выбор по весам
- `LottyAB.Tests/DecisionTests.cs` - тесты

### B3: Эксперименты (lifecycle + ревью)
- `LottyAB.Application/Handlers/SubmitExperimentHandler.cs` - Draft → InReview
- `LottyAB.Application/Handlers/ReviewExperimentHandler.cs` - InReview → Approved
- `LottyAB.Application/Handlers/StartExperimentHandler.cs` - Approved → Running
- `LottyAB.Tests/ExperimentTests.cs` - тесты переходов

### B4: События и атрибуция
- `LottyAB.Application/Handlers/SendEventsHandler.cs` - валидация, дедупликация
- `LottyAB.Infrastructure/Services/EventAttributionService.cs` - background атрибуция
- `LottyAB.Tests/EventTests.cs` - тесты

### B5: Guardrails
- `LottyAB.Infrastructure/Services/GuardrailMonitoringService.cs` - мониторинг
- `LottyAB.Tests/GuardrailTests.cs` - тесты

### B6: Отчётность
- `LottyAB.Application/Handlers/GetExperimentReportHandler.cs` - построение отчётов
- `LottyAB.Tests/ReportTests.cs` - тесты

### B7: Архитектура
- `docs/DECISIONS.md` - архитектурные решения
- `docs/COMPLIANCE_MATRIX.md` - трассировка требований
- `docs/REPOSITORY_MAP.md` - этот файл

### B8: Тестирование
- `LottyAB.Tests/` - интеграционные тесты
- `LottyAB.Tests/BaseTestFactory.cs` - SQLite In-Memory

### B9: Наблюдаемость
- `LottyAB.Api/Program.cs` - Health/Ready endpoints

### B10: Инженерная дисциплина
- `.editorconfig` (корень репозитория) — правила форматирования для C#, JSON, YAML
- `dotnet format src/LottyAB/LottyAB.sln` — применить форматирование
- `dotnet format src/LottyAB/LottyAB.sln --verify-no-changes` — проверить (lint)

---

## Полезные команды

### Запуск системы
```bash
docker-compose up -d              # Запуск PostgreSQL + API
curl http://localhost/api/health  # Проверка health
curl http://localhost/api/ready   # Проверка ready
docker-compose logs -f api        # Логи API
docker-compose down               # Остановка
```

### Работа с тестами
```bash
cd src/LottyAB
dotnet test                                     # Все тесты
dotnet test --filter "FullyQualifiedName~SmokeTests"  # Только Smoke тесты
dotnet test --collect:"XPlat Code Coverage"    # С покрытием
dotnet test --verbosity normal                  # Verbose вывод
```

### Линтинг и форматирование
```bash
# Из корня репозитория:
dotnet format src/LottyAB/LottyAB.sln                        # Форматировать код
dotnet format src/LottyAB/LottyAB.sln --verify-no-changes    # Проверить (lint, exit 0 = OK)
```

### Навигация по коду
```bash
# Найти все handlers
find ./LottyAB.Application/Handlers -name "*Handler.cs"

# Найти все контроллеры
find ./LottyAB.Api/Controllers -name "*Controller.cs"

# Найти использование DecideHandler
grep -r "DecideHandler" --include="*.cs"
```