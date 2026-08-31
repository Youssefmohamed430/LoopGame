using LoopGame.Domain.Entities.Narrative;
using LoopGame.Application.Dtos;
using LoopGame.Application.Dtos.NarrativeDtos;

namespace LoopGame.Application.Mapping;

public class MapsterConfiguration : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PracticeTask, PracticeDto>()
            .Map(dest => dest.ShiftNumber, src => src.Shift != null ? src.Shift.ShiftNumber : 0)
            .Map(dest => dest.TestCases, src => src.TestCases);

        config.NewConfig<PracticeDto, PracticeTask>()
            .Map(dest => dest.TestCases, src => src.TestCases);

        config.NewConfig<TestCase, TestCaseDto>();
        config.NewConfig<TestCaseDto, TestCase>();

        // Choice Mappings
        config.NewConfig<Choice, ChoiceDto>();
        config.NewConfig<ChoiceDto, Choice>();

        config.NewConfig<CreateChoiceDto, Choice>();
        config.NewConfig<Choice, CreateChoiceDto>();

        config.NewConfig<UpdateChoiceDto, Choice>()
            .IgnoreNullValues(true);

        // Narrative Mappings
        config.NewConfig<Shift, ShiftDto>();
        config.NewConfig<StoryBeat, BeatDto>();
    }
}
