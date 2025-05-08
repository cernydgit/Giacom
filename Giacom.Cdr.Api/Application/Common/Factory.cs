namespace Giacom.Cdr.Application.Common
{
    public class Factory<T> : IFactory<T>
    {
        private readonly IServiceProvider serviceProvider;
        public Factory(IServiceProvider sp) => serviceProvider = sp;

        public T Create() 
        {
            return ActivatorUtilities.GetServiceOrCreateInstance<T>(serviceProvider);
        }
    }
}
