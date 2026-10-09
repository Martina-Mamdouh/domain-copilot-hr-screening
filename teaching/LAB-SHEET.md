# Agentic HR Screening: Hands-on Lab Sheet

**Target Audience:** Post-graduate Software & AI Engineers  
**Estimated Duration:** 2.5 - 3 Hours  
**Difficulty:** Advanced  
**Domain & Twist:** HR Talent Screening (D6) with Offline Resilience (T2)

---

## 1. Lab Objectives
By the end of this lab, participants will be able to:
1. Deploy and orchestrate a multi-agent AI pipeline locally using Docker Compose.
2. Verify grounded RAG retrieval and citation compliance (`[Doc_X_Chunk_Y]`).
3. Analyze execution traces to observe token consumption and PII anonymization boundaries.
4. Test automated failover mechanisms from Cloud LLMs (Gemini) to Local LLMs (Ollama) under degraded network conditions.
5. Modify pipeline guards, scoring rubrics, and diagnostic telemetry in C#/.NET 8.

---

## 2. Prerequisites & Environment Setup
Ensure your local workstation meets the following requirements:
- **Docker Desktop** installed and running with at least 8 GB of allocated RAM.
- **.NET 8 SDK** and **Node.js (v18+)** installed locally.
- Terminal access (PowerShell, Bash, or WSL2).
- Cloned repository:
  ```bash
  git clone <repo-url>
  cd domain-copilot-hr-screening
  cp .env.example .env
  docker compose up -d --build
  ```

---

## 3. Guided Steps: System Walkthrough

### Step 1: Ingestion & Vector Indexing (FR-1)
1. Navigate to `http://localhost:4200` and authenticate as Recruiter ( `recruiter@copilot.local` | `Recruiter@123456` ).
2. Go to the Knowledge Base tab and upload `Software_Engineer_Policy.pdf`.
3. Check container logs (`docker logs domain-copilot-api`) to observe document parsing, chunking (500 tokens with 50-token overlap), and embedding generation.

### Step 2: Grounded RAG & Refusal Testing (FR-2 / BP-1)
1. Navigate to Copilot Chat.
2. Query: "What are the mandatory qualifications for a Senior Backend Engineer?"
3. Verify: The generated response must explicitly cite sources in the strict format `[Doc_X_Chunk_Y]`.
4. Submit an out-of-corpus query: "What is the best recipe for chocolate cake?"
5. Verify: The system must politely refuse to answer without hallucinating.

### Step 3: Multi-Agent Screening Execution (FR-4 / BP-2)
1. Open the Screening view and submit a candidate CV against the Senior Engineer JD.
2. Watch the real-time pipeline status:
   - Agent 1 (Extractor) parses skills and validates minimum experience.
   - Agent 2 (Sanitizer) redacts candidate PII.
   - Agent 3 (Evaluator) calculates weighted score and suggests interview probes.
3. Switch user persona to Hiring Manager (`manager@domain.com`).
4. Access the Approval Gate: Review the AI evaluation, modify the recommendation to SHORTLIST, input an override reason, and save the decision.

### Step 4: Testing Offline Resilience (T2 Twist)
1. Simulate a cloud outage by invalidating your `GEMINI_API_KEY` in `.env` or disconnecting your internet connection.
2. Trigger a new screening run.
3. Log in as System Admin (`admin@copilot.local` ) and open Execution Traces.
4. Verify: The trace logs confirm that the primary provider failed and the system gracefully routed execution to local Ollama (`llama3.2:3b`).

---

## 4. Stretch Challenges (Coding Exercises)

### Challenge 1: Heuristic Length Guard in PromptInjectionGuard
Currently, the guard checks for regex patterns like "ignore previous instructions".
**Task:** Update the security guard to immediately fail with an Early Exit if the input CV text contains fewer than 50 characters (preventing empty/stub submissions from invoking LLMs).

### Challenge 2: Penalize Missing Cloud Architecture in Agent 3 Rubric
**Task:** Modify the system prompt configuration for Agent 3 so that if a candidate lacks demonstrable "Cloud Architecture" experience, their final score is strictly penalized by 20 points, accompanied by an explicit note in the reasoning string.

### Challenge 3: Granular Latency Telemetry for Agent 2
**Task:** In `ScreeningPipelineService.cs`, measure the execution duration of Agent 2 (Sanitizer) using `System.Diagnostics.Stopwatch`. Record this duration inside the diagnostic trace data.

---

## 5. Instructor Answer Key & Code Solutions

### Solution for Challenge 1 (PromptInjectionGuard.cs)
```csharp
public bool ValidateInput(string cvText)
{
    // Heuristic short-circuit gate
    if (string.IsNullOrWhiteSpace(cvText) || cvText.Trim().Length < 50)
    {
        _logger.LogWarning("Input rejected: CV payload contains fewer than 50 characters.");
        return false;
    }

    // Existing regex injection patterns
    var adversarialPattern = new Regex(@"(ignore\s+previous|bypass\s+rules|system\s+prompt)", RegexOptions.IgnoreCase);
    return !adversarialPattern.IsMatch(cvText);
}
```

### Solution for Challenge 2 (EvaluatorPromptTemplate)
Update the prompt template to include this strict instruction:
```plaintext
CRITICAL RUBRIC CONSTRAINT:
Inspect the candidate's technical skills for "Cloud Architecture" or "Cloud Computing".
If this competency is missing, you MUST automatically deduct 20 points from the calculated score 
and append the following sentence to the reasoning field: 
"[DEDUCTION: Lacks verified Cloud Architecture expertise]."
```

### Solution for Challenge 3 (ScreeningPipelineService.cs)
```csharp
var stopwatch = Stopwatch.StartNew();
var sanitizedProfile = await _agent2Sanitizer.SanitizeAsync(extractedSkills, cancellationToken);
stopwatch.Stop();

_telemetryLogger.LogInformation("Agent 2 (Bias Sanitizer) completed in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);

// Append to execution trace
trace.AddMetadata("Agent2_Latency_Ms", stopwatch.ElapsedMilliseconds);
```

---

## 6. Lab Deliverables & Assessment Criteria
Trainees must submit:
1. Passing test suite results: `dotnet test`.
2. Output of automated evaluation script: `npm run eval:qa` showing $\ge 90\%$ hit-rate.
3. Git commit diff demonstrating clean implementations for Challenges 1, 2, and 3.
