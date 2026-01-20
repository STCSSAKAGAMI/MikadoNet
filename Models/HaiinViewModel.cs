using MikadoNet.Dtos;

namespace MikadoNet.Models;

public class HaiinViewModel
{
    public IReadOnlyList<TantoDto> Assignees { get; set; } = Array.Empty<TantoDto>();
}
