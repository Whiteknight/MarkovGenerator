using System;

namespace MarkovText
{
    public static class RandomHelper
    {
        private static readonly Random _random = new Random();

        public static int Get(int max)
        {
            return _random.Next(max);
        }
    }
}