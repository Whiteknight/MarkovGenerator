using System.Collections.Generic;

namespace MarkovText
{
    public interface IMarkovChain<T>
    {
        IDictionary<T, IMarkovNode<T>> Nodes { get; }
        void Train(IEnumerable<T> sequence);
        IEnumerable<T> GenerateSequence();
    }
}