namespace Puchalapalli.Aspects.Diagnostics
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using Extensions.DateTime;
    using Extensions.Primitives;
    using JetBrains.Annotations;
    using PostSharp.Aspects;
    using PostSharp.Aspects.Dependencies;
    using PostSharp.Extensibility;
    using PostSharp.Patterns.Model;

    [Serializable]
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Delegate)]
    [AspectTypeDependency(AspectDependencyAction.Commute, AspectDependencyPosition.Any, typeof(TimingAttribute))]
    [AspectTypeDependency(
            AspectDependencyAction.Order,
            AspectDependencyPosition.After,
            typeof(NotifyPropertyChangedAttribute), Target = AspectDependencyTarget.Type)]
    [AspectTypeDependency(
          AspectDependencyAction.Order,
          AspectDependencyPosition.After,
          typeof(NotifyPropertyChangedAttribute))]
    [AspectTypeDependency(AspectDependencyAction.None, typeof(NotifyPropertyChangedAttribute))]
    [SuppressMessage("POSTSHARP", "LA0039")]
    [SuppressMessage("POSTSHARP", "PS0114")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class TimingAttribute : OnMethodBoundaryAspect
    {
        private const double DEFAULT_THRESHOLD = 0.6D;
        private const double DEFAULT_REPORT_FREQUENCY = 5D;
        private const bool DEFAULT_IS_CUMULATIVE = false;
        // This field is initialized and serialized at build time, then deserialized at runtime.
        private readonly string category;
        private readonly bool prefixType;
        private readonly bool increaseDepth;
        private readonly bool isCumulative= DEFAULT_IS_CUMULATIVE;
        private readonly TimeSpan frequency = TimeSpan.FromSeconds(DEFAULT_REPORT_FREQUENCY);
        private readonly double threshold= DEFAULT_THRESHOLD;

        // These fields are initialized at runtime. They do not need to be serialized.
        //[NonSerialized]
        //private string enteringMessage;
        [NonSerialized]
        private string exitingMessage;
        [NonSerialized]
        private Stopwatch stopwatch;
        [NonSerialized]
        private TimeSpan cumulative;
        [NonSerialized]
        private DateTime lastReported;
        [NonSerialized]
        private int count;
        [NonSerialized]
        private static readonly DepthCounter depth = new DepthCounter();


        /// <summary>
        /// Diagnostic PostSharp Aspect which records a method's execution time, and optionally records the cumulative execution time.
        /// </summary>
        /// <param name="cumulative">Record cumulative execution time</param>
        /// <param name="frequency">Report Frequency (in seconds) of a Cumulative <see cref="TimingAttribute">Timing Aspect</see></param>
        /// <param name="prefixType">Prefix Type Name before Method Name when reporting timing</param>
        /// <param name="increaseDepth"></param>
        public TimingAttribute(bool cumulative = DEFAULT_IS_CUMULATIVE, double frequency = -1, bool prefixType = false, bool increaseDepth = true)
        {
            // Default constructor, invoked at build time.
            this.isCumulative = cumulative || frequency> -1;
            this.prefixType = prefixType;
            this.increaseDepth = increaseDepth;
            this.frequency = TimeSpan.FromSeconds(frequency > -1 ? frequency : DEFAULT_REPORT_FREQUENCY);
        }

        /// <summary>
        /// Diagnostic PostSharp Aspect which records a method's execution time
        /// </summary>
        /// <param name="threshold">Minimum threshold for reporting</param>
        /// <param name="prefixType">Prefix Type Name before Method Name when reporting timing</param>
        /// <param name="increaseDepth"></param>
        public TimingAttribute(double threshold, bool prefixType=false, bool increaseDepth=true)
        {
            // Constructor specifying the minimum method duration, invoked at build time.
            this.threshold = threshold;
            this.prefixType = prefixType;

            this.increaseDepth = increaseDepth;
        }

        /// <summary>
        /// Diagnostic PostSharp Aspect which records a method's execution time, and optionally records the cumulative execution time.
        /// </summary>
        /// <param name="category">Tracing Category</param>
        /// <param name="cumulative">Record cumulative execution time</param>
        /// <param name="frequency">Report Frequency (in seconds) of a Cumulative <see cref="TimingAttribute">Timing Aspect</see></param>
        /// <param name="prefixType">Prefix Type Name before Method Name when reporting timing</param>
        /// <param name="increaseDepth"></param>
        public TimingAttribute(string category, bool cumulative = DEFAULT_IS_CUMULATIVE, double frequency = -1, bool prefixType = false, bool increaseDepth = true) : this(cumulative: cumulative, frequency: frequency, prefixType: prefixType, increaseDepth: increaseDepth)
        {
            // Constructor specifying the tracing category, and minimum method duration, invoked at build time.
            this.category = category;
        }


        /// <summary>
        /// Diagnostic PostSharp Aspect which records a method's execution time
        /// </summary>
        /// <param name="category">Tracing Category</param>
        /// <param name="threshold">Minimum threshold for reporting</param>
        /// <param name="prefixType">Prefix Type Name before Method Name when reporting timing</param>
        /// <param name="increaseDepth"></param>
        public TimingAttribute(string category, double threshold = DEFAULT_THRESHOLD, bool prefixType = false, bool increaseDepth = true) : this(threshold, prefixType, increaseDepth)
        {
            // Constructor specifying the tracing category, and minimum method duration, invoked at build time.
            this.category = category;
        }


        // Invoked only once at runtime from the static constructor of type declaring the target method.
        public sealed override void RuntimeInitialize([NotNull] MethodBase method)
        {
            var methodName = //method.DeclaringType.FullName + "." +
                (prefixType ? $"{method.DeclaringType?.Name}." : "") +
                (method.IsConstructor ? $"new {method.DeclaringType?.Name}" : method.Name) + "()";
            //this.enteringMessage = "Entering " + methodName;
            //this.exitingMessage = "Exiting " + methodName;
            this.exitingMessage = " --> " + (methodName + ":");
            cumulative = TimeSpan.Zero;
            lastReported = DateTime.Now;
        }

        // Invoked at runtime before that target method is invoked.                
        [SuppressMessage("POSTSHARP", "PS0114")]
        public sealed override void OnEntry(MethodExecutionArgs args)
        { 
            if (increaseDepth)
                depth.Increase();
            stopwatch = Stopwatch.StartNew();
            //Trace.WriteLine(this.enteringMessage, this.category);
        }

        // Invoked at runtime after the target method is invoked (in a finally block).        
        [SuppressMessage("POSTSHARP", "PS0114")]
        public sealed override void OnExit(MethodExecutionArgs args)
        {
            stopwatch.Stop();
            var elapsed = stopwatch.Elapsed;
            var elapsedTime = elapsed.TotalSeconds;
            if (increaseDepth)
                depth.Decrease();
            if (isCumulative)
            {
                count++;
                cumulative += elapsed;
                var now = DateTime.Now;
                if (now - lastReported < frequency)
                {
                    return;
                }
                lastReported = now;
            }
            else
            {
                if (elapsedTime < threshold)
                    return;
            }
            
            var header = $"{depth}{exitingMessage}".PadRight(100 - depth*3);

            var line = isCumulative
                ? $"{header}{elapsedTime.ToFloatingString()} [{cumulative.FormatFriendly()}] x [{count} @ {(count / cumulative.TotalSeconds).ToFloatingString()}/s]"
                : $"{header}{elapsedTime.ToFloatingString()}";

            Trace.WriteLine(line, this.category);
        }
    }
}