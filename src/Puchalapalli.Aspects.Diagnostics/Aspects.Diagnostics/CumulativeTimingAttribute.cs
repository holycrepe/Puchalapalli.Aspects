namespace Puchalapalli.Aspects.Diagnostics
{
    using System;
    using PostSharp.Aspects.Dependencies;
    using PostSharp.Patterns.Model;

    [Serializable]
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Delegate)]
    [AspectTypeDependency(AspectDependencyAction.None, typeof(NotifyPropertyChangedAttribute))]
    public class CumulativeTimingAttribute : TimingAttribute
    {
        /// <summary>
        /// Diagnostic PostSharp Aspect which records a method's cumulative execution time
        /// </summary>
        /// <param name="frequency">Report Frequency (in seconds) of a Cumulative <see cref="TimingAttribute">Timing Aspect</see></param>
        /// <param name="prefixType">Prefix Type Name before Method Name when reporting timing</param>
        /// <param name="increaseDepth"></param>
        public CumulativeTimingAttribute(double frequency = -1, bool prefixType = false, bool increaseDepth = false)
            : base(true, frequency, prefixType, increaseDepth)
        {
        }

        /// <summary>
        /// Diagnostic PostSharp Aspect which records a method's cumulative execution time
        /// </summary>
        /// <param name="category">Tracing Category</param>
        /// <param name="frequency">Report Frequency (in seconds) of a Cumulative <see cref="TimingAttribute">Timing Aspect</see></param>
        /// <param name="prefixType">Prefix Type Name before Method Name when reporting timing</param>
        /// <param name="increaseDepth"></param>
        public CumulativeTimingAttribute(string category, double frequency = -1, bool prefixType=false, bool increaseDepth = false)
            : base(category, frequency, prefixType, increaseDepth)
        {
        }
    }
}