using Models.Helpers;

namespace Models.DataModels.SimpleModels
{
    [ExposeToApi]
    public class DemoModel : BaseModel
    {
        public string Name { get; set; }
    }
}
