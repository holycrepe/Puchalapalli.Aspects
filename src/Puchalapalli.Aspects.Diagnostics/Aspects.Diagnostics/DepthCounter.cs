namespace Puchalapalli.Aspects.Diagnostics
{
    using System;

    public class DepthCounter
    {
        public DepthCounter Decrease()
        {
            Depth -= 1;
            return this;
        }
        public DepthCounter Increase()
        {
            Depth += 1;
            return this;
        }

        public override string ToString()
            =>  this;

        public int Depth;

        public static implicit operator int(DepthCounter value)
            => value.Depth;
        public static implicit operator string(DepthCounter value)
            => new string('\t', Math.Max(0, value.Depth));
    }
}