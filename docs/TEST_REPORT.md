# TEST REPORT

## Команды запуска тестов

### Все тесты
```bash
cd src/LottyAB
dotnet test
```

### С подробным выводом
```bash
cd src/LottyAB
dotnet test --verbosity normal
```

### Запуск конкретного класса тестов
```bash
cd src/LottyAB
dotnet test --filter "FullyQualifiedName~DecisionTests"
dotnet test --filter "FullyQualifiedName~TargetingDslTests"
dotnet test --filter "FullyQualifiedName~FullExperimentFlowTests"
dotnet test --filter "FullyQualifiedName~GuardrailMonitoringTests"
dotnet test --filter "FullyQualifiedName~EventAttributionTests"
dotnet test --filter "FullyQualifiedName~RolloutTests"
dotnet test --filter "FullyQualifiedName~ConflictResolutionTests"
```

### Проверка форматирования
```bash
cd src/LottyAB
dotnet format --verify-no-changes
```

---

## Тестовые наборы

| Класс | Тип | Кол-во | Что покрывает |
|-------|-----|--------|---------------|
| `SmokeTests` | Integration | 7 | Health/Ready, Login, Decide (happy + negative), Events |
| `DecisionTests` | Integration | 9 | Decide: variant/default, stickiness, targeting, weights (50/50, 70/30, 80/20), concurrent limit |
| `ExperimentsTests` | Integration | 16 | CRUD, lifecycle (submit/review/start/pause/complete/delete), invalid transitions, conflict |
| `ApproverGroupsTests` | Integration | 18 | CRUD группы, ревью (by group / not in group / threshold / duplicate / without group) |
| `GuardrailTests` | Integration | 8 | CRUD guardrail, валидация (negative threshold / empty metric / zero window / non-existent) |
| `GuardrailMonitoringTests` | Integration | 9 | Фоновый мониторинг: срабатывание Pause/RollbackToControl, история триггеров, деактивация guardrail, пропуск неактивных и non-running |
| `EventAttributionTests` | Integration | 7 | Атрибуция: exposure→conversion, нет exposure, >7 дней, уже атрибутировано, смешанные decisions, exposure не атрибутируется, error event |
| `ReportsTests` | Integration | 6 | Отчёт по периоду, разрез по вариантам, метрики, фиксация исхода |
| `FullExperimentFlowTests` | Integration | 3 | Сквозной путь: flag -> experiment -> decide -> events -> report |
| `UsersTests` | Integration | 8 | CRUD пользователей, авторизация по роли |
| `FeatureFlagsTests` | Integration | 6 | CRUD флагов, уникальность ключа, обновление default |
| `EventTypesTests` | Integration | 7 | CRUD типов событий, валидация, дедупликация |
| `RolloutTests` | Integration | 8 | Gradual ramp (fraction/weights/snapshot/статусы/граничные значения), rollout winner (DefaultValue/без id/invalid id) |
| `ConflictResolutionTests` | Integration | 8 | MutualExclusion: no competitors, priority winner/loser, equal priority (детерминизм/XOR), разные домены, без политики, multi-domain |
| `AutopilotRampTests` | Integration | 8 | Autopilot: advance (all gates), block (impressions/time), pause (guardrail+Pause), step-back (guardrail+StepBack), history записана, skip when disabled, mark completed |
| `TargetingDslTests` | Unit | 56 | DSL: `==`, `!=`, `>`, `>=`, `<`, `<=`, `IN`, `NOT IN`, `AND`, `OR`, `NOT`, вложенность, отсутствующий атрибут, ошибки |

**Итого: 184 тест-кейса**

---

## Покрытие

Тесты используют integration-first подход: каждый тест поднимает реальный in-memory (SQLite) экземпляр приложения через `WebApplicationFactory`. Это обеспечивает контрактное и интеграционное покрытие критических путей без моков.

| Слой | Метод покрытия | Оценка |
|------|----------------|--------|
| Controllers (API layer) | Интеграционные тесты через HTTP | >90% |
| Application (Handlers, Validators) | Интеграционные тесты | >85% |
| Domain (Entities, Enums) | Через интеграцию | >80% |
| Infrastructure (EF Core, Services) | Через интеграцию | >75% |
| Targeting DSL (Parser, Evaluator) | Unit-тесты (TargetingDslTests) | >95% |

Для просмотра точного line/branch покрытия:
```bash
cd src/LottyAB
dotnet test --collect:"XPlat Code Coverage"

dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

---

## Негативные тесты

| Тест | Класс | HTTP-статус |
|------|-------|-------------|
| `Login_WithInvalidCredentials_ReturnsUnauthorized` | SmokeTests | 401 |
| `Login_WithNonExistentUser_ReturnsNotFound` | SmokeTests | 404 |
| `Decide_WithNonExistentFeatureFlag_ReturnsNotFound` | SmokeTests | 404 |
| `Decide_WithPausedExperiment_ReturnsDefault` | DecisionTests | 200 (`isDefault=true`) |
| `Decide_WithTargeting_RespectsRules` | DecisionTests | 200 (UK -> `isDefault=true`) |
| `Decide_ConcurrentExperimentLimit_ReturnsDefaultAfterThree` | DecisionTests | 200 (4-й -> `isDefault=true`) |
| `CreateExperiment_WithInvalidWeights_ReturnsUnprocessableEntity` | ExperimentsTests | 422 |
| `StartExperiment_InDraftStatus_ReturnsUnprocessableEntity` | ExperimentsTests | 422 |
| `StartExperiment_InReviewStatus_ReturnsUnprocessableEntity` | ExperimentsTests | 422 |
| `ReviewExperiment_InDraftStatus_ReturnsUnprocessableEntity` | ExperimentsTests | 422 |
| `ReviewExperiment_AfterApproved_ReturnsUnprocessableEntity` | ExperimentsTests | 422 |
| `UpdateExperiment_InRunningStatus_ReturnsUnprocessableEntity` | ExperimentsTests | 422 |
| `CreateExperiment_DuplicateActiveFlag_ReturnsConflict` | ExperimentsTests | 409 |
| `GetExperiments_WithoutAuth_ReturnsUnauthorized` | ExperimentsTests | 401 |
| `CreateApproverGroup_NonAdmin_ReturnsForbidden` | ApproverGroupsTests | 403 |
| `ReviewExperiment_ByApproverNotInGroup_ReturnsUnprocessableEntity` | ApproverGroupsTests | 422 |
| `ReviewExperiment_DuplicateReview_ReturnsUnprocessableEntity` | ApproverGroupsTests | 422 |
| `GetApproverGroup_NonExistingId_ReturnsNotFound` | ApproverGroupsTests | 404 |
| `CreateGuardrail_WithNegativeThreshold_ReturnsBadRequest` | GuardrailTests | 400 |
| `CreateGuardrail_WithInvalidMetricKey_ReturnsBadRequest` | GuardrailTests | 400 |
| `CreateGuardrail_WithZeroObservationWindow_ReturnsBadRequest` | GuardrailTests | 400 |
| `CreateGuardrail_WithNonExistentExperiment_ReturnsNotFound` | GuardrailTests | 404 |
| `Ramp_Fails_If_Experiment_Not_Running` | RolloutTests | 422 |
| `Ramp_Fails_If_New_Fraction_Is_Less_Than_Current` | RolloutTests | 422 |
| `Ramp_Fails_If_New_Fraction_Above_One` | RolloutTests | 422 |
| `Complete_RolloutWinner_Invalid_VariantId_Throws_NotFoundException` | RolloutTests | 404 |
| `MutualExclusion_LowerPriority_AlwaysLoses` | ConflictResolutionTests | 200 (`isDefault=true`) |
| `MutualExclusion_MultipleDomains_ConflictOnSharedDomain` | ConflictResolutionTests | 200 (`isDefault=true`) |
| `MissingAttribute_ReturnsFalse` | TargetingDslTests | (unit -> false) |
| `Parse_EmptyOrWhitespace_Throws` | TargetingDslTests | (unit -> exception) |
| `Parse_InvalidComparison_Throws` | TargetingDslTests | (unit -> exception) |
| `Evaluator_NullAttributes_ReturnsFalse` | TargetingDslTests | (unit -> false) |
| `Evaluator_InvalidSyntax_ReturnsFalse` | TargetingDslTests | (unit -> false) |

---

## Ограничения

- Интеграционные тесты используют `SQLite` (in-memory) вместо PostgreSQL — поведение LIKE, индексы и некоторые SQL-функции могут отличаться.
- Нагрузочные тесты отсутствуют.
