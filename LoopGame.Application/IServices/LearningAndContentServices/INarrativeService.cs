namespace LoopGame.Application.IServices.LearningAndContentServices;

public interface INarrativeService
{
    Task<Result<NarrativeFlowDto>> StartShift(int playerId,int shiftId);
    
}