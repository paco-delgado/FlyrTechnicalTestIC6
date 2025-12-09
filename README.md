# FlyrTech - Principal Engineer Challenge

## 🚨 Incident Report: Data Consistency Issues

**Severity:** High
**Component:** Journey Service
**Description:** 
Our operations team has reported intermittent data loss in the Journey Service. During peak traffic windows, when multiple updates occur simultaneously for the same journey, some segment status changes appear to be lost.

The current hypothesis is that we have a concurrency issue in how we handle journey updates, but we need a senior engineer to investigate, confirm the root cause, and implement a robust solution.

## 🎯 Your Mission

As a Principal Engineer, your task is to take ownership of this problem space:

1. **Investigate & Diagnose:** Analyze the codebase to identify the root cause of the data loss.
2. **Architectural Analysis:** Produce a technical document that:
   - Explains the root cause.
   - Proposes multiple potential solutions with a trade-off analysis (complexity, performance, reliability).
   - Recommends a path forward for production.
   - Outlines how you would monitor and prevent this in the future.
3. **Implementation:** Implement your recommended fix to resolve the issue.

**Note:** We care deeply about **how you think**. We want to see how you approach complex distributed system problems, how you evaluate architectural trade-offs, and how you communicate your decisions.

## 🛠️ Getting Started

**1. Fork & Clone:**
Fork this repository and clone it to your local machine.

**2. Environment Setup:**
The service uses Redis. You can start it via Docker:
```powershell
docker run -d -p 6379:6379 --name redis-flyrtech redis:latest
```

**3. Exploration:**
The codebase is a .NET 8 solution with Clean Architecture.
- `FlyrTech.Api`: The entry point.
- `FlyrTech.Infrastructure`: Service implementations.
- `FlyrTech.Core`: Domain definitions.
- `FlyrTech.Tests`: Unit and integration tests.

We have a test suite that might help reproduce the reported issue. Look for `JourneyRaceConditionTests.cs`.

## 📦 Deliverables

Please submit a link to your forked repository containing:

1. **Analysis Document:** A markdown file (e.g., `ANALYSIS.md`) covering your diagnosis, design proposals, and recommendations.
2. **Code Implementation:** The fix for the issue.
3. **Tests:** Any additional tests you added to verify your solution.

## 🔧 Technical Details

### Prerequisites
- .NET 8 SDK
- Redis (Local or Docker)

### Building & Running
```powershell
dotnet build FlyrTech.sln
dotnet test
```

### Configuration
The API connects to Redis on `localhost:6379` by default. Configuration is in `appsettings.json`.

## License
MIT
