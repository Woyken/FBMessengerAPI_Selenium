using System;
using System.Collections.Generic;

namespace MessengerAPI.Services
{
    public enum TaskStatus
    {
        InProgress,
        Completed,
        Failed,
        NotFound,
        ConfirmationRequired,
    }

    struct TaskIdentifier
    {
        TaskStatus status;
        Guid id;
        DateTime aliveUntil;
    }

    public interface IMessengerServices
    {
        ISingleMessengerService GetMessenger(Guid token);
        ISingleMessengerService GetCreateMessenger(Guid token);
        bool KeepAlive(Guid token);
        void Cleanup();
    }

    public class MessengerServices : IMessengerServices, IDisposable
    {
        private List<ISingleMessengerService> _messengers = new List<ISingleMessengerService>();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose (bool disposing)
        {
            if(disposing)
            {
                for (int i = 0; i < _messengers.Count; i++)
                {
                    _messengers[i]?.Dispose();
                }
            }
        }

        ISingleMessengerService IMessengerServices.GetMessenger(Guid token)
        {
            var value = _messengers.Find((ISingleMessengerService d) => {
                return d.id == token;
                });
            if(null != value)
            {
                value.KeepAlive();
                return value;
            }
            return null;
        }

        ISingleMessengerService IMessengerServices.GetCreateMessenger(Guid token)
        {
            var value = _messengers.Find((ISingleMessengerService d) => {
                return d.id == token;
                });
            if(null != value)
            {
                value.KeepAlive();
                return value;
            }
            var newValue = new SingleMessengerService(token);
            _messengers.Add(newValue);
            return newValue;
        }

        bool IMessengerServices.KeepAlive(Guid token)
        {
            var value = _messengers.Find((ISingleMessengerService d) => {
                return d.id == token;
                });
            if(null == value)
            {
                return false;
            }
            value.KeepAlive();
            return true;
        }

        void IMessengerServices.Cleanup()
        {
            for (int i = 0; i < _messengers.Count; i++)
            {
                if(_messengers[i].aliveUntil <= DateTime.Now)
                {
                    var removeMe = _messengers[i];
                    _messengers.Remove(removeMe);
                    removeMe.Dispose();
                }
            }
        }
    }
}