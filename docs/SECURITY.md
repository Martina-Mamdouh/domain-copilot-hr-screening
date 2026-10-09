# Security & Compliance (Domain Copilot)

This document outlines the security measures, risk mitigation strategies, and compliance implementations integrated into the Domain Copilot architecture, specifically addressing OWASP Top 10 vulnerabilities for both LLMs and Web Applications.

---

## 1. OWASP LLM Top 10 Mitigation

### LLM01: Prompt Injection
Prompt injection occurs when an attacker manipulates the LLM's input to bypass security filters or alter its intended instructions.
* **Mitigation**: 
  1. **Prompt Injection Guard**: We implemented a deterministic Regex-based `PromptInjectionGuard` service that executes *before* any text reaches the LLM. It actively scans for malicious keywords (e.g., "ignore previous instructions", "system prompt", "bypass"). If detected, the pipeline halts immediately, refusing to process the CV.
  2. **Delimiters & Strict Formatting**: All prompts use strict delimiters (`Job Description: {jd}`, `Candidate CV: {cv}`) to clearly separate system instructions from untrusted user data.

### LLM02: Insecure Output Handling
Blindly trusting LLM output can lead to XSS, SSRF, or backend execution flaws.
* **Mitigation**: LLM outputs are strictly validated using C# Regex parsing. In Agent 3, we expect specific formats (`SCORE: [val]`, `RECOMMENDATION: [val]`). Any deviation defaults to a safe `Hold` state. Furthermore, the Angular frontend utilizes native sanitization to prevent XSS if an LLM returns rogue HTML.

### LLM06: Sensitive Information Disclosure
Transmitting Personally Identifiable Information (PII) to third-party LLMs violates data privacy laws (GDPR/CCPA).
* **Mitigation**: We introduced a dedicated **Sanitization Agent (Agent 2)**. This agent sits inside the trust boundary and explicitly strips/redacts Names, Genders, Ages, and Addresses from the candidate profile *before* the data is sent to the primary Evaluator Agent.

### LLM07: Insecure Plugin Design
Unrestricted agent tools can be weaponized.
* **Mitigation**: The system does not use open-ended autonomous tool execution (like ReAct with arbitrary API access). The pipeline is deterministic and tightly sandboxed. The only external API call permitted is to the trusted Vector Store.

---

## 2. OWASP Web Top 10 Mitigation

### A01:2021-Broken Access Control
* **Mitigation**: We implemented strict Role-Based Access Control (RBAC) via JWTs.
  - **Recruiter**: Can upload CVs and initiate evaluations (`[Authorize(Roles = "Recruiter")]`).
  - **Hiring Manager**: Can view candidates, approve/reject, and edit decisions (`[Authorize(Roles = "HiringManager,Admin")]`).
  - **Admin**: Can view system-level Execution Traces, Tokens, and cost metrics (`[Authorize(Roles = "Admin")]`).

### A03:2021-Injection (SQL Injection)
* **Mitigation**: Direct SQL queries are strictly prohibited. The system interacts with the SQL Server exclusively via **Entity Framework Core (EF Core)** using LINQ, which automatically parameterizes all queries and entirely neutralizes SQL injection risks.

### A05:2021-Security Misconfiguration
* **Mitigation**: 
  - Cross-Origin Resource Sharing (CORS) is explicitly configured to allow only the trusted Angular frontend origin (`http://localhost:4200` or production URL).
  - Detailed error messages and stack traces are suppressed in the production environment to prevent information leakage.

### A07:2021-Identification and Authentication Failures
* **Mitigation**: The system relies on secure JWT (JSON Web Tokens) with short expiration times and proper cryptographic signing to handle authentication. Passwords are hashed using secure algorithms via ASP.NET Core Identity.

---

## 3. Compliance & Auditability
* **Immutable Audit Logs**: Every critical action, specifically a Manager overriding an AI decision, is committed to an append-only `AuditLogs` table. The log captures the user's ID, a timestamp, the required reasoning, and the exact JSON snapshot of the state before and after the change.
* **Traceability**: All execution traces, including exact prompts and responses, are tied via `CandidateEvaluationId` to guarantee that every decision can be investigated historically.
