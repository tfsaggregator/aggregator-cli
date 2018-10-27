using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.Engine
{
    public class TemporaryWorkItemId : WorkItemId<int>
    {
        private static int watermark = -1;

        public TemporaryWorkItemId()
            : base(watermark--)
        {
        }
    }

    public class PermanentWorkItemId : WorkItemId<int>
    {
        public PermanentWorkItemId(int id)
            : base(id)
        {
        }
    }

    public abstract class WorkItemId<T> : IEquatable<WorkItemId<T>>
    {
        public T Value
        {
            get;
            private set;
        }

        public WorkItemId(T id)
        {
            this.Value = id;
        }

        public bool Equals(WorkItemId<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<T>.Default.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((WorkItemId<T>)obj);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<T>.Default.GetHashCode(Value);
        }

        public static bool operator ==(WorkItemId<T> left, WorkItemId<T> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(WorkItemId<T> left, WorkItemId<T> right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return this.Value.ToString();
        }
    }

}
