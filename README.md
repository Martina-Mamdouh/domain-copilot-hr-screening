# DomainCopilot HR Screening

## 🚀 How to Run the Project

This project requires a Gemini API Key to run the AI Agents. 
For security reasons, API keys are not committed to the repository.

### Setup Instructions

1. **Get an API Key**: Go to [Google AI Studio](https://aistudio.google.com/) and create a free Gemini API Key.
2. **Configure Environment Variables**:
   - In the root of the project, you will find a file named `.env.example`.
   - Copy this file and rename it to `.env`.
   - Open the `.env` file and replace `your_gemini_api_key_here` with your actual API key.

```bash
# Example
GEMINI_API_KEY=AIzaSyA...
```

3. **Run the Project**:
   Start the application using Docker Compose:
```bash
docker-compose up -d --build
```

The system will automatically read your API key from the `.env` file and inject it into the API container. If Gemini fails, it will gracefully fallback to a local Ollama model (`phi3` or `llama3.2`).
