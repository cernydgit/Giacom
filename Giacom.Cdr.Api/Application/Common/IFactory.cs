namespace Giacom.Cdr.Application.Common
{
    public interface IFactory<out T> 
    {
        T Create();
    }
}
