using System;
using MessengerAPI.Services;

public class ServiceStatus
{
    public ServiceStatus(ISingleMessengerService service)
    {
        if(null == service)
        {
            token = Guid.Empty;
            status = TaskStatus.NotFound;
            return;
        }
        token = service.id;
        status = service.TaskStatus;
    }
    public Guid token;
    /// <summary>
    /// Shows current status of the service
    /// InProgress = 0,
    /// Completed = 1,
    /// Failed = 2,
    /// NotFound = 3,
    /// ConfirmationRequired = 4
    /// </summary>
    public TaskStatus status;
}