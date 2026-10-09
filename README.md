# Domain Copilot: HR Talent Screening (D6)

This repository contains the complete implementation for the **Domain Copilot** assessment, focusing on the HR Talent Screening domain. It strictly adheres to all architectural constraints, Binding Principles, and Agentic Workflow specifications.

## 1. Domain & Twist Calculation
Based on the provided National ID (NID) assignment rules:
- **Domain Calculation:** `(last two digits of NID) mod 7`. This evaluated to **D6** (HR Talent Screening).
- **Twist Calculation:** `(sum of all digits in NID) mod 8`. This evaluated to **T2** (Offline & Resilient Fallback).
- **Implementation:** The system seamlessly falls back to a locally hosted Ollama model (Llama 3.2) if the primary cloud LLM provider is unavailable, ensuring business continuity.

## 2. Project Documentation
Detailed architectural and engineering documentation can be found in the `docs/` folder:
- [System Design (Part A vs MVP)](docs/SYSTEM-DESIGN.md)
- [Architecture & Diagrams](docs/ARCHITECTURE.md)
- [Agentic Workflow & Design](docs/AGENTIC-WORKFLOW.md)
- [Security & Compliance](docs/SECURITY.md)
- [Automated Evaluation (FR-3)](docs/EVALUATION.md)
- [Business Requirements (BRD)](docs/BRD.md)
- [AI Usage Log](docs/AI-USAGE-LOG.md)
- [Teaching Pack (Slides, Lab Sheet, Outcomes)](teaching/)

---

## 3. Quickstart Guide
The entire platform (Frontend, Backend API, Database, and Local LLM) is fully containerized. To run the system locally:

1. Ensure Docker Desktop is installed and running.
2. Open your terminal at the root of this repository.
3. Run the following command:
   ```bash
   docker-compose up -d --build
   ```
4. The Angular UI will be accessible at: **http://localhost:4200**
5. The Backend API Swagger documentation will be accessible at: **http://localhost:8080/swagger**

### Running Tests & Automated Evaluations
To run the standard unit tests for the backend:
```bash
cd src/DomainCopilot.Tests
dotnet test
```

To run the automated agentic evaluation script (FR-3):
```bash
cd eval
npm install
npm run eval:qa
```

---

## 4. The 5-Minute Demo Path
Follow these steps to evaluate the core features of the system:

1. **Login:** Use the Recruiter credentials (below) to access the system.
2. **Ingest Corpus (FR-1):** Navigate to the *Knowledge Base* tab and upload a sample HR Policy or Job Description (TXT/PDF). 
3. **Chat & Retrieve (FR-2):** Go to the *Copilot Chat*, ask a policy-related question. Notice the strict formatting and `[Doc_X_Chunk_Y]` citations.
4. **Agentic Screening (FR-4):** Upload a sample Candidate CV. The 3-Agent pipeline will automatically extract skills, sanitize PII, and generate a recommended score and interview probes.
5. **Human-in-the-Loop (Approval Gate):** Log out and log back in using the **Hiring Manager** credentials. Click on the pending candidate, view the AI reasoning, and explicitly Approve, Reject, or Edit the decision.
6. **Observability (FR-9):** Log out and log in using the **Admin** credentials. Open the candidate review again to see the hidden *Execution Traces & Diagnostics* section, detailing token usage, latency, and the specific model used (Gemini vs Ollama).

---

## 5. Demo Credentials (RBAC)

The system enforces Role-Based Access Control. Use the following credentials to test different personas:

| Role | Email | Password | Permissions |
|---|---|---|---|
| **Recruiter** | `recruiter@domain.com` | `Recruiter!123` | Upload CVs, View Chat, Start Screening |
| **Hiring Manager** | `manager@domain.com` | `Manager!123` | View candidates, Override decisions (Approval Gate) |
| **System Admin** | `admin@domain.com` | `Admin!123` | View Observability traces, Cost metrics, and System health |

*(Note: In the current MVP local testing environment, these are mock roles simulated by the frontend Auth Service toggle for ease of demonstration without requiring complex external identity setup).*

---

## 6. Video Submissions

- **Product Demo Video (5-8 minutes):** [Insert YouTube Unlisted Link Here]
- **Teaching Sample Video (10 minutes):** [Insert YouTube Unlisted Link Here]
