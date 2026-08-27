namespace LoopGame.Application.Services.LearningAndContentServices;

public class ChoiceService
    (IUnitOfWork unitOfWork)
    : IChoiceService
{
    public async Task<Result<List<ChoiceDto>>> GetChoices(int BeatId, int PlayerId)
    {
        var player = await unitOfWork.GetRepository<Player>()
            .FindAsync(p => p.PlayerId == PlayerId);
        if (player == null)
            return Result.Failure< List<ChoiceDto>>(new Error());

        var Beat = await unitOfWork.GetRepository<StoryBeat>()
            .FindAsync(b => b.BeatId == BeatId,new string[] {"Choices"});

        if (player.CurrentShiftId != Beat.ShiftId)
            return Result.Failure<List<ChoiceDto>>(new Error());

        var listOfChoices = Beat.Choices
            .Select(c => c.Adapt<ChoiceDto>())
            .ToList();

        return Result.Success(listOfChoices);
    }
    public async Task<Result<List<ChoiceDto>>> AddChoice(List<ChoiceDto> choices)
    {
        foreach (var choice in choices)
        {
            await unitOfWork.GetRepository<Choice>()
                .AddAsync(choice.Adapt<Choice>());
        }
        await unitOfWork.SaveAsync();

        return Result.Success(choices);
    }


    public Task<Result<ChoiceDto>> SubmitChoice(int choiceid)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<ChoiceDto>> UpdateChoice(int choiceid, ChoiceDto choicedto)
    {
        var choice = unitOfWork.GetRepository<Choice>()
            .FindWithTracking(c => c.ChoiceId == choiceid);

        choice.ChoiceText = choicedto.ChoiceText ?? choice.ChoiceText;
        choice.ConsequenceId = choicedto.ConsequenceId;

        await unitOfWork.GetRepository<Choice>()
            .UpdateAsync(choice);

        await unitOfWork.SaveAsync();

        return Result.Success(choicedto);
    }
}
