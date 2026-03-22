# COMPLIANCE MATRIX

Матрица соответствия требований из task.md критериям из criteria.md.

## Легенда статусов

- [OK] **Реализовано** - критерий полностью выполнен и проверяем
- [PARTIAL] **Частично** - критерий выполнен частично или требует доработки
- [NO] **Не реализовано** - критерий не выполнен

---

## B1. Запуск и воспроизводимость (10 баллов)

| ID задания | ID критерия | Проблема/риск | Где реализовано | Как проверяется | Какие данные нужны | Статус |
|------------|-------------|---------------|-----------------|-----------------|-------------------|--------|
| D.4 | B1-1 | Без явных предусловий невозможно воспроизвести запуск системы, что блокирует проверку жюри | `RUNBOOK.md` раздел "Предусловия" | Открыть RUNBOOK.md и проверить наличие списка зависимостей: .NET 10, Docker, PostgreSQL, порты 5432 и 80 | Нет | [OK] |
| D.4 | B1-2 | Без точных команд запуска жюри не сможет поднять систему за 30 минут | `RUNBOOK.md` раздел "Быстрый старт" | Выполнить: `docker-compose up -d` и проверить что контейнеры запустились | docker-compose.yml, Dockerfile | [OK] |
| D.4 | B1-3 | Если требуются неописанные ручные действия, система не воспроизводима | `docker-compose.yml`, `DbInitializer.cs` | Выполнить только `docker-compose up -d` без других команд. Система должна инициализировать БД автоматически | Нет | [OK] |
| D.4 | B1-4 | Если сервисы не стартуют или падают, демонстрация невозможна | `Program.cs`, `docker-compose.yml` | Выполнить `docker-compose ps` и проверить статус "Up (healthy)" для обоих контейнеров | Нет | [OK] |
| D.4 | B1-5 | Без сквозного happy-path нельзя подтвердить работоспособность критичного пути decide-event-report | `FullExperimentFlowTests.cs` | Запустить интеграционный тест: `dotnet test --filter FullyQualifiedName~FullExperimentFlowTests` | admin@lottyab.com / lottyab | [OK] |

---

## B2. Feature Flags и выдача вариантов (10 баллов)

| ID задания | ID критерия | Проблема/риск | Где реализовано | Как проверяется | Какие данные нужны | Статус |
|------------|-------------|---------------|-----------------|-----------------|-------------------|--------|
| 1.3, 3.4 | B2-1 | При отсутствии активного эксперимента система возвращает не default, из-за чего ломается контрольный сценарий и сравнение вариантов | `DecisionService.cs` | `POST /api/decide` для флага без активного эксперимента; ожидается возврат default и isDefault=true | Флаг с default="green", отсутствует активный эксперимент, subjectId="test-user" | [OK] |
| 2.7, 3.4 | B2-2 | Пользователь вне таргетинга получает экспериментальный вариант, что искажает метрики и нарушает сегментацию | `TargetingEvaluator.cs` | Создать эксперимент с правилом `country == "US"`, отправить decide с country="UK", ожидается isDefault=true | Эксперимент с таргетингом, subjectId="uk-user", attributes: {country: "UK"} | [OK] |
| 3.4 | B2-3 | При применимости эксперимента возвращается default вместо варианта, что делает A/B тестирование невозможным | `DecisionService.cs` | Создать running эксперимент, отправить decide для подходящего пользователя, ожидается variant из эксперимента | Running эксперимент, subjectId="us-user", attributes: {country: "US"} | [OK] |
| 3.5.1 | B2-4 | Результат меняется без изменения конфигурации, что делает UX нестабильным и метрики несравнимыми | `VariantSelector.cs` (хеш от subjectId+experimentId) | Тест `DecisionTests.Decide_SameSubject_ReturnsSameVariant`: 100 запросов с одним subjectId должны вернуть один variantKey | SubjectId="consistent-user", running эксперимент | [OK] |
| 2.2, 3.4 | B2-5 | Фактическое распределение не соответствует весам, что нарушает корректность статистического анализа | `VariantSelector.cs` | Тесты: `Decide_With50_50Weights` (475-525), `Decide_With70_30Weights` (650-750/250-350), `Decide_With80_20Weights` (750-850/150-250) для 1000 пользователей | 1000 уникальных subjectId, эксперимент с заданными весами | [OK] |

---

## B3. Эксперименты: жизненный цикл и ревью (10 баллов)

| ID задания | ID критерия | Проблема/риск | Где реализовано | Как проверяется | Какие данные нужны | Статус |
|------------|-------------|---------------|-----------------|-----------------|-------------------|--------|
| 2.4, 2.5 | B3-1 | Переход отсутствует или даёт неверный статус, что блокирует процесс ревью | `SubmitExperimentHandler.cs` | `POST /api/experiments/{id}/submit`, проверить что status изменился с Draft на InReview | Эксперимент в статусе Draft | [OK] |
| 2.4, 2.5 | B3-2 | При выполнении условий статус не меняется корректно, что блокирует запуск одобренного эксперимента | `ReviewExperimentHandler.cs` | Создать эксперимент, submit, approve (достичь порога одобрений), проверить status=Approved | Эксперимент InReview, Approver с правами, минимальный порог=1 | [OK] |
| 2.4 | B3-3 | Эксперимент запускается без нужных одобрений, что нарушает политику безопасности | `StartExperimentHandler.cs` | Попытка `POST /api/experiments/{id}/start` без одобрений должна вернуть 400/422 | Эксперимент InReview без одобрений | [OK] |
| 2.5 | B3-4 | Можно выполнить переход вне правил (например Draft-Running напрямую), что нарушает процесс | `ExperimentStateMachine.cs` или валидация в handlers | Попытка выполнить недопустимый переход, ожидается 400/422 | Эксперимент в любом статусе | [OK] |
| 0.2, 2.4 | B3-5 | Неназначенный пользователь может повлиять на ревью, что нарушает разграничение прав | `ReviewExperimentHandler.cs`, `AuthorizationService` | Попытка approve от пользователя без роли Approver/Admin, ожидается 403 Forbidden | Пользователь с ролью Viewer | [OK] |

---

## B4. События и атрибуция (10 баллов)

| ID задания | ID критерия | Проблема/риск | Где реализовано | Как проверяется | Какие данные нужны | Статус |
|------------|-------------|---------------|-----------------|-----------------|-------------------|--------|
| 4.2, 4.5 | B4-1 | Событие с неверным типом поля принимается как валидное, что приводит к некорректным метрикам | `SendEventsValidator.cs`, `EventTypeRepository` | Отправить событие с eventTimestamp="invalid-date", ожидается rejected=1 с описанием ошибки | EventTypeKey="exposure", decisionId=valid GUID, eventTimestamp="not-a-date" | [OK] |
| 4.2, 4.5 | B4-2 | Неполное событие принимается, что приводит к ошибкам в атрибуции и отчётах | `SendEventsValidator.cs` | Отправить событие без обязательного поля (например, без decisionId), ожидается rejected=1 | EventTypeKey="exposure", без decisionId | [OK] |
| 4.3, 4.5 | B4-3 | Дубликат учитывается как новое событие, что завышает метрики конверсий/кликов | `SendEventsHandler.cs`, дедупликация по EventId | Тест `SendEvents_WithDuplicateEventIds`: отправить 2 события с одним eventId, ожидается accepted=1, duplicates=1 | EventId="dup-001", одинаковый для двух событий | [OK] |
| 4.4 | B4-4 | Экспозиция не связана с решением, что делает невозможной корректную атрибуцию конверсий | `Event` entity, `DecisionId` foreign key | Отправить событие exposure, проверить что в БД Events.DecisionId = переданному decisionId | DecisionId из предыдущего /decide, eventTypeKey="exposure" | [OK] |
| 4.4 | B4-5 | Конверсия учитывается без подтверждённой экспозиции, что искажает воронку и метрики | `EventAttributionService.cs` | Отправить conversion без exposure (или exposure с другим decisionId), проверить что IsAttributed=false | DecisionId без соответствующего exposure события | [OK] |

---

## B5. Устойчивость и safety-механики (10 баллов)

| ID задания | ID критерия | Проблема/риск | Где реализовано | Как проверяется | Какие данные нужны | Статус |
|------------|-------------|---------------|-----------------|-----------------|-------------------|--------|
| 6.2 | B5-1 | Метрика контроля не задана или невалидна, что делает guardrail бесполезным | `GuardrailRule` entity, `MetricKey` property | Создать guardrail, проверить что в БД GuardrailRules.MetricKey сохранён корректный ключ метрики | MetricKey="error_rate", experimentId | [OK] |
| 6.2 | B5-2 | Порог не задан или не используется, что делает невозможным автоматическое срабатывание | `GuardrailRule` entity, `Threshold` property | Создать guardrail с порогом 0.05, проверить что в БД GuardrailRules.Threshold = 0.05 | Threshold=0.05, MetricKey="error_rate" | [OK] |
| 6.3 | B5-3 | Превышение есть, но система его не обнаруживает, что приводит к деградации без остановки | `GuardrailMonitoringService.cs` | Создать эксперимент с guardrail (error_rate > 0.05), отправить события с ошибками >5%, проверить срабатывание | Эксперимент, события с errors превышающими порог | [OK] |
| 6.4 | B5-4 | Действие не выполняется или не соответствует правилу, что не защищает продукт от деградации | `GuardrailMonitoringService.cs`, `PauseExperimentHandler` | После превышения порога проверить что эксперимент перешёл в статус Paused или выполнен rollback | Running эксперимент с guardrail, события превышающие порог | [OK] |
| 6.5 | B5-5 | Срабатывание не отражено в истории, что делает невозможным анализ и аудит | `GuardrailTriggerHistory` entity | Проверить что в БД GuardrailTriggerHistory появилась запись с: metricKey, threshold, actualValue, triggeredAt, action | После срабатывания guardrail | [OK] |
| 3.6 | B5-6 | Ограничение не применяется, один пользователь постоянно участвует во всех экспериментах | `DecideHandler.cs` — `MaxConcurrentExperiments = 3`, проверка по `SubjectParticipation` за 30 дней | Тест `DecisionTests.Decide_ConcurrentExperimentLimit_ReturnsDefaultAfterThree`: 4 эксперимента, один subjectId → 3 variant + 1 default | 4 running эксперимента, один subjectId | [OK] |

---

## B6. Отчётность и принятие решения (10 баллов)

| ID задания | ID критерия | Проблема/риск | Где реализовано | Как проверяется | Какие данные нужны | Статус |
|------------|-------------|---------------|-----------------|-----------------|-------------------|--------|
| 5.2 | B6-1 | Фильтр периода отсутствует или не влияет, что делает невозможным анализ за нужный период | `GetExperimentReportHandler.cs`, query parameters | Тест `GetExperimentReport_WithDateRange`: `GET /api/reports/experiments/{id}?startDate=2026-01-01&endDate=2026-02-01` | ExperimentId, события в разные даты | [OK] |
| 5.3 | B6-2 | Есть только агрегат без разреза вариантов, что делает невозможным сравнение A vs B | `ExperimentReportResponse`, `VariantMetrics` | Проверить что в ответе GET /api/reports/experiments/{id} есть метрики отдельно для каждого варианта | Эксперимент с 2+ вариантами, события по каждому | [OK] |
| 5.4 | B6-3 | В отчёте нет части выбранных метрик, что не позволяет оценить эффект по нужным показателям | `GetExperimentReportHandler.cs`, `MetricCalculationService` | Создать эксперимент с primaryMetricKey="conversion_rate", проверить что в отчёте есть эта метрика | Эксперимент с заданной primaryMetricKey, события | [OK] |
| 2.6 | B6-4 | Исход нельзя зафиксировать или набор исходов неполный, что не позволяет закрыть эксперимент с решением | `CompleteExperimentHandler.cs`, `ECompletionOutcome` enum | `POST /api/experiments/{id}/complete` с outcome: RolloutWinner/Rollback/NoEffect | Running эксперимент | [OK] |
| 2.6 | B6-5 | Решение сохраняется без объяснения, что делает невозможным понимание почему выбран этот вариант | `Experiment` entity, `OutcomeComment` property | Отправить complete с comment="Green button performed better", проверить в БД Experiments.OutcomeComment | Comment в запросе | [OK] |

---

## B7. Архитектура и ориентируемость (9 баллов)

| ID задания | ID критерия | Проблема/риск | Где реализовано | Как проверяется | Какие данные нужны | Статус |
|------------|-------------|---------------|-----------------|-----------------|-------------------|--------|
| D.5 | B7-1 | Нейминг создаёт двусмысленность и требует догадок, что замедляет ориентирование в коде | Весь проект: Controllers, Entities, Handlers, Services | Проверить примеры: `DecideController`, `FeatureFlag`, `SendEventsRequest`, `GuardrailTriggerHistory` - названия отражают назначение | Нет | [OK]   |
| D.5 | B7-2 | Границы модулей неочевидны или ответственности смешаны, что усложняет изменения | Структура проектов: Api, Application, Domain, Infrastructure, Contracts | Проверить разделение: Domain - модели, Application - бизнес-логика (CQRS), Infrastructure - EF Core, Api - controllers | Нет | [OK]   |
| D.6 | B7-3 | Матрица отсутствует или не помогает проверке, что усложняет быструю оценку работы | **Этот документ** `COMPLIANCE_MATRIX.md` | Открыть COMPLIANCE_MATRIX.md и проверить заполнение всех строк для B1-B10 | Нет | [OK]   |
| D.5 | B7-4 | Решения не зафиксированы или противоречат фактической реализации, что создаёт недопонимание | `DECISIONS.md` | Открыть DECISIONS.md и проверить описание решений: Clean Architecture, CQRS, EF Core, JWT, Background Services | Нет | [OK]   |
| D.4 | B7-5 | Диаграмма отсутствует или не помогает понять границы системы | `docs/architecture/C4-Context.md` | Открыть диаграмму: акторы (Experimenter/Approver/Viewer/Admin/Product App) → LOTTY A/B Platform → PostgreSQL | Нет | [OK]   |
| D.4 | B7-6 | Диаграмма отсутствует или не помогает понять разбиение на сервисы/контейнеры | `docs/architecture/C4-Container.md` | Открыть диаграмму: LottyAB.Api, LottyAB.Application, LottyAB.Infrastructure (с Background Services), PostgreSQL | Нет | [OK]   |
| D.4 | B7-7 | Детализации нет или она не помогает объяснить критичный путь decide-event-report | `docs/architecture/C4-Component.md` | Открыть диаграмму: DecideController→DecideHandler→TargetingEvaluator+HashVariantSelector, EventsController→SendEventsHandler, ReportsController→GetExperimentReportHandler, GuardrailMonitoringService | Нет | [OK]   |
| D.5 | B7-8 | Карта отсутствует и по репозиторию сложно ориентироваться | `docs/REPOSITORY_MAP.md` | Открыть REPOSITORY_MAP.md: точки входа (Program.cs), структура папок, критичный поток decide/events/report | Нет | [OK]   |
| D.7 | B7-9 | Ограничения не зафиксированы или всплывают только в вопросах жюри | `DECISIONS.md` | Открыть DECISIONS.md и проверить раздел "Ограничения и упрощения" | Нет | [OK]    |

---

## B8. Тестирование (3 балла)

| ID задания | ID критерия | Проблема/риск | Где реализовано | Как проверяется | Какие данные нужны | Статус |
|------------|-------------|---------------|-----------------|-----------------|-------------------|--------|
| D.5 | B8-1 | Негативные сценарии не проверяются тестами, что повышает риск регрессий | `SmokeTests.cs`, `UsersTests.cs`, `ExperimentsTests.cs` | Проверить наличие тестов: `Login_WithInvalidCredentials`, `Decide_WithNonExistentFeatureFlag`, `CreateUser_WithoutAdminRole` | Нет | [OK] |
| D.5 | B8-2 | Нет тестов на критичное взаимодействие между модулями | `FullExperimentFlowTests.cs` | Проверить тест `CompleteExperimentWorkflow_FromCreationToReporting`: создание флага - эксперимент - decide - события - отчёт | admin credentials, тестовые данные | [OK] |
| D.4 | B8-4 | Отчёт отсутствует или непроверяем, что не позволяет оценить покрытие тестами | `docs/TEST_REPORT.md` | Открыть TEST_REPORT.md, проверить: команды запуска (`dotnet test`), покрытие (>75%), ~144 тест-кейса, список негативных тестов | Нет | [OK] |

---

## B9. Эксплуатационная готовность и наблюдаемость (6 баллов)

| ID задания | ID критерия | Проблема/риск | Где реализовано | Как проверяется | Какие данные нужны | Статус |
|------------|-------------|---------------|-----------------|-----------------|-------------------|--------|
| 3.7 | B9-1 | Проверка отсутствует, неработоспособна или поведение не соответствует договору | `HealthController.cs`, endpoint `/api/ready` | `curl http://localhost/api/ready`, ожидается 200 OK в течение 180 секунд после старта | Нет | [OK] |
| 3.7 | B9-2 | Проверка отсутствует, неработоспособна или возвращает неверный код | `HealthController.cs`, endpoint `/api/health` | `curl http://localhost/api/health`, ожидается 200 OK пока процесс жив | Нет | [OK] |
| D.4 | B9-3 | Метрики отсутствуют или по демо нельзя понять, что именно и где измеряется | `Program.cs` — `app.UseHttpMetrics()` + `app.MapMetrics()` (`prometheus-net.AspNetCore` 8.2.1) | `curl http://localhost/metrics`, проверить: `http_requests_received_total`, `http_request_duration_seconds` | Запущенный сервис | [OK] |
| D.4 | B9-4 | Логи неструктурированные (plain text), что усложняет автоматический анализ и поиск | `Program.cs` — `builder.Host.UseSerilog(...)` + `RenderedCompactJsonFormatter` (Serilog.AspNetCore 9.0.0) | `docker-compose logs api` — каждая строка в формате CLEF JSON: `{"@t":"...","@m":"...","@l":"Info",...}` | Запущенный сервис | [OK] |
| D.5 | B9-6 | Поведение при росте данных не описано/не реализовано, что создаёт риск деградации | `docs/PERFORMANCE.md` — ожидаемый RPS, горячие таблицы, стратегия партиционирования, масштабирование | Открыть PERFORMANCE.md: таблица нагрузки, стратегия архивации Events, горизонтальное масштабирование | Нет | [OK] |
| D.5 | B9-7 | Горячие пути без мер оптимизации (индексов), что приводит к медленным запросам | `AppDbContext.OnModelCreating` + SQL в `docs/PERFORMANCE.md` | `\d+ "Events"` в psql — индексы на DecisionId, IsAttributed+EventTimestamp, EventId; `\d+ "FeatureFlags"` — уникальный индекс на Key | Запущенная БД | [OK] |

---

## B10. Инженерная дисциплина (2 балла)

| ID задания | ID критерия | Проблема/риск | Где реализовано | Как проверяется | Какие данные нужны | Статус |
|------------|-------------|---------------|-----------------|-----------------|-------------------|--------|
| D.5 | B10-1 | Линтинг не автоматизирован, что приводит к несогласованному стилю кода | `.editorconfig` (корень репозитория), команда `dotnet format --verify-no-changes` | Выполнить `dotnet format --verify-no-changes src/LottyAB/LottyAB.sln` в корне репозитория; ожидается exit code 0 | Нет | [OK] |
| D.5 | B10-2 | Форматирование не автоматизировано, что приводит к различиям в отступах и стиле | `.editorconfig` (корень репозитория) | Выполнить `dotnet format src/LottyAB/LottyAB.sln` в корне репозитория; код форматируется автоматически | Нет | [OK] |

---

## Дополнительные фичи (бонус)

> Оценивается по критериям FX-1 (4 балла — рабочий сценарий) и FX-2 (3 балла — ограничения/условия). Итог: `min(20, сумма)`.

### #7 — Notifications Platform

| ID задания | ID критерия | Проблема/риск | Где реализовано | Как проверяется | Какие данные нужны | Статус |
|------------|-------------|---------------|-----------------|-----------------|-------------------|--------|
| 7 | FX-1 | Команда не узнаёт о событиях (guardrail, запуск, завершение) без ручной проверки — задержка реакции | `INotificationService` (Application/Interfaces), `NotificationService` (Infrastructure/Services); Telegram (`POST .../bot{token}/sendMessage`, `parse_mode=HTML`) и Slack (`POST .../chat.postMessage`, `Authorization: Bearer`); поддержка нескольких `ChatIds`/`ChannelIds` через запятую; env-vars: `Notifications__Telegram__BotToken/ChatIds`, `Notifications__Slack__BotToken/ChannelIds` | Задать env-vars → `POST /api/experiments/{id}/submit` → уведомление «На ревью»; одобрить → «Одобрено»; `start` → «Запущен»; `complete` → «Завершён»; guardrail trigger → «Guardrail сработал / откат к контролю». `GuardrailMonitoringTests` (9 тестов) подтверждает срабатывание Pause/RollbackToControl | Telegram Bot Token + Chat ID **или** Slack Bot Token + Channel ID; запущенный сервис с env-vars | [OK] |
| 7 | FX-2 | Ошибка отправки не должна нарушить основной процесс; пустая конфигурация не должна ломать сервис | `docs/DECISIONS.md` → «#7 Notifications Platform»; код: `NotificationService` — пустой токен/список → канал пропускается без ошибки; ошибки отправки → `LogWarning`, нет исключения; только outbound push, нет retry | Запустить без `Notifications__*` env-vars → сервис стартует, все API-эндпоинты отвечают 200; задать заведомо неверный токен → в логах `[WRN] NotificationService`, основной ответ 200 | Сервис без `Notifications__*` env-vars | [OK] |

### #10 — Gradual Ramp + Rollout Winner

| ID задания | ID критерия | Проблема/риск | Где реализовано | Как проверяется | Какие данные нужны | Статус |
|------------|-------------|---------------|-----------------|-----------------|-------------------|--------|
| 10 | FX-1 | Невозможно безопасно расширять аудиторию без повторного review-цикла; победивший вариант не применяется автоматически к 100% пользователей | **Ramp:** `POST /api/experiments/{id}/ramp` (`RampExperimentHandler`): `newWeight = oldWeight / oldFraction * newFraction`, snapshot `ExperimentVersionEntity`, инвалидация `flag:{key}`. **Rollout Winner:** `CompleteExperimentHandler` при `Outcome=RolloutWinner + WinnerVariantId`: `FeatureFlag.DefaultValue = winner.Value`, инвалидация кеша. **Autopilot:** `AutopilotRampService` (BackgroundService): ступени (steps), три gate — safety (guardrail triggers), time (MinMinutesPerStep), impressions (MinImpressionsPerStep); SafetyAction: Pause / RollbackToControl / StepBack; история в `RampPlanHistoryEntity`; API: `POST /autopilot`, `GET /autopilot`, `GET /autopilot/history`, `POST /autopilot/enable`, `POST /autopilot/disable` | `RolloutTests` (8 тестов): `Ramp_Increases_AudienceFraction_And_Scales_Weights` (0.1→0.5, вес 0.05→0.25, ratio сохранён); `Complete_RolloutWinner_Updates_FeatureFlag_DefaultValue`. `AutopilotRampTests` (8 тестов): `AdvancesStep_WhenAllGatesPass` (0.05→0.25, CurrentStepIndex 0→1); `PausesExperiment_WhenGuardrailTriggered`; `StepsBack_WhenGuardrailTriggered`; `MarksCompleted_WhenAtFinalStep`; `RecordsHistory_WhenAdvancing`; `IsSkipped_WhenDisabled` | Running эксперимент AudienceFraction=0.05; `POST /autopilot` с steps=[0.25,0.5,1.0], minImpressions=10, minMinutes=60; подождать 60 мин + 10 decisions → автоматически 0.05→0.25 | [OK] |
| 10 | FX-2 | Неочевидные ограничения раскатки (только увеличение, только Running, необязательность WinnerVariantId) могут приводить к ошибкам использования | `docs/DECISIONS.md` → «#10 Autopilot Ramp-up»; валидации в `RampExperimentHandler`: `Status != Running` → 422; `NewFraction ≤ oldFraction` → 422; `NewFraction > 1.0` → 422. `CompleteExperimentHandler`: `WinnerVariantId` необязателен — без него DefaultValue не меняется. `CreateRampPlanHandler`: steps обязаны быть строго возрастающими в [0.05..1.0] и > текущей AudienceFraction; конфликт (план уже есть) → 409; эксперимент не Running → 422; `SetRampPlanEnabledHandler` позволяет вручную паузить/возобновить автопилот без изменения статуса эксперимента | `Ramp_Fails_If_Experiment_Not_Running` → 422; `Ramp_Fails_If_New_Fraction_Is_Less_Than_Current` → 422; `DoesNotAdvance_WhenInsufficientImpressions`; `DoesNotAdvance_WhenTimeTooShort`; `IsSkipped_WhenDisabled` | Running эксперимент AudienceFraction=0.1; тест `POST /ramp` с 0.05 → 422 | [OK] |

### #11 — Conflict Resolution (MutualExclusion)

| ID задания | ID критерия | Проблема/риск | Где реализовано | Как проверяется | Какие данные нужны | Статус |
|------------|-------------|---------------|-----------------|-----------------|-------------------|--------|
| 11 | FX-1 | Несколько экспериментов, тестирующих одну зону продукта, одновременно попадают к пользователю — результаты загрязняют друг друга | `EConflictPolicy.MutualExclusion`; поля `ConflictDomains` (строка, домены через запятую), `ConflictPolicy`, `Priority` в `ExperimentEntity` + `ExperimentCacheEntry`; `DecideHandler.IsConflictDomainWinner`: победитель = max Priority; при равном Priority → `MaxBy SHA256(subjectId:experimentId)`; проигравший получает `isDefault=true` | `ConflictResolutionTests` (8 тестов): `HigherPriority_WinnerGetsVariant` (Priority 10 → `isDefault=false`); `LowerPriority_AlwaysLoses` (Priority 5 → `isDefault=true` для любого субъекта); `EqualPriority_ExactlyOneWins` (XOR: ровно один из двух даёт вариант); `EqualPriority_WinnerIsDeterministic` (тот же субъект → тот же исход при повторном вызове); `DifferentDomains_NoConflict` (оба дают вариант); `MultipleDomains_ConflictOnSharedDomain` | Два Running эксперимента с `ConflictDomains="checkout"`, `ConflictPolicy=MutualExclusion`; Exp A Priority=10, Exp B Priority=5; любой subjectId → A всегда получает вариант, B — default | [OK] |
| 11 | FX-2 | Без зафиксированных ограничений conflict resolution жюри не оценит производительность, корректность edge cases и eventual consistency | `docs/DECISIONS.md` → «#11 Conflict Resolution»; проверка только при `ConflictPolicy=MutualExclusion && ConflictDomains != null`; конкуренты ищутся live DB-запросом (не из Redis-кеша) — дополнительный запрос к БД на каждый `decide` с доменом; нет кросс-флагового кеша для доменов; ConflictDomains — строка через запятую, case-insensitive; eventual consistency: Redis TTL 60 с может содержать устаревший статус конкурента | `NoPolicySet_NoConflict` → без `ConflictPolicy` нет проверки конфликта; `NoCompetitors_AlwaysWins` → нет конкурентов → всегда вариант; документация в `docs/DECISIONS.md` описывает дополнительный DB-запрос и отсутствие retry | Достаточно прочитать `docs/DECISIONS.md` и запустить `ConflictResolutionTests` | [OK] |

---
