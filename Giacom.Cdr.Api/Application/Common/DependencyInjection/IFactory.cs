namespace Giacom.Cdr.Application.Common.DependencyInjection
{
    public interface IFactory<out T> 
    {
        T Create();
    }
}
