using System;

namespace VeraciLib.Interfaces.Infra;

public interface IConfigService
{
    Task<DateTime> GetLastDateTimeForTwitterCheck();
    Task SetLastDateTimeForTwitterCheck(DateTime last);
}
