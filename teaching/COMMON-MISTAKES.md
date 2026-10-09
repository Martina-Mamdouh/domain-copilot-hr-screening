# 5 Common Mistakes in Enterprise LLM & RAG Systems

This document highlights five critical misconceptions engineers frequently encounter when transitioning from building LLM prototypes to enterprise-grade production systems.

---

## 1. The "Mega-Prompt" Fallacy
* **The Misconception**: Believing that providing a single, massive 4000-token prompt with 20 different instructions (Extract, Format, Sanitize, Score, Recommend) is the most efficient way to use an LLM.
* **Why it's wrong**: LLMs suffer from "Lost in the Middle" syndrome. Mega-prompts lead to severe hallucinations, ignored instructions, and massive token costs for unqualified inputs.
* **The Correction**: **Agentic Workflows**. Break the prompt into a linear pipeline of smaller, specialized agents (e.g., Extractor $\rightarrow$ Sanitizer $\rightarrow$ Evaluator) that pass typed DTO contracts to each other.

## 2. Blind Trust in Vector Similarity (RAG)
* **The Misconception**: Assuming that if Cosine Similarity returns a chunk of text, it definitively answers the user's question.
* **Why it's wrong**: Vector search finds *semantic closeness*, not factual truth. Searching for "How to fire an employee" might retrieve chunks about "How to hire an employee" simply because the embedding space clusters HR verbs closely.
* **The Correction**: **Grounded Generation (BP-1)**. The LLM must explicitly be instructed to read the retrieved chunks and output a refusal (e.g., "Not found in documents") if the context does not explicitly answer the query, rather than hallucinating based on general knowledge.

## 3. Ignoring PII on the Trust Boundary (OWASP LLM06)
* **The Misconception**: Sending raw candidate resumes (containing names, ages, gender, and addresses) directly to external APIs like OpenAI or Gemini for evaluation.
* **Why it's wrong**: This is a direct violation of GDPR/CCPA. Additionally, it exposes the LLM to unconscious bias, leading to unfair hiring scores based on demographics.
* **The Correction**: **Data Anonymization Agents**. Implement a Sanitizer Agent or a deterministic Regex scrubber inside the application's DMZ (trust boundary) that explicitly redacts PII *before* the payload is sent to the final decision-making LLM.

## 4. Failing Open on LLM Timeouts (No Circuit Breakers)
* **The Misconception**: Assuming the cloud LLM provider (OpenAI, Gemini) will always maintain 100% uptime and sub-second latency.
* **Why it's wrong**: API rate limits and network partitions happen. If your code directly awaits the HTTP response without a fallback, the entire application thread locks up, crashing the backend.
* **The Correction**: **Resilient Fallbacks (Twist T2)**. Implement the Circuit Breaker pattern (using libraries like Polly). If the primary provider fails, immediately route the request to a degraded offline local model (e.g., Ollama) to maintain business continuity.

## 5. Lack of Immutable Audit Trails (Human-in-the-Loop)
* **The Misconception**: Allowing the LLM to directly write its decision (e.g., "Reject Candidate") to the database and sending an automated email without human oversight.
* **Why it's wrong**: "The AI rejected me" is not a legally defensible stance in HR screening. It violates Binding Principle #2 (The human holds the pen).
* **The Correction**: **Approval Gates & Audit Logs**. The AI should only output a "Recommendation" in a `Pending` state. A human Manager must explicitly approve or override the decision, generating an immutable Audit Log entry capturing the *Reasoning* and the *Exact Tokens* used.
