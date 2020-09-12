using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace aggregator.Engine
{
    [DebuggerDisplay("TempId {" + nameof(Value) + "}")]
    public class TemporaryWorkItemId : WorkItemId
    {
        internal TemporaryWorkItemId(Tracker tracker)
            : base(tracker.GetNextWatermark())
        {
        }
    }

    public class PermanentWorkItemId : WorkItemId
    {
        public PermanentWorkItemId(int id)
            : base(id)
        {
        }
    }

    public abstract class WorkItemId : IEquatable<WorkItemId>
    {
        public int Value
        {
            get;
        }

        protected WorkItemId(int id)
        {
            this.Value = id;
        }

        public virtual bool Equals(WorkItemId other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<int>.Default.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((WorkItemId)obj);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<int>.Default.GetHashCode(Value);
        }

        public static bool operator ==(WorkItemId left, WorkItemId right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(WorkItemId left, WorkItemId right)
        {
            return !Equals(left, right);
        }

        public static implicit operator int(WorkItemId id) => id.Value;

        public override string ToString()
        {
            return this.Value.ToString();
        }
    }

}
