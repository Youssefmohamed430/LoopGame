namespace LoopGame.Application.IServices.LearningAndContentServices;

public interface IPracticeService
{
    PracticeDto GetTaskAsync(int TaskId,int PlayerId);
    CodeSubmitResponseDto SubmitCode(int PlayerId, CodeSubmitRequestDto code);
}
