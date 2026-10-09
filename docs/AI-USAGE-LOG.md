# AI Usage Log

This document records the utilization of AI pair programming assistants (specifically Antigravity Agent and Google Gemini) during the development of the Domain Copilot platform, fulfilling the requirement for transparency in AI-assisted development.

## 1. Where AI Excelled (Significant Acceleration)

### 1.1 Boilerplate and CRUD Generation
* **Usage**: Generated the initial scaffolding for the Clean Architecture, including `MediatR` commands and EF Core configurations.
* **Impact**: Saved hours of repetitive typing, allowing the focus to shift immediately to business logic.

### 1.2 Frontend Component Styling
* **Usage**: Prompting the AI to convert standard HTML structures into modern, "Glassmorphism" styled UI components using Vanilla CSS.
* **Impact**: The UI achieved a premium look and feel with minimal manual CSS tweaking.

### 1.3 Regex & Parsing Patterns
* **Usage**: Creating complex Regex patterns to safely parse structured outputs (`SCORE: \d+`, `RECOMMENDATION: \w+`) from the noisy Agent 3 LLM responses.
* **Impact**: Prevented runtime parsing errors when the Local LLM (Ollama) included markdown formatting in its responses.

---

## 2. Where AI Struggled & Required Human Engineering

### 2.1 Context Loss in Multi-Agent Design
* **The Struggle**: When initially prompted to create the screening logic, the AI attempted to build a massive, single-prompt instruction. This caused the model to hallucinate skills or completely ignore the Job Description.
* **Human Correction**: I explicitly mandated the AI to break the logic into a **3-Agent Pipeline** (Extractor -> Sanitizer -> Evaluator). I defined strict DTO "contracts" for each agent to pass state sequentially.

### 2.2 Blind Implementation of In-Memory RAG
* **The Struggle**: The AI suggested using external cloud Vector Databases (like Pinecone) or complex Python dependencies, which violated the "self-contained execution" constraint of the assignment.
* **Human Correction**: I instructed the AI to build a pure C# in-memory Cosine Similarity search over the EF Core database using `System.Numerics.Tensors`, ensuring zero external dependencies.

### 2.3 The "Offline Fallback" (T2 Twist) Nuances
* **The Struggle**: The AI assistant generated fallback logic that blocked the main thread or did not properly capture timeout exceptions when the primary API failed.
* **Human Correction**: I refactored the generated code into `ResilientLLMService`, utilizing proper CancellationToken propagation and implementing a formal Circuit Breaker pattern to seamlessly switch to the local `Ollama` instance running on port 11434.

---

## 3. Prompt Engineering Lessons Learned
* **Delimiters are crucial**: Without strong XML or Markdown delimiters (`<CandidateCV>`, `<JobDescription>`), the local 3B model was easily confused between the prompt instructions and the user data.
* **Chain of Thought (CoT)**: Forcing the Evaluator Agent to output its `REASONING` *before* outputting the final `RECOMMENDATION` significantly improved the accuracy and logical consistency of its decisions.
