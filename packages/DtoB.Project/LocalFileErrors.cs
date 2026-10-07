namespace DtoB.Project;

public static class LocalFileErrors
{
    // InvalidDataException derives from SystemException, not IOException.
    public static bool IsExpected(Exception error) => error is
        InvalidDataException or IOException or UnauthorizedAccessException or
        ArgumentException or NotSupportedException;
}
