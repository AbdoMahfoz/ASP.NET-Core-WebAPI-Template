namespace Models.GenericControllerDTOs;

public class BaseDto
{
    public string Id { get; set; }
}

public class DemoRequestDto
{
    public string Name { get; set; }
}

public class DemoResponseDto : BaseDto
{
    public string Name { get; set; }
}