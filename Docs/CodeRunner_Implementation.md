# SHIFT Code Runner Service & Application Integration Documentation

This document outlines all architectural components, file changes, security controls, API specifications, and testing results implemented for the **SHIFT Code Runner Service** (`CodeRunner`) and its integration with `LoopGame.Application`.

---

## 1. System Architecture Overview

```
[ SHIFT Backend / PracticeService ]
                │
                ▼
  [ LoopGame.Application.CodeExecutionService ]
                │
                ▼ (HTTP POST /execute)
  [ CodeRunner.Api (CodeExecutionController) ]
                │
                ▼
  [ CodeRunner.Services.CodeExecutionService ]
                │
                ▼
  [ DockerSandboxService ]
                │
                ▼ (Docker daemon API)
┌────────────────────────────────────────────────────────┐
│ Docker Sandbox Container (shift-c-runner:latest)       │
│                                                        │
│  - Linux environment (Alpine 3.20) + gcc + musl-dev   │
│  - Non-root execution user (sandboxuser UID 1000)      │
│  - Security isolation: --network none, memory limits,  │
│    CPU quota (0.5), process cap (64), read-only opt    │
│  - Workspace: /workspace/source.c → /workspace/program │
└────────────────────────────────────────────────────────┘
```

---

## 2. Changes Implemented in `CodeRunner` Project

### 2.1. Docker Sandbox Execution Environment
- **File**: `CodeRunner/Docker/Dockerfile.c-runner`
  - Pre-builds an immutable, lightweight execution container image (`shift-c-runner:latest`) based on `alpine:3.20`.
  - Includes `gcc` and standard C headers (`musl-dev`).
  - Configures non-root user `sandboxuser` (`UID 1000`) and isolated `/workspace` working directory.

### 2.2. Options & Configuration
- **File**: `CodeRunner/Options/CodeRunnerOptions.cs`
  - Configurable server-side options (`DockerImage`, `TimeoutSeconds` = 5, `MemoryLimitMb` = 128, `CpuLimit` = 0.5, `MaxProcesses` = 64, `MaxOutputBytes` = 1MB).
- **File**: `CodeRunner/appsettings.json`
  - Binds options section `CodeRunner`.

### 2.3. Data Transfer Models (DTOs)
- **File**: `CodeRunner/Models/ExecuteCodeRequest.cs`: Accepts `language`, `source_code`, `test_cases`.
- **File**: `CodeRunner/Models/TestCaseRequest.cs`: Accepts `test_case_id`, `input`, `expected_output`.
- **File**: `CodeRunner/Models/ExecuteCodeResponse.cs`: Returns `success`, `status`, `compile_error`, `results`.
- **File**: `CodeRunner/Models/TestCaseResult.cs`: Returns `test_case_id`, `passed`, `status`, `actual_output`, `execution_time_ms`, `exit_code`, `error`.

### 2.4. Docker Sandbox Infrastructure Abstraction
- **File**: `CodeRunner/Services/ISandboxService.cs`
  - Interface exposing container management (`CreateSandboxAsync`, `WriteFileAsync`, `CompileAsync`, `RunAsync`, `DestroySandboxAsync`).
- **File**: `CodeRunner/Services/DockerSandboxService.cs`
  - Spawns container per request with strict security parameters (`--network none`, `--memory 128m`, `--cpus 0.5`, `--pids-limit 64`, `--user 1000:1000`).
  - Encodes source code as Base64 to safely write `source.c` inside the container without shell injection risks.
  - Compiles C source code (`gcc -O2 -Wall -std=c11 /workspace/source.c -o /workspace/program`).
  - Streams STDIN/STDOUT/STDERR and manages execution timeout enforcement (5s hard limit).
  - Guarantees container cleanup via `try ... finally` logic.

### 2.5. Workflow Orchestration & Normalization Engine
- **File**: `CodeRunner/Services/ICodeExecutionService.cs`
- **File**: `CodeRunner/Services/CodeExecutionService.cs`
  - Validates C language request.
  - Orchestrates creation -> compilation -> test case execution -> cleanup pipeline.
  - If compilation fails, immediately halts test execution and returns `CompilationError` with compiler stderr.
  - Implements deterministic output normalization (normalizes CRLF to LF, trims trailing line spaces, and trims trailing newlines).
  - Evaluates statuses: `Passed`, `WrongAnswer`, `RuntimeError`, `Timeout`, `OutputLimitExceeded`.

### 2.6. REST API Controller & Program Entry point
- **File**: `CodeRunner/Controllers/CodeExecutionController.cs`
  - Exposes `POST /execute`.
  - Validates request payload and maps execution responses to `200 OK` (or `400 Bad Request` for invalid requests).
- **File**: `CodeRunner/Program.cs`
  - Registers options, services, OpenAPI/Swagger, and JSON naming policy (`snake_case`).

### 2.7. Container Orchestration Setup
- **File**: `compose.yaml`
  - Configures `coderunner` service and Docker socket mounting (`/var/run/docker.sock:/var/run/docker.sock`) for local development execution.

---

## 3. Changes Implemented in `LoopGame.Application`

### 3.1. Client HTTP Proxy Service
- **File**: `LoopGame.Application/Services/LearningAndContentServices/CodeExecutionService.cs`
  - Implements `ICodeExecutionService` inside `LoopGame.Application`.
  - Acts as a **Typed HTTP Client Proxy**.
  - Maps Domain entities (`TestCase`) to JSON request payload (`language: "c"`, `source_code`, `test_cases`).
  - Sends HTTP POST request to `CodeRunner.Api` (`/execute`).
  - Reads response payload and converts `CodeRunner` results into `LoopGame.Domain.ValueObjects.TestCaseResult` instances consumed by `PracticeService`.
  - Handles network failures and non-200 responses gracefully.

### 3.2. Dependency Injection Registration
- **File**: `LoopGame.Application/DependencyInjection.cs`
  - Registers `ICodeExecutionService` with `AddHttpClient<ICodeExecutionService, CodeExecutionService>()`.
  - Configures default base address from configuration key `CodeRunner:BaseUrl` (defaulting to `http://localhost:5000`).

---

## 4. Verification & Testing Strategy

### 4.1. Unit Tests
- **File**: `LoopGame.Tests/Services/CodeExecutionServiceTests.cs`
  - `ExecuteAsync_CompilationError_ReturnsCompilationErrorAndNoResults`: Mocks compilation failure and verifies no test cases are executed.
  - `ExecuteAsync_SuccessfulCompilationAndExecution_ReturnsResults`: Mocks successful compilation and test case execution.
  - `NormalizeOutput_HandlesLineEndingsAndWhitespace`: Tests output normalization for Windows CRLF vs Linux LF line endings and trailing whitespace.
  - `EvaluateTestCaseResult_Timeout_ReturnsTimeoutStatus`: Tests timeout status evaluation.
  - `EvaluateTestCaseResult_RuntimeError_ReturnsRuntimeErrorStatus`: Tests segfault/runtime crash handling.

### 4.2. Docker Integration Tests
- **File**: `LoopGame.Tests/Services/DockerSandboxIntegrationTests.cs`
  - `RealDocker_ExecuteCProgram_CalculatesDoubleInput`: Compiles real C code inside Docker sandbox, pipes STDIN `5` and `10`, verifies outputs `10` and `20`.
  - `RealDocker_ExecuteInvalidCCode_ReturnsCompilationError`: Verifies gcc syntax error output.
  - `RealDocker_InfiniteLoop_TimesOutAndCleansUp`: Executes a C program with `while(1);`, verifies hard timeout termination (2s/5s) and automatic container cleanup.

### 4.3. Test Execution Results
All 76 unit and integration tests passed cleanly:
```bash
~/.dotnet/dotnet test LoopGame.Tests/LoopGame.Tests.csproj
Passed!  - Failed: 0, Passed: 76, Skipped: 0, Total: 76
```
