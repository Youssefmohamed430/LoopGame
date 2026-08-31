# NarrativeService CRUD & Shift/StoryBeat Management — Implementation Report

> Build status: ✅ `Build succeeded — 0 Errors` (dotnet build, Debug configuration)

---

## 1. Files Modified

| File | Change |
|------|--------|
| `LoopGame.Application/IServices/LearningAndContentServices/INarrativeService.cs` | **Replaced** — added 8 new method signatures |
| `LoopGame.Application/Services/LearningAndContentServices/NarrativeService.cs` | **Replaced** — implemented all 8 new methods + preserved `StartShift` |
| `LoopGame.Application/DependencyInjection.cs` | Registered `INarrativeService` + `IChoiceService` (they were missing from DI) |
| `LoopGame.Application/Mapping/MapsterConfiguration.cs` | Added `ShiftDetailDto`, `CreateStoryBeatDto`, `UpdateStoryBeatDto` Mapster configs |
| `LoopGame/GlobalUsings.cs` | Added `LoopGame.Application.Dtos`, `LoopGame.Application.Dtos.NarrativeDtos`, `IServices.LearningAndContentServices` |
| `LoopGame/Extensions/ResultHttpMapping.cs` | Extended `StatusCodeFor` to handle all Narrative error codes (404 / 409) |

## 2. Files Created

| File | Purpose |
|------|---------|
| `LoopGame.Domain/Abstractions/NarrativeErrors.cs` | Static error definitions for all narrative operations |
| `LoopGame.Application/Dtos/NarrativeDtos/CreateShiftDto.cs` | Admin request model for creating a shift |
| `LoopGame.Application/Dtos/NarrativeDtos/UpdateShiftDto.cs` | Admin request model for partial shift updates |
| `LoopGame.Application/Dtos/NarrativeDtos/ShiftDetailDto.cs` | Full admin response — shift + narrative + consequence beats |
| `LoopGame.Application/Dtos/NarrativeDtos/CreateStoryBeatDto.cs` | Admin request model for creating a beat (narrative or consequence) |
| `LoopGame.Application/Dtos/NarrativeDtos/UpdateStoryBeatDto.cs` | Admin request model for partial beat updates |
| `LoopGame/Controllers/NarrativeAdminController.cs` | All admin REST endpoints |

---

## 3. DTOs Reused (No Duplication)

| DTO | Reused From | Where |
|-----|-------------|-------|
| `ShiftDto` | `Dtos/NarrativeDtos/ShiftDto.cs` | `GetAllShifts()` return, Mapster `StartShift` |
| `BeatDto` | `Dtos/NarrativeDtos/BeatDto.cs` | `CreateStoryBeat`, `GetStoryBeat`, `UpdateStoryBeat`, `AssignBeatToShift` |
| `NarrativeFlowDto` | `Dtos/NarrativeDtos/NarrativeFlowDto.cs` | `StartShift` (unchanged) |
| `ShiftUnlockCondition` | `Domain/ValueObjects/ShiftUnlockCondition.cs` | `CreateShiftDto`, `UpdateShiftDto`, `ShiftDetailDto` |
| `StoryBeatContent` | `Domain/ValueObjects/StoryBeatContent.cs` | `CreateStoryBeatDto`, `UpdateStoryBeatDto` |
| `DesktopEvent` | `Domain/ValueObjects/DesktopEvent.cs` | `CreateStoryBeatDto`, `UpdateStoryBeatDto` |

---

## 4. DTOs Created (and Why)

| DTO | Reason |
|-----|--------|
| `CreateShiftDto` | No existing create-shift request model existed |
| `UpdateShiftDto` | Needed separate partial-update model with `ClearUnlockCondition` sentinel |
| `ShiftDetailDto` | `ShiftDto` is a slim runtime DTO (no beats, no UnlockCondition, no CreatedAt); admin reads need the full picture |
| `CreateStoryBeatDto` | No existing create-beat model; also handles the `InjectPosition` for consequence beats |
| `UpdateStoryBeatDto` | Needed partial-update semantics with `ReorderSiblings` flag |

---

## 5. Methods Added to `INarrativeService`

```csharp
// Shift (Admin)
Task<Result<ShiftDetailDto>> CreateShift(CreateShiftDto dto);
Task<Result<ShiftDetailDto>> GetShift(int shiftId);
Task<Result<List<ShiftDto>>>  GetAllShifts();
Task<Result<ShiftDetailDto>> UpdateShift(int shiftId, UpdateShiftDto dto);
Task<Result>                  DeleteShift(int shiftId);

// StoryBeat (Admin)
Task<Result<BeatDto>> CreateStoryBeat(CreateStoryBeatDto dto);
Task<Result<BeatDto>> GetStoryBeat(int beatId);
Task<Result<BeatDto>> UpdateStoryBeat(int beatId, UpdateStoryBeatDto dto);
Task<Result>          DeleteStoryBeat(int beatId);
Task<Result<BeatDto>> AssignBeatToShift(int beatId, int shiftId);
```

---

## 6. Controller Endpoints Added

All on `NarrativeAdminController` — route base: `/api/admin`

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/admin/shifts` | All shifts (ordered by chapter → shift number) |
| `GET` | `/api/admin/shifts/{shiftId}` | Single shift with narrative + consequence beats |
| `POST` | `/api/admin/shifts` | Create shift |
| `PUT` | `/api/admin/shifts/{shiftId}` | Update shift metadata |
| `DELETE` | `/api/admin/shifts/{shiftId}` | Delete shift (safe — 409 if dependencies) |
| `GET` | `/api/admin/beats/{beatId}` | Single beat |
| `POST` | `/api/admin/beats` | Create beat (narrative or consequence) |
| `PUT` | `/api/admin/beats/{beatId}` | Update beat |
| `PUT` | `/api/admin/beats/{beatId}/assign-shift/{shiftId}` | Move beat to another shift |
| `DELETE` | `/api/admin/beats/{beatId}` | Delete beat (safe — 409 if active queues) |

---

## 7. Validation Rules Implemented

### Shift
- Title required, non-empty
- `ChapterNumber` > 0
- `ShiftNumber` > 0
- `(ChapterNumber, ShiftNumber)` unique — maps to the `UQ_Shift_Number` index
- Shift must exist before update/delete
- **Delete**: blocked if `PlayerShiftProgress` records exist (historical data)
- **Delete**: blocked if `StoryBeat` rows still reference the shift (`Restrict` FK)

### StoryBeat
- `BeatKey` required, non-empty, globally unique (`HasIndex.IsUnique` on `BeatKey`)
- `ContentJson.Text` required
- Narrative beats: `SequenceOrder` required + unique per `(ShiftId, SequenceOrder)` 
- Consequence beats: `SequenceOrder` must be null (enforced by `CHK_Beat_SequenceOrder`)
- Consequence beats: `InjectPosition` must be `"start"` or `"end"` (`CHK_Consequence_InjectPosition`)
- Referenced `ShiftId` must exist

### Dependencies / Safety
| Operation | Guard |
|-----------|-------|
| Delete shift | Blocked if `ShiftProgresses.Count > 0` OR `StoryBeats.Count > 0` |
| Delete beat | Blocked if `ConsequenceQueue` rows with `Status = pending` reference a consequence linked via choices of this beat |
| Delete consequence beat | Blocked if its own `Consequence` has pending queue entries |
| Move/reassign consequence beat | Blocked if it has pending queue entries |
| SequenceOrder change | Conflict check against target shift + exclude self |
| BeatKey | Immutable after creation (Ink bridge contract) |

---

## 8. EF / Database Changes

**No schema changes were made.** All existing constraints were respected:

| Constraint | Observed Behaviour |
|------------|--------------------|
| `UQ_Shift_Number` `(ChapterNumber, ShiftNumber)` | Validated in service before insert/update |
| `HasIndex(BeatKey).IsUnique()` | Validated in service before insert |
| `CHK_Beat_SequenceOrder` | Enforced at service level; consequence beats always get `SequenceOrder = null` |
| `CHK_Consequence_InjectPosition` | Validated at service level (`"start"` \| `"end"`) |
| `StoryBeat → Shift` `OnDelete.Restrict` | DeleteShift guards against existing beats |
| `Choice → StoryBeat` `OnDelete.Cascade` | Acknowledged in DeleteStoryBeat — cascade is safe once queue check passes |
| `Consequence → StoryBeat` `OnDelete.Cascade` | Same — Consequence row auto-deleted with beat |
| `ConsequenceQueue → Consequence` `OnDelete.Restrict` | Primary guard for DeleteStoryBeat and move operations |
| `PlayerShiftProgress → Shift` `OnDelete.Restrict` | Primary guard for DeleteShift |

---

## 9. Assumptions Made

1. **BeatKey is immutable** — it is the Ink bridge identifier. `UpdateStoryBeatDto` does not expose `BeatKey`. If authors need to rename a key, that should be a separate deliberate operation.
2. **Consequence row auto-created** — when `CreateStoryBeat` is called with `BeatType = Consequence`, the service automatically creates the `Consequence` entity (with `BeatId` and `InjectPosition`). The caller does not need a separate endpoint to create the `Consequence` row.
3. **No soft-delete** — the existing project has no `IsDeleted`/`IsActive` pattern. The service returns explicit `409 Conflict` errors instead of silently failing or cascading dangerously.
4. **Authorization placeholder** — `NarrativeAdminController` has a `// TODO(auth): [Authorize(Roles = "Admin")]` comment, matching the same style as the existing `EconomyController` TODO. No auth middleware was modified.
5. **`ChoiceService` and `IChoiceService` were not registered in DI** before this task. They were added as a side-fix (they would have caused a runtime error if a controller injected them).
6. **`GetAllShifts` loads all shifts** without pagination. The existing project has a `PagedResult<T>` DTO, but no other listing endpoint uses it yet. This can be upgraded to paged when the project requires it.

---

## 10. Open Decisions for the Developer

> [!IMPORTANT]
> These decisions require developer input — they were not assumed.

1. **Authorization**: The controller has no `[Authorize]` attribute yet. Once your RBAC pipeline lands, add `[Authorize(Roles = "Admin")]` (or the project's equivalent policy) to `NarrativeAdminController`. Players must not reach these endpoints.

2. **`GetAllShifts` pagination**: Currently returns all shifts. If the content library grows large, add `[FromQuery] int page = 1, [FromQuery] int pageSize = 20` parameters and use `PagedResult<ShiftDto>`.

3. **Soft-delete vs hard-delete for beats**: The current implementation guards against deleting beats with active consequence queue entries (returns 409). An alternative is to add an `IsActive` flag to `StoryBeat` and soft-delete — but this requires a schema change. Discuss with the team before implementing.

4. **Moving a consequence beat between shifts**: Currently blocked if any `ConsequenceQueue` entry (even `fired`) references it. The guard is specifically on `Status = pending`. `fired` entries are allowed — this is intentional to preserve history. Confirm this matches the expected behaviour.

5. **`UpdateStoryBeat` BeatType change**: Changing a beat from `Narrative` to `Consequence` (or vice versa) is supported but has wide side-effects (SequenceOrder cleared, Consequence row must be created/deleted). Consider whether this should be restricted to only beat fields that don't change the type.

6. **`AssignBeatToShift` and SequenceOrder**: If a narrative beat is moved to a new shift and its `SequenceOrder` conflicts, the service returns a 409. The caller must first update the `SequenceOrder` via `UpdateStoryBeat`, then assign. An alternative is to have `AssignBeatToShift` accept an optional new `SequenceOrder` — consider if this UX is desired.
