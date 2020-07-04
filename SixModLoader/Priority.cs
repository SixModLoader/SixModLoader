using System;

namespace SixModLoader
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly | AttributeTargets.Method)]
    public class PriorityAttribute : Attribute
    {
        public int Priority { get; set; }

        public PriorityAttribute(Priority priority)
        {
            Priority = (int) priority;
        }

        public PriorityAttribute(int priority)
        {
            Priority = priority;
        }
    }

    public enum Priority
    {
        Lowest = -100,
        Low = -10,
        Normal = 0,
        High = 10,
        Highest = 100
    }
}