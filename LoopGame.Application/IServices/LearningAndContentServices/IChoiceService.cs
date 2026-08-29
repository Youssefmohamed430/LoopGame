using LoopGame.Application.Dtos;
using LoopGame.Domain.Abstractions;

namespace LoopGame.Application.IServices.LearningAndContentServices;

public interface IChoiceService
{
    Task<Result<List<ChoiceDto>>> GetChoices(int BeatId, int PlayerId);
    Task<Result<List<ChoiceDto>>> AddChoice(List<CreateChoiceDto> choices);
    Task<Result<ChoiceDto>> UpdateChoice(int choiceid, UpdateChoiceDto choicedto);
    Task<Result<ChoiceDto>> SubmitChoice(int choiceid, int PlayerId);
}

