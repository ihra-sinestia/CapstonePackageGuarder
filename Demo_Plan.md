# 🤖 Capstone Package Guarder (CPG)
**AI Security Agent Assignment - Ciklum AI Academy**

## 📌 Project Summary
CPG is an autonomous C# agent built entirely with Microsoft Semantic Kernel and the Gemini Pro/Flash API. 
Its core purpose is to autonomously analyze codebases, retrieve security insights via semantic search (RAG), and execute safe remediation commands via tool-calling.

## 🚀 Live Demo Workflow (Video Script)

### Step 1: Agent Initialization & RAG
- The agent ingests local security knowledge (`vulnerabilities_kb.md`) and shards it into Vector Memory spaces.

### Step 2: Auto-Discovery & Tool Use
- **Action:** User prompts the agent to scan a directory (`../`).
- **Tool Call (`find_projects`):** Agent natively navigates the filesystem to detect C# (.csproj) solutions.
- **Tool Call (`get_project_packages`):** Agent extracts installed NuGet packages and automatically compares them against the Vector DB for vulnerabilities.

### Step 3: LLM Reasoning
- The logic determines that `TestApp.csproj` is using an explicitly vulnerable package (`Newtonsoft.Json 12.0.1`), flagged via RAG.
- The Agent reports the detailed threat level back to the user and halts for permission.

### Step 4: Resolution & Write Operations
- **Action:** User gives explicit permission (`"Yes, update it"`).
- **Tool Call (`update_project_package`):** Agent executes a targeted safe reversion to `13.0.3` directly via the terminal.

### Step 5: Continuous Monitoring & Secondary Scan
- **Action:** To demonstrate persistent scanning, the user uncomments a hidden vulnerable dependency (`AutoMapper 6.0.0`) in the `TestApp.csproj` file and saves it.
- **Action:** The user asks: "Please check for vulnerable packages again."
- **Confirmation:** The agent dynamically scans the updated project, identifies the newly exposed `AutoMapper` vulnerability via the RAG database, and offers to update it to the secure `13.0.1` version!
