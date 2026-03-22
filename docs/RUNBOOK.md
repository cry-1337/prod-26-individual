# RUNBOOK

## Содержание

1. [Быстрый старт](#быстрый-старт)
2. [Запуск системы](#запуск-системы)
3. [Проверка работоспособности](#проверка-работоспособности)
4. [Запуск тестов](#запуск-тестов)
5. [Демонстрация бонусных фич](#демонстрация-бонусных-фич)
6. [Остановка системы](#остановка-системы)

---

## Быстрый старт

```bash
# 1. Запуск всей системы (PostgreSQL + API)
docker-compose up -d

# 2. Проверка здоровья (подождите 30-60 сек при первом запуске)
curl http://localhost/api/health  # -> {"status":"Healthy"}
curl http://localhost/api/ready   # -> {"status":"Healthy"}

# 3. Запуск тестов
cd src/LottyAB
dotnet test                       # Запуск всех тестов

# 4. Остановка
docker-compose down
```

---

## Запуск системы

```bash
docker-compose up -d
```

**Ожидаемый вывод:**
```
[+] Running 3/3
 ✔ Network lame84712_default       Created
 ✔ Container lottyab-postgres      Started
 ✔ Container lottyab-api           Started
```

**Проверка статуса:**
```bash
docker-compose ps
```

**Ожидаемый вывод:**
```
NAME                IMAGE                  STATUS         PORTS
lottyab-api         lame84712-api          Up (healthy)   0.0.0.0:80->80/tcp
lottyab-postgres    postgres:16-alpine     Up (healthy)   0.0.0.0:5432->5432/tcp
```

**Просмотр логов:**
```bash
# Все сервисы
docker-compose logs -f

# Только API
docker-compose logs -f api

# Только PostgreSQL
docker-compose logs -f postgres
```

---

### Инициализация базы данных

Миграции применяются **автоматически** при первом запуске приложения.

База данных будет инициализирована с:
- Дефолтным администратором: `admin@lottyab.com` / `lottyab`
- Базовыми типами событий: `exposure`, `conversion`, `click`, `page_view`
- Дефолтными метриками: `conversion_rate`, `click_through_rate`

---

## Проверка работоспособности

### 1. Health Check
```bash
curl http://localhost/api/health
```

**Ожидаемый вывод:**
```json
{"status":"Healthy"}
```

**HTTP Status:** `200 OK`

---

### 2. Readiness Check

```bash
curl http://localhost/api/ready
```

**Ожидаемый вывод:**
```json
{"status":"Healthy"}
```

**HTTP Status:** `200 OK`

**Важно:** Endpoint `/api/ready` должен вернуть `200 OK` в течение **180 секунд** после запуска приложения.

---

### 3. Swagger UI

```
http://localhost/swagger
```

Откроется интерактивная документация API с возможностью тестирования эндпоинтов.

---

### 4. Проверка аутентификации

**Вход с дефолтным администратором:**

```bash
curl -X POST http://localhost/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@lottyab.com","password":"lottyab"}'
```

**Ожидаемый вывод:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-02-16T14:30:00Z",
  "user": {
    "id": "00000000-0000-0000-0000-000000000001",
    "email": "admin@lottyab.com",
    "name": "System Administrator",
    "role": "Admin"
  }
}
```

**HTTP Status:** `200 OK`

---

### 5. Создание Feature Flag

```bash
# Сохраните токен из предыдущего запроса
TOKEN="<ваш-access-token>"

curl -X POST http://localhost/api/feature-flags \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"key":"test-flag","name":"Test Flag","description":"Test feature flag","valueType":"Boolean","defaultValue":"false"}'
```

**Ожидаемый вывод:**
```json
{
  "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "key": "test-flag",
  "name": "Test Flag",
  "description": "Test feature flag",
  "valueType": "Boolean",
  "defaultValue": "false",
  "isActive": true,
  "createdAt": "2026-02-16T...",
  "updatedAt": "2026-02-16T..."
}
```

**HTTP Status:** `201 Created`

---

## Запуск тестов

### Все тесты

```bash
cd src/LottyAB
dotnet test --verbosity normal
```

**Ожидаемый вывод:**
```
Тесты выполнены успешно
 Общее время: ~10-15 секунд
```

---

### Запуск с покрытием кода

```bash
cd src/LottyAB
dotnet test --collect:"XPlat Code Coverage"
```

**Ожидаемый вывод:**
```
Тесты выполнены успешно

Attachments:
  D:\!githb\lame84712\src\LottyAB\TestResults\...\coverage.cobertura.xml
```

---

### Запуск только Smoke Tests

```bash
cd src/LottyAB
dotnet test --filter "FullyQualifiedName~SmokeTests"
```

**Ожидаемый вывод:**
```
Smoke тесты выполнены успешно
```

---

### Проверка форматирования кода

**Проверка:**
```bash
cd src/LottyAB
dotnet format --verify-no-changes
```

**Ожидаемый вывод (если всё ОК):**
```
No files need formatting.
```

**Автоформатирование:**
```bash
dotnet format
```

---

## Демонстрация бонусных фич

### FX-10: Autopilot Ramp-up

Автопилот (`AutopilotRampService`) автоматически продвигает долю аудитории через заданные ступени
каждые 60 секунд, если выполнены gates (время, impressions, guardrail-безопасность).

> **Для быстрого демо** используйте `minImpressionsPerStep=0` и `minMinutesPerStep=0` —
> автопилот сработает уже на следующей итерации (≤ 60 сек).

**Предварительные условия:** эксперимент находится в статусе `Running`.

```bash
TOKEN="<ваш-access-token>"
EXPERIMENT_ID="<id-running-эксперимента>"

# 1. Создать план автопилота: ступени 10% → 50% → 100%
#    minImpressions=0 и minMinutes=0 для мгновенного демо
curl -X POST "http://localhost/api/experiments/$EXPERIMENT_ID/autopilot" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "steps": [0.1, 0.5, 1.0],
    "minImpressionsPerStep": 0,
    "minMinutesPerStep": 0,
    "safetyAction": "Pause"
  }'
```

**Ожидаемый ответ (`201 Created`):**
```json
{
  "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "experimentId": "<EXPERIMENT_ID>",
  "steps": [0.1, 0.5, 1.0],
  "currentStepIndex": 0,
  "minImpressionsPerStep": 0,
  "minMinutesPerStep": 0,
  "safetyAction": "Pause",
  "isEnabled": true,
  "isCompleted": false,
  "stepEnteredAt": "2026-02-22T..."
}
```

```bash
# 2. Подождать ≤ 60 сек, затем проверить текущий план
curl "http://localhost/api/experiments/$EXPERIMENT_ID/autopilot" \
  -H "Authorization: Bearer $TOKEN"
```

**После первого цикла (`currentStepIndex` увеличился до 1, `audienceFraction` = 0.1):**
```json
{
  "currentStepIndex": 1,
  "isCompleted": false,
  "stepEnteredAt": "2026-02-22T..."
}
```

```bash
# 3. Посмотреть историю шагов автопилота
curl "http://localhost/api/experiments/$EXPERIMENT_ID/autopilot/history" \
  -H "Authorization: Bearer $TOKEN"
```

**Ожидаемый ответ:**
```json
[
  {
    "action": "Advanced",
    "fromFraction": 0.05,
    "toFraction": 0.1,
    "reason": "All gates passed",
    "timestamp": "2026-02-22T..."
  }
]
```

```bash
# 4. Отключить автопилот (если нужно)
curl -X POST "http://localhost/api/experiments/$EXPERIMENT_ID/autopilot/disable" \
  -H "Authorization: Bearer $TOKEN"

# 5. Включить снова
curl -X POST "http://localhost/api/experiments/$EXPERIMENT_ID/autopilot/enable" \
  -H "Authorization: Bearer $TOKEN"
```

**Возможные SafetyAction при нарушении guardrail:**

| Значение | Поведение |
|----------|-----------|
| `Pause` | Эксперимент ставится на паузу |
| `RollbackToControl` | Эксперимент завершается с исходом `Rollback` |
| `StepBack` | Доля аудитории откатывается на предыдущую ступень |

---

### FX-11: Conflict Resolution (Mutual Exclusion)

Два эксперимента с одним доменом (`ConflictDomains`) и политикой `MutualExclusion`
не могут показываться одному пользователю одновременно. Победитель определяется по `Priority`.

```bash
# При решении: эксперимент с более низким Priority вернёт isDefault=true
curl -X GET "http://localhost/api/decide?flagKey=my-flag&subjectId=user-123" \
  -H "Authorization: Bearer $TOKEN"
```

**Ответ для проигравшего эксперимента:**
```json
{
  "value": "false",
  "isDefault": true,
  "reason": "Conflict resolution: not the winner in domain"
}
```

---

### FX-7: Уведомления (Telegram / Slack)

Уведомления автоматически отправляются при изменении статуса эксперимента, срабатывании
guardrail или шаге автопилота. Настройка через переменные окружения:

```bash
# .env или docker-compose environment:
NOTIFICATIONS__TELEGRAM__BOTTOKEN=<token>
NOTIFICATIONS__TELEGRAM__CHATID=<chat_id>
NOTIFICATIONS__SLACK__WEBHOOKURL=<webhook_url>
```

---

## Остановка системы

**Остановка всех сервисов:**
```bash
docker-compose down
```

**Ожидаемый вывод:**
```
[+] Running 3/3
 ✔ Container lottyab-api       Removed
 ✔ Container lottyab-postgres  Removed
 ✔ Network lame84712_default   Removed
```

**Остановка с удалением данных БД:**
```bash
docker-compose down -v
```

**Полная очистка (включая образы):**
```bash
docker-compose down -v --rmi all
```

---

### Работа с базой данных

**Подключение к PostgreSQL через psql:**
```bash
docker exec -it lottyab-postgres psql -U lottyab -d lottyab
```

**Просмотр таблиц:**
```bash
docker exec -it lottyab-postgres psql -U lottyab -d lottyab -c "\dt"
```

**Ожидаемый вывод:**
```
                    List of relations
 Schema |            Name             | Type  | Owner
--------+-----------------------------+-------+--------
 public | Decisions                   | table | lottyab
 public | EventTypes                  | table | lottyab
 public | Events                      | table | lottyab
 public | Experiments                 | table | lottyab
 public | FeatureFlags                | table | lottyab
 public | GuardrailTriggerHistory     | table | lottyab
 public | MetricDefinitions           | table | lottyab
 public | Reviews                     | table | lottyab
 public | Users                       | table | lottyab
 public | Variants                    | table | lottyab
```

**Подсчет записей в таблицах:**
```bash
docker exec -it lottyab-postgres psql -U lottyab -d lottyab -c "
SELECT
  schemaname,
  tablename,
  n_tup_ins as total_rows
FROM pg_stat_user_tables
ORDER BY tablename;"
```