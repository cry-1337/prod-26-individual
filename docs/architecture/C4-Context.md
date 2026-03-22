# C4 Context — LOTTY A/B Platform

Внешние акторы и граница системы.

```mermaid
flowchart TB
    classDef person  fill:#08427b,color:#fff,stroke:#052e56
    classDef system  fill:#1168bd,color:#fff,stroke:#0b4884
    classDef extSys  fill:#6b6b6b,color:#fff,stroke:#444

    P1(["Experimenter\nСоздаёт эксперименты"])
    P2(["Approver\nОдобряет запуск"])
    P3(["Viewer / PM\nЧитает отчёты"])
    P4(["Admin\nУправляет платформой"])

    S["LOTTY A/B Platform\nФлаги · Decide · События\nОтчёты · Guardrails"]

    E(["Product Application\nЗапрашивает варианты\nОтправляет события"])

    P1 -->|"HTTPS/REST"| S
    P2 -->|"HTTPS/REST"| S
    P3 -->|"HTTPS/REST"| S
    P4 -->|"HTTPS/REST"| S
    E  -->|"POST /decide\nPOST /events"| S

    class P1,P2,P3,P4 person
    class S system
    class E extSys
```

| Актор | Роль |
|---|---|
| **Experimenter** | Создаёт флаги и эксперименты, отправляет на ревью |
| **Approver** | Ревьюит эксперименты, одобряет / отклоняет запуск |
| **Viewer / PM** | Читает отчёты, принимает решение о раскатке / откате |
| **Admin** | Управляет пользователями, аппрувер-группами, каталогами |
| **Product Application** | Запрашивает значения флагов, отправляет события |
