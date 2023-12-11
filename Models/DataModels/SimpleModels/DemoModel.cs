using Models.GenericControllerDTOs;
using Models.Helpers;

namespace Models.DataModels.SimpleModels;

[ExposeToApi(typeof(DemoRequestDto), typeof(DemoResponseDto))]
public class DemoModel : BaseModel
{
    public string Name { get; set; }
}