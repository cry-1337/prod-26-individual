# C4 Container — LOTTY A/B Platform (Level 2)

Контейнеры системы, их ответственности и взаимодействия.

```mermaid
flowchart TB
    classDef person    fill:#08427b,color:#fff,stroke:#052e56
    classDef container fill:#1168bd,color:#fff,stroke:#0b4884
    classDef infra     fill:#1565c0,color:#fff,stroke:#0d47a1,stroke-dasharray:4 2
    classDef db        fill:#0d47a1,color:#fff,stroke:#0a2f6b
    classDef extPerson fill:#6b6b6b,color:#fff,stroke:#444

    Users(["Experimenter / Approver\nViewer / Admin"])
    Prod(["Product Application"])

    subgraph platform["  LOTTY A/B Platform  "]
        direction TB
        Api["**LottyAB.Api**\nASP.NET Core 10\nREST · JWT · Health/Ready"]
        App["**LottyAB.Application**\nC# · MediatR · FluentValidation\nDecide · Events · Reports · Lifecycle"]
        Infra["**LottyAB.Infrastructure**\nEF Core 10 · BackgroundService\nEventAttributionService ⟳30s\nGuardrailMonitoringService ⟳60s"]
        Db[("**PostgreSQL 16**\nФлаги · Эксперименты\nСобытия · Решения\nМетрики · Guardrails")]
    end

    Users -->|"HTTPS/REST + JWT"| Api
    Prod  -->|"POST /decide\nPOST /events"| Api

    Api   -->|"MediatR"| App
    App   -->|"EF Core"| Infra
    Infra -->|"SQL / Npgsql"| Db

    class Users,Prod extPerson
    class Api,App container
    class Infra infra
    class Db db
```

| Контейнер | Технология | Ответственность |
|---|---|---|
| **LottyAB.Api** | ASP.NET Core 10 | REST-эндпоинты, JWT-аутентификация, ролевая авторизация, health/ready |
| **LottyAB.Application** | C#, MediatR, FluentValidation | CQRS-обработчики, бизнес-сервисы (TargetingEvaluator, HashVariantSelector) |
| **LottyAB.Infrastructure** | EF Core 10, BackgroundService | Persistence + два фоновых воркера |
| **PostgreSQL 16** | СУБД | Единое хранилище всех данных платформы |
