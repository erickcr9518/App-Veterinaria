namespace VetPlatform.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"\"{entityName}\" ({key}) no fue encontrado.")
    {
    }
}
