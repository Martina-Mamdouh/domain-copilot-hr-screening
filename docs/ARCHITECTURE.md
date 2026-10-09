# System Architecture (Domain Copilot)

This document provides a comprehensive architectural overview of the Domain Copilot HR Screening system using the C4 Model approach, sequence diagrams, and Architecture Decision Records (ADRs).

---

## 1. C4 Model Diagrams

### Level 1: System Context Diagram
Shows the system's interactions with its primary actors and external dependencies.

```mermaid
C4Context
    title System Context for Domain Copilot

    Person(recruiter, "Recruiter", "Uploads candidate CVs and initiates screening pipeline.")
    Person(hiringManager, "Hiring Manager", "Reviews AI evaluations, edits/approves decisions, and conducts interviews.")
    Person(admin, "System Admin", "Monitors execution traces, token costs, and system health.")
    
    System(domainCopilot, "Domain Copilot System", "Automates resume screening, protects against bias, and retrieves domain-specific guidelines.")
    
    System_Ext(gemini, "Google Gemini API", "Primary High-Capacity LLM Provider.")
    System_Ext(ollama, "Local Ollama Llama3", "Fallback Offline LLM Provider (T2 Twist).")

    Rel(recruiter, domainCopilot, "Uploads CVs & runs screening")
    Rel(hiringManager, domainCopilot, "Reviews evaluations & executes Approval Gate")
    Rel(admin, domainCopilot, "Views Observability & Traces")
    Rel(domainCopilot, gemini, "Sends sanitized prompts, receives evaluations")
    Rel(domainCopilot, ollama, "Fails over automatically when offline")
```

### Level 2: Container Diagram
Details the high-level executable units of the system.

```mermaid
C4Container
    title Container Diagram for Domain Copilot

    Person(recruiter, "Recruiter", "Uploads CVs")
    Person(hiringManager, "Hiring Manager", "Reviews & Approves")
    Person(admin, "System Admin", "Observability")
    
    Container_Boundary(c1, "Domain Copilot Platform") {
        Container(spa, "Single Page App", "Angular 18", "Provides the dashboard, chat, and review modal.")
        Container(api, "Web API", ".NET 8 Web API", "Handles requests, authorization, and orchestrates the agent pipeline.")
        ContainerDb(db, "Relational DB", "SQL Server", "Stores Candidates, Audits, and Execution Traces.")
        ContainerDb(vector, "Vector Store", "In-Process FAISS/Memory", "Stores embedded chunks of JDs and Policies.")
    }
    
    System_Ext(gemini, "Google Gemini API", "Primary LLM")
    
    Rel(recruiter, spa, "Uploads CVs", "HTTPS")
    Rel(hiringManager, spa, "Reviews & Approves", "HTTPS")
    Rel(admin, spa, "Views Traces", "HTTPS")
    Rel(spa, api, "Makes API calls to", "JSON/HTTPS")
    Rel(api, db, "Reads from and writes to", "EF Core")
    Rel(api, vector, "Queries semantic similarity", "Cosine Distance")
    Rel(api, gemini, "Sends LLM requests", "gRPC/REST")
```

### Level 3: Component Diagram (API Application Layer)
Zooming into the Web API backend to show the internal services and MediatR handlers.

```mermaid
C4Component
    title Component Diagram for Domain Copilot API

    Container_Boundary(api, "Web API Application") {
        Component(controllers, "API Endpoints", "ASP.NET Minimal APIs", "Routing and JWT Authorization.")
        Component(mediator, "MediatR Bus", "CQRS", "Dispatches commands and queries.")
        
        Component(screeningHandler, "EvaluateCandidateHandler", "MediatR Handler", "Coordinates the evaluation request.")
        Component(pipelineSvc, "ScreeningPipelineService", "Service", "Implements the 3-Agent Workflow.")
        Component(injectionGuard, "PromptInjectionGuard", "Service", "Regex-based heuristic safety check.")
        
        Component(resilientLLM, "ResilientLLMService", "Service", "Implements Circuit Breaker & Fallback.")
        Component(geminiClient, "GeminiService", "Provider", "Talks to Gemini.")
        Component(ollamaClient, "OllamaService", "Provider", "Talks to Local Llama3.")
    }
    
    Rel(controllers, mediator, "Sends requests")
    Rel(mediator, screeningHandler, "Routes to")
    Rel(screeningHandler, pipelineSvc, "Calls pipeline")
    Rel(pipelineSvc, injectionGuard, "Validates input")
    Rel(pipelineSvc, resilientLLM, "Requests Text Generation")
    Rel(resilientLLM, geminiClient, "Primary Route")
    Rel(resilientLLM, ollamaClient, "Fallback Route")
```

### Layer Dependency Diagram (Clean Architecture)
Illustrates the strict inner-pointing dependencies of the backend following Clean Architecture principles.

```mermaid
flowchart TD
    subgraph Infrastructure Layer
        Persistence[EF Core / SQL Server]
        External[Gemini / Ollama Providers]
    end

    subgraph Presentation Layer
        WebAPI[Controllers / Minimal APIs]
    end

    subgraph Application Layer
        UseCases[MediatR Handlers / DTOs]
        Interfaces[Repository & Service Interfaces]
    end

    subgraph Core Layer
        Entities[Domain Entities / Enums]
    end

    WebAPI -->|References| ApplicationLayer
    Infrastructure -->|Implements| ApplicationLayer
    ApplicationLayer -->|References| CoreLayer
    
    style CoreLayer fill:#4f46e5,stroke:#fff,color:#fff
    style ApplicationLayer fill:#0ea5e9,stroke:#fff,color:#fff
    style Infrastructure fill:#64748b,stroke:#fff,color:#fff
    style PresentationLayer fill:#0284c7,stroke:#fff,color:#fff
```

---

## 2. Sequence Diagram: Candidate Screening Workflow
Illustrates the exact flow of data through the 3-Agent pipeline, up to the Approval Gate.

```mermaid
sequenceDiagram
    actor RC as Recruiter
    actor HM as Hiring Manager
    participant API as Web API
    participant Guard as PromptInjectionGuard
    participant Ag1 as Agent 1 (Extractor)
    participant Ag2 as Agent 2 (Sanitizer)
    participant Ag3 as Agent 3 (Evaluator)
    participant DB as SQL Database

    RC->>API: POST /evaluate (Upload CV Text & JD)
    API->>Guard: Check for Prompt Injections
    alt Malicious Injection Detected
        Guard-->>API: Return Security Reject
        API->>DB: Save Traces & Evaluation (Reject)
        API-->>RC: 200 OK (Rejected by Guard)
    else Clean CV
        API->>Ag1: Extract Skills & Align with JD
        Ag1-->>API: Extracted Skills & Minimum Check
        
        API->>Ag2: Strip PII & Demographics
        Ag2-->>API: Anonymized Profile [REDACTED]
        
        API->>Ag3: Score, Recommend & Generate Probes
        Ag3-->>API: JSON: Score, Recommendation, Reasoning, Probes
        
        API->>DB: Save CandidateEvaluation (PendingHumanApproval)
        API->>DB: Save AgentExecutionTraces (Observability)
        API-->>RC: 200 OK (Evaluation Created & Awaiting Approval)
    end

    opt Human-in-the-Loop Review
        HM->>API: GET /candidates (View Pending Evaluations)
        HM->>API: POST /review (Approve / Reject / Edit-and-Approve)
        API->>DB: Append to AuditLogEntry & Update Status
        API-->>HM: 200 OK (Decision Persisted)
    end
```

---

## 3. Data-Flow & Trust Boundaries
This diagram highlights where PII is stripped to maintain compliance.

```mermaid
flowchart TD
    subgraph Untrusted Zone
        User[User Input / CV Upload]
    end
    
    subgraph Demilitarized Zone (DMZ)
        Guard[Injection Guard: Drops Malicious Payloads]
    end
    
    subgraph Core System (PII Present)
        Agent1[Agent 1: Extracts Raw Skills]
    end

    subgraph Trust Boundary (Sanitization)
        Agent2{Agent 2: Anonymizer}
        note[Strips Name, Age, Gender, Ethnicity]
    end

    subgraph External LLM Zone (PII Free)
        Agent3[Agent 3: Evaluator]
        Gemini[Google Gemini API]
    end

    User -->|Raw Text| Guard
    Guard -->|Clean Text| Agent1
    Agent1 -->|Raw Skills| Agent2
    Agent2 -->|Anonymized Profile| Agent3
    Agent3 <--> Gemini
```

---

## 4. Entity Relationship Diagram (ERD)

```mermaid
erDiagram
    CandidateEvaluation ||--o{ AuditLogEntry : "Generates"
    CandidateEvaluation ||--o{ AgentExecutionTrace : "Traced By"
    CandidateEvaluation {
        uniqueidentifier Id PK
        string CandidateAlias
        float WeightedScore
        int RecommendedDecision
        int Status
        string ManagerOverrideReason
        string InterviewProbes
        datetime CreatedAtUtc
    }
    AuditLogEntry {
        uniqueidentifier Id PK
        uniqueidentifier CandidateEvaluationId FK
        string ActionType
        string OldValuesJson
        string NewValuesJson
        uniqueidentifier PerformedByUserId
        datetime TimestampUtc
    }
    AgentExecutionTrace {
        uniqueidentifier Id PK
        uniqueidentifier CandidateEvaluationId FK
        string AgentName
        string ProviderUsed
        int PromptTokens
        int CompletionTokens
        long ExecutionDurationMs
        string Status
    }
    CorpusDocument ||--o{ DocumentChunk : "Split into"
    CorpusDocument {
        string DocId PK
        string Title
        string Type
    }
    DocumentChunk {
        uniqueidentifier Id PK
        string DocId FK
        int ChunkIndex
        string Content
        string EmbeddingVector
    }
```

---

## 5. Architecture Decision Records (ADRs)

### ADR-001: Clean Architecture & CQRS via MediatR
* **Context**: The project required a decoupled, testable backend.
* **Decision**: We implemented Clean Architecture principles, isolating `Core/Entities` from `Infrastructure`. CQRS was enforced using the `MediatR` library to separate read operations (Queries) from write operations (Commands).
* **Consequences**: Enhanced testability and modularity, though it slightly increased boilerplate code for simple CRUD operations.

### ADR-002: Resilient Provider Fallback (Offline Twist T2)
* **Context**: The system must survive network partitions or API rate limits (Twist T2).
* **Decision**: We implemented `IResilientLLMService`, wrapping API calls in a `try-catch` block. If `GeminiService` fails or times out, it automatically delegates to `OllamaService` running a local Llama 3 model on port 11434.
* **Consequences**: Guaranteed uptime. Execution traces were updated to explicitly log `ProviderUsed` so administrators know when the system operated in degraded mode.

### ADR-003: Linear Multi-Agent Pipeline with Early Exit
* **Context**: Processing full agent pipelines is costly in terms of latency and tokens.
* **Decision**: We adopted a linear Pipeline Orchestration pattern instead of autonomous interacting agents. If the `PromptInjectionGuard` or `Agent 1` flags the candidate as an immediate fail, the pipeline executes an **Early Exit**, skipping Agents 2 and 3.
* **Consequences**: Significantly reduced LLM API costs and dropped latency for unqualified candidates by ~60%.

### ADR-004: Immutable Audit Logging for Manager Overrides
* **Context**: HR Screening systems require strict legal traceability (Human holds the pen).
* **Decision**: We implemented an `AuditLogEntry` table linked to `CandidateEvaluation`. Any state change (Approve, Reject, or Edit) creates an immutable record containing the `OldValues` and `NewValues` in JSON, along with the required `ManagerOverrideReason`.
* **Consequences**: Full compliance with Binding Principle #2. The tables are append-only to prevent tampering.
