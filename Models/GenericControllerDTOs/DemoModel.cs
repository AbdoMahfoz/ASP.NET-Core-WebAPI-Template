namespace Models.GenericControllerDTOs
{
    public class BaseDTO
    {
        public string Id { get; set; }
    }

    public class DemoRequestDTO
    {
        public string Name { get; set; }
    }

    public class DemoResponseDTO : BaseDTO
    {
        public string Name { get; set; }
    }
}