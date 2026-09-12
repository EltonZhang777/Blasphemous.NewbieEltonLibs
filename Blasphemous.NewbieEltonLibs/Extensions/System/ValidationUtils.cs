using Blasphemous.ModdingAPI;
using System;

namespace Blasphemous.NewbieEltonLibs.Extensions.System;

/// <summary>
/// Provides common predicate-based argument validation.
/// </summary>
public static class ValidationUtils
{
    /// <summary>
    /// Validates a value once and optionally logs or throws when it is invalid.
    /// </summary>
    /// <typeparam name="T">The type of value to validate.</typeparam>
    /// <param name="obj">The value to validate.</param>
    /// <param name="validate">The predicate that determines whether the value is valid.</param>
    /// <param name="logToModLog">Whether to log an invalid-argument message.</param>
    /// <param name="throwError">Whether to throw an <see cref="ArgumentException" /> for an invalid value.</param>
    /// <returns><see langword="true" /> when the value is valid; otherwise, <see langword="false" /> unless <paramref name="throwError" /> is enabled.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validate" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when validation fails and <paramref name="throwError" /> is enabled.</exception>
    public static bool Validate<T>(T obj, Func<T, bool> validate, bool logToModLog = true, bool throwError = false)
    {
        if (validate == null)
        {
            throw new ArgumentNullException(nameof(validate));
        }

        bool isValid = validate(obj);
        if (isValid)
        {
            return true;
        }

        string errorMessage = $"`{obj}` of type `{typeof(T)}` isn't a valid argument";
        if (logToModLog)
        {
            ModLog.Error(errorMessage);
        }

        if (throwError)
        {
            throw new ArgumentException(errorMessage);
        }

        return false;
    }
}
