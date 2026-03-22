# C4 Component — критичный путь (Level 3)

Компоненты `LottyAB.Api + Application` на пути **decide → event → report / guardrail**.

```mermaid
flowchart LR
    classDef actor   fill:#08427b,color:#fff,stroke:#052e56
    classDef ctrl    fill:#0277bd,color:#fff,stroke:#01579b
    classDef handler fill:#1565c0,color:#fff,stroke:#0d47a1
    classDef svc     fill:#283593,color:#fff,stroke:#1a237e
    classDef bg      fill:#4a148c,color:#fff,stroke:#311b92
    classDef db      fill:#004d40,color:#fff,stroke:#00251a

    Prod(["Product App"])
    PM(["PM / Analyst"])

    subgraph core["LottyAB.Api + Application"]
        direction TB

        subgraph decide["Decide"]
            DC["DecideController\nPOST /api/decide"]
            DH["DecideHandler"]
            TE["TargetingEvaluator\nDSL: AND/OR, ==, IN, >"]
            VS["HashVariantSelector\nSHA-256 → стабильный вариант"]
            DC --> DH --> TE & VS
        end

        subgraph events["Events"]
            EC["EventsController\nPOST /api/events"]
            EH["SendEventsHandler\nВалидация · Дедупликация"]
            EC --> EH
        end

        subgraph reports["Reports"]
            RC["ReportsController\nGET /api/reports/..."]
            RH["GetReportHandler\nАгрегация по вариантам"]
            RC --> RH
        end

        subgraph guardrails["Guardrails"]
            GC["GuardrailsController\nPOST/GET /guardrails"]
        end
    end

    BG["⟳ Background Services\nEventAttributionService\nGuardrailMonitoringService"]
    DB[("PostgreSQL 16")]

    Prod --> DC
    Prod --> EC
    PM   --> RC
    PM   --> GC

    DH  --> DB
    EH  --> DB
    RH  --> DB
    GC  --> DB
    BG  --> DB

    class Prod,PM actor
    class DC,EC,RC,GC ctrl
    class DH,EH,RH handler
    class TE,VS svc
    class BG bg
    class DB db
```

## Критичный путь

| Путь | Ключевые компоненты |
|---|---|
| **decide** | DecideController → DecideHandler → TargetingEvaluator + HashVariantSelector → Decision в БД |
| **event** | EventsController → SendEventsHandler → валидация + дедуп → Events в БД |
| **attribution** | EventAttributionService (30 сек) → связывает Events с Decisions по DecisionId |
| **report** | ReportsController → GetReportHandler → агрегация Events по Variant |
| **guardrail** | GuardrailMonitoringService (60 сек) → пересчёт метрик → Pause / Rollback + TriggerHistory |
