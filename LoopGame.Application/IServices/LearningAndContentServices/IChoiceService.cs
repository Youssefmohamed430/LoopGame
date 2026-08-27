namespace LoopGame.Application.IServices.LearningAndContentServices;

public interface IChoiceService
{
    Task<Result<List<ChoiceDto>>> GetChoices(int BeatId,int PlayerId);
    Task<Result<List<ChoiceDto>>> AddChoice(List<ChoiceDto> choices);
    Task<Result<ChoiceDto>> UpdateChoice(int choiceid,ChoiceDto choice);
    Task<Result<ChoiceDto>> SubmitChoice(int choiceid);
}
