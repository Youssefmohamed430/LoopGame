# SHIFT Game — Database ER Diagram & Schema Documentation
**SQL Server (Unified `dbo` Schema) | ASP.NET Identity | .NET 8+ EF Core | Version 2.2 | Graduate Project — Helwan University, CS Department**

---

## Table of Contents

1. [System Overview](#1-system-overview)
2. [ER Diagram (Text Notation)](#2-er-diagram-text-notation)
3. [Unified Schema & Logical Module Architecture](#3-unified-schema--logical-module-architecture)
   - 3.1 [Identity & Access Module](#31-identity--access-module)
   - 3.2 [Content & Narrative Module](#32-content--narrative-module)
   - 3.3 [Runtime Player State Module](#33-runtime-player-state-module)
   - 3.4 [Economy & Finance Module](#34-economy--finance-module)
   - 3.5 [Stealth Assessment Module](#35-stealth-assessment-module)
   - 3.6 [AI Pipeline & Audit Module](#36-ai-pipeline--audit-module)
4. [Entity Definitions & Column Reference](#4-entity-definitions--column-reference)
   - 4.1 [Identity & Access Domain](#41-identity--access-domain)
   - 4.2 [Content & Narrative Domain](#42-content--narrative-domain)
   - 4.3 [Runtime Player State Domain](#43-runtime-player-state-domain)
   - 4.4 [Economy & Finance Domain](#44-economy--finance-domain)
   - 4.5 [Stealth Assessment Domain](#45-stealth-assessment-domain)
   - 4.6 [AI Pipeline & Audit Domain](#46-ai-pipeline--audit-domain)
5. [JSON Data Contracts & C# Class Representations](#5-json-data-contracts--c-class-representations)
   - 5.1 [`Shift.unlock_condition`](#51-shiftunlock_condition)
   - 5.2 [`StoryBeat.content_json`](#52-storybeatcontent_json)
   - 5.3 [`StoryBeat.desktop_event`](#53-storybeatdesktop_event)
   - 5.4 [Consequence Beats — How `StoryBeat` serves as consequence content](#54-consequence-beats--how-storybeat-serves-as-consequence-content)
   - 5.5 [`SideTaskTemplate.slots_schema`](#55-sidetasktemplateslots_schema)
   - 5.6 [`PlayerSave.desktop_state`](#56-playersavedesktop_state)
   - 5.7 [`PracticeAttempt.test_results` & `SideTaskSubmission.test_results`](#57-practiceattempttest_results--sidetasksubmissiontest_results)
   - 5.8 [`PlayerSideTask.filled_slots`](#58-playersidetaskfilled_slots)
   - 5.9 [`AssessmentEvent.payload`](#59-assessmenteventpayload)
   - 5.10 [`AiGenerationLog.parsed_slots`](#510-aigenerationlogparsed_slots)
6. [Relationships Summary](#6-relationships-summary)
7. [SQL Server & EF Core 8 Implementation Notes](#7-sql-server--ef-core-8-implementation-notes)
8. [Index Strategy](#8-index-strategy)

---

## 1. System Overview

SHIFT is a narrative-driven web game teaching introductory C programming (Helwan CS111/COM101 curriculum) through situated workplace simulation, stealth assessment, and adaptive practice gates.

The database is implemented on **SQL Server** using a **single unified database schema (`dbo`)** for all tables. Logical domain grouping (Identity, Narrative Content, Player Progress, Economy, Assessment, AI Audit) is enforced at the C# application and Entity Framework Core layer rather than splitting tables across multiple physical database schemas.

**Key Design & Architectural Principles:**
- **Unified Database Schema (`dbo`):** All 24 tables reside in the default `dbo` schema, streamlining database deployment, connection strings, migrations, and SQL Server user permissions.
- **ASP.NET Core Identity Integration:** Native ASP.NET Core Identity (`IdentityUser<int>`, `IdentityRole<int>`) manages security credentials in `ApplicationUser`, linked 1:1 to a domain-specific `Player` profile.
- **Consequence-as-Beat Model:** Consequences reuse the `StoryBeat` table (with `beat_type = 'consequence'` and `shift_id` pointing to the target shift). The lightweight `Consequence` table links a consequence beat to the `Choice` that triggers it, and the `ConsequenceQueue` tracks per-player pending/fired status at runtime.
- **Primary & Foreign Keys:** All entities use `INT IDENTITY` or `BIGINT IDENTITY` surrogate primary keys.
- **Precision Timestamps & Currencies:** All timestamps are `DATETIME2(3)` in UTC. Currency values use `DECIMAL(10,2)` representing Egyptian Pounds (EGP).
- **JSON Storage & Validation:** Complex dynamic states use `NVARCHAR(MAX)` with SQL Server `ISJSON()` check constraints and map to strongly-typed C# record models in EF Core 8 via `HasConversion` / `ToJson()`.
- **Student Privacy:** Raw academic IDs are **never stored**; only their SHA-256 hex hashes (`CHAR(64)`) are maintained.

---

## 2. ER Diagram (Text Notation)

The following diagram uses crow's-foot notation to depict entity relationships across all database tables in the unified `dbo` schema.  
`||--o{` = one-to-many · `||--||` = one-to-one · `}o--o{` = many-to-many (resolved via junction)

```
┌──────────────────┐         ┌──────────────────┐         ┌─────────────────┐
│ ApplicationUser  │ 1     1 │      Player      │ *     1 │    ClassCode    │
│   (Identity)     │─────────│ (Player Profile) │─────────│   (Identity)    │
└────────┬─────────┘         └────────┬─────────┘         └─────────────────┘
         │ 1                          │ 1
   ┌─────┴─────┐        ┌─────────────┼──────────────┬──────────────────┬──────────────────┐
   │           │        │             │              │                  │                  │
   ▼ *         ▼ *      ▼ 1           ▼ *            ▼ *                ▼ 1                ▼ *
┌──────┐ ┌──────────┐┌──────────┐ ┌────────────┐  ┌──────────────────┐┌─────────────┐   ┌──────────────────┐
│ User │ │ Refresh  ││PlayerSave│ │PlayerChoice│  │ AssessmentEvent  ││PlayerEconomy│   │  PlayerSideTask  │
│ Role │ │  Token   │└──────────┘ └─────┬──────┘  └──────────────────┘└──────┬──────┘   └────────┬─────────┘
└──────┘ └──────────┘                   │ *                                  │ 1                 │ * evaluated by
                                        ▼                                    ▼ *                 ▼
                                  ┌──────────┐                         ┌───────────┐    ┌───────────────────┐
                                  │  Choice  │──┐                      │Transaction│    │SideTaskSubmission │
                                  └─────┬────┘  │ consequence_id       └───────────┘    └───────────────────┘
                                        │ *     │ (nullable)                                     │ * template
                                        ▼       ▼                                                ▼
                              ┌────────────────────────┐                                ┌───────────────────┐
                              │       StoryBeat        │                                │ SideTaskTemplate  │
                              │ beat_type: narrative    │                                └────────┬──────────┘
                              │          | consequence  │                                         │ 1
                              └─────┬──────────────────┘                                         ▼ *
                                    │ *                                                 ┌───────────────────┐
                                    ▼                                                   │  AiGenerationLog  │
                              ┌──────────┐     1       * ┌──────────────┐               └───────────────────┘
                              │  Shift   ├───────────────┤ PracticeTask │
                              └──────────┘               └──────┬───────┘
                                    ▲                            │ 1
                                    │                            ▼ *
                              ┌─────┴──────────┐         ┌──────────────┐
                              │  Consequence   │         │PracticeAttempt│
                              │ beat_id (1:1)  │         └──────────────┘
                              └────────┬───────┘
                                       │ 1
                                       ▼ *
                              ┌────────────────┐
                              │ConsequenceQueue│
                              │  (per-player)  │
                              └────────────────┘
```

---

## 3. Unified Schema & Logical Module Architecture

Using a single default database schema (`dbo`) simplifies maintenance while keeping clean separation of concerns in application code:

| Logical Domain | Tables | Application Layer Responsibilities | Backup Strategy |
|---|---|---|---|
| **Identity & Access** | `ApplicationUser`, `ApplicationRole`, `ApplicationUserRole`, `ClassCode`, `RefreshToken` | Authentication, authorization, password security, session refresh tokens. | Hourly |
| **Content & Narrative** | `Shift`, `StoryBeat`, `Choice`, `Consequence`, `PracticeTask`, `TestCase`, `SideTaskTemplate` | Pre-authored story shifts, dialogue beats, choices, tasks, and test blueprints. | Weekly |
| **Runtime Player State** | `Player`, `PlayerSave`, `PlayerChoice`, `PlayerShiftProgress`, `PracticeAttempt`, `ConsequenceQueue`, `PlayerSideTask`, `SideTaskSubmission` | Active gameplay progress, LoopOS desktop saves, code submissions, queued consequences. | Hourly |
| **Economy & Finance** | `PlayerEconomy`, `Transaction`, `ShopItem`, `PlayerInventory`, `SahmSubscription` | Virtual EGP balance ledger, salary management, virtual shop, Sahm AI subscriptions. | Daily |
| **Stealth Assessment** | `AssessmentEvent`, `ConceptMasterySnapshot` | Educational research analytics, stealth learning event logs, concept mastery tracking. | Daily |
| **AI & System Audit** | `AiGenerationLog`, `AuditLog` | LLM call logging, prompt audits, admin security audit trails, retention cleanup. | Daily |

---

### 3.1 Identity & Access Module

- **Role & Purpose:** Manages student and administrator authentication, password hashing, roles (`player`, `admin`, `super_admin`), and JWT refresh token revocation.
- **.NET Integration:** Integrated via ASP.NET Core Identity using `ApplicationDbContext` (extending `IdentityDbContext<ApplicationUser, ApplicationRole, int>`).
- **Data Flow:**
  1. Student authenticates via `POST /api/auth/login`.
  2. ASP.NET Identity validates credentials, issues JWT, and inserts a SHA-256 hash entry into `RefreshToken`.
  3. `ApplicationUser.user_id` links 1:1 to `Player.user_id`.

---

### 3.2 Content & Narrative Module

- **Role & Purpose:** Holds immutable game narrative assets authored by storywriters: shifts, beats, choices, normalized consequences, practice tasks, test cases, and side-task templates.
- **Data Flow:**
  1. Narrative services query `Shift`, `StoryBeat`, `Choice`, and `Consequence` during gameplay.
  2. Code execution engine queries `TestCase` rows to evaluate submitted student C code.

---

### 3.3 Runtime Player State Module

- **Role & Purpose:** Tracks real-time player interaction: current shift progress, desktop saves, choice choices, code submissions, and active side tasks.
- **Data Flow:**
  1. Desktop save state is serialized to JSON and persisted to `PlayerSave` every 30 seconds.
  2. Submitting a choice logs an immutable `PlayerChoice` and queues any linked `Consequence` rows into `ConsequenceQueue`.
  3. At shift transition, pending `ConsequenceQueue` entries with matching `trigger_shift_id` are processed.

---

### 3.4 Economy & Finance Module

- **Role & Purpose:** Single source of truth for player currency (EGP) balance, salary bands, shop catalogue, inventory, and Sahm AI assistant subscription levels.
- **Data Flow:**
  1. Earning rewards logs a positive `Transaction` record and updates `PlayerEconomy.balance`.
  2. Purchasing shop items logs a negative `Transaction` record and creates a `PlayerInventory` or `SahmSubscription` entry.

---

### 3.5 Stealth Assessment Module

- **Role & Purpose:** Collects learning analytics based on Evidence-Centered Design (ECD). Stores raw telemetry (`AssessmentEvent`) and periodic mastery scores (`ConceptMasterySnapshot`).
- **Data Flow:**
  1. Gameplay events emit `AssessmentEvent` telemetry.
  2. Background calculation services update `ConceptMasterySnapshot` scores to populate teacher heatmaps and feed AI task generators.

---

### 3.6 AI Pipeline & Audit Module

- **Role & Purpose:** Records calls made to LLM APIs (Google Gemini 1.5 Flash / OpenRouter) for dynamic side-task slot generation, alongside system administration audit trails (`AuditLog`).
- **Data Flow:**
  1. Side-task generation writes prompt, response, latency, token costs, and slot values to `AiGenerationLog`.
  2. Scheduled cleanup jobs purge logs older than 2 years based on `expires_at`.

---

## 4. Entity Definitions & Column Reference

---

### 4.1 Identity & Access Domain

#### `ApplicationUser`

**Purpose:** Core user account entity integrating ASP.NET Core Identity (`IdentityUser<int>`). Stores authentication credentials, contact information, display names, and account status.

```sql
CREATE TABLE ApplicationUser (
    user_id                 INT           IDENTITY(1,1) PRIMARY KEY,
    user_name               NVARCHAR(256) NOT NULL UNIQUE,
    normalized_user_name     NVARCHAR(256) NOT NULL UNIQUE,
    email                   NVARCHAR(256) NOT NULL UNIQUE,
    normalized_email        NVARCHAR(256) NOT NULL UNIQUE,
    email_confirmed         BIT           NOT NULL DEFAULT 0,
    password_hash           NVARCHAR(MAX) NULL,
    security_stamp          NVARCHAR(MAX) NULL,
    concurrency_stamp       NVARCHAR(MAX) NULL,
    phone_number            NVARCHAR(MAX) NULL,
    phone_number_confirmed  BIT           NOT NULL DEFAULT 0,
    two_factor_enabled      BIT           NOT NULL DEFAULT 0,
    lockout_end             DATETIMEOFFSET NULL,
    lockout_enabled         BIT           NOT NULL DEFAULT 1,
    access_failed_count     INT           NOT NULL DEFAULT 0,
    display_name            NVARCHAR(100) NULL,
    is_active               BIT           NOT NULL DEFAULT 1,
    created_at              DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),
    deleted_at              DATETIME2(3)  NULL
);
```

| Column | Type | Constraints | Description |
|---|---|---|---|
| `user_id` | `INT` | PK, IDENTITY | Surrogate primary key (ASP.NET Identity Key) |
| `user_name` | `NVARCHAR(256)` | NOT NULL, UNIQUE | Account username |
| `normalized_user_name` | `NVARCHAR(256)` | NOT NULL, UNIQUE | Upper-case normalized username for fast ASP.NET lookup |
| `email` | `NVARCHAR(256)` | NOT NULL, UNIQUE | User email address |
| `normalized_email` | `NVARCHAR(256)` | NOT NULL, UNIQUE | Upper-case normalized email |
| `password_hash` | `NVARCHAR(MAX)` | NULL | ASP.NET Identity password hash |
| `security_stamp` | `NVARCHAR(MAX)` | NULL | Security stamp invalidated on security credential updates |
| `display_name` | `NVARCHAR(100)` | NULL | User's display name |
| `is_active` | `BIT` | DEFAULT 1 | Active account status flag |
| `created_at` | `DATETIME2(3)` | NOT NULL | UTC creation timestamp |
| `deleted_at` | `DATETIME2(3)` | NULL | Soft delete timestamp |

---

#### `ApplicationRole`

**Purpose:** Roles table integrating ASP.NET Core Identity (`IdentityRole<int>`). Pre-populated with `player`, `admin`, `super_admin`.

```sql
CREATE TABLE ApplicationRole (
    role_id           INT           IDENTITY(1,1) PRIMARY KEY,
    name              NVARCHAR(256) NOT NULL UNIQUE,
    normalized_name   NVARCHAR(256) NOT NULL UNIQUE,
    concurrency_stamp NVARCHAR(MAX) NULL
);
```

---

#### `ApplicationUserRole`

**Purpose:** Junction table linking `ApplicationUser` to `ApplicationRole` (`IdentityUserRole<int>`).

```sql
CREATE TABLE ApplicationUserRole (
    user_id INT NOT NULL,
    role_id INT NOT NULL,
    PRIMARY KEY (user_id, role_id),
    CONSTRAINT FK_UserRole_User FOREIGN KEY (user_id) REFERENCES ApplicationUser(user_id),
    CONSTRAINT FK_UserRole_Role FOREIGN KEY (role_id) REFERENCES ApplicationRole(role_id)
);
```

---

#### `ClassCode`

**Purpose:** Represents an academic class section (e.g., "CS111-2026-S1"). Students enter a valid class code during profile setup.

```sql
CREATE TABLE ClassCode (
    class_code_id   INT           IDENTITY(1,1) PRIMARY KEY,
    code            VARCHAR(20)   NOT NULL UNIQUE,
    description     NVARCHAR(200) NULL,
    instructor_id   INT           NULL,           -- FK → ApplicationUser
    is_active       BIT           NOT NULL DEFAULT 1,
    created_at      DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),
    expires_at      DATETIME2(3)  NULL,
    deleted_at      DATETIME2(3)  NULL,
    CONSTRAINT FK_ClassCode_Instructor FOREIGN KEY (instructor_id) REFERENCES ApplicationUser(user_id)
);
```

---

#### `RefreshToken`

**Purpose:** Stores SHA-256 hashes of JWT refresh tokens for active sessions.

```sql
CREATE TABLE RefreshToken (
    token_id        INT           IDENTITY(1,1) PRIMARY KEY,
    user_id         INT           NOT NULL,      -- FK → ApplicationUser
    token_hash      CHAR(64)      NOT NULL UNIQUE,
    issued_at       DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),
    expires_at      DATETIME2(3)  NOT NULL,
    revoked_at      DATETIME2(3)  NULL,
    user_agent      NVARCHAR(500) NULL,
    ip_address      VARCHAR(45)   NULL,
    CONSTRAINT FK_RefreshToken_User FOREIGN KEY (user_id) REFERENCES ApplicationUser(user_id)
);
```

---

### 4.2 Content & Narrative Domain

#### `Shift`

**Purpose:** Represents a narrative workday / chapter shift.

```sql
CREATE TABLE Shift (
    shift_id        INT           IDENTITY(1,1) PRIMARY KEY,
    shift_number    INT           NOT NULL,
    chapter_number  INT           NOT NULL,
    title           NVARCHAR(200) NOT NULL,
    description     NVARCHAR(1000) NULL,
    is_capstone     BIT           NOT NULL DEFAULT 0,
    unlock_condition NVARCHAR(MAX) NULL,             -- JSON: prerequisite rules
    created_at      DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Shift_Number UNIQUE (chapter_number, shift_number),
    CONSTRAINT CHK_Shift_UnlockJSON CHECK (unlock_condition IS NULL OR ISJSON(unlock_condition) = 1)
);
```

---

#### `StoryBeat`

**Purpose:** A single narrative delivery unit within a shift. Serves two roles via `beat_type`:
- **`narrative`** — A standard beat in the shift’s ordered sequence (has `sequence_order`).
- **`consequence`** — A deferred beat that fires in a future shift when triggered by a prior choice (has `sequence_order = NULL`; its `shift_id` points to the **target** shift where it will be injected).

Both types share the same structure (`app`, `sender_name`, `content_json`, `desktop_event`), so the narrative engine processes them with identical rendering logic.

```sql
CREATE TABLE StoryBeat (
    beat_id          INT           IDENTITY(1,1) PRIMARY KEY,
    shift_id         INT           NOT NULL,     -- FK → Shift (target shift for consequence beats)
    beat_key         VARCHAR(100)  NOT NULL UNIQUE,
    beat_type        VARCHAR(20)   NOT NULL DEFAULT 'narrative'
                     CHECK (beat_type IN ('narrative', 'consequence')),
    sequence_order   INT           NULL,         -- NULL for consequence beats
    app              VARCHAR(50)   NOT NULL
                     CHECK (app IN ('WhatsUpp','MailLoop','LoopCode',
                                    'System','VideoCall','Notification')),
    sender_name      NVARCHAR(100) NULL,
    content_json     NVARCHAR(MAX) NOT NULL,
    desktop_event    NVARCHAR(MAX) NULL,
    delay_seconds    DECIMAL(5,1)  NOT NULL DEFAULT 0,
    has_choices      BIT           NOT NULL DEFAULT 0,
    created_at       DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT CHK_Beat_SequenceOrder CHECK (
        (beat_type = 'narrative'   AND sequence_order IS NOT NULL)
        OR
        (beat_type = 'consequence' AND sequence_order IS NULL)
    ),
    CONSTRAINT FK_Beat_Shift FOREIGN KEY (shift_id) REFERENCES Shift(shift_id),
    CONSTRAINT CHK_Beat_ContentJSON CHECK (ISJSON(content_json) = 1),
    CONSTRAINT CHK_Beat_EventJSON CHECK (desktop_event IS NULL OR ISJSON(desktop_event) = 1)
);
```

| Column | Type | Constraints | Description |
|---|---|---|---|
| `beat_id` | `INT` | PK, IDENTITY | Surrogate primary key |
| `shift_id` | `INT` | FK → `Shift`, NOT NULL | For narrative beats: the shift this beat belongs to. For consequence beats: the **target shift** where the beat fires |
| `beat_key` | `VARCHAR(100)` | NOT NULL, UNIQUE | Stable string key used in Ink.js scripts and API responses |
| `beat_type` | `VARCHAR(20)` | CHECK constraint | `narrative` = standard shift beat, `consequence` = deferred injection beat |
| `sequence_order` | `INT` | NULL | Delivery order within shift (1-indexed). NULL for consequence beats (they have no fixed position in the base sequence) |
| `app` | `VARCHAR(50)` | CHECK constraint | Which LoopOS app delivers this beat |
| `sender_name` | `NVARCHAR(100)` | NULL | Fictional character who sends this message |
| `content_json` | `NVARCHAR(MAX)` | NOT NULL, ISJSON | Full beat payload: text, choice previews, image URLs, etc. |
| `desktop_event` | `NVARCHAR(MAX)` | NULL, JSON | Optional desktop side-effect |
| `delay_seconds` | `DECIMAL(5,1)` | DEFAULT 0 | Simulated typing delay |
| `has_choices` | `BIT` | DEFAULT 0 | When 1, the frontend renders choice buttons |
| `created_at` | `DATETIME2(3)` | NOT NULL | Content authoring timestamp |

---

#### `Choice`

**Purpose:** Represents 1 of 4 choices presented to the player at a narrative choice beat. Optionally links to a `Consequence` to trigger a deferred beat in a future shift.

```sql
CREATE TABLE Choice (
    choice_id          INT           IDENTITY(1,1) PRIMARY KEY,
    beat_id            INT           NOT NULL,      -- FK → StoryBeat (beat_type = 'narrative')
    choice_index       TINYINT       NOT NULL CHECK (choice_index BETWEEN 1 AND 4),
    choice_text        NVARCHAR(500) NOT NULL,
    tier               VARCHAR(20)   NOT NULL
                       CHECK (tier IN ('Ideal','Acceptable','Debt','Mistake')),
    consequence_id     INT           NULL,          -- FK → Consequence (NULL = no deferred effect)
    immediate_feedback NVARCHAR(500) NULL,
    egp_delta          DECIMAL(8,2)  NOT NULL DEFAULT 0,

    CONSTRAINT UQ_Choice_Beat_Index  UNIQUE (beat_id, choice_index),
    CONSTRAINT FK_Choice_Beat        FOREIGN KEY (beat_id)        REFERENCES StoryBeat(beat_id),
    CONSTRAINT FK_Choice_Consequence FOREIGN KEY (consequence_id) REFERENCES Consequence(consequence_id)
);
```

| Column | Type | Constraints | Description |
|---|---|---|---|
| `choice_id` | `INT` | PK, IDENTITY | Surrogate primary key |
| `beat_id` | `INT` | FK → `StoryBeat` | The narrative beat this choice belongs to |
| `choice_index` | `TINYINT` | CHECK 1–4 | Button position (1 = top-left, 4 = bottom-right) |
| `choice_text` | `NVARCHAR(500)` | NOT NULL | Text displayed on the choice button |
| `tier` | `VARCHAR(20)` | CHECK constraint | Quality tier: Ideal, Acceptable, Debt, Mistake |
| `consequence_id` | `INT` | FK → `Consequence`, NULL | If set, selecting this choice queues the linked consequence for future injection. NULL = no deferred effect |
| `immediate_feedback` | `NVARCHAR(500)` | NULL | Subtle in-world feedback shown after selection |
| `egp_delta` | `DECIMAL(8,2)` | DEFAULT 0 | EGP adjustment on selection |

---

#### `Consequence` (Lightweight Beat Pointer)

**Purpose:** Links a `Choice` to a consequence `StoryBeat` (one with `beat_type = 'consequence'`). The `Consequence` table is intentionally lightweight — all consequence content (text, app, sender, desktop events) lives inside the `StoryBeat` row. The target shift is derived from `StoryBeat.shift_id`.

```sql
CREATE TABLE Consequence (
    consequence_id   INT           IDENTITY(1,1) PRIMARY KEY,
    beat_id          INT           NOT NULL UNIQUE, -- FK → StoryBeat (1:1, beat_type = 'consequence')
    inject_position  VARCHAR(10)   NOT NULL DEFAULT 'start'
                     CHECK (inject_position IN ('start', 'end')),

    CONSTRAINT FK_Consequence_Beat FOREIGN KEY (beat_id) REFERENCES StoryBeat(beat_id)
);
```

| Column | Type | Constraints | Description |
|---|---|---|---|
| `consequence_id` | `INT` | PK, IDENTITY | Surrogate primary key |
| `beat_id` | `INT` | FK → `StoryBeat`, UNIQUE (1:1) | The consequence beat containing the injected content. `StoryBeat.shift_id` determines the target shift |
| `inject_position` | `VARCHAR(10)` | CHECK 'start' / 'end' | Where to inject the beat within the target shift’s narrative flow |

---

#### `PracticeTask`

**Purpose:** Mandatory coding exercise within a shift gate.

```sql
CREATE TABLE PracticeTask (
    task_id         INT           IDENTITY(1,1) PRIMARY KEY,
    shift_id        INT           NOT NULL,
    task_order      TINYINT       NOT NULL,
    title           NVARCHAR(200) NOT NULL,
    description     NVARCHAR(MAX) NOT NULL,
    starter_code    NVARCHAR(MAX) NULL,
    concept_tag     VARCHAR(50)   NOT NULL,
    difficulty      VARCHAR(20)   NOT NULL DEFAULT 'Standard'
                     CHECK (difficulty IN ('SpacedRetrieval','Standard','Challenge')),
    max_attempts    SMALLINT      NOT NULL DEFAULT 0,
    egp_reward      DECIMAL(8,2)  NOT NULL DEFAULT 0,
    created_at      DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_PracticeTask_Shift FOREIGN KEY (shift_id) REFERENCES Shift(shift_id)
);
```

---

#### `TestCase`

**Purpose:** Test inputs & expected outputs for evaluating student code submissions.

```sql
CREATE TABLE TestCase (
    test_case_id    INT           IDENTITY(1,1) PRIMARY KEY,
    task_id         INT           NULL,
    template_id     INT           NULL,
    test_input      NVARCHAR(MAX) NOT NULL,
    expected_output NVARCHAR(MAX) NOT NULL,
    is_hidden       BIT           NOT NULL DEFAULT 0,
    description     NVARCHAR(500) NULL,
    CONSTRAINT FK_TestCase_Task FOREIGN KEY (task_id) REFERENCES PracticeTask(task_id),
    CONSTRAINT FK_TestCase_Template FOREIGN KEY (template_id) REFERENCES SideTaskTemplate(template_id),
    CONSTRAINT CHK_TestCase_Parent CHECK ((task_id IS NOT NULL AND template_id IS NULL) OR (task_id IS NULL AND template_id IS NOT NULL))
);
```

---

#### `SideTaskTemplate`

**Purpose:** Skeleton blueprint used by AI for dynamic side project task generation.

```sql
CREATE TABLE SideTaskTemplate (
    template_id          INT           IDENTITY(1,1) PRIMARY KEY,
    template_key         VARCHAR(100)  NOT NULL UNIQUE,
    concept_tag          VARCHAR(50)   NOT NULL,
    rank_required        VARCHAR(30)   NOT NULL DEFAULT 'Intern'
                          CHECK (rank_required IN ('Intern','Fresh','Experienced Junior','Senior','Lead')),
    title_template       NVARCHAR(300) NOT NULL,
    description_template NVARCHAR(MAX) NOT NULL,
    slots_schema         NVARCHAR(MAX) NOT NULL,             -- JSON slot definition
    egp_min              DECIMAL(8,2)  NOT NULL DEFAULT 500,
    egp_max              DECIMAL(8,2)  NOT NULL DEFAULT 3000,
    is_active            BIT           NOT NULL DEFAULT 1,
    created_at           DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CHK_Template_SlotsJSON CHECK (ISJSON(slots_schema) = 1)
);
```

---

### 4.3 Runtime Player State Domain

#### `Player`

**Purpose:** Player profile entity linked 1:1 to `ApplicationUser`. Contains player rank, class code, and career progress metrics.

```sql
CREATE TABLE Player (
    player_id           INT          IDENTITY(1,1) PRIMARY KEY,
    user_id             INT          NOT NULL UNIQUE,   -- FK → ApplicationUser (1:1)
    student_id_hash     CHAR(64)     NOT NULL UNIQUE,   -- SHA-256 of student ID
    class_code_id       INT          NOT NULL,          -- FK → ClassCode
    rank                VARCHAR(30)  NOT NULL DEFAULT 'Intern'
                         CHECK (rank IN ('Intern','Fresh','Experienced Junior','Senior','Lead')),
    current_shift_id    INT          NULL,              -- FK → Shift
    total_play_time_sec INT          NOT NULL DEFAULT 0,
    created_at          DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    deleted_at          DATETIME2(3) NULL,
    CONSTRAINT FK_Player_User FOREIGN KEY (user_id) REFERENCES ApplicationUser(user_id),
    CONSTRAINT FK_Player_ClassCode FOREIGN KEY (class_code_id) REFERENCES ClassCode(class_code_id),
    CONSTRAINT FK_Player_Shift FOREIGN KEY (current_shift_id) REFERENCES Shift(shift_id)
);
```

---

#### `PlayerSave`

**Purpose:** Stores full serialised desktop state (desktop icons, window positions, active state).

```sql
CREATE TABLE PlayerSave (
    save_id         INT           IDENTITY(1,1) PRIMARY KEY,
    player_id       INT           NOT NULL,
    slot_number     TINYINT       NOT NULL CHECK (slot_number IN (1,2,3)),
    save_label      NVARCHAR(100) NULL,
    desktop_state   NVARCHAR(MAX) NOT NULL,             -- JSON payload
    saved_at        DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_PlayerSave_Player FOREIGN KEY (player_id) REFERENCES Player(player_id),
    CONSTRAINT UQ_PlayerSave UNIQUE (player_id, slot_number),
    CONSTRAINT CHK_PlayerSave_DesktopJSON CHECK (ISJSON(desktop_state) = 1)
);
```

---

#### `PlayerShiftProgress`

**Purpose:** Tracks player progression through shifts.

```sql
CREATE TABLE PlayerShiftProgress (
    progress_id     INT           IDENTITY(1,1) PRIMARY KEY,
    player_id       INT           NOT NULL,
    shift_id        INT           NOT NULL,
    status          VARCHAR(20)   NOT NULL DEFAULT 'in_progress'
                     CHECK (status IN ('in_progress','gate_pending','completed')),
    started_at      DATETIME2(3)  NULL,
    completed_at    DATETIME2(3)  NULL,
    gate_attempts   SMALLINT      NOT NULL DEFAULT 0,
    CONSTRAINT FK_ShiftProgress_Player FOREIGN KEY (player_id) REFERENCES Player(player_id),
    CONSTRAINT FK_ShiftProgress_Shift FOREIGN KEY (shift_id) REFERENCES Shift(shift_id),
    CONSTRAINT UQ_PlayerShift UNIQUE (player_id, shift_id)
);
```

---

#### `PlayerChoice`

**Purpose:** Immutable record of every choice made by a player.

```sql
CREATE TABLE PlayerChoice (
    player_choice_id INT          IDENTITY(1,1) PRIMARY KEY,
    player_id       INT           NOT NULL,
    beat_id         INT           NOT NULL,
    choice_id       INT           NOT NULL,
    tier            VARCHAR(20)   NOT NULL,
    chosen_at       DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),
    session_context NVARCHAR(MAX) NULL,
    CONSTRAINT FK_PlayerChoice_Player FOREIGN KEY (player_id) REFERENCES Player(player_id),
    CONSTRAINT FK_PlayerChoice_Beat FOREIGN KEY (beat_id) REFERENCES StoryBeat(beat_id),
    CONSTRAINT FK_PlayerChoice_Choice FOREIGN KEY (choice_id) REFERENCES Choice(choice_id)
);
```

---

#### `PracticeAttempt`

**Purpose:** Logs code submissions against mandatory practice gate tasks.

```sql
CREATE TABLE PracticeAttempt (
    attempt_id      INT           IDENTITY(1,1) PRIMARY KEY,
    player_id       INT           NOT NULL,
    task_id         INT           NOT NULL,
    submitted_code  NVARCHAR(MAX) NOT NULL,
    tier            VARCHAR(20)   NOT NULL
                     CHECK (tier IN ('Ideal','Acceptable','Debt','Mistake')),
    test_results    NVARCHAR(MAX) NOT NULL,             -- JSON test output
    time_spent_sec  INT           NOT NULL DEFAULT 0,
    hint_used       BIT           NOT NULL DEFAULT 0,
    submitted_at    DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_PracticeAttempt_Player FOREIGN KEY (player_id) REFERENCES Player(player_id),
    CONSTRAINT FK_PracticeAttempt_Task FOREIGN KEY (task_id) REFERENCES PracticeTask(task_id),
    CONSTRAINT CHK_PracticeAttempt_ResultsJSON CHECK (ISJSON(test_results) = 1)
);
```

---

#### `ConsequenceQueue`

**Purpose:** Per-player runtime queue of pending consequences. When a player selects a `Choice` with a linked `Consequence`, a row is inserted here with `status = 'pending'`. At shift start, the engine queries this table to find due consequences and injects them.

```sql
CREATE TABLE ConsequenceQueue (
    queue_id         INT           IDENTITY(1,1) PRIMARY KEY,
    player_id        INT           NOT NULL,  -- FK → Player
    consequence_id   INT           NOT NULL,  -- FK → Consequence
    status           VARCHAR(20)   NOT NULL DEFAULT 'pending'
                     CHECK (status IN ('pending','fired','dismissed')),
    queued_at        DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),
    fired_at         DATETIME2(3)  NULL,

    CONSTRAINT UQ_Queue_Player_Consequence UNIQUE (player_id, consequence_id),
    CONSTRAINT FK_Queue_Player      FOREIGN KEY (player_id)      REFERENCES Player(player_id),
    CONSTRAINT FK_Queue_Consequence FOREIGN KEY (consequence_id) REFERENCES Consequence(consequence_id)
);
```

| Column | Type | Constraints | Description |
|---|---|---|---|
| `queue_id` | `INT` | PK, IDENTITY | Surrogate primary key |
| `player_id` | `INT` | FK → `Player` | Player receiving consequence |
| `consequence_id` | `INT` | FK → `Consequence` | Consequence to inject (target shift derived via `Consequence → StoryBeat.shift_id`) |
| `status` | `VARCHAR(20)` | CHECK constraint | `pending` (waiting), `fired` (injected), `dismissed` (expired/recovered) |
| `queued_at` | `DATETIME2(3)` | NOT NULL | UTC when the triggering choice was made |
| `fired_at` | `DATETIME2(3)` | NULL | UTC when consequence was injected into the shift narrative |

---

#### `PlayerSideTask`

**Purpose:** AI-generated side task instance assigned to a player.

```sql
CREATE TABLE PlayerSideTask (
    side_task_id    INT           IDENTITY(1,1) PRIMARY KEY,
    player_id       INT           NOT NULL,
    template_id     INT           NOT NULL,
    ai_log_id       INT           NULL,
    resolved_title  NVARCHAR(300) NOT NULL,
    resolved_description NVARCHAR(MAX) NOT NULL,
    filled_slots    NVARCHAR(MAX) NOT NULL,             -- JSON slot values
    egp_reward      DECIMAL(8,2)  NOT NULL,
    status          VARCHAR(20)   NOT NULL DEFAULT 'active'
                     CHECK (status IN ('active','submitted','abandoned','expired')),
    assigned_at     DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),
    deadline_at     DATETIME2(3)  NULL,
    completed_at    DATETIME2(3)  NULL,
    CONSTRAINT FK_PlayerSideTask_Player FOREIGN KEY (player_id) REFERENCES Player(player_id),
    CONSTRAINT FK_PlayerSideTask_Template FOREIGN KEY (template_id) REFERENCES SideTaskTemplate(template_id),
    CONSTRAINT CHK_PlayerSideTask_SlotsJSON CHECK (ISJSON(filled_slots) = 1)
);
```

---

#### `SideTaskSubmission`

**Purpose:** Submission log for AI side tasks.

```sql
CREATE TABLE SideTaskSubmission (
    submission_id   INT           IDENTITY(1,1) PRIMARY KEY,
    side_task_id    INT           NOT NULL,
    player_id       INT           NOT NULL,
    submitted_code  NVARCHAR(MAX) NOT NULL,
    tier            VARCHAR(20)   NOT NULL
                     CHECK (tier IN ('Ideal','Acceptable','Debt','Mistake')),
    test_results    NVARCHAR(MAX) NOT NULL,             -- JSON
    sahm_hints_used TINYINT       NOT NULL DEFAULT 0,
    time_spent_sec  INT           NOT NULL DEFAULT 0,
    egp_earned      DECIMAL(8,2)  NOT NULL DEFAULT 0,
    submitted_at    DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_SideTaskSubmission_SideTask FOREIGN KEY (side_task_id) REFERENCES PlayerSideTask(side_task_id),
    CONSTRAINT FK_SideTaskSubmission_Player FOREIGN KEY (player_id) REFERENCES Player(player_id),
    CONSTRAINT CHK_SideTaskSubmission_ResultsJSON CHECK (ISJSON(test_results) = 1)
);
```

---

### 4.4 Economy & Finance Domain

#### `PlayerEconomy`

**Purpose:** Single source of truth for player currency (EGP) balance and salary tier.

```sql
CREATE TABLE PlayerEconomy (
    economy_id      INT           IDENTITY(1,1) PRIMARY KEY,
    player_id       INT           NOT NULL UNIQUE,
    balance         DECIMAL(10,2) NOT NULL DEFAULT 0 CHECK (balance >= 0),
    salary_tier     INT           NOT NULL DEFAULT 1 CHECK (salary_tier BETWEEN 1 AND 5),
    total_earned    DECIMAL(12,2) NOT NULL DEFAULT 0,
    total_spent     DECIMAL(12,2) NOT NULL DEFAULT 0,
    updated_at      DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Economy_Player FOREIGN KEY (player_id) REFERENCES Player(player_id)
);
```

---

#### `Transaction`

**Purpose:** Immutable ledger log of all credits and debits.

```sql
CREATE TABLE [Transaction] (
    transaction_id  INT           IDENTITY(1,1) PRIMARY KEY,
    player_id       INT           NOT NULL,
    amount          DECIMAL(10,2) NOT NULL,
    transaction_type VARCHAR(30)  NOT NULL
                     CHECK (transaction_type IN ('salary','bonus','side_task','purchase','penalty','bug_bounty')),
    description     NVARCHAR(500) NOT NULL,
    reference_id    INT           NULL,
    balance_after   DECIMAL(10,2) NOT NULL,
    created_at      DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Transaction_Player FOREIGN KEY (player_id) REFERENCES Player(player_id)
);
```

---

#### `ShopItem`

**Purpose:** Catalogue of shop items available in LoopOS.

```sql
CREATE TABLE ShopItem (
    item_id         INT           IDENTITY(1,1) PRIMARY KEY,
    item_key        VARCHAR(100)  NOT NULL UNIQUE,
    display_name    NVARCHAR(200) NOT NULL,
    category        VARCHAR(30)   NOT NULL
                     CHECK (category IN ('sahm_tier','camera','desk_item','workspace')),
    description     NVARCHAR(500) NULL,
    price           DECIMAL(10,2) NOT NULL CHECK (price > 0),
    rank_required   VARCHAR(30)   NULL,
    is_one_way      BIT           NOT NULL DEFAULT 0,
    asset_key       VARCHAR(200)  NULL,
    is_available    BIT           NOT NULL DEFAULT 1,
    sort_order      INT           NOT NULL DEFAULT 0
);
```

---

#### `PlayerInventory`

**Purpose:** Items owned by players.

```sql
CREATE TABLE PlayerInventory (
    inventory_id    INT           IDENTITY(1,1) PRIMARY KEY,
    player_id       INT           NOT NULL,
    item_id         INT           NOT NULL,
    purchased_at    DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),
    egp_paid        DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_Inventory_Player FOREIGN KEY (player_id) REFERENCES Player(player_id),
    CONSTRAINT FK_Inventory_Item FOREIGN KEY (item_id) REFERENCES ShopItem(item_id),
    CONSTRAINT UQ_PlayerInventory UNIQUE (player_id, item_id)
);
```

---

#### `SahmSubscription`

**Purpose:** History and active tier of Sahm AI assistant subscription.

```sql
CREATE TABLE SahmSubscription (
    subscription_id INT           IDENTITY(1,1) PRIMARY KEY,
    player_id       INT           NOT NULL,
    tier            VARCHAR(20)   NOT NULL CHECK (tier IN ('Free','Pro','Team','Enterprise')),
    activated_at    DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),
    daily_hint_limit TINYINT      NOT NULL DEFAULT 3,
    hints_used_today TINYINT      NOT NULL DEFAULT 0,
    last_hint_reset  DATE         NOT NULL DEFAULT CAST(SYSUTCDATETIME() AS DATE),
    CONSTRAINT FK_Sahm_Player FOREIGN KEY (player_id) REFERENCES Player(player_id)
);
```

---

### 4.5 Stealth Assessment Domain

#### `AssessmentEvent`

**Purpose:** Central telemetry log for stealth assessment analysis.

```sql
CREATE TABLE AssessmentEvent (
    event_id        BIGINT        IDENTITY(1,1) PRIMARY KEY,
    player_id       INT           NOT NULL,
    event_type      VARCHAR(50)   NOT NULL
                     CHECK (event_type IN ('choice_submission','practice_attempt','hint_request','side_task_submission','desktop_interaction','consequence_trigger','gate_cleared','shift_completed')),
    concept_tag     VARCHAR(50)   NULL,
    tier            VARCHAR(20)   NULL,
    payload         NVARCHAR(MAX) NULL,                 -- JSON telemetry
    session_id      UNIQUEIDENTIFIER NULL,
    recorded_at     DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Assessment_Player FOREIGN KEY (player_id) REFERENCES Player(player_id),
    CONSTRAINT CHK_Assessment_PayloadJSON CHECK (payload IS NULL OR ISJSON(payload) = 1)
);
```

---

#### `ConceptMasterySnapshot`

**Purpose:** Computed mastery scores per CS111 concept per player.

```sql
CREATE TABLE ConceptMasterySnapshot (
    snapshot_id     INT           IDENTITY(1,1) PRIMARY KEY,
    player_id       INT           NOT NULL,
    shift_id        INT           NOT NULL,
    concept_tag     VARCHAR(50)   NOT NULL,
    mastery_score   DECIMAL(5,4)  NOT NULL CHECK (mastery_score BETWEEN 0 AND 1),
    evidence_count  INT           NOT NULL DEFAULT 0,
    snapshotted_at  DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Mastery_Player FOREIGN KEY (player_id) REFERENCES Player(player_id),
    CONSTRAINT FK_Mastery_Shift FOREIGN KEY (shift_id) REFERENCES Shift(shift_id)
);
```

---

### 4.6 AI Pipeline & Audit Domain

#### `AiGenerationLog`

**Purpose:** Audit log of all calls made to LLMs for generating side task slot values.

```sql
CREATE TABLE AiGenerationLog (
    log_id          INT           IDENTITY(1,1) PRIMARY KEY,
    player_id       INT           NOT NULL,
    template_id     INT           NOT NULL,
    model_name      VARCHAR(100)  NOT NULL,
    prompt_text     NVARCHAR(MAX) NOT NULL,
    raw_response    NVARCHAR(MAX) NULL,
    parsed_slots    NVARCHAR(MAX) NULL,                 -- JSON
    is_valid        BIT           NOT NULL DEFAULT 0,
    validation_error NVARCHAR(500) NULL,
    latency_ms      INT           NOT NULL DEFAULT 0,
    estimated_cost_usd DECIMAL(8,6) NULL,
    created_at      DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),
    expires_at      DATETIME2(3)  NOT NULL DEFAULT DATEADD(YEAR, 2, SYSUTCDATETIME()),
    CONSTRAINT FK_AiLog_Player FOREIGN KEY (player_id) REFERENCES Player(player_id),
    CONSTRAINT FK_AiLog_Template FOREIGN KEY (template_id) REFERENCES SideTaskTemplate(template_id),
    CONSTRAINT CHK_AiLog_SlotsJSON CHECK (parsed_slots IS NULL OR ISJSON(parsed_slots) = 1)
);
```

---

#### `AuditLog`

**Purpose:** Security audit log tracking admin and super-admin actions.

```sql
CREATE TABLE AuditLog (
    audit_id        BIGINT        IDENTITY(1,1) PRIMARY KEY,
    user_id         INT           NULL,                 -- FK → ApplicationUser
    player_id       INT           NULL,                 -- FK → Player
    action          NVARCHAR(200) NOT NULL,
    entity_type     VARCHAR(50)   NULL,
    entity_id       INT           NULL,
    old_value       NVARCHAR(MAX) NULL,                 -- JSON
    new_value       NVARCHAR(MAX) NULL,                 -- JSON
    ip_address      VARCHAR(45)   NULL,
    user_agent      NVARCHAR(500) NULL,
    occurred_at     DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Audit_User FOREIGN KEY (user_id) REFERENCES ApplicationUser(user_id),
    CONSTRAINT FK_Audit_Player FOREIGN KEY (player_id) REFERENCES Player(player_id)
);
```

---

## 5. JSON Data Contracts & C# Class Representations

This section provides concrete JSON examples and corresponding C# model classes used by EF Core 8 (`System.Text.Json`) for all dynamic JSON columns in the database.

---

### 5.1 `Shift.unlock_condition`

**JSON Example:**
```json
{
  "prerequisite_shift_id": 2,
  "min_rank": "Intern",
  "required_concept": "variables",
  "min_mastery_score": 0.70
}
```

**C# Model Representation:**
```csharp
public record ShiftUnlockCondition(
    [property: JsonPropertyName("prerequisite_shift_id")] int? PrerequisiteShiftId,
    [property: JsonPropertyName("min_rank")] string MinRank,
    [property: JsonPropertyName("required_concept")] string RequiredConcept,
    [property: JsonPropertyName("min_mastery_score")] decimal MinMasteryScore
);
```

---

### 5.2 `StoryBeat.content_json`

**JSON Example:**
```json
{
  "text": "Hey engineer, we need you to review the print statement for the customer receipt.",
  "avatar": "youssef_lead.png",
  "sound_effect": "notification_chime.wav",
  "choices": [
    { "index": 1, "text": "I will fix the printf format string right away." },
    { "index": 2, "text": "Let me double check the variables first." }
  ]
}
```

**C# Model Representation:**
```csharp
public record BeatChoicePreview(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("text")] string Text
);

public record StoryBeatContent(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("avatar")] string? Avatar,
    [property: JsonPropertyName("sound_effect")] string? SoundEffect,
    [property: JsonPropertyName("choices")] List<BeatChoicePreview>? Choices
);
```

---

### 5.3 `StoryBeat.desktop_event`

**JSON Example:**
```json
{
  "event_type": "UnlockApp",
  "app_name": "LoopCode",
  "notification_title": "New Assignment Received",
  "payload": { "task_id": 14 }
}
```

**C# Model Representation:**
```csharp
public record DesktopEvent(
    [property: JsonPropertyName("event_type")] string EventType,
    [property: JsonPropertyName("app_name")] string AppName,
    [property: JsonPropertyName("notification_title")] string NotificationTitle,
    [property: JsonPropertyName("payload")] Dictionary<string, object>? Payload
);
```

---

### 5.4 Consequence Beats — How `StoryBeat` serves as consequence content

Consequences do **not** have their own JSON content column. Instead, the consequence’s narrative content lives in a regular `StoryBeat` row (with `beat_type = 'consequence'`). The `Consequence` table is just a lightweight pointer.

**Authoring Flow:**
```
Content Team creates:
├── StoryBeat (shift_id=5, beat_type='consequence', sequence_order=NULL)
│      ├── content_json: { "text": "Because you skipped error handling..." }
│      └── desktop_event: { "event_type": "IncomingEmail", ... }
│
├── Consequence (beat_id → StoryBeat above, inject_position='start')
│
└── Choice (consequence_id → Consequence above)
```

**Runtime Flow:**
```
1. Player selects Choice → backend checks Choice.consequence_id
2. If set → INSERT ConsequenceQueue { player_id, consequence_id, status='pending' }
3. Player enters Shift 5 → backend queries:
   SELECT cq.*, c.beat_id, c.inject_position, sb.*
   FROM ConsequenceQueue cq
   JOIN Consequence c ON cq.consequence_id = c.consequence_id
   JOIN StoryBeat sb ON c.beat_id = sb.beat_id
   WHERE cq.player_id = @playerId
     AND sb.shift_id = 5
     AND cq.status = 'pending'
4. Inject the StoryBeat as a normal beat at inject_position (start/end)
5. UPDATE ConsequenceQueue SET status='fired', fired_at=SYSUTCDATETIME()
```

**C# Service Example:**
```csharp
public async Task<List<StoryBeat>> GetPendingConsequenceBeats(int playerId, int shiftId)
{
    return await _db.ConsequenceQueues
        .Where(cq => cq.PlayerId == playerId && cq.Status == "pending")
        .Join(_db.Consequences, cq => cq.ConsequenceId, c => c.ConsequenceId, (cq, c) => new { cq, c })
        .Join(_db.StoryBeats, x => x.c.BeatId, sb => sb.BeatId, (x, sb) => new { x.cq, x.c, sb })
        .Where(x => x.sb.ShiftId == shiftId)
        .Select(x => x.sb)
        .ToListAsync();
}
```

---

### 5.5 `SideTaskTemplate.slots_schema`

**JSON Example:**
```json
{
  "slots": [
    { "name": "product_name", "type": "string", "description": "Local Egyptian e-commerce item" },
    { "name": "price", "type": "decimal", "min": 50.0, "max": 1500.0 }
  ]
}
```

**C# Model Representation:**
```csharp
public record SlotDefinition(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("min")] decimal? Min,
    [property: JsonPropertyName("max")] decimal? Max
);

public record SideTaskSlotsSchema(
    [property: JsonPropertyName("slots")] List<SlotDefinition> Slots
);
```

---

### 5.6 `PlayerSave.desktop_state`

**JSON Example:**
```json
{
  "open_windows": ["WhatsUpp", "LoopCode"],
  "active_window": "LoopCode",
  "wallpaper_id": "dark_matrix",
  "window_positions": {
    "WhatsUpp": { "x": 100, "y": 150, "width": 400, "height": 600 },
    "LoopCode": { "x": 520, "y": 150, "width": 800, "height": 700 }
  }
}
```

**C# Model Representation:**
```csharp
public record WindowRect(
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height
);

public record DesktopState(
    [property: JsonPropertyName("open_windows")] List<string> OpenWindows,
    [property: JsonPropertyName("active_window")] string ActiveWindow,
    [property: JsonPropertyName("wallpaper_id")] string WallpaperId,
    [property: JsonPropertyName("window_positions")] Dictionary<string, WindowRect> WindowPositions
);
```

---

### 5.7 `PracticeAttempt.test_results` & `SideTaskSubmission.test_results`

**JSON Example:**
```json
[
  { "test_case_id": 101, "passed": true, "actual_output": "Hello World\n", "execution_time_ms": 12 },
  { "test_case_id": 102, "passed": false, "actual_output": "Hello\n", "execution_time_ms": 8 }
]
```

**C# Model Representation:**
```csharp
public record TestCaseResult(
    [property: JsonPropertyName("test_case_id")] int TestCaseId,
    [property: JsonPropertyName("passed")] bool Passed,
    [property: JsonPropertyName("actual_output")] string ActualOutput,
    [property: JsonPropertyName("execution_time_ms")] int ExecutionTimeMs
);
```

---

### 5.8 `PlayerSideTask.filled_slots`

**JSON Example:**
```json
{
  "product_name": "Khamis Leather Jacket",
  "price": 850.50,
  "quantity": 12
}
```

**C# Model Representation:**
```csharp
public class FilledTaskSlots : Dictionary<string, object> { }
```

---

### 5.9 `AssessmentEvent.payload`

**JSON Example:**
```json
{
  "beat_id": 42,
  "choice_id": 165,
  "time_taken_seconds": 14.2,
  "previous_attempts": 2
}
```

**C# Model Representation:**
```csharp
public record ChoiceAssessmentPayload(
    [property: JsonPropertyName("beat_id")] int BeatId,
    [property: JsonPropertyName("choice_id")] int ChoiceId,
    [property: JsonPropertyName("time_taken_seconds")] double TimeTakenSeconds,
    [property: JsonPropertyName("previous_attempts")] int PreviousAttempts
);
```

---

### 5.10 `AiGenerationLog.parsed_slots`

**JSON Example:**
```json
{
  "product_name": "El-Mokattam Spices Set",
  "price": 120.00
}
```

**C# Model Representation:**
```csharp
public record ParsedAiSlots(
    [property: JsonPropertyName("slots")] Dictionary<string, object> Slots
);
```

---

## 6. Relationships Summary

| Parent Table | Child Table | FK Column(s) | Cardinality | Notes |
|---|---|---|---|---|
| `ApplicationUser` | `Player` | `user_id` | 1 : 1 | Profile extension for student players |
| `ApplicationUser` | `ApplicationUserRole` | `user_id` | 1 : many | ASP.NET Identity roles |
| `ApplicationRole` | `ApplicationUserRole` | `role_id` | 1 : many | ASP.NET Identity roles |
| `ClassCode` | `Player` | `class_code_id` | 1 : many | Each player belongs to a class |
| `ApplicationUser` | `RefreshToken` | `user_id` | 1 : many | Multi-device active sessions |
| `Shift` | `StoryBeat` | `shift_id` | 1 : many | Shift narrative beats (both narrative and consequence types) |
| `StoryBeat` | `Choice` | `beat_id` | 1 : 4 | Exactly 4 choices per choice beat |
| `StoryBeat` | `Consequence` | `beat_id` | 1 : 1 | Consequence beat linked 1:1 to a StoryBeat |
| `Consequence` | `Choice` | `consequence_id` | 1 : many | Choice(s) that trigger a consequence |
| `Consequence` | `ConsequenceQueue` | `consequence_id` | 1 : many | Per-player runtime queue entries |
| `Player` | `ConsequenceQueue` | `player_id` | 1 : many | Player's pending/fired consequences |
| `Player` | `PlayerSave` | `player_id` | 1 : 3 | 3 named save slots per player |
| `Player` | `PlayerShiftProgress` | `player_id` | 1 : many | Track shift gate state per player |
| `Player` | `PlayerChoice` | `player_id` | 1 : many | Immutable choice history |
| `PracticeTask` | `PracticeAttempt` | `task_id` | 1 : many | Code submission attempts |
| `PracticeTask` | `TestCase` | `task_id` | 1 : many | Unit test evaluation cases |
| `SideTaskTemplate` | `TestCase` | `template_id` | 1 : many | AI task template evaluation cases |
| `SideTaskTemplate` | `PlayerSideTask` | `template_id` | 1 : many | Template blueprint → instances |
| `PlayerSideTask` | `SideTaskSubmission` | `side_task_id` | 1 : many | Submissions against AI tasks |
| `Player` | `PlayerEconomy` | `player_id` | 1 : 1 | Single EGP balance record |
| `Player` | `Transaction` | `player_id` | 1 : many | Financial credit/debit ledger |
| `ShopItem` | `PlayerInventory` | `item_id` | 1 : many | Purchased shop items |
| `Player` | `AssessmentEvent` | `player_id` | 1 : many | Stealth assessment event stream |
| `Player` | `ConceptMasterySnapshot` | `player_id` | 1 : many | Per-shift concept mastery snapshots |
| `SideTaskTemplate` | `AiGenerationLog` | `template_id` | 1 : many | LLM audit log entries |
| `ApplicationUser` | `AuditLog` | `user_id` | 1 : many | Administrative audit log |

---

## 7. SQL Server & EF Core 8 Implementation Notes

### EF Core 8 DbContext & Schema Configuration

In .NET 8, all entities map to the default `dbo` schema in `ApplicationDbContext`:

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // DbSets for Application Domains
    public DbSet<ClassCode> ClassCodes => Set<ClassCode>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<StoryBeat> StoryBeats => Set<StoryBeat>();
    public DbSet<Choice> Choices => Set<Choice>();
    public DbSet<Consequence> Consequences => Set<Consequence>();
    public DbSet<PracticeTask> PracticeTasks => Set<PracticeTask>();
    public DbSet<TestCase> TestCases => Set<TestCase>();
    public DbSet<SideTaskTemplate> SideTaskTemplates => Set<SideTaskTemplate>();
    public DbSet<PlayerSave> PlayerSaves => Set<PlayerSave>();
    public DbSet<PlayerShiftProgress> PlayerShiftProgresses => Set<PlayerShiftProgress>();
    public DbSet<PlayerChoice> PlayerChoices => Set<PlayerChoice>();
    public DbSet<PracticeAttempt> PracticeAttempts => Set<PracticeAttempt>();
    public DbSet<ConsequenceQueue> ConsequenceQueues => Set<ConsequenceQueue>();
    public DbSet<PlayerSideTask> PlayerSideTasks => Set<PlayerSideTask>();
    public DbSet<SideTaskSubmission> SideTaskSubmissions => Set<SideTaskSubmission>();
    public DbSet<PlayerEconomy> PlayerEconomies => Set<PlayerEconomy>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<ShopItem> ShopItems => Set<ShopItem>();
    public DbSet<PlayerInventory> PlayerInventories => Set<PlayerInventory>();
    public DbSet<SahmSubscription> SahmSubscriptions => Set<SahmSubscription>();
    public DbSet<AssessmentEvent> AssessmentEvents => Set<AssessmentEvent>();
    public DbSet<ConceptMasterySnapshot> ConceptMasterySnapshots => Set<ConceptMasterySnapshot>();
    public DbSet<AiGenerationLog> AiGenerationLogs => Set<AiGenerationLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unified Schema Configuration (Default: dbo)
        modelBuilder.HasDefaultSchema("dbo");

        // Identity ASP.NET Core Entity Configuration
        modelBuilder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("ApplicationUser");
            b.HasQueryFilter(u => u.DeletedAt == null);
        });

        modelBuilder.Entity<ApplicationRole>().ToTable("ApplicationRole");
        modelBuilder.Entity<IdentityUserRole<int>>().ToTable("ApplicationUserRole");

        // Global Soft-Delete Query Filters
        modelBuilder.Entity<Player>().HasQueryFilter(p => p.DeletedAt == null);
        modelBuilder.Entity<ClassCode>().HasQueryFilter(c => c.DeletedAt == null);

        // Native EF Core 8 Dynamic JSON Mappings
        modelBuilder.Entity<PlayerSave>().OwnsOne(p => p.DesktopState, b => b.ToJson());
        modelBuilder.Entity<StoryBeat>().OwnsOne(s => s.ContentJson, b => b.ToJson());

        // StoryBeat discriminator for beat_type
        modelBuilder.Entity<StoryBeat>()
            .Property(b => b.BeatType)
            .HasDefaultValue("narrative");

        // Consequence 1:1 with StoryBeat
        modelBuilder.Entity<Consequence>()
            .HasOne(c => c.Beat)
            .WithOne(sb => sb.Consequence)
            .HasForeignKey<Consequence>(c => c.BeatId);

        // Choice → Consequence (optional)
        modelBuilder.Entity<Choice>()
            .HasOne(c => c.Consequence)
            .WithMany()
            .HasForeignKey(c => c.ConsequenceId)
            .IsRequired(false);
    }
}
```

---

## 8. Index Strategy

| Table | Index Name | Indexed Columns | Type | Rationale |
|---|---|---|---|---|
| `ApplicationUser` | `IX_User_NormalizedEmail` | `(normalized_email)` | Filtered Unique | Fast ASP.NET login lookup |
| `RefreshToken` | `IX_RefreshToken_User_Expiry` | `(user_id, expires_at)` WHERE `revoked_at IS NULL` | Filtered Composite | Active session lookup & token rotation |
| `Player` | `IX_Player_User` | `(user_id)` | Unique | Fast 1:1 user to player profile navigation |
| `Player` | `IX_Player_ClassCode` | `(class_code_id)` WHERE `deleted_at IS NULL` | Filtered | Class student listing queries |
| `StoryBeat` | `IX_Beat_Shift_Seq` | `(shift_id, beat_type, sequence_order)` | Composite | Sequential narrative beat streaming (filters by beat_type) |
| `Consequence` | `IX_Consequence_Beat` | `(beat_id)` | Unique | 1:1 beat-to-consequence lookup |
| `ConsequenceQueue` | `IX_Queue_Player_Status` | `(player_id, status)` | Composite | Player pending consequence lookup at shift start |
| `PlayerChoice` | `IX_Choice_Player_Beat` | `(player_id, beat_id)` | Composite | Choice replay and duplicate guard |
| `PracticeAttempt` | `IX_Attempt_Player_Task` | `(player_id, task_id, submitted_at DESC)` | Composite | Anti-struggle detector & hint logic |
| `Transaction` | `IX_Transaction_Player_Date` | `(player_id, created_at DESC)` | Composite | Player ledger & financial history |
| `AssessmentEvent` | `IX_Assessment_Player_Type` | `(player_id, event_type, recorded_at DESC)` | Composite | AI weakest concept calculation |
| `AiGenerationLog` | `IX_AiLog_Expiry` | `(expires_at)` | Standard | Scheduled 2-year retention cleanup job |

---

*Document updated from SHIFT SRS v2.2 — Helwan University CS Department, 2026.*  
*Database Engine: SQL Server 2019+ / Azure SQL Database (Unified `dbo` Schema) | Backend Framework: ASP.NET Core 8 Entity Framework Core.*
