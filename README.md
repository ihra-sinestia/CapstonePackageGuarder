# 🤖 Capstone Package Guarder (CPG)

**Autonomous AI Security Agent** | Built for Ciklum AI Academy Capstone Project

Capstone Package Guarder (CPG) is an autonomous C# agent designed to identify and remediate vulnerable NuGet packages in .NET projects. Leveraging **Microsoft Semantic Kernel** and **Google Gemini API**, it uses Retrieval-Augmented Generation (RAG) to cross-reference installed dependencies against a custom security knowledge base.

---

## 🚀 Key Features
- **Project Auto-Discovery**: Recursively finds all `.csproj` files in any specified directory tree.
- **Dependency Analysis**: High-precision parsing of NuGet references using XML `XDocument` (avoiding comments and false positives).
- **RAG-Powered Auditing**: Semantic search across a local vector memory of known CVEs and security advisories.
- **Intelligent Remediation**: Autonomous version comparison and terminal-based package updates using `dotnet add package`.
- **Resilient API Handling**: Built-in 429 (Rate Limit) retry logic with exponential backoff for stable operation on free AI tiers.

---

## 🛠 Technology Stack
- **Language**: C# / .NET 9.0
- **AI Orchestration**: Microsoft Semantic Kernel
- **LLM Model**: Google Gemini 2.5 Flash
- **Vector Memory**: Volatile (In-memory) with extension support for Qdrant.
- **Configuration**: .NET User Secrets for secure API management.

---

## ⚙️ Setup & Installation

### 1. Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Google Gemini API Key (get one at [Google AI Studio](https://aistudio.google.com/))

### 2. Configure API Key
The agent looks for the `GeminiApiKey` in the system's Secret Store. Run this from the `CapstonePackageGuarder` folder:
```bash
dotnet user-secrets set "GeminiApiKey" "YOUR_GEMINI_API_KEY_HERE"
```

### 3. Build & Run
```bash
dotnet build
dotnet run --project CapstonePackageGuarder
```

---

## 🧪 Testing with TestApp
The repository includes a `TestApp` project pre-configured with vulnerable dependencies for evaluation.

1. **Initial State**: `TestApp.csproj` contains `Newtonsoft.Json 12.0.1` (vulnerable).
2. **Execution**:
   - Start CPG and type: `Check my test project at ../TestApp/TestApp.csproj`
   - CPG will detect the vulnerability, explain the risk, and offer an update.
3. **Advanced Scenario (Continuous Scan)**:
   - Uncomment the `AutoMapper` package in `TestApp.csproj`.
   - Re-scan to see how the agent dynamically detects the new threat.

---

## 🏗 Architecture
The agent follows a modular plugin architecture. See [architecture.mmd](./architecture.mmd) for a full Mermaid visualization of the data flow.
