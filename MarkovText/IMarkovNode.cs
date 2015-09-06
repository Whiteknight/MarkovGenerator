namespace MarkovText
{
    public interface IMarkovNode<T>
    {
        T GetRandomNext();
        void AddNext(T token);
        T Value { get; }
    }
}