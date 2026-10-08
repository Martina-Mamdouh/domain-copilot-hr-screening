# Contributing to Domain Copilot HR Screening

First off, thank you for considering contributing to Domain Copilot! This platform uses an advanced Agentic Workflow and Retrieval-Augmented Generation (RAG) to automate HR resume screening and answer questions based on internal guidelines.

## Architecture Overview

The system is built with a clear separation of concerns:

### Backend (.NET 8, Clean Architecture)
- **Core**: Contains entities (e.g., `ScreeningResult`, `AgentExecutionTrace`) and interfaces (`IGroundedGenerationService`, `IResilientLLMService`).
- **Features (Vertical Slice)**: Groups application logic by feature. 
  - `Screening`: Holds the 3-Agent Evaluator Pipeline.
  - `Corpus`: Handles RAG ingestion, Vector DB chunking, Hybrid Retrieval (Lexical + Semantic RRF), and Grounded Generation.
- **Infrastructure**: Implementations of external services (e.g., `ResilientLLMService` using Gemini as Primary and Ollama as Fallback).

### Frontend (Angular 17+, Standalone Components)
- **Design Philosophy**: Premium, dark-mode Glassmorphism UI built with pure CSS to ensure a vibrant and modern user experience.
- **Features**:
  - `Dashboard`: Live overview of candidate evaluations.
  - `Screening Upload`: Interface to run new candidates through the Agentic Pipeline.
  - `Copilot Chat`: Direct conversational interface to interact with the Corpus, showing verified sources.
  - `Corpus Ingestion`: Tools to upload organizational guidelines to the Vector DB.

## Getting Started

### Prerequisites
1. **.NET 8 SDK**
2. **Node.js** (v18+)
3. **Local LLM / API Keys**: You will need either a Gemini API key or a local Ollama instance running.

### Setting up the Backend
1. Navigate to `src/DomainCopilot.Api`
2. Run `dotnet restore`
3. Update `appsettings.Development.json` with your API keys.
4. Run `dotnet run` or use Visual Studio. The API runs on `http://localhost:5255`.

### Setting up the Frontend
1. Navigate to `src/DomainCopilot.Web`
2. Run `npm install`
3. Run `npm start`
4. The Angular dev server will host the UI at `http://localhost:4200`.

## Contribution Guidelines
- **Code Style**: Follow standard C# naming conventions for backend. Follow Angular Style Guide for frontend.
- **Agent Responses**: If modifying LLM Prompts, ensure you maintain the JSON/Regex extraction formats used by the parsers.
- **Testing**: All core AI agent flows (Retrieval, Fallbacks, Pipelines) must be covered by xUnit Tests. Run `dotnet test` before submitting a PR.
- **UI Design**: Any new frontend components must adhere to the `glass-panel` and dark mode theme variables defined in `styles.css`.

Happy Coding!
