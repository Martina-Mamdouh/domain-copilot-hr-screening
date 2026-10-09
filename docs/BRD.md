# Business Requirements Document (BRD) & Traceability Matrix

## 1. Executive Summary
The **Domain Copilot (D6 - HR Talent Screening)** aims to revolutionize the initial CV screening process by automating skill extraction and alignment with Job Descriptions. It mitigates human bias, significantly reduces Time-to-Hire, and ensures transparent, fair, and auditable hiring decisions.

## 2. Business Objectives
1. **Reduce Screening Time**: Decrease the time HR professionals spend manually filtering unqualified candidates by 80%.
2. **Standardize Evaluation**: Ensure all candidates are evaluated against the exact same rubric without human bias or fatigue.
3. **Auditable Compliance**: Maintain an immutable log of why a candidate was accepted or rejected to comply with fair hiring laws.
4. **Offline Resilience (Twist T2)**: Ensure the hiring pipeline never completely halts due to cloud provider outages.

## 3. Personas & Target Users
* **The Recruiter**: Responsible for uploading CVs and Job Descriptions, initiating the AI screening, and reviewing the high-level match percentages.
* **The Hiring Manager**: The ultimate decision-maker. Needs to see the AI's "Reasoning" and "Interview Probes" to validate the candidate before taking them to a technical interview. Must have the authority to override AI decisions (Approval Gate).
* **The System Administrator / Auditor**: Monitors the platform's API costs, latency, and model choices (Gemini vs Ollama) to ensure operational health and security compliance.

## 4. Explicit Out-of-Scope
To ensure the MVP remains focused and achievable within the hackathon timeframe, the following features are explicitly deferred:
* **Direct Integration with External ATS**: No direct APIs linking to Workday or Greenhouse in this phase.
* **Automated Emailing**: The system will not send automated rejection/acceptance emails to candidates.
* **Complex Multi-modal Processing**: The MVP handles text-based CVs (PDF text extraction/TXT) but will not parse images, charts, or video interviews.

## 5. Assumptions & Risks
* **Assumption**: The provided company guidelines (Corpus) are well-written and free of inherently biased language.
* **Assumption**: The target deployment environment (Docker) has sufficient RAM (min 8GB) to run the `Ollama 3B` fallback model effectively.
* **Risk (Latency)**: Utilizing a 3-agent pipeline introduces higher latency than a single prompt. *Mitigation*: Implemented an Early-Exit pattern (circuit breaker) if the candidate fails Agent 1 or the Injection Guard.
* **Risk (Hallucination)**: LLMs generating false skills not present in the CV. *Mitigation*: Bounded generation enforced by strict RAG and an explicit `Agent 1` extractor that isolates evidence.

---

## 6. Requirements Traceability Matrix (RTM)
This matrix maps the required Functional Requirements (FR) and Binding Principles to their actual technical implementation in the codebase.

| Req ID | Description | Component / File Implementation | Status |
|---|---|---|---|
| **BP-1** | Grounded, never guessing (No Hallucination) | `GroundedGenerationService.cs` (Validates cosine distance and rejects out-of-corpus prompts). | ✅ Done |
| **BP-2** | Human holds the pen (Approval Gate) | `SubmitReviewHandler.cs`, `dashboard.component.html` (Review Modal). | ✅ Done |
| **BP-3** | Everything is observable (Traces) | `AgentExecutionTrace.cs`, `GetEvaluationsHandler.cs` (Admin Traces view). | ✅ Done |
| **FR-1** | Corpus Ingestion (PDF/TXT) | `DocumentIngestionService.cs`, `corpus-ingestion.component.ts`. | ✅ Done |
| **FR-2** | Hybrid Retrieval (Copilot Chat) | `CopilotChatEndpoint.cs`, `VectorStore.cs` (In-Memory FAISS alternative). | ✅ Done |
| **FR-3** | Automated Evaluation (Metrics) | `eval/run_qa_eval.js`, `eval/run_eval.js`, `EvaluationRunMetrics` table. | ✅ Done |
| **FR-4** | Multi-Agent Workflow (3 Agents) | `ScreeningPipelineService.cs` (Agent 1, Agent 2, Agent 3 explicit roles). | ✅ Done |
| **FR-5** | Orchestration & Early Exit | `ScreeningPipelineService.cs` (Pipeline breaks if Prompt Injection or Agent 1 fails). | ✅ Done |
| **FR-7/9** | Role-Based Access Control & Logs | `AuthService.ts`, `SubmitReviewHandler.cs` (AuditLogs generation). | ✅ Done |
| **T2** | Offline / Degraded Resilience | `ResilientLLMService.cs`, `OllamaService.cs`, `GeminiService.cs`. | ✅ Done |
