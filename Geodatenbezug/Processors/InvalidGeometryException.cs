namespace Geodatenbezug.Processors;

/// <summary>
/// The exception that is thrown when a geometry is invalid.
/// </summary>
public class InvalidGeometryException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidGeometryException"/> class.
    /// </summary>
    public InvalidGeometryException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidGeometryException"/> class with a specified feature ID.
    /// </summary>
    /// <param name="featureId">The feature ID.</param>
    public InvalidGeometryException(int featureId)
        : base(BuildMessage(featureId))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidGeometryException"/> class with a specified message.
    /// </summary>
    /// <param name="message">The message.</param>
    public InvalidGeometryException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidGeometryException"/> class with a specified message and inner exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner exception.</param>
    public InvalidGeometryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Returns the message with the specified feature ID.
    /// </summary>
    /// <param name="featureId">The feature ID.</param>
    /// <returns>The exception message.</returns>
    internal static string BuildMessage(int featureId) => $"Invalid geometry for feature with ID {featureId}";
}
