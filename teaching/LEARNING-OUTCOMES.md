# Learning Outcomes & Assessment Rubric

This document outlines the pedagogical framework for the Agentic HR Screening module, utilizing Bloom’s Taxonomy to target postgraduate engineering levels.

## 1. Learning Outcomes (Bloom's Taxonomy)

By completing this module and the associated Lab Sheet, trainees will be able to:

1. **Analyze (Level 4)** the architectural trade-offs between monolithic LLM prompts and Multi-Agent linear pipelines in enterprise systems.
2. **Implement (Level 3)** deterministic Early-Exit patterns and Prompt Injection defenses (OWASP LLM01) to protect production pipelines.
3. **Evaluate (Level 5)** the security and privacy resilience of an LLM system against Sensitive Data Disclosure (OWASP LLM06) via automated PII sanitization.
4. **Synthesize (Level 6)** resilient fallback architectures (Twist T2) that switch from Cloud LLMs (Gemini) to Local LLMs (Ollama) under degraded network conditions.
5. **Measure (Level 5)** Retrieval-Augmented Generation (RAG) performance using automated evaluation suites (Retrieval Hit-Rate $\ge 90\%$, Refusal Correctness, and Token/Latency Telemetry).

---

## 2. Assessment & Rubric Map

The following table maps the theoretical learning outcomes to the practical Lab Components, establishing clear passing criteria:

| Learning Outcome | Lab Component / Challenge | Assessment Method | Passing Criteria (Success Metric) |
|---|---|---|---|
| **Analyze Multi-Agent Trade-offs** | Guided Step 3 (Execution Traces) | Instructor Q&A / Discussion | Trainee explains why Agent 1 costs fewer tokens than Agent 3 using the Admin Trace dashboard. |
| **Evaluate PII & Groundedness** | Guided Step 2 & Step 3 | UI & Log Inspection | Trainee verifies strict citation formatting `[Doc_X_Chunk_Y]` and confirms PII redaction by Agent 2. |
| **Synthesize Resilient Fallback (T2)** | Guided Step 4 (Offline Fallback) | Cloud Disconnection Test | Trainee invalidates the cloud API key; pipeline successfully completes screening via local Ollama (`llama3.2:3b`). |
| **Implement Injection Defense** | Challenge 1 (Enhance Guard) | Code Review & Automated QA | Trainee updates `PromptInjectionGuard.cs` to block short CVs. Evaluation harness maintains $100\%$ refusal on adversarial queries. |
| **Evaluate & Tune Agent Rubrics** | Challenge 2 (Rubric Weights) | Functional Output Check | Trainee modifies Agent 3 prompt; CV lacking "Cloud Architecture" triggers an automatic 20-point deduction. |
| **Measure Telemetry & Observability** | Challenge 3 (Latency Telemetry) | Code Execution & Log Trace | Trainee wraps Agent 2 in a `Stopwatch` and records elapsed milliseconds inside diagnostic metadata. |

---

## 3. Grading Policy (100 Points Total)

- **Foundation (50 Points)**:
  - Successful Docker bringup and ingestion walkthrough.
  - Demonstrated understanding of RAG citations and refusal behavior.
  - Verified local failover to Ollama under simulated network drop (T2).
- **Practical Challenges (30 Points)**:
  - Challenge 1 (Guard Implementation): 10 Points.
  - Challenge 2 (Rubric Tuning): 10 Points.
  - Challenge 3 (Telemetry Injection): 10 Points.
- **Evaluation & Code Quality (20 Points)**:
  - Running `dotnet test` with 0 failures: 10 Points.
  - Running `npm run eval:qa` achieving $\ge 90\%$ Hit-Rate: 10 Points.

*Grade Scale:*
- **Pass**: $\ge 70$ Points.
- **Distinction**: $\ge 90$ Points with all 3 Stretch Challenges fully functional.
