# Agentic Workflow

This document outlines the primary agentic workflow for the Domain Copilot (HR Screening) platform.

## Architecture

The workflow follows a multi-step pattern utilizing specialized AI agents:

1. **Information Extraction**: Given a candidate CV, the `Extractor Agent` queries the RAG system to pull relevant experiences mapped to the Job Description (JD).
2. **Evaluation**: The `Evaluator Agent` takes the extracted information and scores it against the defined Rubric.
3. **Approval Gate**: Before any final rejection or acceptance is sent to the candidate, the workflow pauses. A human HR expert must review the evaluation and either approve or override the decision.
4. **Final Action**: Upon approval, the `Action Agent` dispatches the final communication.

## Observability

All agent interactions, prompts, token usage, and decisions are logged into the trace system to ensure full auditability.
