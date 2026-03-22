# PERFORMANCE — Поведение под нагрузкой и ростом данных

## Горячие пути и индексы

### Критичные пути

| Путь | Запрос | Индекс |
|------|--------|--------|
| `POST /api/decide` | Поиск флага по `FeatureFlags.Key` | `IX_FeatureFlags_Key` (unique) |
| `POST /api/decide` | Поиск активного эксперимента по `Experiments.(FeatureFlagId, Status)` | `IX_Experiments_FeatureFlagId_Status` |
| `POST /api/decide` | Проверка участия субъекта по `SubjectParticipation.(SubjectId, ParticipatedAt)` | `IX_SubjectParticipation_SubjectId_ParticipatedAt` |
| `POST /api/events` | Дедупликация по `Events.EventId` | `IX_Events_EventId` (unique) |
| `POST /api/events` | Поиск типа события по `EventTypes.EventKey` | `IX_EventTypes_EventKey` (unique) |
| Attribution service | Выборка неатрибутированных событий по `Events.(IsAttributed, EventTimestamp)` | `IX_Events_IsAttributed_EventTimestamp` |
| Attribution service | Выборка событий по решению `Events.DecisionId` | `IX_Events_DecisionId` |
| Guardrail monitor | Выборка истории срабатываний `GuardrailTriggerHistory.ExperimentId` | `IX_GuardrailTriggerHistory_ExperimentId` |
| Reports | Выборка событий по решению и времени `Decisions.(ExperimentId, Timestamp)` | `IX_Decisions_ExperimentId_Timestamp` |

### SQL

```sql
CREATE UNIQUE INDEX "IX_FeatureFlags_Key"
    ON "FeatureFlags" ("Key");

CREATE INDEX "IX_Experiments_FeatureFlagId_Status"
    ON "Experiments" ("FeatureFlagId", "Status");

CREATE INDEX "IX_SubjectParticipation_SubjectId_ParticipatedAt"
    ON "SubjectParticipation" ("SubjectId", "ParticipatedAt");

CREATE UNIQUE INDEX "IX_Events_EventId"
    ON "Events" ("EventId");

CREATE UNIQUE INDEX "IX_EventTypes_EventKey"
    ON "EventTypes" ("EventKey");

CREATE INDEX "IX_Events_IsAttributed_EventTimestamp"
    ON "Events" ("IsAttributed", "EventTimestamp");

CREATE INDEX "IX_Events_DecisionId"
    ON "Events" ("DecisionId");

CREATE INDEX "IX_Decisions_ExperimentId_Timestamp"
    ON "Decisions" ("ExperimentId", "Timestamp");

CREATE INDEX "IX_GuardrailTriggerHistory_ExperimentId"
    ON "GuardrailTriggerHistory" ("ExperimentId");
```

---

## Ожидаемая нагрузка

| Эндпоинт | Ожидаемый RPS | Характер |
|----------|---------------|----------|
| `POST /api/decide` | 500–2000 | Read-heavy, latency-critical |
| `POST /api/events` | 1000–5000 | Write-heavy, bulk |
| `GET /api/reports/...` | 10–50 | Read, допустима задержка |
| `GET /api/health`, `/ready` | 1–5 | Служебный |

---

## Рост данных

### Таблицы с высоким темпом роста

| Таблица | Темп роста | Стратегия |
|---------|------------|-----------|
| `Events` | Высокий — по одному на каждое событие из продукта | Индексы по `DecisionId`, `EventTimestamp`, `IsAttributed`; при необходимости — партиционирование по `EventTimestamp` (range partitioning в PostgreSQL) |
| `Decisions` | Средний — по одному на каждый `decide`-запрос | Индекс по `(ExperimentId, Timestamp)`; архивация завершённых экспериментов |
| `SubjectParticipation` | Средний — по одной записи на участника эксперимента | Составной PK `(SubjectId, ExperimentId)` исключает дубли; используется 30-дневное окно при проверке лимита |
| `GuardrailTriggerHistory` | Низкий — только при срабатывании | Индекс по `ExperimentId` |

---

## Масштабирование

| Сценарий | Подход |
|----------|--------|
| Горизонтальное масштабирование API | Stateless API — можно запускать несколько инстансов за балансировщиком |
| Конкурентные фоновые сервисы | `EventAttributionService` и `GuardrailMonitoringService` — singleton hosted services; при нескольких инстансах потребуется distributed lock (например, `pg_advisory_lock`) |
| Большой объём событий | Вынести приём событий в очередь (Kafka / RabbitMQ) и обрабатывать асинхронно |
| Кэширование decide | Флаги и конфигурация экспериментов изменяются редко — можно кэшировать in-process (IMemoryCache) с TTL 30–60 сек |

---

## Мониторинг под нагрузкой

Метрики доступны на `/metrics` (Prometheus format, `prometheus-net.AspNetCore`):

- `http_requests_received_total` — счётчик HTTP-запросов по методу/пути/коду
- `http_request_duration_seconds` — гистограмма задержек
- `dotnet_gc_collections_total` — GC активность
- `dotnet_threadpool_threads_total` — активность thread pool
