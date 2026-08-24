# SHIFT Game — Backend Architecture & Implementation Guide
**ASP.NET Core 8 | Clean Architecture | SQL Server | EF Core 8**
*Graduate Project — Helwan University, CS Department | 2026*

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Solution Structure & Folder Layout](#2-solution-structure--folder-layout)
3. [Layer Responsibilities](#3-layer-responsibilities)
   - 3.1 [Domain Layer](#31-domain-layer)
   - 3.2 [Application Layer](#32-application-layer)
   - 3.3 [Infrastructure Layer](#33-infrastructure-layer)
   - 3.4 [API Layer (Presentation)](#34-api-layer-presentation)
4. [NuGet Packages & Technology Decisions](#4-nuget-packages--technology-decisions)
5. [Services Catalog](#5-services-catalog)
   - 5.1 [AuthService](#51-authservice)
   - 5.2 [NarrativeService](#52-narrativeservice)
   - 5.3 [ChoiceService](#53-choiceservice)
   - 5.4 [PracticeService](#54-practiceservice)
   - 5.5 [CodeExecutionService](#55-codeexecutionservice)
   - 5.6 [SideTaskService](#56-sidetaskservice)
   - 5.7 [AiOrchestrationService](#57-aiorchestrationservice)
   - 5.8 [EconomyService](#58-economyservice)
   - 5.9 [ShopService](#59-shopservice)
   - 5.10 [SahmService](#510-sahmservice)
   - 5.11 [AssessmentService](#511-assessmentservice)
   - 5.12 [AdminService](#512-adminservice)
   - 5.13 [SaveService](#513-saveservice)
6. [Background Jobs (IHostedService)](#6-background-jobs-ihostedservice)
7. [API Endpoints Reference](#7-api-endpoints-reference)
   - 7.1 [Auth Endpoints](#71-auth-endpoints)
   - 7.2 [Game / Narrative Endpoints](#72-game--narrative-endpoints)
   - 7.3 [Code / Practice Endpoints](#73-code--practice-endpoints)
   - 7.4 [Side Task Endpoints](#74-side-task-endpoints)
   - 7.5 [Economy & Shop Endpoints](#75-economy--shop-endpoints)
   - 7.6 [Sahm AI Assistant Endpoints](#76-sahm-ai-assistant-endpoints)
   - 7.7 [Assessment Endpoints](#77-assessment-endpoints)
   - 7.8 [Admin Endpoints](#78-admin-endpoints)
8. [Cross-Cutting Concerns](#8-cross-cutting-concerns)
   - 8.1 [Authentication & Authorization](#81-authentication--authorization)
   - 8.2 [Exception Handling Middleware](#82-exception-handling-middleware)
   - 8.3 [Assessment Event Pipeline (Channel)](#83-assessment-event-pipeline-channel)
   - 8.4 [Docker Code Runner Integration](#84-docker-code-runner-integration)
9. [EF Core Configuration Notes](#9-ef-core-configuration-notes)
10. [Program.cs — Service Registration Overview](#10-programcs--service-registration-overview)

---

## 1. Architecture Overview

SHIFT uses **Clean Architecture** مقسمة على 4 projects داخل solution واحد. الفكرة الأساسية إن الـ **Domain** و **Application** مش بيعرفوا حاجة عن الـ database أو الـ HTTP — كل التبعيات بتتحرك من الخارج للداخل.

```
┌─────────────────────────────────────────────────────────┐
│                    SHIFT.API (Presentation)              │
│         Controllers · DTOs · Middleware · Program.cs     │
├─────────────────────────────────────────────────────────┤
│                 SHIFT.Application                        │
│    Interfaces · Services · DTOs · Background Jobs        │
├─────────────────────────────────────────────────────────┤
│                 SHIFT.Infrastructure                     │
│   DbContext · Repositories · AI Clients · Code Runner    │
├─────────────────────────────────────────────────────────┤
│                    SHIFT.Domain                          │
│        Entities · Enums · Constants · Domain Events      │
└─────────────────────────────────────────────────────────┘
```

**قاعدة الـ Dependency:**
- `SHIFT.Domain` ← لا يعتمد على أي project تاني
- `SHIFT.Application` ← يعتمد على `SHIFT.Domain` فقط
- `SHIFT.Infrastructure` ← يعتمد على `SHIFT.Application` + `SHIFT.Domain`
- `SHIFT.API` ← يعتمد على الكل، بس بيعمل الـ DI Registration

---

## 2. Solution Structure & Folder Layout

```
SHIFT.sln
│
├── src/
│   │
│   ├── SHIFT.Domain/                          ← Project 1
│   │   ├── Entities/
│   │   │   ├── Identity/
│   │   │   │   ├── ApplicationUser.cs
│   │   │   │   ├── ApplicationRole.cs
│   │   │   │   └── ClassCode.cs
│   │   │   ├── Narrative/
│   │   │   │   ├── Shift.cs
│   │   │   │   ├── StoryBeat.cs
│   │   │   │   ├── Choice.cs
│   │   │   │   └── Consequence.cs
│   │   │   ├── Player/
│   │   │   │   ├── Player.cs
│   │   │   │   ├── PlayerSave.cs
│   │   │   │   ├── PlayerChoice.cs
│   │   │   │   ├── PlayerShiftProgress.cs
│   │   │   │   └── ConsequenceQueue.cs
│   │   │   ├── Code/
│   │   │   │   ├── PracticeTask.cs
│   │   │   │   ├── TestCase.cs
│   │   │   │   └── PracticeAttempt.cs
│   │   │   ├── SideTask/
│   │   │   │   ├── SideTaskTemplate.cs
│   │   │   │   ├── PlayerSideTask.cs
│   │   │   │   └── SideTaskSubmission.cs
│   │   │   ├── Economy/
│   │   │   │   ├── PlayerEconomy.cs
│   │   │   │   ├── Transaction.cs
│   │   │   │   ├── ShopItem.cs
│   │   │   │   ├── PlayerInventory.cs
│   │   │   │   └── SahmSubscription.cs
│   │   │   ├── Assessment/
│   │   │   │   ├── AssessmentEvent.cs
│   │   │   │   └── ConceptMasterySnapshot.cs
│   │   │   └── Audit/
│   │   │       ├── AiGenerationLog.cs
│   │   │       └── AuditLog.cs
│   │   ├── ValueObjects/
│   │   │   ├── DesktopState.cs            ← JSON value objects
│   │   │   ├── StoryBeatContent.cs
│   │   │   ├── DesktopEvent.cs
│   │   │   ├── ShiftUnlockCondition.cs
│   │   │   └── TestCaseResult.cs
│   │   ├── Enums/
│   │   │   ├── ChoiceTier.cs              ← Ideal / Acceptable / Debt / Mistake
│   │   │   ├── BeatType.cs                ← narrative / consequence
│   │   │   ├── BeatApp.cs                 ← WhatsUpp / MailLoop / LoopCode / ...
│   │   │   ├── PlayerRank.cs              ← Intern / Fresh / ...
│   │   │   ├── SideTaskStatus.cs
│   │   │   ├── ShiftProgressStatus.cs
│   │   │   └── TransactionType.cs
│   │   └── Constants/
│   │       ├── SalaryTiers.cs             ← salary per rank
│   │       ├── HintLimits.cs              ← per Sahm tier
│   │       └── EgpPenalties.cs            ← abandonment penalty etc.
│   │
│   ├── SHIFT.Application/                     ← Project 2
│   │   ├── Interfaces/
│   │   │   ├── IAuthService.cs
│   │   │   ├── INarrativeService.cs
│   │   │   ├── IChoiceService.cs
│   │   │   ├── IPracticeService.cs
│   │   │   ├── ICodeExecutionService.cs
│   │   │   ├── ISideTaskService.cs
│   │   │   ├── IAiOrchestrationService.cs
│   │   │   ├── IEconomyService.cs
│   │   │   ├── IShopService.cs
│   │   │   ├── ISahmService.cs
│   │   │   ├── IAssessmentService.cs
│   │   │   ├── IAdminService.cs
│   │   │   └── ISaveService.cs
│   │   ├── Services/                          ← Implementations هنا في Application
│   │   │   ├── AuthService.cs
│   │   │   ├── NarrativeService.cs
│   │   │   ├── ChoiceService.cs
│   │   │   ├── PracticeService.cs
│   │   │   ├── SideTaskService.cs
│   │   │   ├── EconomyService.cs
│   │   │   ├── ShopService.cs
│   │   │   ├── SahmService.cs
│   │   │   ├── AssessmentService.cs
│   │   │   ├── AdminService.cs
│   │   │   └── SaveService.cs
│   │   ├── DTOs/
│   │   │   ├── Auth/
│   │   │   │   ├── RegisterRequestDto.cs
│   │   │   │   ├── LoginRequestDto.cs
│   │   │   │   └── AuthResponseDto.cs
│   │   │   ├── Game/
│   │   │   │   ├── ShiftStartResponseDto.cs
│   │   │   │   ├── StoryBeatDto.cs
│   │   │   │   └── ChoiceSubmitRequestDto.cs
│   │   │   ├── Code/
│   │   │   │   ├── PracticeTaskDto.cs
│   │   │   │   ├── CodeSubmitRequestDto.cs
│   │   │   │   └── CodeSubmitResponseDto.cs
│   │   │   ├── Economy/
│   │   │   │   ├── ShopItemDto.cs
│   │   │   │   └── TransactionDto.cs
│   │   │   ├── SideTask/
│   │   │   │   ├── SideTaskDto.cs
│   │   │   │   └── SideTaskSubmitRequestDto.cs
│   │   │   └── Admin/
│   │   │       ├── DashboardDto.cs
│   │   │       └── ContentManagementDto.cs
│   │   ├── BackgroundJobs/
│   │   │   ├── DailyHintResetJob.cs       ← IHostedService
│   │   │   ├── MasteryComputeJob.cs       ← IHostedService (triggered)
│   │   │   └── AiLogCleanupJob.cs         ← IHostedService (scheduled)
│   │   └── Pipelines/
│   │       └── AssessmentEventChannel.cs  ← System.Threading.Channels
│   │
│   ├── SHIFT.Infrastructure/                  ← Project 3
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   └── Configurations/            ← IEntityTypeConfiguration<T>
│   │   │       ├── ShiftConfiguration.cs
│   │   │       ├── StoryBeatConfiguration.cs
│   │   │       ├── ChoiceConfiguration.cs
│   │   │       ├── ConsequenceConfiguration.cs
│   │   │       ├── PlayerConfiguration.cs
│   │   │       ├── PlayerSaveConfiguration.cs
│   │   │       └── ...
│   │   ├── ExternalClients/
│   │   │   ├── GeminiAiClient.cs          ← HttpClient → Gemini API
│   │   │   └── DockerCodeRunner.cs        ← HttpClient → Docker runner
│   │   └── DependencyInjection.cs         ← AddInfrastructure() extension
│   │
│   └── SHIFT.API/                             ← Project 4
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── GameController.cs
│       │   ├── CodeController.cs
│       │   ├── SideTaskController.cs
│       │   ├── EconomyController.cs
│       │   ├── ShopController.cs
│       │   ├── SahmController.cs
│       │   ├── AssessmentController.cs
│       │   └── AdminController.cs
│       ├── Middleware/
│       │   ├── ExceptionHandlingMiddleware.cs
│       │   └── AuditLoggingMiddleware.cs
│       ├── Extensions/
│       │   └── ClaimsPrincipalExtensions.cs  ← GetPlayerId() helper
│       └── Program.cs
│
└── tests/
    ├── SHIFT.Domain.Tests/
    ├── SHIFT.Application.Tests/
    └── SHIFT.Infrastructure.Tests/
```

---

## 3. Layer Responsibilities

### 3.1 Domain Layer

`SHIFT.Domain` هو قلب الـ solution. بيحتوي على:

- **Entities:** كل الـ 24 table موجودة كـ C# class هنا. مفيش أي reference لـ EF Core أو ASP.NET هنا.
- **Value Objects:** الـ JSON columns المتمثلة كـ strongly-typed records (`DesktopState`, `StoryBeatContent`, إلخ).
- **Enums:** بتحل محل الـ magic strings اللي في الـ CHECK constraints — مثلاً `ChoiceTier.Ideal` بدل `"Ideal"`.
- **Constants:** أرقام زي `SalaryTiers.Intern = 2000` بدل ما تكون هاردكودد في كل مكان.

> **قاعدة صارمة:** مفيش `using Microsoft.EntityFrameworkCore;` في أي ملف داخل `SHIFT.Domain`.

---

### 3.2 Application Layer

`SHIFT.Application` بيحتوي على الـ business logic الحقيقي:

- **Interfaces:** كل service بيعرف نفسه بـ interface هنا (مثلاً `IEconomyService`). الـ Infrastructure بيـimplementها.
- **Services:** الـ concrete implementations اللي بتستخدم الـ `DbContext` مباشرةً عبر الـ interfaces.
- **DTOs:** الـ request/response objects اللي بتتنقل بين الـ Controllers والـ Services.
- **Background Jobs:** الـ `IHostedService` implementations.
- **AssessmentEventChannel:** الـ non-blocking pipeline للـ assessment events.

> **ملاحظة عملية:** في Clean Architecture الكلاسيكية، الـ Application بيتكلم مع الـ Infrastructure عبر interfaces بس. هنا بسبب EF Core، الـ `DbContext` نفسه ممكن يتـinject مباشرةً في الـ Services — ده مقبول وبيوفر complexity زيادة.

---

### 3.3 Infrastructure Layer

`SHIFT.Infrastructure` بيعمل implement لكل حاجة بتتكلم مع العالم الخارجي:

- **`ApplicationDbContext`:** الـ EF Core DbContext مع كل الـ entity configurations.
- **`Configurations/`:** كل entity ليها class منفصل بيعمل `IEntityTypeConfiguration<T>` — ده بيخلي الـ `OnModelCreating` نظيف.
- **`GeminiAiClient`:** بيبعت HTTP requests لـ Google Gemini API.
- **`DockerCodeRunner`:** بيبعت الكود للـ sandboxed Docker container وبيرجع النتائج.
- **`DependencyInjection.cs`:** extension method `AddInfrastructure(services, config)` بتـregister الـ DbContext والـ Clients.

---

### 3.4 API Layer (Presentation)

`SHIFT.API` مسؤول عن:

- **Controllers:** بتاخد الـ HTTP request، بتعمل validate على الـ DTO، بتدي الشغل للـ Service، وبترجع الـ response.
- **Middleware:** Exception handling و audit logging.
- **`Program.cs`:** كل الـ service registrations والـ middleware pipeline.

---

## 4. NuGet Packages & Technology Decisions

### SHIFT.Domain
```
لا يحتاج أي NuGet packages خارجية
```

### SHIFT.Application
| Package | Version | السبب |
|---|---|---|
| `MediatR` | 12.x | Optional — لو هتستخدم CQRS pattern لاحقاً |
| `FluentValidation` | 11.x | Validation على الـ DTOs |
| `Microsoft.Extensions.Logging.Abstractions` | 8.x | Logging interfaces |
| `Microsoft.Extensions.Options` | 8.x | Options pattern للـ configs |

### SHIFT.Infrastructure
| Package | Version | السبب |
|---|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | 8.x | SQL Server provider |
| `Microsoft.EntityFrameworkCore.Tools` | 8.x | Migrations |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 8.x | ASP.NET Identity integration |
| `Microsoft.Extensions.Http` | 8.x | `IHttpClientFactory` للـ Gemini و Docker |

### SHIFT.API
| Package | Version | السبب |
|---|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.x | JWT validation |
| `System.IdentityModel.Tokens.Jwt` | 7.x | JWT generation |
| `Swashbuckle.AspNetCore` | 6.x | Swagger / OpenAPI |
| `Microsoft.AspNetCore.OpenApi` | 8.x | Minimal API OpenAPI support |

### ليه مفيش SignalR؟
الـ narrative beats بتترجع كلها في response واحدة لـ `POST /api/game/shift/{id}/start`. الـ frontend عنده الـ queue كاملة ويعمل sequencing محلياً باستخدام `delay_seconds`. مفيش push حقيقي محتاج من السيرفر.

### ليه مفيش Hangfire؟
الـ background jobs في SHIFT بسيطة ومش محتاجة persistence أو retry dashboard. الـ `BackgroundService` الـ built-in في .NET كافي. لو في المستقبل احتجت retry على الـ MasteryComputeJob، ساعتها تضيف Hangfire.

### ليه مفيش Unit of Work؟
الـ `DbContext` في EF Core هو نفسه الـ Unit of Work — بيعمل track للـ changes وبيـcommit كلها في `SaveChangesAsync()` واحدة. إضافة UoW layer فوقيه زيادة complexity من غير فايدة، خصوصاً إنك شغال على database واحدة.

---

## 5. Services Catalog

---

### 5.1 AuthService

**المسؤولية:** Registration، Login، JWT generation، Refresh token rotation، Logout.

**يعتمد على:** `ApplicationDbContext`، `UserManager<ApplicationUser>`، `IConfiguration` (JWT settings)

**Interface الرئيسية:**
```csharp
public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(string refreshToken, int userId);
}
```

**أهم منطق بيعمله:**
- SHA-256 hashing للـ `student_id` قبل الـ INSERT
- SHA-256 hashing للـ refresh token قبل التخزين في `RefreshToken` table
- Token rotation: يعمل revoke للقديم ويحط جديد في نفس الـ call
- Lockout check على `lockout_end` و increment لـ `access_failed_count` عند فشل الـ password

---

### 5.2 NarrativeService

**المسؤولية:** تحميل الـ shift beats، inject الـ consequences، التحقق من unlock conditions.

**يعتمد على:** `ApplicationDbContext`

**Interface الرئيسية:**
```csharp
public interface INarrativeService
{
    Task<ShiftStartResponseDto> StartOrResumeShiftAsync(int playerId, int shiftId);
    Task<bool> CheckUnlockConditionAsync(int playerId, int shiftId);
    Task<List<StoryBeatDto>> GetPendingConsequenceBeatsAsync(int playerId, int shiftId);
}
```

**أهم منطق بيعمله:**
- يقرأ `Shift.unlock_condition` JSON ويعمله deserialize لـ `ShiftUnlockCondition`
- يعمل query على `ConsequenceQueue` للـ pending consequences اللي `StoryBeat.shift_id = @shift_id`
- يعمل UPDATE على `ConsequenceQueue` → `status = 'fired'` بعد الـ injection
- يرتب الـ consequence beats حسب `inject_position` (start → prepend, end → append)
- يعمل MERGE على `PlayerShiftProgress`

---

### 5.3 ChoiceService

**المسؤولية:** تسجيل اختيار اللاعب، تطبيق الـ EGP delta، قيد الـ Consequence لو موجود.

**يعتمد على:** `ApplicationDbContext`، `IEconomyService`، `IAssessmentService`

**Interface الرئيسية:**
```csharp
public interface IChoiceService
{
    Task<ChoiceResultDto> SubmitChoiceAsync(int playerId, ChoiceSubmitRequestDto dto);
}
```

**أهم منطق بيعمله:**
- INSERT `PlayerChoice` (immutable record)
- يستدعي `IEconomyService.ApplyEgpDeltaAsync()` لتطبيق الـ balance change
- لو `Choice.consequence_id IS NOT NULL` → INSERT `ConsequenceQueue` بـ `status = 'pending'`
- يبعت `AssessmentEvent` عبر الـ `AssessmentEventChannel` (non-blocking)

---

### 5.4 PracticeService

**المسؤولية:** عرض practice tasks، استقبال الكود المبعوت، تقييم النتيجة، تحديث gate status.

**يعتمد على:** `ApplicationDbContext`، `ICodeExecutionService`، `IEconomyService`، `IAssessmentService`

**Interface الرئيسية:**
```csharp
public interface IPracticeService
{
    Task<PracticeTaskDto> GetTaskAsync(int playerId, int taskId);
    Task<CodeSubmitResponseDto> SubmitCodeAsync(int playerId, CodeSubmitRequestDto dto);
}
```

**أهم منطق بيعمله:**
- يجيب الـ `TestCase[]` كلها (visible + hidden) للتقييم
- بيبعتهم لـ `ICodeExecutionService` وبياخد `TestCaseResult[]`
- Tier computation بناءً على pass rate
- لو Ideal/Acceptable → UPDATE `PlayerShiftProgress.status = 'completed'` + EGP reward
- لو فشل → increment `gate_attempts`، لو وصل `max_attempts` → gate locked
- يعمل emit لـ `practice_attempt` و `gate_cleared` events

---

### 5.5 CodeExecutionService

**المسؤولية:** إرسال الكود للـ Docker sandboxed runner وإرجاع النتائج.

**يعتمد على:** `IHttpClientFactory` (named client: `"CodeRunner"`)

**Interface الرئيسية:**
```csharp
public interface ICodeExecutionService
{
    Task<List<TestCaseResult>> ExecuteAsync(string code, List<TestCase> testCases);
}
```

**أهم منطق بيعمله:**
- بيبعت HTTP POST لـ Docker runner endpoint مع الكود والـ test cases
- Timeout مضبوط على **5 ثواني** — لو فات → يرجع نتيجة `Mistake` tier تلقائياً
- بيعمل deserialize للـ response لـ `List<TestCaseResult>`
- هو الوحيد اللي يتكلم مع الـ external code runner — كل التانيين بيستخدمونه عبر الـ interface

---

### 5.6 SideTaskService

**المسؤولية:** إدارة AI side tasks: التحقق، العرض، الـ submission، والـ abandonment.

**يعتمد على:** `ApplicationDbContext`، `ICodeExecutionService`، `IEconomyService`، `IAssessmentService`

**Interface الرئيسية:**
```csharp
public interface ISideTaskService
{
    Task<SideTaskDto?> GetActiveTaskAsync(int playerId);
    Task<CodeSubmitResponseDto> SubmitSideTaskAsync(int playerId, SideTaskSubmitRequestDto dto);
    Task<AbandonResultDto> AbandonTaskAsync(int playerId, int sideTaskId);
    Task AssignNewTaskAsync(int playerId);          // يُستدعى داخلياً بعد gate clear
}
```

**أهم منطق بيعمله:**
- التحقق من وجود active task قبل إنشاء واحد جديد
- EGP earned calculation حسب tier: Ideal=100%, Acceptable=75%, Debt=25%, Mistake=0%
- Abandonment penalty: 100 EGP (مع `MAX(0, balance - 100)` للـ clamping)
- يستدعي `IAiOrchestrationService.GenerateTaskSlotsAsync()` لإنشاء task جديد

---

### 5.7 AiOrchestrationService

**المسؤولية:** التواصل مع Gemini API لتوليد side task slots وreframing الـ academic tasks.

**يعتمد على:** `IHttpClientFactory` (named client: `"GeminiAI"`), `ApplicationDbContext`

**Interface الرئيسية:**
```csharp
public interface IAiOrchestrationService
{
    Task<Dictionary<string, object>> GenerateTaskSlotsAsync(
        SideTaskTemplate template, 
        PlayerAiContext context);
    
    Task<ReframedTaskDto> ReframeAcademicTaskAsync(
        string rawTitle, 
        string rawDescription, 
        string conceptTag);
}
```

**أهم منطق بيعمله:**
- بيبني الـ prompt من الـ `slots_schema` JSON + الـ Egyptian cultural context
- Retry loop: يجرب 3 مرات قبل الـ fallback لـ default slot values
- Validation على الـ response: JSON parseable + all slots present + types match + values in range
- INSERT `AiGenerationLog` بعد كل call (سواء نجح أو فشل)
- `expires_at = NOW() + 2 years` على كل log row

---

### 5.8 EconomyService

**المسؤولية:** كل العمليات المالية: EGP delta، salary، bonus، penalty.

**يعتمد على:** `ApplicationDbContext`

**Interface الرئيسية:**
```csharp
public interface IEconomyService
{
    Task<decimal> ApplyEgpDeltaAsync(int playerId, decimal delta, 
                                      TransactionType type, string description);
    Task<decimal> PayShiftSalaryAsync(int playerId, int shiftId);
    Task<BalanceDto> GetBalanceAsync(int playerId);
    Task<List<TransactionDto>> GetTransactionHistoryAsync(int playerId, int page);
}
```

**أهم منطق بيعمله:**
- كل عملية بتحصل داخل `BeginTransactionAsync()` مع `UPDLOCK` على `PlayerEconomy`
- `balance = MAX(0, balance + delta)` — مينفعش يبقى سالب (CHECK constraint في الـ DB)
- INSERT `Transaction` record بعد كل تغيير في الـ balance
- Salary computation: base salary حسب `salary_tier` + performance bonus من tier distribution في `PlayerChoice` للـ shift ده

---

### 5.9 ShopService

**المسؤولية:** عرض الكتالوج، شراء الأيتمز، التحقق من الـ rank و balance.

**يعتمد على:** `ApplicationDbContext`، `IEconomyService`

**Interface الرئيسية:**
```csharp
public interface IShopService
{
    Task<List<ShopItemDto>> GetCatalogAsync(int playerId);
    Task<PurchaseResultDto> PurchaseItemAsync(int playerId, int itemId);
}
```

**أهم منطق بيعمله:**
- LEFT JOIN مع `PlayerInventory` لإرجاع `is_owned` flag مع كل item
- Guard checks بالترتيب: item available → rank met → balance sufficient → not already owned
- الشراء بيحصل في DB transaction واحدة تشمل: UPDATE balance، INSERT Transaction، INSERT PlayerInventory
- Sahm tier upgrade: INSERT `SahmSubscription` جديد مع الـ `daily_hint_limit` الجديد

---

### 5.10 SahmService

**المسؤولية:** توليد hints، التحقق من الـ daily limit، إدارة الـ subscription.

**يعتمد على:** `ApplicationDbContext`، `IAiOrchestrationService`

**Interface الرئيسية:**
```csharp
public interface ISahmService
{
    Task<HintResponseDto> RequestHintAsync(int playerId, HintRequestDto dto);
    Task<SahmStatusDto> GetStatusAsync(int playerId);
}
```

**أهم منطق بيعمله:**
- Lazy reset: لو `last_hint_reset < TODAY` → يعمل reset للـ `hints_used_today = 0` أول ما يجي request
- `hints_used_today >= daily_hint_limit` → 429 Too Many Requests
- الـ hint level بيتحدد حسب الـ tier: Free=3 bullets فقط / Pro=detailed+snippet / Team&Enterprise=unlimited detail
- Emit `hint_request` assessment event

---

### 5.11 AssessmentService

**المسؤولية:** استقبال events وحساب mastery scores.

**يعتمد على:** `ApplicationDbContext`، `AssessmentEventChannel`

**Interface الرئيسية:**
```csharp
public interface IAssessmentService
{
    void EmitEvent(AssessmentEventDto eventDto);         // non-blocking
    Task ComputeMasterySnapshotAsync(int playerId, int shiftId);
    Task<List<ConceptMasteryDto>> GetPlayerMasteryAsync(int playerId);
    Task<DashboardDto> GetClassDashboardAsync(int classCodeId);
}
```

**أهم منطق بيعمله:**
- `EmitEvent()` مش async — بس بيكتب على الـ `Channel<AssessmentEventDto>` وبيرجع فوراً (non-blocking)
- Background worker بيقرأ من الـ Channel وبيعمل batch INSERT لـ `AssessmentEvent` (كل 50 event أو كل 2 ثانية)
- Mastery computation: weighted sum بناءً على event type + recency decay → sigmoid → [0, 1]
- Evidence weights: `gate_cleared=3.0`, `practice_attempt(Ideal)=2.5`, `hint_request=-0.3` إلخ

---

### 5.12 AdminService

**المسؤولية:** إدارة المحتوى (shifts/beats/choices)، إدارة المستخدمين، export البيانات.

**يعتمد على:** `ApplicationDbContext`، `IAiOrchestrationService`

**Interface الرئيسية:**
```csharp
public interface IAdminService
{
    Task<List<ShiftDto>> GetAllShiftsAsync();
    Task<int> CreateStoryBeatAsync(CreateStoryBeatDto dto);
    Task<int> CreateChoiceAsync(CreateChoiceDto dto);
    Task<int> CreateConsequenceAsync(CreateConsequenceDto dto);
    Task SoftDeleteUserAsync(int targetUserId, int adminUserId);
    Task<byte[]> ExportClassReportAsync(int classCodeId);
    Task<ReframedTaskDto> ReframeTaskAsync(ReframeTaskRequestDto dto);
}
```

**أهم منطق بيعمله:**
- `SoftDeleteUserAsync()`: UPDATE `ApplicationUser.is_active = 0` + `deleted_at` + UPDATE `Player.deleted_at` + REVOKE كل `RefreshToken` للـ user ده
- Global query filters على `ApplicationUser` و `Player` بتحجب الـ soft-deleted records تلقائياً
- INSERT `AuditLog` بعد كل action admin

---

### 5.13 SaveService

**المسؤولية:** حفظ وتحميل الـ LoopOS desktop state.

**يعتمد على:** `ApplicationDbContext`

**Interface الرئيسية:**
```csharp
public interface ISaveService
{
    Task<SaveResultDto> SaveDesktopStateAsync(int playerId, SaveRequestDto dto);
    Task<DesktopState?> LoadDesktopStateAsync(int playerId, int slot);
}
```

**أهم منطق بيعمله:**
- MERGE (UPSERT) على `PlayerSave` باستخدام `(player_id, slot_number)` كـ composite key
- يعمل serialize لـ `DesktopState` record لـ JSON قبل الحفظ
- 3 slots فقط للاعب (slot_number IN 1, 2, 3)

---

## 6. Background Jobs (IHostedService)

### DailyHintResetJob

```csharp
// SHIFT.Application/BackgroundJobs/DailyHintResetJob.cs
public class DailyHintResetJob : BackgroundService
{
    // يحسب الوقت المتبقي لمنتصف الليل UTC
    // بعد كل reset ينام لـ 24 ساعة
    // UPDATE SahmSubscription SET hints_used_today = 0
    //   WHERE last_hint_reset < CAST(SYSUTCDATETIME() AS DATE)
}
```

**التوقيت:** يشتغل مرة واحدة يومياً عند **00:00:00 UTC**.
**ملاحظة:** الـ SahmService فيه lazy reset كـ double-safety — لو السيرفر وقع وراح الـ job.

---

### MasteryComputeJob

```csharp
// SHIFT.Application/BackgroundJobs/MasteryComputeJob.cs
public class MasteryComputeJob : BackgroundService
{
    // يشتغل كـ triggered job — مش scheduled
    // بيستنى على Channel<MasteryComputeRequest>
    // لما يجيه player_id + shift_id → يعمل compute وINSERT snapshot
}
```

**التشغيل:** Triggered (مش scheduled) — يتبعتله signal من `AssessmentService` بعد كل `shift_completed` event.
**السبب:** الـ mastery computation ممكن تاخد وقت — محتاجة تتعمل في background ومش تبطئ الـ HTTP response.

---

### AiLogCleanupJob

```csharp
// SHIFT.Application/BackgroundJobs/AiLogCleanupJob.cs
public class AiLogCleanupJob : BackgroundService
{
    // يشتغل مرة في الأسبوع
    // DELETE FROM AiGenerationLog WHERE expires_at < SYSUTCDATETIME()
    // DELETE FROM AuditLog WHERE occurred_at < DATEADD(YEAR, -2, SYSUTCDATETIME())
}
```

**التوقيت:** أسبوعياً (مثلاً كل أحد الساعة 2 صبح UTC).

---

### AssessmentEventWorker (Channel Consumer)

```csharp
// SHIFT.Application/Pipelines/AssessmentEventChannel.cs
// هذا مش IHostedService مستقل — ده BackgroundService بيقرأ من Channel

public class AssessmentEventWorker : BackgroundService
{
    // بيقرأ من Channel<AssessmentEventDto>
    // بيعمل batch: كل 50 event أو كل 2 ثانية
    // INSERT AssessmentEvent (batch insert بـ AddRange)
}
```

**السبب:** الـ Assessment events بتتبعت من كل action في اللعبة. لو عملناها sync في كل HTTP request، هتبطئ كل response. الـ Channel بيخلي الـ HTTP response يرجع فوراً والـ INSERT يحصل في background.

---

## 7. API Endpoints Reference

### 7.1 Auth Endpoints

| Method | Route | Controller Action | Service Call | Auth |
|---|---|---|---|---|
| POST | `/api/auth/register` | `Register` | `IAuthService.RegisterAsync()` | Anonymous |
| POST | `/api/auth/login` | `Login` | `IAuthService.LoginAsync()` | Anonymous |
| POST | `/api/auth/refresh` | `Refresh` | `IAuthService.RefreshTokenAsync()` | Anonymous |
| POST | `/api/auth/logout` | `Logout` | `IAuthService.LogoutAsync()` | `[Authorize]` |

**Request/Response Examples:**

```
POST /api/auth/register
Body: { "email", "display_name", "student_id", "password", "class_code" }
201 : { "access_token", "refresh_token", "player_id" }
400 : { "error": "Invalid or expired class code" }
409 : { "error": "Email already in use" }

POST /api/auth/login
Body: { "email", "password" }
200 : { "access_token", "refresh_token", "role", "player_id" }
401 : { "error": "Invalid credentials" }
423 : { "error": "Account locked. Try again after {lockout_end}" }

POST /api/auth/refresh
Body: { "refresh_token" }
200 : { "access_token", "refresh_token" }
401 : { "error": "Refresh token invalid or expired" }
```

---

### 7.2 Game / Narrative Endpoints

| Method | Route | Controller Action | Service Call | Auth |
|---|---|---|---|---|
| GET | `/api/progress/state` | `GetPlayerState` | `INarrativeService` + `ISaveService` | Player |
| POST | `/api/game/shift/{shiftId}/start` | `StartShift` | `INarrativeService.StartOrResumeShiftAsync()` | Player |
| GET | `/api/game/shift/{shiftId}/progress` | `GetShiftProgress` | `INarrativeService` | Player |
| POST | `/api/progress/choice` | `SubmitChoice` | `IChoiceService.SubmitChoiceAsync()` | Player |
| PUT | `/api/progress/save` | `SaveState` | `ISaveService.SaveDesktopStateAsync()` | Player |
| POST | `/api/progress/reset` | `ResetProgress` | `IAdminService` (player-triggered) | Player |

**Request/Response Examples:**

```
POST /api/game/shift/{shiftId}/start
200 : { "beats": [StoryBeatDto], "consequence_beats": [StoryBeatDto], "shift_meta": {...} }
403 : { "error": "Shift locked", "requires": { "min_rank", "prerequisite_shift_id" } }

POST /api/progress/choice
Body: { "beat_id", "choice_id", "session_id" }
200 : { "tier", "immediate_feedback", "new_balance", "consequence_queued": bool }

PUT /api/progress/save
Body: { "slot_number": 1|2|3, "save_label": "...", "desktop_state": {...} }
200 : { "saved_at" }
```

---

### 7.3 Code / Practice Endpoints

| Method | Route | Controller Action | Service Call | Auth |
|---|---|---|---|---|
| GET | `/api/code/task/{taskId}` | `GetTask` | `IPracticeService.GetTaskAsync()` | Player |
| POST | `/api/progress/practice` | `SubmitCode` | `IPracticeService.SubmitCodeAsync()` | Player |

**Request/Response Examples:**

```
GET /api/code/task/{taskId}
200 : { "task": PracticeTaskDto, "visible_test_cases": [], "gate_attempts", "max_attempts" }

POST /api/progress/practice
Body: { "task_id", "submitted_code", "time_spent_sec", "hint_used": bool }
200 : {
  "tier": "Ideal|Acceptable|Debt|Mistake",
  "test_results": [{ "test_case_id", "passed", "actual_output", "execution_time_ms" }],
  "gate_cleared": bool,
  "egp_earned": decimal,
  "new_balance": decimal,
  "struggle_detected": bool,
  "max_attempts_reached": bool
}
```

---

### 7.4 Side Task Endpoints

| Method | Route | Controller Action | Service Call | Auth |
|---|---|---|---|---|
| GET | `/api/sahm/task` | `GetActiveTask` | `ISideTaskService.GetActiveTaskAsync()` | Player |
| POST | `/api/sahm/task/submit` | `SubmitTask` | `ISideTaskService.SubmitSideTaskAsync()` | Player |
| POST | `/api/sahm/task/abandon` | `AbandonTask` | `ISideTaskService.AbandonTaskAsync()` | Player |

**Request/Response Examples:**

```
GET /api/sahm/task
200 : { "side_task_id", "title", "description", "egp_reward", "deadline_at", "status" }
404 : { "message": "No active side task" }

POST /api/sahm/task/submit
Body: { "side_task_id", "submitted_code", "time_spent_sec", "sahm_hints_used" }
200 : { "tier", "test_results", "egp_earned", "new_balance" }
410 : { "error": "Task deadline has passed" }
409 : { "error": "Task already submitted or abandoned" }

POST /api/sahm/task/abandon
Body: { "side_task_id" }
200 : { "penalty_applied": -100, "new_balance", "cooldown_minutes": 10 }
```

---

### 7.5 Economy & Shop Endpoints

| Method | Route | Controller Action | Service Call | Auth |
|---|---|---|---|---|
| GET | `/api/economy/balance` | `GetBalance` | `IEconomyService.GetBalanceAsync()` | Player |
| GET | `/api/economy/transactions` | `GetTransactions` | `IEconomyService.GetTransactionHistoryAsync()` | Player |
| GET | `/api/shop/items` | `GetCatalog` | `IShopService.GetCatalogAsync()` | Player |
| POST | `/api/shop/purchase` | `Purchase` | `IShopService.PurchaseItemAsync()` | Player |
| GET | `/api/shop/inventory` | `GetInventory` | `IShopService` | Player |
| POST | `/api/sahm/upgrade` | `UpgradeSahm` | `IShopService.PurchaseItemAsync()` (Sahm item) | Player |

**Request/Response Examples:**

```
GET /api/shop/items
200 : { "items": [ShopItemDto + is_owned flag], "current_balance" }

POST /api/shop/purchase
Body: { "item_id" }
201 : { "item", "new_balance", "inventory_id" }
402 : { "error": "Insufficient balance", "balance", "price" }
403 : { "error": "Requires rank: Senior" }
409 : { "error": "Already owned" }

POST /api/sahm/upgrade
Body: { "target_tier": "Pro|Team|Enterprise" }
201 : { "new_tier", "daily_hint_limit", "new_balance" }
400 : { "error": "Sahm upgrade is one-way. Cannot downgrade." }
```

---

### 7.6 Sahm AI Assistant Endpoints

| Method | Route | Controller Action | Service Call | Auth |
|---|---|---|---|---|
| POST | `/api/sahm/hint` | `RequestHint` | `ISahmService.RequestHintAsync()` | Player |
| GET | `/api/sahm/status` | `GetStatus` | `ISahmService.GetStatusAsync()` | Player |

**Request/Response Examples:**

```
POST /api/sahm/hint
Body: { "task_id", "task_type": "practice|side_task", "error_message", "current_code" }
200 : { "hint_text", "tier", "hints_remaining" }
429 : { "error": "Daily hint limit reached", "limit", "tier", "resets_at": "midnight UTC" }

GET /api/sahm/status
200 : { "tier", "hints_used_today", "daily_hint_limit", "hints_remaining", "last_hint_reset" }
```

---

### 7.7 Assessment Endpoints

| Method | Route | Controller Action | Service Call | Auth |
|---|---|---|---|---|
| GET | `/api/assessment/mastery` | `GetMyMastery` | `IAssessmentService.GetPlayerMasteryAsync()` | Player |

*(Instructor/Admin assessment endpoints موجودة في Admin section)*

---

### 7.8 Admin Endpoints

| Method | Route | Controller Action | Service Call | Auth |
|---|---|---|---|---|
| GET | `/api/admin/dashboard` | `GetDashboard` | `IAssessmentService.GetClassDashboardAsync()` | Admin/Instructor |
| GET | `/api/admin/reports/at-risk` | `GetAtRisk` | `IAssessmentService` | Admin/Instructor |
| GET | `/api/admin/reports/performance/export` | `ExportReport` | `IAdminService.ExportClassReportAsync()` | Admin/Instructor |
| GET | `/api/admin/users` | `GetUsers` | `IAdminService` | Admin/SuperAdmin |
| DELETE | `/api/admin/users/{userId}` | `SoftDeleteUser` | `IAdminService.SoftDeleteUserAsync()` | SuperAdmin |
| POST | `/api/admin/users/{userId}/role` | `AssignRole` | `IAdminService` | SuperAdmin |
| GET | `/api/super/content/shifts` | `GetShifts` | `IAdminService.GetAllShiftsAsync()` | SuperAdmin |
| POST | `/api/super/content/beats` | `CreateBeat` | `IAdminService.CreateStoryBeatAsync()` | SuperAdmin |
| POST | `/api/super/content/choices` | `CreateChoice` | `IAdminService.CreateChoiceAsync()` | SuperAdmin |
| POST | `/api/super/content/consequences` | `CreateConsequence` | `IAdminService.CreateConsequenceAsync()` | SuperAdmin |
| GET | `/api/admin/sheets` | `GetTaskBank` | `IAdminService` | Instructor |
| POST | `/api/admin/sheets/reframe` | `ReframeTask` | `IAiOrchestrationService.ReframeAcademicTaskAsync()` | Instructor |
| POST | `/api/admin/sheets/publish` | `PublishSheet` | `IAdminService` | Instructor |
| GET | `/api/admin/audit-log` | `GetAuditLog` | `IAdminService` | SuperAdmin |
| POST | `/api/admin/maintenance/cleanup` | `TriggerCleanup` | (triggers `AiLogCleanupJob`) | SuperAdmin |
| GET | `/api/admin/class-codes` | `GetClassCodes` | `IAdminService` | Admin/SuperAdmin |
| POST | `/api/admin/class-codes` | `CreateClassCode` | `IAdminService` | Admin/SuperAdmin |
| PUT | `/api/admin/class-codes/{id}/deactivate` | `DeactivateClassCode` | `IAdminService` | Admin/SuperAdmin |

---

## 8. Cross-Cutting Concerns

### 8.1 Authentication & Authorization

**JWT Configuration:**
```csharp
// في Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new() {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero   // مهم — عشان الـ 15 دقيقة تبقى صارمة
        };
    });
```

**Authorization Policies:**
```csharp
builder.Services.AddAuthorization(options => {
    options.AddPolicy("PlayerOnly",    p => p.RequireRole("player"));
    options.AddPolicy("AdminOnly",     p => p.RequireRole("admin", "super_admin"));
    options.AddPolicy("SuperAdminOnly",p => p.RequireRole("super_admin"));
    options.AddPolicy("Instructor",    p => p.RequireRole("instructor", "admin", "super_admin"));
});
```

**Player ID Extraction Helper:**
```csharp
// SHIFT.API/Extensions/ClaimsPrincipalExtensions.cs
public static class ClaimsPrincipalExtensions
{
    public static int GetPlayerId(this ClaimsPrincipal user)
        => int.Parse(user.FindFirstValue("player_id")!);
}

// استخدام في Controller:
var playerId = User.GetPlayerId();
```

---

### 8.2 Exception Handling Middleware

```csharp
// SHIFT.API/Middleware/ExceptionHandlingMiddleware.cs
// بيمسك كل الـ exceptions ويحولها لـ ProblemDetails response موحدة

// 400 → ValidationException (FluentValidation)
// 401 → UnauthorizedException
// 403 → ForbiddenException
// 404 → NotFoundException
// 402 → InsufficientBalanceException
// 409 → ConflictException (already owned / already submitted)
// 410 → GoneException (task expired)
// 429 → RateLimitException (hint limit)
// 500 → Generic server error (بيحجب الـ stack trace في Production)
```

بدل ما كل Service يرجع `null` أو error code، بترمي exception مناسبة والـ middleware بتمسكها وترجع الـ HTTP status الصح.

---

### 8.3 Assessment Event Pipeline (Channel)

```
HTTP Request
    ↓
Service (e.g. ChoiceService)
    ↓ بيكتب على Channel (non-blocking - بيرجع فوراً)
Channel<AssessmentEventDto>
    ↓ (background worker بيقرأ)
AssessmentEventWorker
    ↓ batch لـ 50 events أو كل 2 ثانية
INSERT INTO AssessmentEvent (bulk)
    ↓ (لو shift_completed)
MasteryComputeJob Channel ← signal
    ↓
INSERT INTO ConceptMasterySnapshot
```

**Registration:**
```csharp
// في Program.cs
builder.Services.AddSingleton(Channel.CreateUnbounded<AssessmentEventDto>());
builder.Services.AddHostedService<AssessmentEventWorker>();
builder.Services.AddHostedService<MasteryComputeJob>();
```

---

### 8.4 Docker Code Runner Integration

الـ `DockerCodeRunner` بيتكلم مع external service (مثلاً Judge0 أو custom runner) عبر HTTP.

```csharp
// SHIFT.Infrastructure/ExternalClients/DockerCodeRunner.cs
// IHttpClientFactory named client: "CodeRunner"

// Request:
// POST http://code-runner:8080/execute
// { "language": "c", "source_code": "...", "stdin": "input1\ninput2" }

// Response:
// { "stdout": "output", "stderr": "...", "exit_code": 0, "time": 0.012 }
```

**Timeout:** 5 ثواني — لو عدى، بيرجع `TestCaseResult { passed: false, actual_output: "TIMEOUT" }`.

**في `Program.cs`:**
```csharp
builder.Services.AddHttpClient("CodeRunner", client => {
    client.BaseAddress = new Uri(builder.Configuration["CodeRunner:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(6); // 1 ثانية overhead فوق الـ 5
});

builder.Services.AddHttpClient("GeminiAI", client => {
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.DefaultRequestHeaders.Add("x-goog-api-key", 
        builder.Configuration["Gemini:ApiKey"]!);
});
```

---

## 9. EF Core Configuration Notes

### Global Query Filters (Soft Delete)

```csharp
// في ApplicationDbContext.OnModelCreating()
modelBuilder.Entity<ApplicationUser>().HasQueryFilter(u => u.DeletedAt == null);
modelBuilder.Entity<Player>().HasQueryFilter(p => p.DeletedAt == null);
modelBuilder.Entity<ClassCode>().HasQueryFilter(c => c.DeletedAt == null);
```

### JSON Column Mappings

```csharp
// EF Core 8 Native JSON
modelBuilder.Entity<PlayerSave>()
    .OwnsOne(p => p.DesktopState, b => b.ToJson("desktop_state"));

modelBuilder.Entity<StoryBeat>()
    .OwnsOne(s => s.ContentJson, b => b.ToJson("content_json"));

// لو مش بتستخدم OwnsOne، استخدم HasConversion:
modelBuilder.Entity<Shift>()
    .Property(s => s.UnlockCondition)
    .HasConversion(
        v => JsonSerializer.Serialize(v, JsonOptions),
        v => JsonSerializer.Deserialize<ShiftUnlockCondition>(v, JsonOptions));
```

### Entity Configurations (منفصلة)

كل entity ليها class منفصل بدل ما يكون كل حاجة في `OnModelCreating`:

```csharp
// SHIFT.Infrastructure/Persistence/Configurations/StoryBeatConfiguration.cs
public class StoryBeatConfiguration : IEntityTypeConfiguration<StoryBeat>
{
    public void Configure(EntityTypeBuilder<StoryBeat> builder)
    {
        builder.ToTable("StoryBeat");
        builder.HasKey(b => b.BeatId);
        
        builder.Property(b => b.BeatType)
               .HasMaxLength(20)
               .HasDefaultValue("narrative");
               
        builder.Property(b => b.App)
               .HasMaxLength(50);

        // CHECK constraint
        builder.HasCheckConstraint("CHK_Beat_SequenceOrder",
            "(beat_type = 'narrative' AND sequence_order IS NOT NULL) OR " +
            "(beat_type = 'consequence' AND sequence_order IS NULL)");

        builder.HasOne(b => b.Shift)
               .WithMany(s => s.StoryBeats)
               .HasForeignKey(b => b.ShiftId);
    }
}

// في OnModelCreating() — scan تلقائي
modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
```

### DB Transactions للـ Critical Flows

```csharp
// Pattern موحد للـ financial operations
await using var transaction = await _db.Database.BeginTransactionAsync();
try
{
    // 1. SELECT balance WITH UPDLOCK (لمنع race conditions)
    var economy = await _db.PlayerEconomies
        .FromSqlRaw("SELECT * FROM PlayerEconomy WITH (UPDLOCK) WHERE player_id = {0}", playerId)
        .FirstAsync();

    // 2. Apply change
    economy.Balance = Math.Max(0, economy.Balance + delta);
    
    // 3. INSERT Transaction record
    _db.Transactions.Add(new Transaction { ... });
    
    await _db.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

---

## 10. Program.cs — Service Registration Overview

```csharp
// SHIFT.API/Program.cs

var builder = WebApplication.CreateBuilder(args);

// ── Identity & Auth ──────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(/* JWT options */);

builder.Services.AddAuthorization(/* policies */);

// ── Infrastructure (DbContext + External Clients) ─────────────
builder.Services.AddInfrastructure(builder.Configuration);
// AddInfrastructure يعمل:
//   AddDbContext<ApplicationDbContext>
//   AddHttpClient("CodeRunner", ...)
//   AddHttpClient("GeminiAI", ...)
//   AddScoped<ICodeExecutionService, DockerCodeRunner>
//   AddScoped<IAiOrchestrationService, GeminiAiClient>

// ── Application Services ──────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<INarrativeService, NarrativeService>();
builder.Services.AddScoped<IChoiceService, ChoiceService>();
builder.Services.AddScoped<IPracticeService, PracticeService>();
builder.Services.AddScoped<ISideTaskService, SideTaskService>();
builder.Services.AddScoped<IEconomyService, EconomyService>();
builder.Services.AddScoped<IShopService, ShopService>();
builder.Services.AddScoped<ISahmService, SahmService>();
builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ISaveService, SaveService>();

// ── Background Jobs ───────────────────────────────────────────
builder.Services.AddSingleton(Channel.CreateUnbounded<AssessmentEventDto>());
builder.Services.AddSingleton(Channel.CreateUnbounded<MasteryComputeRequest>());
builder.Services.AddHostedService<AssessmentEventWorker>();
builder.Services.AddHostedService<MasteryComputeJob>();
builder.Services.AddHostedService<DailyHintResetJob>();
builder.Services.AddHostedService<AiLogCleanupJob>();

// ── API ───────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(/* JWT security definition */);
builder.Services.AddProblemDetails();

var app = builder.Build();

// ── Middleware Pipeline (ORDER MATTERS) ───────────────────────
app.UseExceptionHandler();          // Global exception handling (ProblemDetails)
app.UseHttpsRedirection();
app.UseAuthentication();            // JWT validation
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();
```

---

*Document: SHIFT Backend Architecture v1.0 | Helwan University CS Department, 2026*
*Stack: ASP.NET Core 8 · EF Core 8 · SQL Server · Clean Architecture · Docker Code Runner · Google Gemini 1.5 Flash*
