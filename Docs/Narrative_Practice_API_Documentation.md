# LoopGame API Documentation
## Narrative & Practice Controllers

**Version:** Based on current source code (September 2026 snapshot)
**Backend:** ASP.NET Core 9, Clean Architecture, PostgreSQL
**Project:** SHIFT — Narrative-Driven Educational Web Game (LoopGame)

---

## Table of Contents

1. [Overview](#1-overview)
2. [Authentication & Authorization](#2-authentication--authorization)
3. [Controller Summary](#3-controller-summary)
4. [NarrativeAdminController](#4-narrativeadmincontroller)
   - 4.1 [Shift Endpoints](#41-shift-endpoints)
   - 4.2 [StoryBeat Endpoints](#42-storybeat-endpoints)
   - 4.3 [Choice Endpoints](#43-choice-endpoints)
5. [NarrativeController](#5-narrativecontroller)
6. [PracticeAdminController](#6-practiceadmincontroller)
   - 6.1 [Practice Task Management](#61-practice-task-management)
   - 6.2 [Test Case Management](#62-test-case-management)
7. [PracticeController](#7-practicecontroller)
8. [DTO Reference](#8-dto-reference)
9. [Enum Reference](#9-enum-reference)
10. [Related Domain Entities](#10-related-domain-entities)
11. [Common Error Responses](#11-common-error-responses)
12. [Endpoint Quick Reference](#12-endpoint-quick-reference)
13. [Frontend Integration Examples](#13-frontend-integration-examples)
14. [cURL Examples](#14-curl-examples)
15. [Error Coverage Audit](#15-error-coverage-audit)
16. [Current Implementation Notes & Warnings](#16-current-implementation-notes--warnings)

---

## 1. Overview

These four controllers cover all **narrative story content** and **coding practice** functionality in the SHIFT game. They divide cleanly into two tiers:

### Admin / Content-Management Controllers

| Controller | What it manages |
|---|---|
| `NarrativeAdminController` | Shifts, StoryBeats, and Choices — authored game content |
| `PracticeAdminController` | Practice Tasks and Test Cases — educator-created coding exercises |

These controllers are intended for **instructors and administrators** who build and maintain the game's story and curriculum. They perform CRUD operations on static game content rather than runtime player state.

### Player / Runtime Controllers

| Controller | What it exposes |
|---|---|
| `NarrativeController` | Player shift lifecycle: start, save, end, get choices, submit choices |
| `PracticeController` | Player code submission, task retrieval, and gate progression |

These controllers are consumed by the **React frontend** during active gameplay. They read dynamic player state and write to runtime tables (`PlayerShiftProgress`, `PracticeAttempt`, `PlayerChoice`, etc.).

### Key Design Distinction

- **Narrative** endpoints serve **story delivery** — sequential beats, choices, and deferred consequences that form the workplace simulation.
- **Practice** endpoints serve **code execution** — the mandatory coding gates a player must pass to advance shifts.

---

## 2. Authentication & Authorization

### Current State

> ⚠️ **IMPORTANT — See Section 16 for full warnings.**

The project uses **JWT Bearer authentication** configured in `DependencyInjection.cs`:

```
POST /api/auth/login  →  access_token (15-minute JWT)  +  refresh_token
```

The JWT contains `ClaimTypes.NameIdentifier` (user/player ID), `ClaimTypes.Email`, and `ClaimTypes.Role`.

### Authorization per Controller (Actual State)

| Controller | `[Authorize]` attribute | Role enforcement |
|---|---|---|
| `NarrativeAdminController` | **None at class level** | `[Authorize(Roles = "Admin")]` is **commented out** |
| `NarrativeController` | **None at class level** | No `[Authorize]` anywhere in the controller |
| `PracticeAdminController` | `//[Authorize(Roles = "Admin")]` | Commented out — not enforced |
| `PracticeController` | **None at class level** | No `[Authorize]` anywhere |

**All four controllers are currently unauthenticated at the code level.** The `[Authorize]` attributes are either absent or commented out with `// TODO` notes.

### Player ID Source

All four controllers accept `playerId` as a **route parameter** (e.g., `{playerId:int}`). It is **not extracted from JWT claims** in the current implementation. This is explicitly flagged in source comments as a TODO.

### Error Responses for Auth

Because `[Authorize]` is not enforced, the following responses **do not currently apply** to these controllers:

- `401 Unauthorized` — will not be returned by these controllers
- `403 Forbidden` — will not be returned by these controllers

The global `UseAuthorization()` middleware is registered in `Program.cs` but has no effect on these controllers without controller-level `[Authorize]` attributes.

---

## 3. Controller Summary

| Controller | Base Route | Purpose | Authentication (Actual) | Intended Role |
|---|---|---|---|---|
| `NarrativeAdminController` | `/api/admin` | Manage Shifts, StoryBeats, Choices | **None enforced** | Admin / Super Admin (intended) |
| `NarrativeController` | `/api/narrative` | Player shift start/save/end, choice retrieval/submission | **None enforced** | Player (intended) |
| `PracticeAdminController` | `/api/admin/practice` | Manage Practice Tasks and Test Cases | **None enforced** | Admin (intended) |
| `PracticeController` | `/api/practice` | Player task retrieval, code submission | **None enforced** | Player (intended) |

---

## 4. NarrativeAdminController

**File:** `LoopGame/Controllers/NarrativeAdminController.cs`
**Base Route:** `/api/admin`
**Authentication:** Not enforced (see Section 16)

This controller injects `INarrativeService` and `IChoiceService`.

---

### 4.1 Shift Endpoints

---

#### GET /api/admin/shifts

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `NarrativeAdminController` |
| Action | `GetAllShifts` |
| HTTP Method | `GET` |
| Route | `/api/admin/shifts` |
| Authentication | Not enforced |
| Role | Admin (intended, not enforced) |
| Request Body | None |
| Response | `List<ShiftDto>` |
| Success Status | `200 OK` |

##### Purpose

Returns all shifts ordered by chapter number then shift number. Used by admin content management panels to browse the game's narrative structure.

##### Parameters

No route, query, or body parameters.

##### Request Body

None.

##### Success Response

**HTTP 200 OK**

```json
[
  {
    "shiftId": 1,
    "shiftNumber": 1,
    "chapterNumber": 1,
    "title": "Variables",
    "description": "Mohamed's first task at Loop...",
    "conceptTag": "Variables",
    "isCapstone": false
  },
  {
    "shiftId": 2,
    "shiftNumber": 2,
    "chapterNumber": 1,
    "title": "Conditionals",
    "description": "...",
    "conceptTag": "Conditionals",
    "isCapstone": false
  }
]
```

**Response Field Table — `ShiftDto`**

| Field | JSON Type | C# Type | Nullable | Description |
|---|---|---|---|---|
| `shiftId` | number | `int` | No | Unique shift identifier |
| `shiftNumber` | number | `int` | No | Position within the chapter |
| `chapterNumber` | number | `int` | No | Chapter this shift belongs to |
| `title` | string | `string` | No | Shift title |
| `description` | string | `string?` | Yes | Optional description |
| `conceptTag` | string | `Concept` enum as string | No | CS concept covered |
| `isCapstone` | boolean | `bool` | No | Whether this is a capstone shift |

##### Business Logic

1. Calls `_narrative.GetAllShifts()` on `NarrativeService`.
2. Queries all `Shift` rows from the database (no filter).
3. Orders by `ChapterNumber` ascending, then `ShiftNumber` ascending.
4. Maps entities to `ShiftDto` via Mapster.
5. Returns `200 OK` with the list.

##### Error Responses

This endpoint has no confirmed error paths from the current implementation (no parameters, no preconditions). The service always returns `Result.Success`.

---

#### GET /api/admin/shifts/{shiftId}

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `NarrativeAdminController` |
| Action | `GetShift` |
| HTTP Method | `GET` |
| Route | `/api/admin/shifts/{shiftId}` |
| Authentication | Not enforced |
| Role | Admin (intended, not enforced) |
| Request Body | None |
| Response | `ShiftDetailDto` |
| Success Status | `200 OK` |

##### Purpose

Returns a single shift with its full detail including all narrative and consequence beats. Used by admin to view/edit a specific shift's content.

##### Parameters

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `shiftId` | Route | `int` | Yes | The shift identifier |

##### Request Body

None.

##### Success Response

**HTTP 200 OK**

```json
{
  "shiftId": 1,
  "shiftNumber": 1,
  "chapterNumber": 1,
  "title": "Variables",
  "description": "Mohamed's first task at Loop...",
  "conceptTag": "Variables",
  "isCapstone": false,
  "unlockCondition": null,
  "createdAt": "2026-01-15T10:00:00Z",
  "narrativeBeats": [
    {
      "beatId": 1,
      "beatKey": "ch1_s1_first_task",
      "beatType": "Narrative",
      "sequenceOrder": 1,
      "app": "MailLoop",
      "senderName": "Youssef",
      "contentJson": {
        "text": "Subject: Tante Layla — First Build...",
        "avatar": "youssef",
        "soundEffect": null,
        "choices": null
      },
      "desktopEvent": null,
      "delaySeconds": 0.0,
      "hasChoices": true,
      "createdAt": "2026-01-15T10:00:00Z",
      "choices": null
    }
  ],
  "consequenceBeats": []
}
```

**Response Field Table — `ShiftDetailDto`**

| Field | JSON Type | C# Type | Nullable | Description |
|---|---|---|---|---|
| `shiftId` | number | `int` | No | Shift identifier |
| `shiftNumber` | number | `int` | No | Number within chapter |
| `chapterNumber` | number | `int` | No | Chapter number |
| `title` | string | `string` | No | Shift title |
| `description` | string | `string?` | Yes | Description |
| `conceptTag` | string | `Concept` enum | No | CS concept |
| `isCapstone` | boolean | `bool` | No | Capstone flag |
| `unlockCondition` | object\|null | `ShiftUnlockCondition?` | Yes | JSON unlock prerequisites |
| `createdAt` | string (ISO 8601) | `DateTime` | No | Creation timestamp |
| `narrativeBeats` | array | `List<BeatDto>` | No | Ordered narrative beats |
| `consequenceBeats` | array | `List<BeatDto>` | No | Consequence beats (unordered) |

**`unlockCondition` object (when not null):**

```json
{
  "prerequisiteShiftId": 1,
  "minRank": "Intern",
  "requiredConcept": "Variables",
  "minMasteryScore": 0.70
}
```

See `BeatDto` documentation in Section 8 for the beat structure.

##### Business Logic

1. Calls `_narrative.GetShift(shiftId)`.
2. Queries `Shift` with `StoryBeats` and `StoryBeats.Choices` eagerly loaded.
3. If shift not found → returns `Narrative.ShiftNotFound` error.
4. Maps shift to `ShiftDetailDto` via Mapster configuration that separates beats by type.
5. `NarrativeBeats` = beats where `BeatType == Narrative`, ordered by `SequenceOrder`.
6. `ConsequenceBeats` = beats where `BeatType == Consequence` (no ordering).

##### Error Responses

**404 Not Found — Shift Not Found**

Trigger: `shiftId` does not match any existing shift.

HTTP Mapping: `"Narrative.ShiftNotFound"` → `404` (confirmed in `ResultHttpMapping.cs`).

```json
{
  "code": "Narrative.ShiftNotFound",
  "description": "No shift exists with the given identifier."
}
```

---

#### POST /api/admin/shifts

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `NarrativeAdminController` |
| Action | `CreateShift` |
| HTTP Method | `POST` |
| Route | `/api/admin/shifts` |
| Authentication | Not enforced |
| Role | Admin / Super Admin (intended, not enforced) |
| Request Body | `CreateShiftDto` |
| Response | `ShiftDetailDto` |
| Success Status | `200 OK` |

##### Purpose

Creates a new shift in the game narrative. Does **not** create any player progress records. Used by content authors when adding new chapters or shifts.

##### Parameters

No route or query parameters.

##### Request Body

```json
{
  "shiftNumber": 1,
  "chapterNumber": 1,
  "title": "Variables",
  "description": "Mohamed's first task at Loop: build Tante Layla's menu.",
  "conceptTag": "Variables",
  "numberOfTasks": 3,
  "isCapstone": false,
  "unlockCondition": {
    "prerequisiteShiftId": null,
    "minRank": null,
    "requiredConcept": null,
    "minMasteryScore": null
  }
}
```

**Request Field Table — `CreateShiftDto`**

| Field | JSON Type | C# Type | Required | Description |
|---|---|---|---|---|
| `shiftNumber` | number | `int` | Yes | Shift number within the chapter |
| `chapterNumber` | number | `int` | Yes | Chapter number |
| `title` | string | `string` | Yes | Shift title (cannot be whitespace) |
| `description` | string | `string?` | No | Optional description |
| `conceptTag` | string | `Concept` enum | Yes | CS concept taught |
| `numberOfTasks` | number | `int` | Yes | How many practice tasks the shift has |
| `isCapstone` | boolean | `bool` | No | Defaults to `false` if omitted |
| `unlockCondition` | object\|null | `ShiftUnlockCondition?` | No | Prerequisite rules; `null` = freely accessible |

**`unlockCondition` fields:**

| Field | JSON Type | C# Type | Required | Description |
|---|---|---|---|---|
| `prerequisiteShiftId` | number\|null | `int?` | No | Shift that must be completed first |
| `minRank` | string\|null | `string?` | No | Minimum player rank required |
| `requiredConcept` | string\|null | `string?` | No | Concept that must meet the mastery threshold |
| `minMasteryScore` | number\|null | `decimal?` | No | Minimum mastery score (0.0 – 1.0) |

##### Success Response

**HTTP 200 OK** — Returns `ShiftDetailDto` (same structure as GET /api/admin/shifts/{shiftId}).

The returned `narrativeBeats` and `consequenceBeats` arrays will be empty for a newly created shift.

##### Service-Level Validation Rules

The following rules are enforced by `NarrativeService.CreateShift()`:

1. `title` must not be null or whitespace → `Narrative.ShiftTitleRequired`
2. `chapterNumber` must be `> 0` → `Narrative.InvalidChapterNumber`
3. `shiftNumber` must be `> 0` → `Narrative.InvalidShiftNumber`
4. The combination `(chapterNumber, shiftNumber)` must be unique → `Narrative.DuplicateShiftNumber`

##### Business Logic

1. Validates title, chapter, shift numbers.
2. Checks uniqueness of `(chapterNumber, shiftNumber)` in the database.
3. Maps `CreateShiftDto` → `Shift` entity via Mapster.
4. Sets `Title` and `Description` using `.Trim()`.
5. Sets `CreatedAt = DateTime.UtcNow`.
6. Persists the entity.
7. Returns `ShiftDetailDto` with empty beat lists.

##### Error Responses

**400 Bad Request — Title Required**

Trigger: `title` is null or whitespace.

HTTP Mapping: `"Narrative.ShiftTitleRequired"` → `400` (default in `ResultHttpMapping.cs`).

```json
{
  "code": "Narrative.ShiftTitleRequired",
  "description": "Shift title is required and cannot be empty."
}
```

**400 Bad Request — Invalid Chapter Number**

Trigger: `chapterNumber` is `<= 0`.

```json
{
  "code": "Narrative.InvalidChapterNumber",
  "description": "Chapter number must be greater than zero."
}
```

**400 Bad Request — Invalid Shift Number**

Trigger: `shiftNumber` is `<= 0`.

```json
{
  "code": "Narrative.InvalidShiftNumber",
  "description": "Shift number must be greater than zero."
}
```

**409 Conflict — Duplicate Shift Number**

Trigger: Another shift with the same `(chapterNumber, shiftNumber)` already exists.

HTTP Mapping: `"Narrative.DuplicateShiftNumber"` → `409` (confirmed in `ResultHttpMapping.cs`).

```json
{
  "code": "Narrative.DuplicateShiftNumber",
  "description": "A shift with this chapter number and shift number already exists."
}
```

---

#### PUT /api/admin/shifts/{shiftId}

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `NarrativeAdminController` |
| Action | `UpdateShift` |
| HTTP Method | `PUT` |
| Route | `/api/admin/shifts/{shiftId}` |
| Authentication | Not enforced |
| Role | Admin / Super Admin (intended, not enforced) |
| Request Body | `UpdateShiftDto` |
| Response | `ShiftDetailDto` |
| Success Status | `200 OK` |

##### Purpose

Updates editable shift metadata (title, numbers, unlock condition, capstone flag, etc.). Does **not** modify player runtime state. All fields are optional — only non-null values are applied (partial update).

##### Parameters

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `shiftId` | Route | `int` | Yes | The shift to update |

##### Request Body

All fields are optional. Send only the fields you want to update.

```json
{
  "shiftNumber": 2,
  "chapterNumber": 1,
  "title": "Updated Title",
  "description": "Updated description",
  "conceptTag": "Conditionals",
  "numberOfTasks": 3,
  "isCapstone": false,
  "unlockCondition": {
    "prerequisiteShiftId": 1,
    "minRank": null,
    "requiredConcept": "Variables",
    "minMasteryScore": 0.70
  },
  "clearUnlockCondition": false
}
```

**Request Field Table — `UpdateShiftDto`**

| Field | JSON Type | C# Type | Required | Description |
|---|---|---|---|---|
| `shiftNumber` | number\|null | `int?` | No | New shift number |
| `chapterNumber` | number\|null | `int?` | No | New chapter number |
| `title` | string\|null | `string?` | No | New title (cannot be whitespace if provided) |
| `description` | string\|null | `string?` | No | New description |
| `conceptTag` | string | `Concept` | No | CS concept (maps to enum) |
| `numberOfTasks` | number\|null | `int?` | No | Number of practice tasks |
| `isCapstone` | boolean\|null | `bool?` | No | Capstone flag |
| `unlockCondition` | object\|null | `ShiftUnlockCondition?` | No | New unlock condition |
| `clearUnlockCondition` | boolean | `bool` | No | Set to `true` to remove the unlock condition; defaults to `false` |

**`clearUnlockCondition` behavior:**
- `true` → sets `UnlockCondition = null` regardless of the `unlockCondition` field
- `false` (default) → `unlockCondition` field is applied if not null; otherwise existing value is kept

##### Success Response

**HTTP 200 OK** — Returns `ShiftDetailDto` (refreshed from database including beats).

##### Service-Level Validation Rules

1. Shift must exist → `Narrative.ShiftNotFound`
2. If `title` provided, must not be whitespace → `Narrative.ShiftTitleRequired`
3. If `chapterNumber` provided, must be `> 0` → `Narrative.InvalidChapterNumber`
4. If `shiftNumber` provided, must be `> 0` → `Narrative.InvalidShiftNumber`
5. If effective `(chapterNumber, shiftNumber)` changes, the new combination must be unique → `Narrative.DuplicateShiftNumber`

##### Business Logic

1. Loads the existing `Shift` by `FindWithTracking`.
2. Validates all provided values.
3. Checks uniqueness if either number is changing.
4. Applies each non-null field individually.
5. Applies `ClearUnlockCondition` sentinel.
6. Saves changes.
7. Re-fetches the shift (with beats) for the response.

##### Error Responses

Same error codes as `POST /api/admin/shifts` apply, plus:

**404 Not Found — Shift Not Found**

```json
{
  "code": "Narrative.ShiftNotFound",
  "description": "No shift exists with the given identifier."
}
```

(All other errors same as POST — see above.)

---

#### DELETE /api/admin/shifts/{shiftId}

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `NarrativeAdminController` |
| Action | `DeleteShift` |
| HTTP Method | `DELETE` |
| Route | `/api/admin/shifts/{shiftId}` |
| Authentication | Not enforced |
| Role | Admin / Super Admin (intended, not enforced) |
| Request Body | None |
| Response | Empty body |
| Success Status | `204 No Content` |

##### Purpose

Deletes a shift only if it is safe to do so. Blocked if the shift has player progress records or still has story beats. Returns `409 Conflict` instead of cascading when dependencies exist.

##### Parameters

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `shiftId` | Route | `int` | Yes | The shift to delete |

##### Request Body

None.

##### Success Response

**HTTP 204 No Content** — Empty body.

##### Business Logic

1. Loads the shift with `ShiftProgresses` and `StoryBeats` navigation properties.
2. If shift not found → `Narrative.ShiftNotFound`.
3. If `ShiftProgresses.Count > 0` → `Narrative.ShiftHasPlayerProgress` (player data preservation).
4. If `StoryBeats.Count > 0` → `Narrative.ShiftHasStoryBeats` (must delete/reassign beats first).
5. Deletes the shift and saves.

##### Error Responses

**404 Not Found — Shift Not Found**

```json
{
  "code": "Narrative.ShiftNotFound",
  "description": "No shift exists with the given identifier."
}
```

**409 Conflict — Shift Has Player Progress**

Trigger: One or more players have a `PlayerShiftProgress` record for this shift.

HTTP Mapping: `"Narrative.ShiftHasPlayerProgress"` → `409`.

```json
{
  "code": "Narrative.ShiftHasPlayerProgress",
  "description": "Cannot delete a shift that has player progress records. Historical data must be preserved."
}
```

**409 Conflict — Shift Has Story Beats**

Trigger: The shift still has associated `StoryBeat` rows.

HTTP Mapping: `"Narrative.ShiftHasStoryBeats"` → `409`.

```json
{
  "code": "Narrative.ShiftHasStoryBeats",
  "description": "Cannot delete a shift that still has story beats assigned to it. Reassign or delete the beats first."
}
```

---

### 4.2 StoryBeat Endpoints

---

#### GET /api/admin/beats/{beatId}

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `NarrativeAdminController` |
| Action | `GetBeat` |
| HTTP Method | `GET` |
| Route | `/api/admin/beats/{beatId}` |
| Authentication | Not enforced |
| Role | Admin (intended) |
| Request Body | None |
| Response | `BeatDto` |
| Success Status | `200 OK` |

##### Purpose

Returns a single story beat by ID including its choices.

##### Parameters

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `beatId` | Route | `int` | Yes | The beat identifier |

##### Success Response

**HTTP 200 OK**

```json
{
  "beatId": 1,
  "beatKey": "ch1_s1_first_task",
  "beatType": "Narrative",
  "sequenceOrder": 1,
  "app": "MailLoop",
  "senderName": "Youssef",
  "contentJson": {
    "text": "Subject: Tante Layla — First Build\nStart with the menu.",
    "avatar": "youssef",
    "soundEffect": null,
    "choices": null
  },
  "desktopEvent": null,
  "delaySeconds": 0.0,
  "hasChoices": true,
  "createdAt": "2026-01-15T10:00:00Z",
  "choices": [
    {
      "choiceId": 1,
      "beatId": 1,
      "choiceIndex": 1,
      "choiceText": "I'll use variables so prices can be changed easily.",
      "tier": "Ideal",
      "consequenceId": null,
      "immediateFeedback": null
    }
  ]
}
```

See Section 8 for full `BeatDto` and `ChoiceDto` field tables.

##### Error Responses

**404 Not Found — Beat Not Found**

```json
{
  "code": "Narrative.BeatNotFound",
  "description": "No story beat exists with the given identifier."
}
```

---

#### POST /api/admin/beats

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `NarrativeAdminController` |
| Action | `CreateBeat` |
| HTTP Method | `POST` |
| Route | `/api/admin/beats` |
| Authentication | Not enforced |
| Role | Admin / Super Admin (intended) |
| Request Body | `CreateStoryBeatDto` |
| Response | `BeatDto` |
| Success Status | `200 OK` |

##### Purpose

Creates a new StoryBeat. The behavior differs significantly based on `beatType`:

- **Narrative** beats: require `sequenceOrder`; `sequenceOrder` must be unique within the shift.
- **Consequence** beats: `sequenceOrder` must be null; `injectPosition` is required; a `Consequence` row is **automatically created** alongside the beat.

##### Parameters

No route or query parameters.

##### Request Body

**For a Narrative beat:**

```json
{
  "shiftId": 1,
  "beatKey": "ch1_s1_menu_file",
  "beatType": "Narrative",
  "sequenceOrder": 2,
  "app": "LoopCode",
  "senderName": "System",
  "contentJson": {
    "text": "Tante_Layla_Menu.txt\n\nCoffee - 50 EGP\nTea - 35 EGP",
    "avatar": null,
    "soundEffect": null,
    "choices": null
  },
  "desktopEvent": null,
  "delaySeconds": 0.0,
  "hasChoices": false,
  "injectPosition": null
}
```

**For a Consequence beat:**

```json
{
  "shiftId": 2,
  "beatKey": "ch1_s1_consequence_variables_learned",
  "beatType": "Consequence",
  "sequenceOrder": null,
  "app": "LoopCode",
  "senderName": "System",
  "contentJson": {
    "text": "Variable concept reinforced.",
    "avatar": null,
    "soundEffect": null,
    "choices": null
  },
  "desktopEvent": null,
  "delaySeconds": 0.0,
  "hasChoices": false,
  "injectPosition": "end"
}
```

**Request Field Table — `CreateStoryBeatDto`**

| Field | JSON Type | C# Type | Required | Description |
|---|---|---|---|---|
| `shiftId` | number | `int` | Yes | The shift this beat belongs to (for consequence beats, this is the **target** shift where it fires) |
| `beatKey` | string | `string` | Yes | Globally unique string key (immutable after creation) |
| `beatType` | string | `BeatType` enum | Yes | `"Narrative"` or `"Consequence"` |
| `sequenceOrder` | number\|null | `int?` | Conditional | Required for Narrative; must be `null` for Consequence |
| `app` | string | `BeatApp` enum | Yes | Which LoopOS app delivers this beat |
| `senderName` | string\|null | `string?` | No | Fictional character sender name |
| `contentJson` | object | `StoryBeatContent` | Yes | Beat content payload |
| `contentJson.text` | string | `string` | Yes | The beat text (required, cannot be whitespace) |
| `contentJson.avatar` | string\|null | `string?` | No | Avatar image key |
| `contentJson.soundEffect` | string\|null | `string?` | No | Sound effect key |
| `contentJson.choices` | array\|null | `List<BeatChoicePreview>?` | No | Preview buttons for choice rendering |
| `desktopEvent` | object\|null | `DesktopEvent?` | No | Optional LoopOS desktop side-effect |
| `delaySeconds` | number | `decimal` | No | Simulated typing delay (default 0) |
| `hasChoices` | boolean | `bool` | No | Whether choices should be shown (default false) |
| `injectPosition` | string\|null | `string?` | Conditional | Required for Consequence beats: `"start"` or `"end"` |

**`desktopEvent` object (when not null):**

| Field | JSON Type | C# Type | Required |
|---|---|---|---|
| `eventType` | string | `string` | Yes |
| `appName` | string\|null | `string?` | No |
| `notificationTitle` | string\|null | `string?` | No |
| `payload` | object\|null | `Dictionary<string, object>?` | No |

##### Success Response

**HTTP 200 OK** — Returns `BeatDto`. The `choices` array will be empty for a newly created beat.

For Consequence beats, the automatically created `Consequence` row is not exposed in the response. The `BeatDto` reflects the beat itself.

##### Service-Level Validation Rules

1. `shiftId` must reference an existing shift → `Narrative.ShiftNotFound`
2. `beatKey` must not be null/whitespace → `Narrative.BeatKeyRequired`
3. `beatKey` must be globally unique → `Narrative.DuplicateBeatKey`
4. `contentJson.text` must not be null/whitespace → `Narrative.ContentTextRequired`
5. For **Narrative** beats: `sequenceOrder` must be provided → `Narrative.SequenceOrderRequired`
6. For **Narrative** beats: `sequenceOrder` must be unique within the shift → `Narrative.SequenceOrderConflict`
7. For **Consequence** beats: `sequenceOrder` must be null → `Narrative.SequenceOrderNotRequiredForConsequenceBeat`
8. For **Consequence** beats: `injectPosition` must be `"start"` or `"end"` → `Narrative.InvalidInjectPosition`

##### Business Logic

1. Validates shift existence.
2. Validates beat key uniqueness.
3. Validates content.
4. Applies type-specific rules.
5. Opens a database transaction.
6. Inserts the `StoryBeat` row.
7. If `beatType == Consequence`: inserts a `Consequence` row linked to the new beat.
8. Commits transaction.
9. Re-fetches the beat with choices (empty for new beats).
10. Returns `BeatDto`.

##### Error Responses

**404 — Shift Not Found**

```json
{
  "code": "Narrative.ShiftNotFound",
  "description": "No shift exists with the given identifier."
}
```

**400 — Beat Key Required**

```json
{
  "code": "Narrative.BeatKeyRequired",
  "description": "Beat key is required and must be unique."
}
```

**409 — Duplicate Beat Key**

HTTP Mapping: `"Narrative.DuplicateBeatKey"` → `409`.

```json
{
  "code": "Narrative.DuplicateBeatKey",
  "description": "A story beat with this key already exists."
}
```

**400 — Content Text Required**

```json
{
  "code": "Narrative.ContentTextRequired",
  "description": "Beat content must include a non-empty text field."
}
```

**400 — Sequence Order Required (Narrative Beat)**

```json
{
  "code": "Narrative.SequenceOrderRequired",
  "description": "Narrative beats must have a sequence order. Consequence beats must not."
}
```

**409 — Sequence Order Conflict**

HTTP Mapping: `"Narrative.SequenceOrderConflict"` → `409`.

```json
{
  "code": "Narrative.SequenceOrderConflict",
  "description": "Another narrative beat already occupies this sequence order in the target shift."
}
```

**400 — Sequence Order Must Be Null (Consequence Beat)**

```json
{
  "code": "Narrative.SequenceOrderNotRequiredForConsequenceBeat",
  "description": "Consequence beats must not have a sequence order."
}
```

**400 — Invalid Inject Position**

```json
{
  "code": "Narrative.InvalidInjectPosition",
  "description": "InjectPosition must be 'start' or 'end'."
}
```

---

#### PUT /api/admin/beats/{beatId}

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `NarrativeAdminController` |
| Action | `UpdateBeat` |
| HTTP Method | `PUT` |
| Route | `/api/admin/beats/{beatId}` |
| Authentication | Not enforced |
| Role | Admin (intended) |
| Request Body | `UpdateStoryBeatDto` |
| Response | `BeatDto` |
| Success Status | `200 OK` |

##### Purpose

Updates editable beat fields. Validates ordering constraints and guards against breaking active player consequence queues. Does not modify historical `PlayerChoice` or `AssessmentEvent` records.

**Important:** `beatKey` is **immutable** after creation (not included in the update DTO).

##### Parameters

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `beatId` | Route | `int` | Yes | The beat to update |

##### Request Body

All fields are optional.

```json
{
  "shiftId": null,
  "beatType": null,
  "sequenceOrder": 5,
  "app": "WhatsUpp",
  "senderName": "Youssef",
  "contentJson": {
    "text": "Updated text content",
    "avatar": "youssef",
    "soundEffect": null,
    "choices": null
  },
  "desktopEvent": null,
  "delaySeconds": 1.5,
  "hasChoices": false,
  "injectPosition": null,
  "reorderSiblings": false
}
```

**Request Field Table — `UpdateStoryBeatDto`**

| Field | JSON Type | C# Type | Required | Description |
|---|---|---|---|---|
| `shiftId` | number\|null | `int?` | No | Move beat to a different shift |
| `beatType` | string\|null | `BeatType?` | No | Change beat type |
| `sequenceOrder` | number\|null | `int?` | No | New position (Narrative beats only) |
| `app` | string\|null | `BeatApp?` | No | New delivery app |
| `senderName` | string\|null | `string?` | No | New sender |
| `contentJson` | object\|null | `StoryBeatContent?` | No | New content |
| `desktopEvent` | object\|null | `DesktopEvent?` | No | New desktop event |
| `delaySeconds` | number\|null | `decimal?` | No | New delay |
| `hasChoices` | boolean\|null | `bool?` | No | Choice flag |
| `injectPosition` | string\|null | `string?` | No | For Consequence beats: `"start"` or `"end"` |
| `reorderSiblings` | boolean | `bool` | No | Caller acknowledgment that reordering is intentional (default `false`) |

##### Error Responses

**404 — Beat Not Found**

```json
{
  "code": "Narrative.BeatNotFound",
  "description": "No story beat exists with the given identifier."
}
```

**404 — Target Shift Not Found** (when `shiftId` is provided)

```json
{
  "code": "Narrative.ShiftNotFound",
  "description": "No shift exists with the given identifier."
}
```

**409 — Cannot Move Consequence Beat with Active Queues**

HTTP Mapping: `"Narrative.ConsequenceBeatCannotChangeShift"` → `409`.

```json
{
  "code": "Narrative.ConsequenceBeatCannotChangeShift",
  "description": "A consequence beat's shift is derived from its content. Moving it between shifts is not supported while the consequence is active."
}
```

**400 — Sequence Order Required (changing to Narrative type)**

```json
{
  "code": "Narrative.SequenceOrderRequired",
  "description": "Narrative beats must have a sequence order. Consequence beats must not."
}
```

**409 — Sequence Order Conflict**

```json
{
  "code": "Narrative.SequenceOrderConflict",
  "description": "Another narrative beat already occupies this sequence order in the target shift."
}
```

**400 — Invalid Inject Position**

```json
{
  "code": "Narrative.InvalidInjectPosition",
  "description": "InjectPosition must be 'start' or 'end'."
}
```

---

#### PUT /api/admin/beats/{beatId}/assign-shift/{shiftId}

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `NarrativeAdminController` |
| Action | `AssignBeatToShift` |
| HTTP Method | `PUT` |
| Route | `/api/admin/beats/{beatId}/assign-shift/{shiftId}` |
| Authentication | Not enforced |
| Role | Admin (intended) |
| Request Body | None |
| Response | `BeatDto` |
| Success Status | `200 OK` |

##### Purpose

Assigns (moves) an existing beat to a different shift. Validates shift existence and sequence order conflicts. Consequence beats with active queue entries cannot be moved.

##### Parameters

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `beatId` | Route | `int` | Yes | The beat to move |
| `shiftId` | Route | `int` | Yes | The target shift |

##### Request Body

None.

##### Error Responses

**404 — Beat Not Found**

```json
{
  "code": "Narrative.BeatNotFound",
  "description": "No story beat exists with the given identifier."
}
```

**409 — Beat Already in Target Shift**

HTTP Mapping: `"Narrative.BeatAlreadyInShift"` → `409`.

```json
{
  "code": "Narrative.BeatAlreadyInShift",
  "description": "The beat is already assigned to this shift."
}
```

**404 — Target Shift Not Found**

```json
{
  "code": "Narrative.ShiftNotFound",
  "description": "No shift exists with the given identifier."
}
```

**409 — Cannot Move Consequence Beat**

```json
{
  "code": "Narrative.ConsequenceBeatCannotChangeShift",
  "description": "A consequence beat's shift is derived from its content. Moving it between shifts is not supported while the consequence is active."
}
```

**409 — Sequence Order Conflict**

```json
{
  "code": "Narrative.SequenceOrderConflict",
  "description": "Another narrative beat already occupies this sequence order in the target shift."
}
```

---

#### DELETE /api/admin/beats/{beatId}

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `NarrativeAdminController` |
| Action | `DeleteBeat` |
| HTTP Method | `DELETE` |
| Route | `/api/admin/beats/{beatId}` |
| Authentication | Not enforced |
| Role | Admin (intended) |
| Request Body | None |
| Response | Empty body |
| Success Status | `204 No Content` |

##### Purpose

Deletes a beat only if safe. Blocked if the beat has pending `ConsequenceQueue` entries (player history protection). Choice rows and the linked `Consequence` row are deleted via database cascade.

##### Parameters

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `beatId` | Route | `int` | Yes | The beat to delete |

##### Business Logic

1. Loads beat with `Choices` and `Consequence` navigation properties.
2. If beat not found → `Narrative.BeatNotFound`.
3. If beat has choices: checks whether any linked consequence has pending queue entries → `Narrative.BeatHasActiveConsequenceQueues`.
4. If beat is a Consequence beat: checks whether its own `Consequence` row has pending queue entries → `Narrative.BeatHasActiveConsequenceQueues`.
5. Deletes the beat (cascade removes choices and consequence rows via FK cascade).

##### Error Responses

**404 — Beat Not Found**

```json
{
  "code": "Narrative.BeatNotFound",
  "description": "No story beat exists with the given identifier."
}
```

**409 — Beat Has Active Consequence Queues**

HTTP Mapping: `"Narrative.BeatHasActiveConsequenceQueues"` → `409`.

```json
{
  "code": "Narrative.BeatHasActiveConsequenceQueues",
  "description": "Cannot delete a beat that has pending or active consequence queue entries. Historical player data must be preserved."
}
```

---

### 4.3 Choice Endpoints

---

#### POST /api/admin/choices

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `NarrativeAdminController` |
| Action | `AddChoice` |
| HTTP Method | `POST` |
| Route | `/api/admin/choices` |
| Authentication | Not enforced |
| Role | Admin (intended) |
| Request Body | `List<CreateChoiceDto>` |
| Response | `List<ChoiceDto>` |
| Success Status | `200 OK` |

##### Purpose

Creates one or more choices for story beats. A beat can have at most 4 choices total. The endpoint accepts a batch to allow adding multiple choices in one request.

##### Request Body

```json
[
  {
    "beatId": 1,
    "choiceIndex": 1,
    "choiceText": "I'll use variables so prices can be changed easily.",
    "tier": "Ideal",
    "consequenceId": null,
    "immediateFeedback": "Smart move, Mohamed!"
  },
  {
    "beatId": 1,
    "choiceIndex": 2,
    "choiceText": "I'll just change them one by one.",
    "tier": "Mistake",
    "consequenceId": null,
    "immediateFeedback": null
  }
]
```

**Request Field Table — `CreateChoiceDto`** (per item in array)

| Field | JSON Type | C# Type | Required | Description |
|---|---|---|---|---|
| `beatId` | number | `int` | Yes | The beat this choice belongs to |
| `choiceIndex` | number | `byte` (1–4) | Yes | Button position (1–4) |
| `choiceText` | string | `string` | Yes | Text shown on the button |
| `tier` | string | `ChoiceTier` enum | Yes | `"Ideal"`, `"Acceptable"`, `"Debt"`, or `"Mistake"` |
| `consequenceId` | number\|null | `int?` | No | Links to a `Consequence` for deferred effects |
| `immediateFeedback` | string\|null | `string?` | No | Toast feedback shown immediately after selection |

##### Success Response

**HTTP 200 OK**

```json
[
  {
    "choiceId": 101,
    "beatId": 1,
    "choiceIndex": 1,
    "choiceText": "I'll use variables so prices can be changed easily.",
    "tier": "Ideal",
    "consequenceId": null,
    "immediateFeedback": "Smart move, Mohamed!"
  }
]
```

##### Service-Level Validation Rules (from `ChoiceService.AddChoice`)

1. Input list cannot be null or empty → `Choice.EmptyChoicesList`
2. Total list size cannot exceed 4 → `Choice.ExceedsMaxChoices`
3. For each choice:
   - `beatId` must be `> 0` → `Choice.InvalidId`
   - `choiceIndex` must be 1–4 → `Choice.InvalidChoiceIndex`
   - `choiceText` cannot be null/whitespace → `Choice.InvalidChoiceText`
4. No duplicate `(beatId, choiceIndex)` within the submitted batch → `Choice.DuplicateChoiceIndex`
5. For each unique `beatId`:
   - Beat must exist → `Choice.BeatNotFound`
   - Beat must have `hasChoices = true` → `Choice.NotAllowedToAddChoice`
   - Existing choices + new choices for this beat must not exceed 4 → `Choice.ExceedsMaxChoices`
   - New `choiceIndex` values must not conflict with existing choices on this beat → `Choice.DuplicateChoiceIndex`
6. If `consequenceId` is provided: the `Consequence` must exist → `Choice.InvalidConsequence`

##### Error Responses

**400 — Empty Choices List**

```json
{ "code": "Choice.EmptyChoicesList", "description": "The choice list cannot be empty." }
```

**400 — Exceeds Max Choices**

```json
{ "code": "Choice.ExceedsMaxChoices", "description": "A story beat cannot have more than 4 choices." }
```

**400 — Invalid ID**

```json
{ "code": "Choice.InvalidId", "description": "Invalid ID provided." }
```

**400 — Invalid Choice Index**

```json
{ "code": "Choice.InvalidChoiceIndex", "description": "Choice index must be between 1 and 4." }
```

**400 — Invalid Choice Text**

```json
{ "code": "Choice.InvalidChoiceText", "description": "Choice text cannot be empty." }
```

**400 — Duplicate Choice Index**

```json
{ "code": "Choice.DuplicateChoiceIndex", "description": "Duplicate choice index found for the story beat." }
```

**404 — Beat Not Found**

HTTP Mapping: `"Choice.BeatNotFound"` → `404`.

```json
{ "code": "Choice.BeatNotFound", "description": "No story beat exists with this identifier." }
```

**400 — Not Allowed to Add Choice**

```json
{ "code": "Choice.NotAllowedToAddChoice", "description": "Beat not allowed to add Choices" }
```

**400 — Invalid Consequence**

```json
{ "code": "Choice.InvalidConsequence", "description": "The specified consequence does not exist." }
```

---

#### PUT /api/admin/choices/{choiceId}

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `NarrativeAdminController` |
| Action | `UpdateChoice` |
| HTTP Method | `PUT` |
| Route | `/api/admin/choices/{choiceId}` |
| Authentication | Not enforced |
| Role | Admin (intended) |
| Request Body | `UpdateChoiceDto` |
| Response | `ChoiceDto` |
| Success Status | `200 OK` |

##### Parameters

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `choiceId` | Route | `int` | Yes | The choice to update |

##### Request Body

All fields optional. Note: `choiceIndex` cannot be changed (not in `UpdateChoiceDto`).

```json
{
  "choiceText": "Updated choice text",
  "tier": "Acceptable",
  "consequenceId": null,
  "immediateFeedback": "Good thinking!"
}
```

**Request Field Table — `UpdateChoiceDto`**

| Field | JSON Type | C# Type | Required | Description |
|---|---|---|---|---|
| `choiceText` | string\|null | `string?` | No | New text (cannot be whitespace if provided) |
| `tier` | string\|null | `ChoiceTier?` | No | New tier |
| `consequenceId` | number\|null | `int?` | No | New consequence reference |
| `immediateFeedback` | string\|null | `string?` | No | New feedback text |

##### Error Responses

**400 — Invalid ID**

```json
{ "code": "Choice.InvalidId", "description": "Invalid ID provided." }
```

**404 — Choice Not Found**

```json
{ "code": "Choice.ChoiceNotFound", "description": "No choice exists with this identifier." }
```

**400 — Invalid Choice Text**

```json
{ "code": "Choice.InvalidChoiceText", "description": "Choice text cannot be empty." }
```

**400 — Invalid Consequence**

```json
{ "code": "Choice.InvalidConsequence", "description": "The specified consequence does not exist." }
```

---

## 5. NarrativeController

**File:** `LoopGame/Controllers/NarrativeController.cs`
**Base Route:** `/api/narrative`
**Authentication:** Not enforced (see Section 16)

This controller injects `INarrativeService` and `IChoiceService`.

> ⚠️ **Player ID Warning:** All endpoints accept `playerId` as a route parameter. It is NOT extracted from JWT claims. Any user can supply any `playerId`.

---

#### POST /api/narrative/{playerId}/shifts/{shiftId}/start

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `NarrativeController` |
| Action | `StartShift` |
| HTTP Method | `POST` |
| Route | `/api/narrative/{playerId}/shifts/{shiftId}/start` |
| Authentication | Not enforced |
| Role | Player (intended) |
| Request Body | None |
| Response | `NarrativeFlowDto` |
| Success Status | `200 OK` |

##### Purpose

Loads the full narrative flow for a player starting or resuming a shift. This is the primary entry point for gameplay. It:

1. Validates the player exists and that `player.CurrentShiftId == shiftId`.
2. Fetches ordered narrative beats for the shift.
3. Finds pending consequence beats targeted at this shift and merges them (start/end injection).
4. Marks merged consequences as `fired`.
5. Creates a `PlayerShiftProgress` record if not yet existing.
6. Creates or resumes a `PlayerSave` record.
7. If resuming, skips already-seen beats based on the saved `BeatId`.

##### Parameters

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `playerId` | Route | `int` | Yes | The player's ID |
| `shiftId` | Route | `int` | Yes | The shift to start/resume |

##### Request Body

None.

##### Success Response

**HTTP 200 OK**

```json
{
  "shiftId": 1,
  "shift": {
    "shiftId": 1,
    "shiftNumber": 1,
    "chapterNumber": 1,
    "title": "Variables",
    "description": "...",
    "conceptTag": "Variables",
    "isCapstone": false
  },
  "beats": [
    {
      "beatId": 1,
      "beatKey": "ch1_s1_first_task",
      "beatType": "Narrative",
      "sequenceOrder": 1,
      "app": "MailLoop",
      "senderName": "Youssef",
      "contentJson": {
        "text": "Subject: Tante Layla — First Build...",
        "avatar": "youssef",
        "soundEffect": null,
        "choices": [
          { "index": 1, "text": "I'll use variables..." }
        ]
      },
      "desktopEvent": null,
      "delaySeconds": 0.0,
      "hasChoices": true,
      "createdAt": "2026-01-15T10:00:00Z",
      "choices": null
    }
  ]
}
```

**Response Field Table — `NarrativeFlowDto`**

| Field | JSON Type | C# Type | Nullable | Description |
|---|---|---|---|---|
| `shiftId` | number | `int` | No | The shift ID |
| `shift` | object | `ShiftDto` | No | Shift metadata |
| `beats` | array | `List<BeatDto>` | No | Ordered beats (narrative + injected consequences) to display |

##### Business Logic (detailed)

1. Validates player via `FindAsync(p => p.PlayerId == playerId, ["CurrentShift"])`.
2. If player not found → `Choice.PlayerNotFound`.
3. If `player.CurrentShiftId != shiftId` → `Choice.ShiftMismatch`.
4. Fetches shift entity.
5. Fetches all `Narrative` beats for the shift ordered by `SequenceOrder`.
6. Fetches pending `ConsequenceQueue` entries where `player_id = playerId`, `status = 'pending'`, and the consequence beat targets this shift.
7. Categorizes consequences as `start` (prepend) or `end` (append) by `InjectPosition`.
8. Marks all found consequences as `status = 'fired'`, sets `FiredAt = DateTime.UtcNow`.
9. Checks if `PlayerShiftProgress` exists for `(playerId, shiftId)`:
   - If **not**: creates a new `PlayerShiftProgress` (status `InProgress`) and a `PlayerSave` pointing to the first beat.
   - If **exists**: reads `PlayerSave.BeatId` and skips beats before that index (resume logic).
10. Saves all changes.
11. Returns merged beat list.

##### Error Responses

**404 — Player Not Found**

HTTP Mapping: `"Choice.PlayerNotFound"` → `404`.

```json
{
  "code": "Choice.PlayerNotFound",
  "description": "No player exists with this identifier."
}
```

**409 — Shift Mismatch**

HTTP Mapping: `"Choice.ShiftMismatch"` → `409`.

```json
{
  "code": "Choice.ShiftMismatch",
  "description": "The player's current shift does not match the story beat shift."
}
```

**404 — Shift Not Found**

```json
{
  "code": "Narrative.ShiftNotFound",
  "description": "No shift exists with the given identifier."
}
```

---

#### POST /api/narrative/{playerId}/shifts/{shiftId}/{beatId}/save

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `NarrativeController` |
| Action | `Save` |
| HTTP Method | `POST` |
| Route | `/api/narrative/{playerId}/shifts/{shiftId}/{beatId}/save` |
| Authentication | Not enforced |
| Role | Player (intended) |
| Request Body | None |
| Response | `NarrativeFlowDto` |
| Success Status | `200 OK` |

##### Purpose

Saves the player's current progress to the given beat within a shift. Updates or creates a `PlayerSave` record. Returns the remaining narrative flow starting from the saved beat.

##### Parameters

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `playerId` | Route | `int` | Yes | The player's ID |
| `shiftId` | Route | `int` | Yes | The shift being played |
| `beatId` | Route | `int` | Yes | The beat to save at |

##### Request Body

None.

##### Success Response

**HTTP 200 OK** — Returns `NarrativeFlowDto` with beats starting from the saved beat.

##### Business Logic

1. Validates player and shift access (same as StartShift).
2. Validates beat exists and belongs to the given shift.
3. Upserts `PlayerSave` (updates `BeatId` if exists, creates new if not).
4. Returns the narrative flow from that beat forward.

##### Error Responses

**404 — Player Not Found**

```json
{ "code": "Choice.PlayerNotFound", "description": "No player exists with this identifier." }
```

**409 — Shift Mismatch**

```json
{ "code": "Choice.ShiftMismatch", "description": "The player's current shift does not match the story beat shift." }
```

**404 — Beat Not Found**

```json
{ "code": "Narrative.BeatNotFound", "description": "No story beat exists with the given identifier." }
```

---

#### POST /api/narrative/{playerId}/shifts/{shiftId}/end

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `NarrativeController` |
| Action | `Save` (overloaded action name, different route) |
| HTTP Method | `POST` |
| Route | `/api/narrative/{playerId}/shifts/{shiftId}/end` |
| Authentication | Not enforced |
| Role | Player (intended) |
| Request Body | None |
| Response | `object { msg: string }` |
| Success Status | `200 OK` |

##### Purpose

Ends the current shift for a player. Validates that the shift gate has been cleared (`IsGateCleared == true`). Advances the player's `CurrentShiftId` to the next shift. Resets the `PlayerSave` beat pointer to 1.

##### Parameters

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `playerId` | Route | `int` | Yes | The player's ID |
| `shiftId` | Route | `int` | Yes | The shift being ended |

##### Request Body

None.

##### Success Response

**HTTP 200 OK**

```json
{
  "msg": "End Shift Success!"
}
```

##### Business Logic

1. Validates player and shift access.
2. Loads `PlayerShiftProgress` for `(playerId, shiftId)`.
3. If progress not found → `Narrative.ShiftNotFound`.
4. If `!shiftProgress.IsGateCleared` → `Narrative.ShiftNotCompleted`.
5. Finds the next shift by `ShiftNumber = currentShift.ShiftNumber + 1`.
6. Updates `player.CurrentShiftId` to the next shift's ID.
7. Resets `PlayerSave.BeatId = 1`.
8. Saves changes.

> ⚠️ **Note:** If no next shift exists (player is on the last shift), this will throw a null reference exception because the code does not guard against `nextshift == null`.

##### Error Responses

**404 — Player Not Found**

```json
{ "code": "Choice.PlayerNotFound", "description": "No player exists with this identifier." }
```

**409 — Shift Mismatch**

```json
{ "code": "Choice.ShiftMismatch", "description": "The player's current shift does not match the story beat shift." }
```

**404 — Shift Not Found** (progress not found)

```json
{ "code": "Narrative.ShiftNotFound", "description": "No shift exists with the given identifier." }
```

**400 — Shift Not Completed** (gate not cleared)

HTTP Mapping: `"Narrative.ShiftNotCompleted"` → `400` (default mapping).

```json
{ "code": "Narrative.ShiftNotCompleted", "description": "Shift Not Completed." }
```

---

#### GET /api/narrative/{playerId}/beats/{beatId}/choices

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `NarrativeController` |
| Action | `GetChoices` |
| HTTP Method | `GET` |
| Route | `/api/narrative/{playerId}/beats/{beatId}/choices` |
| Authentication | Not enforced |
| Role | Player (intended) |
| Request Body | None |
| Response | `List<ChoiceDto>` |
| Success Status | `200 OK` |

##### Purpose

Retrieves all choices available to the player for a specific story beat. Validates that the player's current shift matches the beat's shift.

##### Parameters

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `playerId` | Route | `int` | Yes | The player's ID |
| `beatId` | Route | `int` | Yes | The beat to get choices for |

##### Success Response

**HTTP 200 OK**

```json
[
  {
    "choiceId": 101,
    "beatId": 1,
    "choiceIndex": 1,
    "choiceText": "I'll use variables so prices can be changed easily.",
    "tier": "Ideal",
    "consequenceId": null,
    "immediateFeedback": "Smart move!"
  },
  {
    "choiceId": 102,
    "beatId": 1,
    "choiceIndex": 2,
    "choiceText": "I'll just change them one by one.",
    "tier": "Mistake",
    "consequenceId": null,
    "immediateFeedback": null
  }
]
```

##### Business Logic

1. Validates player exists.
2. Validates beat exists with `Choices` loaded.
3. Validates `player.CurrentShiftId == beat.ShiftId`.
4. Maps `beat.Choices` to `List<ChoiceDto>`.

##### Error Responses

**400 — Invalid ID**

```json
{ "code": "Choice.InvalidId", "description": "Invalid ID provided." }
```

**404 — Player Not Found**

```json
{ "code": "Choice.PlayerNotFound", "description": "No player exists with this identifier." }
```

**404 — Beat Not Found**

```json
{ "code": "Choice.BeatNotFound", "description": "No story beat exists with this identifier." }
```

**409 — Shift Mismatch**

```json
{ "code": "Choice.ShiftMismatch", "description": "The player's current shift does not match the story beat shift." }
```

---

#### POST /api/narrative/{playerId}/choices/{choiceId}/submit

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `NarrativeController` |
| Action | `SubmitChoice` |
| HTTP Method | `POST` |
| Route | `/api/narrative/{playerId}/choices/{choiceId}/submit` |
| Authentication | Not enforced |
| Role | Player (intended) |
| Request Body | None |
| Response | `ChoiceDto` |
| Success Status | `200 OK` |

##### Purpose

Submits a player's choice selection for a story beat. This is a significant gameplay action that:

1. Records the choice as a `PlayerChoice` (immutable audit record).
2. Optionally queues a `ConsequenceQueue` entry if the choice has a linked consequence.
3. Increments the player's `GateAttempts` counter in `PlayerShiftProgress`.
4. Emits a `choice_submission` assessment event via `IEventPublisher`.

##### Parameters

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `playerId` | Route | `int` | Yes | The player's ID |
| `choiceId` | Route | `int` | Yes | The choice selected |

##### Request Body

None.

##### Success Response

**HTTP 200 OK** — Returns the `ChoiceDto` of the submitted choice.

```json
{
  "choiceId": 101,
  "beatId": 1,
  "choiceIndex": 1,
  "choiceText": "I'll use variables so prices can be changed easily.",
  "tier": "Ideal",
  "consequenceId": null,
  "immediateFeedback": "Smart move!"
}
```

##### Business Logic

1. Loads player with `ShiftProgresses`.
2. Loads choice with `Beat.Shift`.
3. Validates `choice.Beat.ShiftId == player.CurrentShiftId` → `Forbidden.Access` if mismatch.
4. Inserts `PlayerChoice { PlayerId, ChoiceId, BeatId, Tier }`.
5. If `choice.ConsequenceId != null` → inserts `ConsequenceQueue { PlayerId, ConsequenceId, status = pending }`.
6. Increments `playerProgress.GateAttempts++` (the first matching `PlayerShiftProgress` for `CurrentShiftId`).
7. Saves all changes.
8. Fires `choice_submission` assessment event (fire-and-forget via `IEventPublisher`).
9. Returns the choice as `ChoiceDto`.

##### Error Responses

**403 — Forbidden Access** (shift mismatch on choice submit)

HTTP Mapping: `"Forbidden.Access"` → `403`.

```json
{
  "code": "Forbidden.Access",
  "description": "You are not allowed to access this choice."
}
```

> Note: Player and choice validation failures throw null references if not found (no explicit guard). This is a bug in the current implementation — see Section 16.

---

## 6. PracticeAdminController

**File:** `LoopGame/Controllers/PracticeAdminController.cs`
**Base Route:** `/api/admin/practice`
**Authentication:** Not enforced (`[Authorize(Roles = "Admin")]` is commented out)

This controller injects `IPracticeService`.

---

### 6.1 Practice Task Management

---

#### GET /api/admin/practice/tasks

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `PracticeAdminController` |
| Action | `GetTask` |
| HTTP Method | `GET` |
| Route | `/api/admin/practice/tasks` |
| Authentication | Not enforced |
| Role | Admin (intended) |
| Request Body | None |
| Response | `List<PracticeDto>` (note: signature says `PracticeDto` but service returns `List<PracticeDto>`) |
| Success Status | `200 OK` |

> ⚠️ **Note:** The action has two unused `[FromQuery]` parameters (`playerId`, `taskId`) that are declared in the method signature but are not used by the service call. The comment says "Must be improve Performance to this method." The actual call is `_practiceService.GetTasks()` with no arguments.

##### Purpose

Returns all practice tasks across all shifts, ordered by `ShiftId` then `TaskOrder`. Includes test cases and shift metadata. Used by admin to manage the full curriculum.

##### Parameters

The action declares `int playerId` and `int taskId` parameters but **does not use them**. They have no effect on the response.

##### Success Response

**HTTP 200 OK**

```json
[
  {
    "taskId": 1,
    "shiftId": 1,
    "taskOrder": 1,
    "title": "Store Coffee Price",
    "description": "Create an integer variable named coffeePrice and print it.",
    "starterCode": "#include <stdio.h>\n\nint main() {\n    // Write your solution here\n    return 0;\n}",
    "conceptTag": "Variables",
    "difficulty": "SpacedRetrieval",
    "maxAttempts": 0,
    "egpReward": 20.0,
    "createdAt": "2026-01-15T10:00:00Z",
    "shiftNumber": 1,
    "testCases": [
      {
        "testCaseId": 1,
        "taskId": 1,
        "sideTaskId": null,
        "testInput": "",
        "expectedOutput": "Coffee - 45 EGP",
        "isHidden": false,
        "description": "coffeePrice should be 45."
      }
    ]
  }
]
```

**Response Field Table — `PracticeDto`**

| Field | JSON Type | C# Type | Nullable | Description |
|---|---|---|---|---|
| `taskId` | number | `int` | No | Task identifier |
| `shiftId` | number | `int` | No | Owning shift |
| `taskOrder` | number | `byte` | No | Order within the shift gate |
| `title` | string | `string` | No | Task title |
| `description` | string | `string` | No | Task description |
| `starterCode` | string\|null | `string?` | Yes | Starter code template |
| `conceptTag` | string | `Concept` enum | No | CS concept |
| `difficulty` | string | `string` | No | `"SpacedRetrieval"`, `"Standard"`, or `"Challenge"` |
| `maxAttempts` | number | `short` | No | 0 = unlimited |
| `egpReward` | number | `decimal` | No | EGP reward on completion |
| `createdAt` | string (ISO 8601) | `DateTime` | No | Creation time |
| `shiftNumber` | number | `int` | No | Shift number (denormalized from join) |
| `testCases` | array\|null | `List<TestCaseDto>?` | Yes | Test cases (admin view includes all) |

##### Error Responses

This endpoint always returns successfully from the current implementation (no error paths in `GetTasks()`).

---

#### POST /api/admin/practice/tasks

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `PracticeAdminController` |
| Action | `AddPracticeTask` |
| HTTP Method | `POST` |
| Route | `/api/admin/practice/tasks` |
| Authentication | Not enforced |
| Role | Admin (intended) |
| Request Body | `CreatePracticeDto` |
| Response | `PracticeDto` |
| Success Status | `200 OK` |

##### Purpose

Creates a new practice task and optionally associates test cases with it. Tasks are linked to a specific shift.

##### Request Body

```json
{
  "shiftId": 1,
  "taskOrder": 1,
  "title": "Store Coffee Price",
  "description": "Create an integer variable named coffeePrice and print it.",
  "starterCode": "#include <stdio.h>\n\nint main() {\n    // Write your solution here\n    return 0;\n}",
  "conceptTag": "Variables",
  "difficulty": "SpacedRetrieval",
  "maxAttempts": 0,
  "egpReward": 20.0,
  "testCases": [
    {
      "testCaseId": 0,
      "taskId": null,
      "sideTaskId": null,
      "testInput": "",
      "expectedOutput": "Coffee - 45 EGP",
      "isHidden": false,
      "description": "coffeePrice should be 45."
    }
  ]
}
```

**Request Field Table — `CreatePracticeDto`**

| Field | JSON Type | C# Type | Required | Description |
|---|---|---|---|---|
| `shiftId` | number | `int` | Yes | Owning shift |
| `taskOrder` | number | `int` | Yes | Order within shift (1–255) |
| `title` | string | `string` | Yes | Task title |
| `description` | string | `string` | Yes | Task description |
| `starterCode` | string\|null | `string?` | No | Optional starter code |
| `conceptTag` | string | `Concept` enum | Yes | CS concept |
| `difficulty` | string | `string` | No | Default: `"Standard"` |
| `maxAttempts` | number | `int` | No | Default: `0` (unlimited) |
| `egpReward` | number | `decimal` | No | Default: `0` |
| `testCases` | array\|null | `List<TestCaseDto>?` | No | Test cases to attach |

##### Success Response

**HTTP 200 OK** — Returns `PracticeDto`.

##### Service-Level Validation Rules

1. `shiftId` must reference an existing shift → `NotFound.Shift`
2. `taskOrder` must be between 1 and 255 → `Task.InvalidTaskOrder`
3. `title` must not be null/whitespace → `Task.InvalidTitle`
4. `description` must not be null/whitespace → `Task.InvalidDescription`
5. `conceptTag` must be a defined `Concept` enum value → `Task.InvalidConceptTag`
6. `egpReward` must be `> 0` → `Task.NegativeEgpReward`
7. `maxAttempts` must be between 0 and 32767 → `Task.MaxAttemptsInvalid`

##### Error Responses

**404 — Shift Not Found**

HTTP Mapping: `"NotFound.Shift"` → `400` (default mapping, not 404).

```json
{ "code": "NotFound.Shift", "description": "Shift Not Found." }
```

**400 — Invalid Task Order**

```json
{ "code": "Task.InvalidTaskOrder", "description": "TaskOrder must be between 1 and 255." }
```

**400 — Invalid Title**

```json
{ "code": "Task.InvalidTitle", "description": "Title is required and cannot be empty." }
```

**400 — Invalid Description**

```json
{ "code": "Task.InvalidDescription", "description": "Description is required and cannot be empty." }
```

**400 — Invalid Concept Tag**

```json
{ "code": "Task.InvalidConceptTag", "description": "ConceptTag must be a valid Concept value." }
```

**400 — Negative EGP Reward**

```json
{ "code": "Task.NegativeEgpReward", "description": "EgpReward Must be Positive number." }
```

**400 — Invalid Max Attempts**

```json
{ "code": "Task.MaxAttemptsInvalid", "description": "MaxAttempts must be between 0 and 32767." }
```

---

#### PUT /api/admin/practice/tasks/{taskId}

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `PracticeAdminController` |
| Action | `UpdatePracticeTask` |
| HTTP Method | `PUT` |
| Route | `/api/admin/practice/tasks/{taskId}` |
| Authentication | Not enforced |
| Role | Admin (intended) |
| Request Body | `UpdatePracticeDto` |
| Response | `PracticeDto` |
| Success Status | `200 OK` |

##### Parameters

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `taskId` | Route | `int` | Yes | The task to update |

##### Request Body

All fields optional.

```json
{
  "taskOrder": 2,
  "title": "Updated Title",
  "description": "Updated description",
  "starterCode": "#include <stdio.h>",
  "conceptTag": "Variables",
  "difficulty": "Challenge",
  "maxAttempts": 5,
  "egpReward": 30.0
}
```

**Request Field Table — `UpdatePracticeDto`**

| Field | JSON Type | C# Type | Required | Description |
|---|---|---|---|---|
| `taskOrder` | number\|null | `byte?` | No | New task order |
| `title` | string\|null | `string?` | No | New title |
| `description` | string\|null | `string?` | No | New description |
| `starterCode` | string\|null | `string?` | No | New starter code |
| `conceptTag` | string\|null | `Concept?` | No | New concept |
| `difficulty` | string\|null | `string?` | No | New difficulty |
| `maxAttempts` | number\|null | `short?` | No | New max attempts |
| `egpReward` | number\|null | `decimal?` | No | New EGP reward |

##### Error Responses

**404 — Task Not Found**

HTTP Mapping: `"NotFound.Task"` → `400` (default mapping, not 404 — see Section 16).

```json
{ "code": "NotFound.Task", "description": "The requested practice task was not found." }
```

**400 — Invalid Concept Tag**

```json
{ "code": "Task.InvalidConceptTag", "description": "ConceptTag must be a valid Concept value." }
```

---

### 6.2 Test Case Management

---

#### POST /api/admin/practice/testcases

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `PracticeAdminController` |
| Action | `AddTestCases` |
| HTTP Method | `POST` |
| Route | `/api/admin/practice/testcases` |
| Authentication | Not enforced |
| Role | Admin (intended) |
| Request Body | `List<TestCaseDto>` |
| Response | `List<TestCaseDto>` |
| Success Status | `200 OK` |

##### Purpose

Adds one or more test cases to existing practice tasks. Each test case must be linked to exactly one task (`taskId` or `sideTaskId`).

##### Request Body

```json
[
  {
    "testCaseId": 0,
    "taskId": 1,
    "sideTaskId": null,
    "testInput": "",
    "expectedOutput": "Coffee - 45 EGP",
    "isHidden": false,
    "description": "coffeePrice should be 45."
  },
  {
    "testCaseId": 0,
    "taskId": 1,
    "sideTaskId": null,
    "testInput": "",
    "expectedOutput": "Coffee - 40 EGP",
    "isHidden": true,
    "description": "Changing the variable value should change the printed result."
  }
]
```

**Request Field Table — `TestCaseDto`** (per item)

| Field | JSON Type | C# Type | Required | Description |
|---|---|---|---|---|
| `testCaseId` | number | `int` | No | Ignored on create (auto-generated) |
| `taskId` | number\|null | `int?` | Conditional | Required if `sideTaskId` is null |
| `sideTaskId` | number\|null | `int?` | Conditional | Required if `taskId` is null |
| `testInput` | string\|null | `string?` | Yes | Input to feed the program (can be empty string) |
| `expectedOutput` | string\|null | `string?` | Yes | Expected stdout output |
| `isHidden` | boolean | `bool` | No | Hidden from players; default `false` |
| `description` | string\|null | `string?` | No | Human-readable description |

##### Service-Level Validation Rules

1. List cannot be null/empty → `TestCases.Empty`
2. For each item: `taskId` must be provided and `> 0` → `TestCase.InvalidTaskId`
3. For each item: `testInput` cannot be null → `TestCase.InvalidTestInput`
4. For each item: `expectedOutput` cannot be null/whitespace → `TestCase.InvalidExpectedOutput`
5. For each item: both `taskId` and `sideTaskId` cannot both be non-null → `TestCase.TestCaseDuplicate`
6. For each unique `taskId`: task must exist → `NotFound.Task`
7. Each test case is created with `SideTaskId = null` (enforced regardless of input)

##### Error Responses

**400 — Test Cases Empty**

```json
{ "code": "TestCases.Empty", "description": "TestCases cannot be empty." }
```

**400 — Invalid Task ID**

```json
{ "code": "TestCase.InvalidTaskId", "description": "TaskId is required and must be a positive number." }
```

**400 — Invalid Test Input**

```json
{ "code": "TestCase.InvalidTestInput", "description": "TestInput is required." }
```

**400 — Invalid Expected Output**

```json
{ "code": "TestCase.InvalidExpectedOutput", "description": "ExpectedOutput is required and cannot be empty." }
```

**400 — Test Case Duplicate**

```json
{ "code": "TestCase.TestCaseDuplicate", "description": "TestCase Cannot be duplicated at [ PracticeTask and SideTask ]" }
```

**400 — Task Not Found**

```json
{ "code": "NotFound.Task", "description": "The requested practice task was not found." }
```

---

#### PUT /api/admin/practice/testcases/{testId}

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `PracticeAdminController` |
| Action | `UpdateTestCase` |
| HTTP Method | `PUT` |
| Route | `/api/admin/practice/testcases/{testId}` |
| Authentication | Not enforced |
| Role | Admin (intended) |
| Request Body | `TestCaseDto` |
| Response | `TestCaseDto` |
| Success Status | `200 OK` |

##### Parameters

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `testId` | Route | `int` | Yes | The test case to update |

##### Request Body

```json
{
  "testCaseId": 1,
  "taskId": 1,
  "sideTaskId": null,
  "testInput": "5",
  "expectedOutput": "Coffee - 5 EGP",
  "isHidden": false,
  "description": "Updated description"
}
```

Only `testInput`, `description`, `expectedOutput`, and `isHidden` are applied by the service. `taskId` and `sideTaskId` in the body have no effect on which task the test case belongs to (the service uses the route `testId` to find the existing test case).

##### Error Responses

**404 — Test Case Not Found**

HTTP Mapping: `"NotFound.TestCase"` → `400` (default mapping — see Section 16).

```json
{ "code": "NotFound.TestCase", "description": "TestCase was not found." }
```

---

## 7. PracticeController

**File:** `LoopGame/Controllers/PracticeController.cs`
**Base Route:** `/api/practice`
**Authentication:** Not enforced

---

#### GET /api/practice/{playerId}/task/{taskId}

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `PracticeController` |
| Action | `GetTask` |
| HTTP Method | `GET` |
| Route | `/api/practice/{playerId}/task/{taskId}` |
| Authentication | Not enforced |
| Role | Player (intended) |
| Request Body | None |
| Response | `PracticeDto` |
| Success Status | `200 OK` |

##### Purpose

Retrieves a practice task for a player. Performs access validation to ensure the player may access this task. Hidden test cases are **filtered out** — only visible (`isHidden == false`) test cases are returned.

##### Parameters

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `playerId` | Route | `int` | Yes | The player's ID |
| `taskId` | Route | `int` | Yes | The practice task ID |

##### Request Body

None.

##### Success Response

**HTTP 200 OK**

```json
{
  "taskId": 1,
  "shiftId": 1,
  "taskOrder": 1,
  "title": "Store Coffee Price",
  "description": "Create an integer variable named coffeePrice and print it.",
  "starterCode": "#include <stdio.h>\n\nint main() {\n    // Write your solution here\n    return 0;\n}",
  "conceptTag": "Variables",
  "difficulty": "SpacedRetrieval",
  "maxAttempts": 0,
  "egpReward": 20.0,
  "createdAt": "2026-01-15T10:00:00Z",
  "shiftNumber": 1,
  "testCases": [
    {
      "testCaseId": 1,
      "taskId": 1,
      "sideTaskId": null,
      "testInput": "",
      "expectedOutput": "Coffee - 45 EGP",
      "isHidden": false,
      "description": "coffeePrice should be 45."
    }
  ]
}
```

Hidden test cases are excluded from the `testCases` array.

##### Business Logic

1. Calls `_practiceService.GetTaskAsync(taskId, playerId)`.
2. Calls `_accessService.ValidateAccessAsync(playerId, taskId)` which:
   - Loads the player with `CurrentShift.PracticeTasks` and `ShiftProgresses`.
   - Checks player exists → `Forbidden.AccessGame`
   - Checks player has an active shift → `Forbidden.Access`
   - Checks the task belongs to the current shift → `Forbidden.Access`
   - Checks a `PlayerShiftProgress` exists → `NotFound.Progress`
3. Loads the task with `TestCases` and `Shift`.
4. If task not found → `NotFound.Task`.
5. Filters `testCases` where `!isHidden`.
6. Returns `PracticeDto`.

##### Error Responses

**400 — Player Not Found (access denied)**

HTTP Mapping: `"Forbidden.AccessGame"` → `400` (default mapping).

```json
{ "code": "Forbidden.AccessGame", "description": "You are not allowed to access this game." }
```

**403 — No Active Shift**

HTTP Mapping: `"Forbidden.Access"` → `403`.

```json
{ "code": "Forbidden.Access", "description": "Player has no active shift." }
```

**403 — Task Not In Current Shift**

```json
{ "code": "Forbidden.Access", "description": "You are not allowed to access this task." }
```

**400 — Progress Not Found**

HTTP Mapping: `"NotFound.Progress"` → `400` (default mapping).

```json
{ "code": "NotFound.Progress", "description": "Player shift progress record was not found." }
```

**400 — Task Not Found**

HTTP Mapping: `"NotFound.Task"` → `400` (default mapping).

```json
{ "code": "NotFound.Task", "description": "The requested practice task was not found." }
```

---

#### POST /api/practice/{playerId}/submit

##### Endpoint Information

| Property | Value |
|---|---|
| Controller | `PracticeController` |
| Action | `SubmitCode` |
| HTTP Method | `POST` |
| Route | `/api/practice/{playerId}/submit` |
| Authentication | Not enforced |
| Role | Player (intended) |
| Request Body | `CodeSubmitRequestDto` |
| Response | `CodeSubmitResponseDto` |
| Success Status | `200 OK` |

##### Purpose

The core gameplay action for the Practice Gate system. Submits player-written C code for evaluation against all test cases (including hidden ones). Determines a `ChoiceTier`, records the attempt, updates gate progression, applies EGP reward, and emits assessment events.

##### Parameters

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `playerId` | Route | `int` | Yes | The player's ID |

##### Request Body

```json
{
  "taskId": 1,
  "submittedCode": "#include <stdio.h>\n\nint main() {\n    int coffeePrice = 45;\n    printf(\"Coffee - %d EGP\\n\", coffeePrice);\n    return 0;\n}",
  "timeSpentSec": 120,
  "hintUsed": false
}
```

**Request Field Table — `CodeSubmitRequestDto`**

| Field | JSON Type | C# Type | Required | Description |
|---|---|---|---|---|
| `taskId` | number | `int` | Yes | The practice task ID being submitted against |
| `submittedCode` | string | `string` | Yes | The player's C source code |
| `timeSpentSec` | number | `int` | Yes | Time spent in seconds |
| `hintUsed` | boolean | `bool` | Yes | Whether a hint was used |

##### Success Response

**HTTP 200 OK**

```json
{
  "tier": "Ideal",
  "testResults": "[{\"testCaseId\":1,\"passed\":true,\"actualOutput\":\"Coffee - 45 EGP\\n\",\"executionTimeMs\":12},{\"testCaseId\":2,\"passed\":true,\"actualOutput\":\"Coffee - 40 EGP\\n\",\"executionTimeMs\":11}]",
  "gateCleared": true,
  "egpEarned": 0.0,
  "newBalance": null,
  "struggleDetected": false,
  "maxAttemptsReached": false
}
```

> ⚠️ **Note on `testResults`:** This field is a **JSON string** (serialized JSON within JSON), not a nested object. It contains the serialized `List<TestCaseResult>`. The frontend must `JSON.parse(response.testResults)` to get the array.

> ⚠️ **Note on `egpEarned` and `newBalance`:** In the current implementation, EGP is applied via `ApplyEgpDeltaAsync`, but `egpEarned` is always `0.0` in the response (field is not set on the DTO), and `newBalance` is always `null` (not set). This is a known incompleteness in the response mapping.

**Response Field Table — `CodeSubmitResponseDto`**

| Field | JSON Type | C# Type | Nullable | Description |
|---|---|---|---|---|
| `tier` | string | `ChoiceTier` enum | No | `"Ideal"`, `"Acceptable"`, `"Debt"`, or `"Mistake"` |
| `testResults` | string | `string?` (JSON serialized) | Yes | Serialized JSON array of test case results |
| `gateCleared` | boolean | `bool` | No | Whether the shift gate was cleared this submission |
| `egpEarned` | number | `decimal` | No | EGP earned (always `0.0` in current implementation — see warning) |
| `newBalance` | number\|null | `decimal?` | Yes | New balance after reward (always `null` in current implementation) |
| `struggleDetected` | boolean | `bool` | No | `true` if player has made more than 4 attempts on this task |
| `maxAttemptsReached` | boolean | `bool` | No | Always `false` in current implementation (field not set) |

**`testResults` parsed structure (after `JSON.parse`):**

```json
[
  {
    "testCaseId": 1,
    "passed": true,
    "actualOutput": "Coffee - 45 EGP\n",
    "executionTimeMs": 12
  }
]
```

(These are `TestCaseResult` value objects from `LoopGame.Domain.ValueObjects`.)

##### Business Logic (Step-by-Step)

1. **Access Validation** — calls `_accessService.ValidateAccessAsync(PlayerId, code.TaskId)`:
   - Loads player, validates shift, validates task belongs to shift, validates progress record.
2. **Load Task** — fetches `PracticeTask` with `TestCases` (ALL, including hidden) and `Shift`.
3. **Attempt Policy** — calls `_attemptPolicy.CheckCanAttempt(PlayerId, code.TaskId, task.MaxAttempts)`:
   - If `maxAttempts == 0`: unlimited, always passes.
   - Checks whether any existing attempt is already `IsCompleted = true` → `TaskCompleted.DuplicateCompletedTask`.
   - Checks whether the attempt count has reached `maxAttempts` → `Practice.MaxAttemptsReached`.
4. **Code Execution** — calls `_codeExecutor.ExecuteAsync(code.SubmittedCode, task.TestCases.ToList())` which calls the CodeRunner microservice.
5. **Tier Calculation** — `_tierPolicy.Calculate(testResults)`:
   - 0 results → `Mistake`
   - All pass → `Ideal` (note: `Acceptable` is a future extension, not currently reachable via practice)
   - Some pass → `Debt`
   - None pass → `Mistake`
6. **Record Attempt** — stages `PracticeAttempt` in repository (does NOT save yet):
   - Sets `IsCompleted = (tier == Ideal || tier == Acceptable)`.
7. **First SaveAsync** — saves the attempt to DB (required before ProgressionService queries it).
8. **Gate Progression** — calls `_progressionService.ProcessSubmissionAsync(ctx.ShiftProgress, tier, code.TaskId)`:
   - Increments `GateAttempts`.
   - Counts distinct `IsCompleted = true` attempts for this player in this shift.
   - If all `numberOfTasks` are completed: marks gate as cleared, sets `Status = Completed`, calls `PayShiftSalaryAsync`.
   - Otherwise (fail): sets `Status = GatePending`.
9. **EGP Reward** — calls `_economyService.ApplyEgpDeltaAsync(PlayerId, task.EgpReward, Bonus, ...)`.
10. **Second SaveAsync** — saves all staged changes (progress update, economy).
11. **Assessment Events** — fires `practice_attempt` event. If gate cleared: fires `gate_cleared` and `shift_completed`, enqueues `ComputeMastery` Hangfire job.
12. **Struggle Detection** — counts all attempts on this task for this player; `struggleDetected = count > 4`.
13. **Returns response**.

##### Tier Calculation Logic

| Test Pass Rate | Tier | Description |
|---|---|---|
| 100% | `Ideal` | All tests pass |
| 1–99% | `Debt` | Some tests pass |
| 0% | `Mistake` | No tests pass |
| No results | `Mistake` | Empty result set |

> Note: `Acceptable` tier requires future code-quality analysis not yet implemented. Currently unreachable.

##### Error Responses

**400 — Player Not Found**

```json
{ "code": "Forbidden.AccessGame", "description": "You are not allowed to access this game." }
```

**403 — No Active Shift**

```json
{ "code": "Forbidden.Access", "description": "Player has no active shift." }
```

**403 — Task Not In Shift**

```json
{ "code": "Forbidden.Access", "description": "You are not allowed to access this task." }
```

**400 — Progress Not Found**

```json
{ "code": "NotFound.Progress", "description": "Player shift progress record was not found." }
```

**400 — Task Not Found**

```json
{ "code": "NotFound.Task", "description": "The requested practice task was not found." }
```

**400 — Duplicate Completed Task**

Trigger: Player has already passed this task and attempts to submit again when `maxAttempts > 0`.

```json
{ "code": "TaskCompleted.DuplicateCompletedTask", "description": "TaskCompleted Cannot be duplicated Complete." }
```

**400 — Max Attempts Reached**

Trigger: Player has exhausted allowed attempts without passing.

```json
{ "code": "Practice.MaxAttemptsReached", "description": "Maximum attempts reached for this task." }
```

---

## 8. DTO Reference

### `ShiftDto`

Used by: `NarrativeAdminController.GetAllShifts()`, embedded in `NarrativeFlowDto`

| Field | JSON Type | C# Type | Nullable | Description |
|---|---|---|---|---|
| `shiftId` | number | `int` | No | Primary key |
| `shiftNumber` | number | `int` | No | Number within chapter |
| `chapterNumber` | number | `int` | No | Chapter |
| `title` | string | `string` | No | Display title |
| `description` | string | `string?` | Yes | Optional description |
| `conceptTag` | string | `Concept` enum | No | CS concept |
| `isCapstone` | boolean | `bool` | No | Capstone flag |

---

### `ShiftDetailDto`

Used by: `NarrativeAdminController` GET/POST/PUT shifts

| Field | JSON Type | C# Type | Nullable | Description |
|---|---|---|---|---|
| `shiftId` | number | `int` | No | Primary key |
| `shiftNumber` | number | `int` | No | — |
| `chapterNumber` | number | `int` | No | — |
| `title` | string | `string` | No | — |
| `description` | string | `string?` | Yes | — |
| `conceptTag` | string | `Concept` | No | — |
| `isCapstone` | boolean | `bool` | No | — |
| `unlockCondition` | object\|null | `ShiftUnlockCondition?` | Yes | JSON prerequisites |
| `createdAt` | string | `DateTime` | No | UTC timestamp |
| `narrativeBeats` | array | `List<BeatDto>` | No | Ordered narrative beats |
| `consequenceBeats` | array | `List<BeatDto>` | No | Unordered consequence beats |

---

### `BeatDto`

Used by: all beat endpoints, embedded in `NarrativeFlowDto`

| Field | JSON Type | C# Type | Nullable | Description |
|---|---|---|---|---|
| `beatId` | number | `int` | No | Primary key |
| `beatKey` | string | `string` | No | Unique string key |
| `beatType` | string | `BeatType` enum | No | `"Narrative"` or `"Consequence"` |
| `sequenceOrder` | number\|null | `int?` | Yes | Position for Narrative beats; null for Consequence |
| `app` | string | `BeatApp` enum | No | Delivery app |
| `senderName` | string\|null | `string?` | Yes | Sender |
| `contentJson` | object | `StoryBeatContent` | No | Beat content |
| `desktopEvent` | object\|null | `DesktopEvent?` | Yes | Desktop side-effect |
| `delaySeconds` | number | `decimal` | No | Typing delay |
| `hasChoices` | boolean | `bool` | No | Whether choices are presented |
| `createdAt` | string | `DateTime` | No | UTC creation |
| `choices` | array\|null | `List<ChoiceDto>?` | Yes | Included when beat is fetched individually |

**`StoryBeatContent` (nested in `contentJson`):**

| Field | JSON Type | C# Type | Nullable | Description |
|---|---|---|---|---|
| `text` | string | `string` | No | Beat text |
| `avatar` | string\|null | `string?` | Yes | Avatar key |
| `soundEffect` | string\|null | `string?` | Yes | Sound effect key |
| `choices` | array\|null | `List<BeatChoicePreview>?` | Yes | Preview items for choice renders |

**`BeatChoicePreview` (in `contentJson.choices`):**

| Field | JSON Type | C# Type | Nullable |
|---|---|---|---|
| `index` | number | `int` | No |
| `text` | string | `string` | No |

---

### `ChoiceDto`

Used by: choice endpoints

| Field | JSON Type | C# Type | Nullable | Description |
|---|---|---|---|---|
| `choiceId` | number | `int` | No | Primary key |
| `beatId` | number | `int` | No | Owning beat |
| `choiceIndex` | number | `byte` | No | Button position 1–4 |
| `choiceText` | string | `string` | No | Button text |
| `tier` | string | `ChoiceTier` enum | No | Quality tier |
| `consequenceId` | number\|null | `int?` | Yes | Linked consequence |
| `immediateFeedback` | string\|null | `string?` | Yes | Toast text after selection |

---

### `NarrativeFlowDto`

Used by: `NarrativeController` start/save

| Field | JSON Type | C# Type | Nullable | Description |
|---|---|---|---|---|
| `shiftId` | number | `int` | No | The shift |
| `shift` | object | `ShiftDto` | No | Shift metadata |
| `beats` | array | `List<BeatDto>` | No | Ordered beats to display |

---

### `PracticeDto`

Used by: both Practice controllers

(See Section 6.1 field table above.)

---

### `CreatePracticeDto`

Used by: `POST /api/admin/practice/tasks`

(See Section 6.1 field table above.)

---

### `UpdatePracticeDto`

Used by: `PUT /api/admin/practice/tasks/{taskId}`

| Field | JSON Type | C# Type | Nullable |
|---|---|---|---|
| `taskOrder` | number\|null | `byte?` | Yes |
| `title` | string\|null | `string?` | Yes |
| `description` | string\|null | `string?` | Yes |
| `starterCode` | string\|null | `string?` | Yes |
| `conceptTag` | string\|null | `Concept?` | Yes |
| `difficulty` | string\|null | `string?` | Yes |
| `maxAttempts` | number\|null | `short?` | Yes |
| `egpReward` | number\|null | `decimal?` | Yes |

---

### `TestCaseDto`

Used by: test case endpoints

| Field | JSON Type | C# Type | Nullable | Description |
|---|---|---|---|---|
| `testCaseId` | number | `int` | No | Ignored on create |
| `taskId` | number\|null | `int?` | Yes | PracticeTask FK |
| `sideTaskId` | number\|null | `int?` | Yes | PlayerSideTask FK |
| `testInput` | string\|null | `string?` | Yes | Input string |
| `expectedOutput` | string\|null | `string?` | Yes | Expected stdout |
| `isHidden` | boolean | `bool` | No | Hidden from players |
| `description` | string\|null | `string?` | Yes | Description |

---

### `CodeSubmitRequestDto`

Used by: `POST /api/practice/{playerId}/submit`

| Field | JSON Type | C# Type | Required | Description |
|---|---|---|---|---|
| `taskId` | number | `int` | Yes | Task being submitted |
| `submittedCode` | string | `string` | Yes | C source code |
| `timeSpentSec` | number | `int` | Yes | Seconds spent |
| `hintUsed` | boolean | `bool` | Yes | Whether hint was used |

---

### `CodeSubmitResponseDto`

Used by: `POST /api/practice/{playerId}/submit`

| Field | JSON Type | C# Type | Nullable | Description |
|---|---|---|---|---|
| `tier` | string | `ChoiceTier` | No | Evaluation result |
| `testResults` | string | `string?` (serialized JSON) | Yes | JSON string of `TestCaseResult[]` |
| `gateCleared` | boolean | `bool` | No | Gate progression result |
| `egpEarned` | number | `decimal` | No | Always `0.0` (not populated) |
| `newBalance` | number\|null | `decimal?` | Yes | Always `null` (not populated) |
| `struggleDetected` | boolean | `bool` | No | More than 4 attempts made |
| `maxAttemptsReached` | boolean | `bool` | No | Always `false` (not populated) |

---

### `ShiftUnlockCondition`

Used in: `ShiftDetailDto.unlockCondition`

| Field | JSON Type | C# Type | Nullable | Description |
|---|---|---|---|---|
| `prerequisiteShiftId` | number\|null | `int?` | Yes | Shift that must be completed |
| `minRank` | string\|null | `string?` | Yes | Minimum player rank |
| `requiredConcept` | string\|null | `string?` | Yes | Concept meeting the mastery threshold |
| `minMasteryScore` | number\|null | `decimal?` | Yes | Mastery score threshold (0.0–1.0) |

---

### `DesktopEvent`

Used in: `BeatDto.desktopEvent`

| Field | JSON Type | C# Type | Nullable | Description |
|---|---|---|---|---|
| `eventType` | string | `string` | No | Event identifier |
| `appName` | string\|null | `string?` | Yes | Target app |
| `notificationTitle` | string\|null | `string?` | Yes | Notification text |
| `payload` | object\|null | `Dictionary<string, object>?` | Yes | Additional data |

---

## 9. Enum Reference

### `Concept`

Used in: `ShiftDto.conceptTag`, `PracticeDto.conceptTag`, `CreateShiftDto.conceptTag`

| Value | Description |
|---|---|
| `Basics` | C fundamentals |
| `Variables` | Variable declaration and usage |
| `Conditionals` | If/else, comparison operators |
| `Loops` | For, while, do-while loops |
| `Functions` | Function declaration and calls |
| `Arrays` | Array operations |
| `Pointers` | Pointer arithmetic |
| `Strings` | String manipulation |
| `Structures` | Struct definition and usage |
| `FileIO` | File input/output |

---

### `BeatType`

Used in: `BeatDto.beatType`, `CreateStoryBeatDto.beatType`

| Value | Description |
|---|---|
| `Narrative` | Standard ordered shift beat |
| `Consequence` | Deferred beat injected in a future shift |

---

### `BeatApp`

Used in: `BeatDto.app`, `CreateStoryBeatDto.app`

| Value | Description |
|---|---|
| `WhatsUpp` | Chat messaging app |
| `MailLoop` | Email client |
| `LoopCode` | Code editor |
| `System` | System modal |
| `VideoCall` | Video call interface |
| `Notification` | Toast notification |

---

### `ChoiceTier`

Used in: `ChoiceDto.tier`, `CodeSubmitResponseDto.tier`, `CreateChoiceDto.tier`

| Value | Numeric | Description |
|---|---|---|
| `Ideal` | 1 | Optimal professional choice |
| `Acceptable` | 2 | Reasonable but imperfect |
| `Debt` | 3 | Functional but creates future problems |
| `Mistake` | 4 | Wrong choice |

JSON serialization is configured to emit the **string name** (not the number): `"tier": "Ideal"` (confirmed by `AddJsonOptions` in `Program.cs` with `JsonStringEnumConverter`).

---

## 10. Related Domain Entities

```
Narrative Module
├── Shift                     — Game narrative chapter/shift
├── StoryBeat                 — Individual narrative delivery unit
│   ├── BeatType              — Narrative or Consequence
│   ├── ContentJson           — Text, avatar, choices (JSON column)
│   └── DesktopEvent          — Optional desktop side-effect (JSON column)
├── Choice                    — Up to 4 choices per beat
│   └── ChoiceTier            — Quality classification
├── Consequence               — Links a Choice to a future consequence beat (1:1 with StoryBeat)
└── ConsequenceQueue          — Per-player runtime queue (pending → fired)

Player Runtime (Narrative)
├── PlayerChoice              — Immutable record of each choice made
├── PlayerShiftProgress       — Shift gate state (in_progress, gate_pending, completed)
└── PlayerSave                — Desktop state checkpoint (current BeatId)

Practice Module
├── PracticeTask              — Coding exercise attached to a shift
│   └── Difficulty            — SpacedRetrieval | Standard | Challenge
├── TestCase                  — Input/output pairs for code evaluation
│   ├── TaskId                — FK to PracticeTask
│   └── SideTaskId            — FK to PlayerSideTask (mutually exclusive with TaskId)
└── PracticeAttempt           — Code submission log
    ├── Tier                  — ChoiceTier (code quality result)
    ├── IsCompleted           — true when Ideal or Acceptable
    └── TestResults           — JSON array of TestCaseResult

Key Relationships:
- Shift 1:N StoryBeat (via ShiftId — for consequence beats, ShiftId = target shift)
- StoryBeat 1:N Choice
- Choice N:1 Consequence (optional)
- Consequence 1:1 StoryBeat
- Consequence 1:N ConsequenceQueue
- Shift 1:N PracticeTask
- PracticeTask 1:N TestCase
- PracticeTask 1:N PracticeAttempt
```

---

## 11. Common Error Responses

### Error Response Format

All domain errors are returned using the `ApiErrorResponse` format:

```json
{
  "code": "ErrorDomain.ErrorName",
  "description": "Human-readable error message."
}
```

This format is defined in `LoopGame/Models/ApiErrorResponse.cs`. The response does NOT include:

- `statusCode`
- `timestamp`
- `traceId`
- `title`
- `detail`
- `errors` (unless it's a model validation failure — see below)

### Model Validation Errors

ASP.NET Core model validation errors (from `[Required]`, `[MaxLength]`, etc.) use a slightly different format defined in `ApiBehaviorExtensions.cs`:

```json
{
  "code": "Validation.Failed",
  "description": "Human-readable message of the first error.",
  "errors": {
    "fieldName": ["Error message 1", "Error message 2"]
  }
}
```

The `errors` field is omitted (`JsonIgnoreCondition.WhenWritingNull`) for domain errors.

### HTTP Status Code Mapping

The mapping is defined in `ResultHttpMapping.StatusCodeFor()`:

| Error Code | HTTP Status |
|---|---|
| `Narrative.ShiftNotFound` | 404 |
| `Narrative.BeatNotFound` | 404 |
| `Narrative.DuplicateShiftNumber` | 409 |
| `Narrative.DuplicateBeatKey` | 409 |
| `Narrative.SequenceOrderConflict` | 409 |
| `Narrative.ShiftHasPlayerProgress` | 409 |
| `Narrative.ShiftHasStoryBeats` | 409 |
| `Narrative.BeatHasActiveConsequenceQueues` | 409 |
| `Narrative.BeatHasConsequenceReference` | 409 |
| `Narrative.BeatHasChoices` | 409 |
| `Narrative.BeatAlreadyInShift` | 409 |
| `Narrative.ConsequenceBeatCannotChangeShift` | 409 |
| `Choice.PlayerNotFound` | 404 |
| `Choice.BeatNotFound` | 404 |
| `Choice.ChoiceNotFound` | 404 |
| `Choice.ShiftMismatch` | 409 |
| `Forbidden.Access` | 403 |
| Everything else (including Practice errors) | 400 |

> ⚠️ **Important:** Many "not found" errors for Practice entities map to **400** (not 404) because their error codes don't have explicit mappings in `ResultHttpMapping.cs`. See Section 16.

---

## 12. Endpoint Quick Reference

| # | Method | Endpoint | Controller | Auth | Role | Request Body | Response |
|---|---|---|---|---|---|---|---|
| 1 | GET | `/api/admin/shifts` | NarrativeAdminController | None | Admin (intended) | None | `List<ShiftDto>` |
| 2 | GET | `/api/admin/shifts/{shiftId}` | NarrativeAdminController | None | Admin (intended) | None | `ShiftDetailDto` |
| 3 | POST | `/api/admin/shifts` | NarrativeAdminController | None | Admin (intended) | `CreateShiftDto` | `ShiftDetailDto` |
| 4 | PUT | `/api/admin/shifts/{shiftId}` | NarrativeAdminController | None | Admin (intended) | `UpdateShiftDto` | `ShiftDetailDto` |
| 5 | DELETE | `/api/admin/shifts/{shiftId}` | NarrativeAdminController | None | Admin (intended) | None | 204 Empty |
| 6 | GET | `/api/admin/beats/{beatId}` | NarrativeAdminController | None | Admin (intended) | None | `BeatDto` |
| 7 | POST | `/api/admin/beats` | NarrativeAdminController | None | Admin (intended) | `CreateStoryBeatDto` | `BeatDto` |
| 8 | PUT | `/api/admin/beats/{beatId}` | NarrativeAdminController | None | Admin (intended) | `UpdateStoryBeatDto` | `BeatDto` |
| 9 | PUT | `/api/admin/beats/{beatId}/assign-shift/{shiftId}` | NarrativeAdminController | None | Admin (intended) | None | `BeatDto` |
| 10 | DELETE | `/api/admin/beats/{beatId}` | NarrativeAdminController | None | Admin (intended) | None | 204 Empty |
| 11 | POST | `/api/admin/choices` | NarrativeAdminController | None | Admin (intended) | `List<CreateChoiceDto>` | `List<ChoiceDto>` |
| 12 | PUT | `/api/admin/choices/{choiceId}` | NarrativeAdminController | None | Admin (intended) | `UpdateChoiceDto` | `ChoiceDto` |
| 13 | POST | `/api/narrative/{playerId}/shifts/{shiftId}/start` | NarrativeController | None | Player (intended) | None | `NarrativeFlowDto` |
| 14 | POST | `/api/narrative/{playerId}/shifts/{shiftId}/{beatId}/save` | NarrativeController | None | Player (intended) | None | `NarrativeFlowDto` |
| 15 | POST | `/api/narrative/{playerId}/shifts/{shiftId}/end` | NarrativeController | None | Player (intended) | None | `{ msg: string }` |
| 16 | GET | `/api/narrative/{playerId}/beats/{beatId}/choices` | NarrativeController | None | Player (intended) | None | `List<ChoiceDto>` |
| 17 | POST | `/api/narrative/{playerId}/choices/{choiceId}/submit` | NarrativeController | None | Player (intended) | None | `ChoiceDto` |
| 18 | GET | `/api/admin/practice/tasks` | PracticeAdminController | None | Admin (intended) | None | `List<PracticeDto>` |
| 19 | POST | `/api/admin/practice/tasks` | PracticeAdminController | None | Admin (intended) | `CreatePracticeDto` | `PracticeDto` |
| 20 | PUT | `/api/admin/practice/tasks/{taskId}` | PracticeAdminController | None | Admin (intended) | `UpdatePracticeDto` | `PracticeDto` |
| 21 | POST | `/api/admin/practice/testcases` | PracticeAdminController | None | Admin (intended) | `List<TestCaseDto>` | `List<TestCaseDto>` |
| 22 | PUT | `/api/admin/practice/testcases/{testId}` | PracticeAdminController | None | Admin (intended) | `TestCaseDto` | `TestCaseDto` |
| 23 | GET | `/api/practice/{playerId}/task/{taskId}` | PracticeController | None | Player (intended) | None | `PracticeDto` |
| 24 | POST | `/api/practice/{playerId}/submit` | PracticeController | None | Player (intended) | `CodeSubmitRequestDto` | `CodeSubmitResponseDto` |

---

## 13. Frontend Integration Examples

### Start a Shift (Narrative)

```javascript
const startShift = async (playerId, shiftId, accessToken) => {
  const response = await fetch(
    `/api/narrative/${playerId}/shifts/${shiftId}/start`,
    {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${accessToken}`
      }
    }
  );

  if (!response.ok) {
    const error = await response.json();
    console.error('Error:', error.code, error.description);
    return null;
  }

  const flow = await response.json();
  // flow.beats contains ordered BeatDto[] to render
  return flow;
};
```

### Submit a Choice

```javascript
const submitChoice = async (playerId, choiceId, accessToken) => {
  const response = await fetch(
    `/api/narrative/${playerId}/choices/${choiceId}/submit`,
    {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${accessToken}`
      }
    }
  );

  const result = await response.json();
  if (!response.ok) {
    console.error(result.code, result.description);
    return null;
  }
  // result.tier, result.immediateFeedback
  return result;
};
```

### Get Practice Task

```javascript
const getPracticeTask = async (playerId, taskId, accessToken) => {
  const response = await fetch(
    `/api/practice/${playerId}/task/${taskId}`,
    {
      headers: {
        'Authorization': `Bearer ${accessToken}`
      }
    }
  );

  if (!response.ok) {
    const error = await response.json();
    console.error(error.code, error.description);
    return null;
  }

  return await response.json();
  // Returns PracticeDto (hidden test cases excluded)
};
```

### Submit Practice Code

```javascript
const submitCode = async (playerId, taskId, code, timeSpent, hintUsed, accessToken) => {
  const response = await fetch(
    `/api/practice/${playerId}/submit`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${accessToken}`
      },
      body: JSON.stringify({
        taskId,
        submittedCode: code,
        timeSpentSec: timeSpent,
        hintUsed
      })
    }
  );

  const result = await response.json();
  if (!response.ok) {
    console.error(result.code, result.description);
    return null;
  }

  // IMPORTANT: testResults is a JSON string, must be parsed separately
  const testResults = result.testResults ? JSON.parse(result.testResults) : [];
  return { ...result, testResults };
};
```

### Save Narrative Progress

```javascript
const saveProgress = async (playerId, shiftId, beatId, accessToken) => {
  const response = await fetch(
    `/api/narrative/${playerId}/shifts/${shiftId}/${beatId}/save`,
    {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${accessToken}`
      }
    }
  );
  return response.ok ? await response.json() : null;
};
```

### Admin: Create a Shift

```javascript
const createShift = async (shiftData, accessToken) => {
  const response = await fetch('/api/admin/shifts', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${accessToken}`
    },
    body: JSON.stringify(shiftData)
  });

  const result = await response.json();
  if (!response.ok) {
    console.error(result.code, result.description);
    return null;
  }
  return result; // ShiftDetailDto
};
```

### Admin: Create a Beat

```javascript
const createBeat = async (beatData, accessToken) => {
  const response = await fetch('/api/admin/beats', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${accessToken}`
    },
    body: JSON.stringify({
      shiftId: beatData.shiftId,
      beatKey: beatData.beatKey,
      beatType: beatData.beatType,
      sequenceOrder: beatData.sequenceOrder ?? null,
      app: beatData.app,
      senderName: beatData.senderName ?? null,
      contentJson: {
        text: beatData.text,
        avatar: beatData.avatar ?? null,
        soundEffect: beatData.soundEffect ?? null,
        choices: null
      },
      desktopEvent: null,
      delaySeconds: 0.0,
      hasChoices: false,
      injectPosition: beatData.injectPosition ?? null
    })
  });

  const result = await response.json();
  if (!response.ok) {
    console.error(result.code, result.description);
    return null;
  }
  return result; // BeatDto
};
```

---

## 14. cURL Examples

### GET All Shifts

```bash
curl -X GET "https://localhost:7048/api/admin/shifts" \
  -H "Authorization: Bearer <JWT>"
```

### GET Single Shift

```bash
curl -X GET "https://localhost:7048/api/admin/shifts/1" \
  -H "Authorization: Bearer <JWT>"
```

### POST Create Shift

```bash
curl -X POST "https://localhost:7048/api/admin/shifts" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <JWT>" \
  -d '{
    "shiftNumber": 1,
    "chapterNumber": 1,
    "title": "Variables",
    "description": "Introduction to variables.",
    "conceptTag": "Variables",
    "numberOfTasks": 3,
    "isCapstone": false,
    "unlockCondition": null
  }'
```

### POST Create Narrative Beat

```bash
curl -X POST "https://localhost:7048/api/admin/beats" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <JWT>" \
  -d '{
    "shiftId": 1,
    "beatKey": "ch1_s1_first_task",
    "beatType": "Narrative",
    "sequenceOrder": 1,
    "app": "MailLoop",
    "senderName": "Youssef",
    "contentJson": {
      "text": "Start with the menu.",
      "avatar": "youssef",
      "soundEffect": null,
      "choices": null
    },
    "desktopEvent": null,
    "delaySeconds": 0.0,
    "hasChoices": true,
    "injectPosition": null
  }'
```

### POST Create Choices (batch)

```bash
curl -X POST "https://localhost:7048/api/admin/choices" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <JWT>" \
  -d '[
    {
      "beatId": 1,
      "choiceIndex": 1,
      "choiceText": "I will use variables.",
      "tier": "Ideal",
      "consequenceId": null,
      "immediateFeedback": "Smart move!"
    },
    {
      "beatId": 1,
      "choiceIndex": 2,
      "choiceText": "I will hardcode everything.",
      "tier": "Mistake",
      "consequenceId": null,
      "immediateFeedback": null
    }
  ]'
```

### POST Start Shift (Player)

```bash
curl -X POST "https://localhost:7048/api/narrative/1/shifts/1/start" \
  -H "Authorization: Bearer <JWT>"
```

### GET Practice Task (Player)

```bash
curl -X GET "https://localhost:7048/api/practice/1/task/1" \
  -H "Authorization: Bearer <JWT>"
```

### POST Submit Code (Player)

```bash
curl -X POST "https://localhost:7048/api/practice/1/submit" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <JWT>" \
  -d '{
    "taskId": 1,
    "submittedCode": "#include <stdio.h>\n\nint main() {\n    int coffeePrice = 45;\n    printf(\"Coffee - %d EGP\\n\", coffeePrice);\n    return 0;\n}",
    "timeSpentSec": 120,
    "hintUsed": false
  }'
```

### POST Submit Choice (Player)

```bash
curl -X POST "https://localhost:7048/api/narrative/1/choices/101/submit" \
  -H "Authorization: Bearer <JWT>"
```

### POST Create Practice Task (Admin)

```bash
curl -X POST "https://localhost:7048/api/admin/practice/tasks" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <JWT>" \
  -d '{
    "shiftId": 1,
    "taskOrder": 1,
    "title": "Store Coffee Price",
    "description": "Create an integer variable named coffeePrice and print it.",
    "starterCode": "#include <stdio.h>\n\nint main() {\n    return 0;\n}",
    "conceptTag": "Variables",
    "difficulty": "SpacedRetrieval",
    "maxAttempts": 0,
    "egpReward": 20.0,
    "testCases": null
  }'
```

### DELETE Shift (Admin)

```bash
curl -X DELETE "https://localhost:7048/api/admin/shifts/1" \
  -H "Authorization: Bearer <JWT>"
```

---

## 15. Error Coverage Audit

| Endpoint | 400 | 401 | 403 | 404 | 409 | 500 | Custom |
|---|---|---|---|---|---|---|---|
| GET `/api/admin/shifts` | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | — |
| GET `/api/admin/shifts/{id}` | ✗ | ✗ | ✗ | ✓ (ShiftNotFound) | ✗ | ✗ | — |
| POST `/api/admin/shifts` | ✓ (title, chapter, shift#) | ✗ | ✗ | ✗ | ✓ (DuplicateNumber) | ✗ | — |
| PUT `/api/admin/shifts/{id}` | ✓ (title, chapter, shift#) | ✗ | ✗ | ✓ (ShiftNotFound) | ✓ (DuplicateNumber) | ✗ | — |
| DELETE `/api/admin/shifts/{id}` | ✗ | ✗ | ✗ | ✓ (ShiftNotFound) | ✓ (HasProgress, HasBeats) | ✗ | — |
| GET `/api/admin/beats/{id}` | ✗ | ✗ | ✗ | ✓ (BeatNotFound) | ✗ | ✗ | — |
| POST `/api/admin/beats` | ✓ (key, content, sequenceOrder, injectPos) | ✗ | ✗ | ✓ (ShiftNotFound) | ✓ (DuplicateKey, SeqConflict) | ✗ | — |
| PUT `/api/admin/beats/{id}` | ✓ (sequenceOrder type mismatch) | ✗ | ✗ | ✓ (BeatNotFound, ShiftNotFound) | ✓ (ConsequenceMove, SeqConflict) | ✗ | — |
| PUT `/api/admin/beats/{id}/assign-shift/{id}` | ✗ | ✗ | ✗ | ✓ (BeatNotFound, ShiftNotFound) | ✓ (AlreadyInShift, ConsequenceMove, SeqConflict) | ✗ | — |
| DELETE `/api/admin/beats/{id}` | ✗ | ✗ | ✗ | ✓ (BeatNotFound) | ✓ (ActiveConsequenceQueues) | ✗ | — |
| POST `/api/admin/choices` | ✓ (empty, index, text, notAllowed) | ✗ | ✗ | ✓ (BeatNotFound) | ✓ (ExceedsMax, DuplicateIndex) | ✗ | — |
| PUT `/api/admin/choices/{id}` | ✓ (invalidId, invalidText) | ✗ | ✗ | ✓ (ChoiceNotFound) | ✗ | ✗ | — |
| POST `/api/narrative/{pid}/shifts/{sid}/start` | ✗ | ✗ | ✗ | ✓ (PlayerNotFound, ShiftNotFound) | ✓ (ShiftMismatch) | ✗ | — |
| POST `/api/narrative/{pid}/shifts/{sid}/{bid}/save` | ✗ | ✗ | ✗ | ✓ (PlayerNotFound, BeatNotFound) | ✓ (ShiftMismatch) | ✗ | — |
| POST `/api/narrative/{pid}/shifts/{sid}/end` | ✓ (ShiftNotCompleted) | ✗ | ✗ | ✓ (PlayerNotFound, ShiftNotFound) | ✓ (ShiftMismatch) | ⚠️ null ref if no next shift | — |
| GET `/api/narrative/{pid}/beats/{bid}/choices` | ✓ (InvalidId) | ✗ | ✗ | ✓ (PlayerNotFound, BeatNotFound) | ✓ (ShiftMismatch) | ✗ | — |
| POST `/api/narrative/{pid}/choices/{cid}/submit` | ✗ | ✗ | ✓ (ForbiddenAccess) | ✗ | ✗ | ⚠️ null ref if player/choice missing | — |
| GET `/api/admin/practice/tasks` | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | — |
| POST `/api/admin/practice/tasks` | ✓ (all fields) | ✗ | ✗ | ✓ (ShiftNotFound → but maps to 400) | ✗ | ✗ | — |
| PUT `/api/admin/practice/tasks/{id}` | ✓ (conceptTag) | ✗ | ✗ | ✓ (TaskNotFound → maps to 400) | ✗ | ✗ | — |
| POST `/api/admin/practice/testcases` | ✓ (all fields) | ✗ | ✗ | ✓ (TaskNotFound → maps to 400) | ✗ | ✗ | — |
| PUT `/api/admin/practice/testcases/{id}` | ✗ | ✗ | ✗ | ✓ (TestCaseNotFound → maps to 400) | ✗ | ✗ | — |
| GET `/api/practice/{pid}/task/{tid}` | ✓ (AccessGame, ProgressNotFound, TaskNotFound) | ✗ | ✓ (ForbiddenAccess) | ✗ | ✗ | ✗ | — |
| POST `/api/practice/{pid}/submit` | ✓ (Access, Progress, MaxAttempts) | ✗ | ✓ (ForbiddenAccess) | ✗ | ✗ | ✗ | — |

**Legend:** ✓ = confirmed, ✗ = not produced, ⚠️ = runtime risk

---

## 16. Current Implementation Notes & Warnings

This section documents every confirmed discrepancy between the current code and the intended architecture/SRS.

---

### ⚠️ W-01: All Four Controllers Are Unauthenticated

**Current Code:** No `[Authorize]` attribute is active on any of the four controllers.

- `NarrativeAdminController`: The comment says `//[Authorize(Roles = "Admin")]` (commented out).
- `NarrativeController`: No `[Authorize]` attribute anywhere.
- `PracticeAdminController`: `//[Authorize(Roles = "Admin")]` commented out.
- `PracticeController`: No `[Authorize]` attribute anywhere.

**What the SRS says:** Admin endpoints should require Admin/SuperAdmin role. Player endpoints should require Player role.

**Impact:** Any unauthenticated HTTP client can call any endpoint. This is a **critical security vulnerability** in a production environment.

---

### ⚠️ W-02: Player ID Comes from Route, Not JWT Claims

**Current Code:** All player-facing endpoints accept `playerId` as a route parameter (`{playerId:int}`). No claim extraction is performed.

**What the SRS says:** Player ID should be extracted from authenticated JWT claims (`ClaimTypes.NameIdentifier`).

**Impact:** A player can supply any `playerId` and act as another player. This is a direct IDOR (Insecure Direct Object Reference) vulnerability.

---

### ⚠️ W-03: Practice Error Codes Map to 400 Instead of 404

**Current Code:** Practice-related error codes such as `"NotFound.Task"`, `"NotFound.Progress"`, `"NotFound.Shift"`, `"Forbidden.AccessGame"` fall into the default `400` case in `ResultHttpMapping.StatusCodeFor()`.

**What the SRS says (and REST convention):** These should return `404 Not Found`.

**Impact:** Frontend developers cannot distinguish between validation errors and not-found errors for Practice entities by HTTP status code alone — they must check the `code` field.

---

### ⚠️ W-04: `CodeSubmitResponseDto` Has Unpopulated Fields

**Current Code:** In `PracticeService.SubmitCode()`, the returned `CodeSubmitResponseDto` does not set:
- `EgpEarned` — always `0.0`
- `NewBalance` — always `null`
- `MaxAttemptsReached` — always `false`

The EGP reward IS applied via `_economyService.ApplyEgpDeltaAsync()`, but the amount is not reflected in the response object.

**Impact:** The frontend cannot display the EGP earned or the new balance from this endpoint's response.

---

### ⚠️ W-05: `testResults` Is a Serialized JSON String

**Current Code:** `CodeSubmitResponseDto.TestResults` is type `string?` and is set via `JsonSerializer.Serialize(testResults)`. This means the response field is a JSON string containing an escaped JSON array.

**What the SRS/architecture suggests:** This should be a proper nested array in the response JSON.

**Impact:** Frontend must call `JSON.parse(response.testResults)` to get the test case results. This is non-standard and surprising.

---

### ⚠️ W-06: `EndShift` Will Crash If No Next Shift Exists

**Current Code:** `NarrativeService.EndShift()` calls:
```csharp
var nextshift = await unitOfWork.GetRepository<Shift>()
    .FindAsync(s => s.ShiftNumber == player.CurrentShift.ShiftNumber + 1);
player.CurrentShiftId = nextshift.ShiftId;
```

If the player is on the last shift, `nextshift` will be `null`, causing a `NullReferenceException` (unhandled, will result in `500 Internal Server Error`).

---

### ⚠️ W-07: `SubmitChoice` Has No Null Guards

**Current Code:** `ChoiceService.SubmitChoice()` does not check whether the player or choice returned from the repository is null before accessing properties. If either is not found, a `NullReferenceException` will occur.

---

### ⚠️ W-08: `GetTask` (PracticeAdminController) Has Unused Parameters

**Current Code:** The `GetTask` action declares `int playerId` and `int taskId` as query parameters but never uses them — it calls `_practiceService.GetTasks()` with no arguments.

---

### ⚠️ W-09: `Acceptable` Tier Is Unreachable in Practice

**Current Code:** `PracticeTierCalculationPolicy.Calculate()` never returns `Acceptable`:

```csharp
if (passed == total)
    return ChoiceTier.Ideal;   // Acceptable = future extension
```

The comment explicitly states this is a documented future extension point. However, `PracticeAttemptService` sets `IsCompleted = true` for both `Ideal` AND `Acceptable`, so if Acceptable were ever returned (e.g., via future code-quality analysis), the gate progression would correctly recognize it.

---

### ⚠️ W-10: JSON Serialization Uses String Enum Names

**Current Code:** `Program.cs` registers `JsonStringEnumConverter`:

```csharp
options.JsonSerializerOptions.Converters.Add(
    new System.Text.Json.Serialization.JsonStringEnumConverter());
```

This means all enum values in responses are **strings** (e.g., `"tier": "Ideal"`, `"beatType": "Narrative"`, `"app": "MailLoop"`), not integers. Requests must also use string values for enums.

---

### ⚠️ W-11: `NarrativeFlowDto.Beats` Uses `List<BeatDto>` Without Choices

**Current Code:** Beats returned by `StartShift` and `Save` are fetched via `GetNarrativeBeats()` which loads `["Choices"]`. However, the `BeatDto.Choices` field is populated only when fetched individually (via `GetStoryBeat`). The Mapster configuration maps `StoryBeat` → `BeatDto` including choices, so choices **should** be included if the navigation property is loaded.

**Recommendation:** Frontend should verify whether the `choices` array is non-null in `NarrativeFlowDto.Beats` before attempting to render choice buttons, rather than relying solely on `hasChoices`.

---

### ⚠️ W-12: `Acceptable` Tier for SideTask vs Practice

The `SideTaskService.SubmitSideTaskAsync()` correctly produces `Acceptable` tier (75% pass rate). The `PracticeTierCalculationPolicy` never produces `Acceptable`. These are two separate submission pipelines with different tier logic.

---

### Information: Two-Save Pattern in Practice Submission

The `PracticeService.SubmitCode()` calls `_uow.SaveAsync()` **twice**:
1. First after recording the `PracticeAttempt` (so `ProgressionService` can count it).
2. Second after gate progression and EGP reward.

This is intentional and documented in the source with a comment.

---

*Generated from actual source code — September 2026*
*This document reflects what the code DOES, not what it was designed to do.*
