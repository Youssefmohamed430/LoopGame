namespace LoopGame.Application.Dtos.SideTaskDtos;

/// <summary>
/// Request payload sent to the external AI side-task generation endpoint.
/// </summary>
public class AiGenerateRequest
{
    public int PlayerId { get; set; }
    public string Concept { get; set; } = string.Empty;
    public string SheetContent { get; set; } = string.Empty;
    public SideTaskReferenceScenarioRequest ReferenceScenario { get; set; } = null!;
}
