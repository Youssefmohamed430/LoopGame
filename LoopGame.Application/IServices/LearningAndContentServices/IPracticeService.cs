namespace LoopGame.Application.IServices.LearningAndContentServices;

public interface IPracticeService
{
    PracticeDto GetTaskByShiftId(int ShiftId);
    void SubmitCode(string code);
}
