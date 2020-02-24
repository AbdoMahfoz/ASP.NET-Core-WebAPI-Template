using Models.GenericControllerDTOs;
using Models.Helpers;

namespace Models.DataModels.SimpleModels
{
    [ExposeToApi(typeof(DemoRequestDTO), typeof(DemoResponseDTO))]
    public class DemoModel : BaseModel
    {
        public string Name { get; set; }
    }
}