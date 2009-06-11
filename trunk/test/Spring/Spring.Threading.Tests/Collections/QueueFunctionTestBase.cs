using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace Spring.Collections
{
    /// <summary>
    /// Basic functionality test cases for implementation of <see cref="IQueue"/>.
    /// </summary>
    /// <author>Kenneth Xu</author>
    public abstract class QueueFunctionTestBase : CollectionFunctionTestBase
    {
        protected override System.Collections.ICollection Collection
        {
            get { throw new NotImplementedException(); }
        }

        protected abstract IQueue Queue { get; }

        public bool Add(object objectToAdd)
        {
            throw new NotImplementedException();
        }

        public bool Offer(object objectToAdd)
        {
            throw new NotImplementedException();
        }

        public object Remove()
        {
            throw new NotImplementedException();
        }

        public object Poll()
        {
            throw new NotImplementedException();
        }

        public object Element()
        {
            throw new NotImplementedException();
        }

        public object Peek()
        {
            throw new NotImplementedException();
        }

        public bool IsEmpty
        {
            get { throw new NotImplementedException(); }
        }
    }
}
