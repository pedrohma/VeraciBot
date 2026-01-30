using System;

namespace VeraciLib.Helper;

public static class StringHelper
{
    public static bool SetIfChanged(string currentValue, string newValue, out string updatedValue)
    {
        updatedValue = currentValue;

        if (string.IsNullOrWhiteSpace(newValue) || currentValue == newValue)
            return false;

        updatedValue = newValue;
        return true;
    }
}
