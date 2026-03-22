# ARCHITECTURE DECISIONS

Документ описывает ключевые архитектурные решения, обоснования и trade-offs.

---

## 1. Clean Architecture + CQRS

### Контекст

A/B платформа имеет чёткое разделение на команды (создание экспериментов, отправка событий) и запросы (decide, отчёты). Разная нагрузка на чтение (decide - high RPS) и запись (события - batching).

### Решение

Применена **Clean Architecture** с разделением на слои:
- **Domain** - бизнес-модели (FeatureFlag, Experiment, Event, Decision)
- **Application** - бизнес-логика (CQRS handlers, services)
- **Infrastructure** - EF Core, репозитории, внешние зависимости
- **Api** - контроллеры, middleware
- **Contracts** - DTOs для API

**CQRS** (Command Query Responsibility Segregation):
- Commands: CreateExperiment, SendEvents, CompleteExperiment, ...
- Queries: GetExperimentReport, GetEventType ...

### Альтернативы

1. **Monolithic Service Layer** - всё в одном слое сервисов
   - Смешивает команды и запросы
   - Сложнее масштабировать чтение отдельно от записи

2. **Event Sourcing + CQRS** - полное ES с event store
   - Overkill для MVP
   - Усложняет отладку
   - Потенциально нужно для audit trail (будущее)

### Обоснование

- **Разделение ответственности**: Domain не зависит от Infrastructure
- **Тестируемость**: handlers легко покрыть unit-тестами
- **Масштабируемость**: чтение (decide/reports) можно вынести в отдельные read replicas
- **Явная бизнес-логика**: каждый handler = один use-case

### Trade-offs

- **Больше кода**: нужны handlers, validators, mappers
- **Выигрыш**: явная структура, легко найти где реализован критерий

### Последствия

- Критичный путь `decide - events - reports` имеет явную структуру
- Можно добавлять новые команды/запросы без рефакторинга существующих
- В будущем можно разделить на read/write databases

---

## 2. MediatR для CQRS

### Проблема

Нужен механизм маршрутизации команд/запросов к соответствующим handlers.

### Решение

Использован **MediatR** - библиотека медиатор-паттерна для .NET.

```csharp
public record DecideCommand(string FeatureFlagKey, string SubjectId) : IRequest<DecideResponse>;

public class DecideHandler : IRequestHandler<DecideCommand, DecideResponse>
{
    public async Task<DecideResponse> Handle(DecideCommand request, CancellationToken ct) { ... }
}
```

### Альтернативы

1. **Ручной dispatcher** - свой механизм маршрутизации
   - - Нужно писать и тестировать самим
   - - Нет поддержки pipelines (validation, logging)

2. **Прямой вызов handlers** - контроллеры напрямую вызывают handlers
   - - Tight coupling между Api и Application
   - - Нет единой точки для cross-cutting concerns

### Обоснование

- **Стандартная библиотека**: зрелая, проверенная в production
- **Pipeline behaviors**: единая точка для валидации, логирования, транзакций
- **Dependency Injection**: автоматическая регистрация handlers
- **Декларативность**: `Send(command)` понятнее чем прямой вызов

### Trade-offs

- **Зависимость**: добавляет внешний NuGet пакет
- **Выигрыш**: чистые контроллеры, расширяемость

**Файлы:**
- `LottyAB.Api/Program.cs:42-46` - регистрация MediatR
- `LottyAB.Application/Handlers/` - все handlers

---

## 3. EF Core + PostgreSQL

### Проблема

Нужно хранилище для: флагов, экспериментов, событий, решений, метрик.

Требования:
- Связи между сущностями (FK constraints)
- Аудит и версионность экспериментов
- Временные фильтры для отчётов

### Решение

**PostgreSQL 16** как основная БД.
**Entity Framework Core 10** как ORM.

### Альтернативы

1. **NoSQL (MongoDB/Cassandra)** - для событий
   - Лучше для высокой нагрузки на запись
   - Нет JOIN'ов - сложнее атрибуция events-decisions
   - Eventual consistency - риск для guardrails

2. **Dapper + чистый SQL** - вместо EF Core
   - Быстрее на сложных запросах
   - Больше кода для маппинга
   - Нет миграций из коробки

3. **Redis distributed cache** - для decide
   - Реализован (см. раздел 7)

### Обоснование

- **FK constraints**: защита от битых ссылок (decisionId - experiment)
- **Временные типы**: timestamptz для событий и атрибуции
- **Миграции**: EF Core Migrations для версионирования схемы

### Trade-offs

- **Масштабирование записи**: PostgreSQL хуже Cassandra на write-heavy нагрузках
- **Decide latency**: без кеша каждый decide идёт в БД
- **Выигрыш**: простота разработки

### Последствия

- События (Events) могут вырастать - нужно партиционирование по timestamp
- Decisions могут вырастать - нужно партиционирование
- Redis для кеша экспериментов реализован (см. раздел 7)

**Файлы:**
- `LottyAB.Infrastructure/Persistence/AppDbContext.cs`
- `LottyAB.Infrastructure/Persistence/Migrations/`

---

## 4. Background Services для атрибуции

### Проблема

События могут приходить не по порядку (exposure после conversion). Нужно связывать события с решениями (decision_id).

### Решение

**Background Services** (Hosted Services в ASP.NET Core):
- `EventAttributionService` - каждые 30 секунд обрабатывает неатрибутированные события
- `GuardrailMonitoringService` - каждые 60 секунд проверяет guardrails

```csharp
public class EventAttributionService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await AttributePendingEventsAsync();
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        }
    }
}
```

### Альтернативы

1. **Real-time обработка в API** - атрибуция при POST /api/events
   - Блокирует ответ клиенту
   - Если exposure ещё не пришла - нужно ждать или откладывать

2. **Message Queue (RabbitMQ/Kafka)** - очередь событий
   - Лучше для production (гарантии доставки, retry)
   - Overkill для MVP
   - Усложняет деплой (нужен отдельный сервис)

3. **Azure Functions / AWS Lambda** - serverless обработка
   - Масштабируется автоматически
   - Vendor lock-in
   - Усложняет локальную разработку

### Обоснование

- **Простота**: встроено в ASP.NET Core, нет внешних зависимостей
- **Асинхронность**: не блокирует API
- **Retry логика**: если атрибуция не прошла - попытается через 30 сек
- **Окно ожидания**: события хранятся до 7 дней (как в ТЗ)

### Trade-offs

- **Нет гарантий**: если процесс упадёт - обработка остановится
- **Delay**: атрибуция не мгновенная (до 30 сек задержка)
- **Single instance**: не работает при горизонтальном масштабировании (нужен distributed lock)
- **Выигрыш**: работает "из коробки", достаточно для демо/MVP

### Последствия

- Для production нужно:
  - Message queue (Kafka/RabbitMQ)
  - Dead letter queue для битых событий

**Файлы:**
- `LottyAB.Infrastructure/Services/EventAttributionService.cs`
- `LottyAB.Infrastructure/Services/GuardrailMonitoringService.cs`

---

## 5. JWT для аутентификации

### Проблема

Нужна аутентификация для разных ролей: Admin, Experimenter, Approver, Viewer.

### Решение

**JWT (JSON Web Tokens)** с claims:
- `sub` - userId
- `email` - email пользователя
- `role` - роль (Admin/Experimenter/Approver/Viewer)

**Примечание:** В классе **LottyAB.Api/Program.cs** явно описаны политики авторизации.

```csharp
[Authorize("EXPERIMENTER")]
public class ExperimentsController { ... }
```

### Альтернативы

1. **Session-based auth** - cookie с session ID
   - Проще отзывать (просто удалить сессию)
   - Stateful - нужен session store (Redis)
   - Не подходит для API-first подхода

2. **OAuth2 / OpenID Connect** - делегирование Auth0/Keycloak
   - Production-ready, SSO
   - Overkill для MVP
   - Внешняя зависимость

3. **API Keys** - статичные ключи
   - Нет ролей
   - Сложно ротировать

### Обоснование

- **Stateless**: не нужен session store
- **API-friendly**: передаётся в Authorization header
- **Claims**: роли и метаданные внутри токена
- **Стандарт**: библиотеки для всех языков

### Trade-offs

- **Нельзя отозвать**: токен валиден до expiry (30 минут в нашем случае)
- **Размер**: JWT больше чем session ID
- **Выигрыш**: простота, масштабируемость

### Последствия

- Для production:
  - Refresh tokens (не реализовано в MVP)
  - Token blacklist для logout (Redis)
  - Короткий TTL (5-15 минут)

**Файлы:**
- `LottyAB.Infrastructure/Authentication/JwtService.cs`
- `LottyAB.Api/Program.cs:20-40` - JWT configuration

---

## 6. SQLite In-Memory для тестов

### Проблема

Тесты должны быть:
- Быстрыми
- Изолированными (не влияют друг на друга)
- Не требуют внешних зависимостей

### Решение

**SQLite In-Memory** для тестов:
```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("DataSource=:memory:"));
```

### Альтернативы

1. **PostgreSQL в Docker** - реальная БД для тестов
   - Ближе к production
   - Медленнее
   - Нужен Docker для CI/CD

2. **In-Memory EF Provider** - встроенный in-memory
   - Не поддерживает FK constraints
   - Не поддерживает транзакции полностью

### Обоснование

- **Скорость**: быстрое выполнение тестов (~10 секунд)
- **Изоляция**: каждый тест создаёт новую БД в памяти
- **FK constraints**: SQLite поддерживает (в отличие от InMemory provider)
- **Простота**: не нужен Docker для запуска тестов

### Trade-offs

- **Не 100% PostgreSQL**: некоторые функции отличаются
- **Нет JSONB**: subject attributes хранятся как TEXT
- **Выигрыш**: быстрые тесты без внешних зависимостей

**Файлы:**
- `LottyAB.Tests/BaseTestFactory.cs`

---

## 7. Redis как distributed cache для decide

### Проблема

`DecideHandler` при каждом вызове делает запрос в PostgreSQL за `FeatureFlagEntity` с экспериментами и вариантами. При горизонтальном масштабировании API in-process кеш (`IMemoryCache`) не разделяется между инстансами - каждый инстанс кеширует независимо, нагрузка на БД не снижается.

### Решение

**Redis 7** через `IDistributedCache` (`StackExchange.Redis`).

Сериализация через приватные cache-DTO внутри `DecideHandler`:

```csharp
private sealed class FeatureFlagCacheEntry { ... List<ExperimentCacheEntry> Experiments }
private sealed class ExperimentCacheEntry  { ... List<VariantCacheEntry> Variants }
private sealed class VariantCacheEntry     { ... }
```

TTL - 60 секунд. Под "Testing" окружением Redis не регистрируется - вместо него `AddDistributedMemoryCache` в `BaseTestFactory`.

### Альтернативы

1. **IMemoryCache** - оставить как было
   - Кеш не разделяется между инстансами
   - При 3 инстансах - 3 независимых кеша, 3x нагрузка на БД

2. **Без кеша** - каждый decide идёт в БД
   - JOIN на experiments + variants на каждый запрос
   - При RPS > 1000 - bottleneck

### Обоснование

- **Горизонтальное масштабирование**: кеш общий для всех инстансов
- **Снижение нагрузки на БД**: повторные запросы к одному флагу за 60 секунд не идут в PostgreSQL
- **Стандартный интерфейс**: `IDistributedCache` - стандарт .NET, легко заменить на другой бэкенд

### Trade-offs

- **Новая инфраструктурная зависимость**: Redis нужен в docker-compose и в prod-деплое
- **Eventual consistency**: изменения флага/эксперимента видны с задержкой до 60 секунд
- **Сложность DTO**: нужно поддерживать маппинг entity <-> cache entry при изменении схемы
- **Выигрыш**: линейное масштабирование decide по инстансам без роста нагрузки на БД

### Последствия

- При изменении полей `FeatureFlagEntity` / `ExperimentEntity` / `VariantEntity` - обновлять соответствующий cache entry класс и методы маппинга
- Cache invalidation не реализован - при изменении флага старое значение живёт до TTL (60 сек)
- Для production: sentinel/cluster вместо standalone Redis, TTL можно вынести в конфиг

**Файлы:**
- `LottyAB.Application/Handlers/DecideHandler.cs` - get-check-set, DTO, маппинг
- `LottyAB.Api/Program.cs` - регистрация `AddStackExchangeRedisCache`
- `LottyAB.Api/appsettings.json` - `ConnectionStrings.Redis`
- `LottyAB.Tests/BaseTestFactory.cs` - `AddDistributedMemoryCache` для тестов
- `docker-compose.yml` - сервис `redis:7-alpine`

---

## Ограничения и упрощения

### Что НЕ реализовано из ТЗ

#### 1. Conflict Resolution (доп. фича #11)

**Проблема:** Несколько экспериментов могут конфликтовать (тестировать одну зону продукта).

**Статус:** - Реализовано

**Реализация:**
- `EConflictPolicy` enum: `MutualExclusion` — один субъект попадает только в один эксперимент в домене
- `ExperimentEntity` расширен: `ConflictDomains` (строка, домены через запятую), `ConflictPolicy` (`EConflictPolicy?`), `Priority` (`int`, по умолчанию 0)
- Поля пробрасываются через Create/Update request → command → handler → entity
- `DecideHandler.IsConflictDomainWinner`: при `MutualExclusion` запрашивает из БД все конкурирующие Running-эксперименты в тех же доменах; побеждает эксперимент с наибольшим `Priority`; при равном приоритете — детерминированно по `SHA256(subjectId:experimentId)` (максимальный score выигрывает); проигравший эксперимент возвращает default
- `ExperimentCacheEntry` (в DecideHandler) расширен соответствующими полями для корректной работы через Redis-кеш

#### 2. Autopilot Ramp-up (доп. фича #10)

**Проблема:** Нет автоматической раскатки с 1% - 5% - 25% - 50% - 100%.

**Статус:** Реализовано полностью

**Реализация:**
- `POST /api/experiments/{id}/ramp` — ручная раскатка: увеличить `AudienceFraction` без повторного review-цикла; веса вариантов масштабируются пропорционально; создаётся snapshot версии
- `CompleteExperimentCommand.WinnerVariantId` — при завершении с `RolloutWinner` + `WinnerVariantId` обновляет `FeatureFlag.DefaultValue` до значения победившего варианта и инвалидирует Redis-кеш
- **Автопилот**: `AutopilotRampService` (BackgroundService) автоматически продвигает долю аудитории через заданные ступени (steps) каждую минуту при выполнении gates:
  - Safety gate: нет триггеров `GuardrailTriggerHistory` с момента входа на текущую ступень
  - Time gate: прошло не менее `MinMinutesPerStep` минут
  - Impression gate: не менее `MinImpressionsPerStep` решений (decisions) с момента входа на ступень
- При нарушении safety gate применяется `SafetyAction`: `Pause` (пауза эксперимента), `RollbackToControl` (завершение с Rollback), `StepBack` (откат на предыдущую ступень; при индексе 0 → пауза)
- `RampPlanEntity`: хранит steps (JSON), CurrentStepIndex, MinImpressions, MinMinutes, SafetyAction, IsEnabled, IsCompleted, StepEnteredAt
- `RampPlanHistoryEntity`: лог каждого действия (Advanced, Paused, RolledBack, SteppedBack, Completed) с fraction from/to и причиной
- API: `POST /api/experiments/{id}/autopilot` (создать план), `GET` (получить план), `GET .../history` (история), `POST .../enable`, `POST .../disable`
- Каждое действие автопилота: сохраняет snapshot версии (`ExperimentVersions`), инвалидирует Redis-кеш флага, отправляет уведомление

#### 3. Notifications Platform (доп. фича #7)

**Проблема:** Нет уведомлений в Telegram/Slack о guardrails, начале экспериментов и т.п.

**Статус:** - Реализовано

**Реализация:**
- `INotificationService` (Application/Interfaces) + `NotificationService` (Infrastructure/Services)
- Telegram: `POST https://api.telegram.org/bot{token}/sendMessage` с `parse_mode=HTML`, поддержка нескольких `ChatIds` через запятую
- Slack: Slack App API `POST https://slack.com/api/chat.postMessage` с `Authorization: Bearer {BotToken}`, поддержка нескольких `ChannelIds` через запятую, проверка поля `ok` в ответе
- Пустой токен/список каналов -> канал пропускается без ошибки; ошибки отправки -> `LogWarning`, не бросает исключение
- Уведомления при: guardrail (пауза/откат), старт, завершение, отправка на ревью, одобрение эксперимента
- Конфигурация через env vars: `Notifications__Telegram__BotToken`, `Notifications__Telegram__ChatIds`, `Notifications__Slack__BotToken`, `Notifications__Slack__ChannelIds`

#### 4. Learnings Library (доп. фича #9)

**Проблема:** Нет поиска похожих экспериментов и базы знаний.

**Статус:** - Не реализовано

**Обоснование:** Руки не дошли

#### 5. Experiment Insights UI (доп. фича #8)

**Проблема:** Нет UI для визуализации графиков метрик.

**Статус:** - Не реализовано (только REST API)

**Обоснование:** Руки не дошли

---

## Известные риски

### 1. Performance риски

#### Decide endpoint — distributed cache
**Риск:** Каждый `/api/decide` идёт в PostgreSQL — высокая latency при RPS > 1000

**Митигация (реализовано):**
- `IDistributedCache` (Redis) в `DecideHandler` — конфигурация флага (experiments + variants) кешируется на 60 секунд; за это время повторный запрос не идёт в БД; кеш разделяется между инстансами
- Два запроса к `SubjectParticipation` объединены в один (`ToListAsync` + in-memory фильтрация)

**Текущее состояние:** Redis реализован; кеш разделяется между инстансами при горизонтальном масштабировании

#### Events таблица растёт без лимита
**Риск:** События накапливаются — медленные запросы, большой размер БД

**Митигация (не реализовано):**
- Партиционирование Events по timestamp (PostgreSQL range partitioning)
- Архивация старых событий (>90 дней) в холодное хранилище
- TTL для неатрибутированных событий (>7 дней)

**Текущее состояние:** Для демо нормально, для production нужна стратегия роста

### 2. Concurrency риски

#### Background Services + горизонтальное масштабирование
**Риск:** При 2+ инстансах API оба `GuardrailMonitoringService` и `EventAttributionService` работают одновременно — дублирование обработки, race conditions

**Митигация (не реализовано):**
- Distributed lock (`pg_advisory_lock` или Redis)
- Выделенный worker-процесс для background tasks

**Текущее состояние:** Работает корректно только для single instance

### 3. Security риски

#### JWT без refresh tokens
**Риск:** Токен нельзя отозвать до истечения TTL (30 минут)

**Митигация (не реализовано):**
- Refresh tokens + token blacklist (Redis)

**Текущее состояние:** Для демо достаточно

### 4. Data Quality риски

#### Нет SRM (Sample Ratio Mismatch) детекции
**Риск:** Перекос трафика между вариантами (ожидается 50/50, реально 70/30) не детектируется автоматически

**Митигация (не реализовано):**
- Chi-square тест для проверки распределения
- Предупреждение в отчёте если фактическое распределение отличается от весов >10%

**Текущее состояние:** Полагаемся на корректность хеш-функции; тесты подтверждают распределение в пределах ±5%

#### События без валидации схемы атрибутов
**Риск:** Subject attributes могут содержать произвольные поля — сложно строить консистентные разрезы в отчётах

**Митигация (не реализовано):**
- JSON Schema для валидации subject attributes
- Каталог обязательных полей (platform, country, app_version)

**Текущее состояние:** Валидация только обязательных полей события, не схемы атрибутов

---

## Заключение

Архитектура проекта построена на принципах:
- **Простота**: стандартные паттерны (Clean Arch, CQRS), зрелые библиотеки
- **Тестируемость**: интеграционные тесты покрывают критичные пути
- **Расширяемость**: легко добавлять новые команды/запросы/метрики
- **Осознанные trade-offs**: упрощения задокументированы, пути миграции понятны

**Критичный путь работает:** decide - events - reports - guardrails

---