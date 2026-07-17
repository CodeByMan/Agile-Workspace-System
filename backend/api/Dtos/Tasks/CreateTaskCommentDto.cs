using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Tasks;

public sealed class CreateTaskCommentDto
{
    [Required, StringLength(2000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
}
