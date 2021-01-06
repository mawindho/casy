using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OLS.Casy.Core.Test
{
    [TestClass]
    public class SmartCollectionTest
    {
        [TestMethod]
        public void AddRange_PassNull_ShallArgumentNullException()
        {
            SmartCollection<string> smartCollection = new SmartCollection<string>();
            try
            {
                smartCollection.AddRange(null);
            }
            catch (ArgumentNullException e)
            {
                StringAssert.Equals(e.ParamName, "range");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void InsertRange_PassNullRange_ShallArgumentNullException()
        {
            SmartCollection<string> smartCollection = new SmartCollection<string>();
            try
            {
                smartCollection.InsertRange(0, null);
            }
            catch (ArgumentNullException e)
            {
                StringAssert.Equals(e.ParamName, "range");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void InsertRange_PassNegativeIndex_ShallIndexOutOfRangeException()
        {
            IEnumerable<string> range = new[] { "Test1", "Test2", "Test3" };
            SmartCollection<string> smartCollection = new SmartCollection<string>();
            try
            {
                smartCollection.InsertRange(-1, range);
            }
            catch (IndexOutOfRangeException)
            {
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void InsertRange_PassInvalidIndex_ShallIndexOutOfRangeException()
        {
            IEnumerable<string> range = new[] { "Test1", "Test2", "Test3" };
            SmartCollection<string> smartCollection = new SmartCollection<string>();
            try
            {
                smartCollection.InsertRange(1, range);
            }
            catch (IndexOutOfRangeException)
            {
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void RemoveRange_PassNull_ShallArgumentNullException()
        {
            SmartCollection<string> smartCollection = new SmartCollection<string>();
            try
            {
                smartCollection.RemoveRange(null);
            }
            catch (ArgumentNullException e)
            {
                StringAssert.Equals(e.ParamName, "range");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void Reset_PassNull_ShallArgumentNullException()
        {
            SmartCollection<string> smartCollection = new SmartCollection<string>();
            try
            {
                smartCollection.Reset(null);
            }
            catch (ArgumentNullException e)
            {
                StringAssert.Equals(e.ParamName, "range");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void AddSorted_PassNull_ShallArgumentNullException()
        {
            SmartCollection<string> smartCollection = new SmartCollection<string>();
            try
            {
                smartCollection.AddSorted(null);
            }
            catch (ArgumentNullException e)
            {
                StringAssert.Equals(e.ParamName, "item");
                return;
            }
            Assert.Fail("No exception was thrown");
        }

        [TestMethod]
        public void AddRemoveRange_PassValidData()
        {
            ManualResetEvent eventAwaiter = new ManualResetEvent(false);
            NotifyCollectionChangedEventArgs eventArgs = null;

            SmartCollection<string> smartCollection = new SmartCollection<string>();
            smartCollection.CollectionChanged += (s, e) =>
            {
                eventArgs = e;
                eventAwaiter.Set();
            };
            Assert.AreEqual(0, smartCollection.Count);
            IEnumerable<string> range1 = new[] { "Test1", "Test2", "Test3" };
            smartCollection.AddRange(range1);

            eventAwaiter.WaitOne(1000);
            Assert.AreEqual(3, smartCollection.Count);
            Assert.IsNotNull(eventArgs);

            var newItems = eventArgs.NewItems;
            Assert.IsNull(newItems);

            var oldItems = eventArgs.OldItems;
            Assert.IsNull(oldItems);

            //Assert.AreEqual(3, newItems.Count);
            //CollectionAssert.Contains(newItems, "Test1");
            //CollectionAssert.Contains(newItems, "Test2");
            //CollectionAssert.Contains(newItems, "Test3");

            eventArgs = null;
            eventAwaiter.Reset();

            IEnumerable<string> range2 = new[] { "Test4", "Test5" };
            smartCollection.AddRange(range2);

            eventAwaiter.WaitOne(1000);
            Assert.AreEqual(5, smartCollection.Count);
            Assert.IsNotNull(eventArgs);
            Assert.IsNull(eventArgs.NewItems);
            Assert.IsNull(eventArgs.OldItems);
            //Assert.AreEqual(2, eventArgs.NewItems.Count);
            //CollectionAssert.Contains(eventArgs.NewItems, "Test4");
            //CollectionAssert.Contains(eventArgs.NewItems, "Test5");
            Assert.IsTrue(smartCollection.IndexOf("Test4") == 3);
            Assert.IsTrue(smartCollection.IndexOf("Test5") == 4);

            eventArgs = null;
            eventAwaiter.Reset();

            IEnumerable<string> range3 = new[] { "Test6", "Test7" };
            smartCollection.InsertRange(1, range3);

            eventAwaiter.WaitOne(1000);
            Assert.AreEqual(7, smartCollection.Count);
            Assert.IsNotNull(eventArgs);
            Assert.IsNull(eventArgs.NewItems);
            Assert.IsNull(eventArgs.OldItems);
            //Assert.AreEqual(2, eventArgs.NewItems.Count);
            //CollectionAssert.Contains(eventArgs.NewItems, "Test6");
            //CollectionAssert.Contains(eventArgs.NewItems, "Test7");
            Assert.IsTrue(smartCollection.IndexOf("Test6") == 1);
            Assert.IsTrue(smartCollection.IndexOf("Test7") == 2);

            eventArgs = null;
            eventAwaiter.Reset();

            IEnumerable<string> range4 = new[] { "Test1", "Test4", "Test6" };
            smartCollection.RemoveRange(range4);

            eventAwaiter.WaitOne(1000);
            Assert.AreEqual(4, smartCollection.Count);
            Assert.IsNotNull(eventArgs);
            Assert.IsNull(eventArgs.NewItems);
            Assert.IsNull(eventArgs.OldItems);
            //Assert.AreEqual(3, eventArgs.OldItems.Count);
            //CollectionAssert.Contains(eventArgs.OldItems, "Test1");
            //CollectionAssert.Contains(eventArgs.OldItems, "Test4");
            //CollectionAssert.Contains(eventArgs.OldItems, "Test6");
            CollectionAssert.DoesNotContain(smartCollection, "Test1");
            CollectionAssert.DoesNotContain(smartCollection, "Test4");
            CollectionAssert.DoesNotContain(smartCollection, "Test6");
            Assert.IsTrue(smartCollection.IndexOf("Test5") == 3);

            eventArgs = null;
            eventAwaiter.Reset();

            IEnumerable<string> range5 = new[] { "Test7", "Test9" };
            smartCollection.Reset(range5);

            eventAwaiter.WaitOne(1000);
            Assert.AreEqual(2, smartCollection.Count);
            Assert.IsNotNull(eventArgs);

            CollectionAssert.DoesNotContain(smartCollection, "Test2");
            CollectionAssert.DoesNotContain(smartCollection, "Test3");
            CollectionAssert.DoesNotContain(smartCollection, "Test5");

            CollectionAssert.Contains(smartCollection, "Test7");
            CollectionAssert.Contains(smartCollection, "Test9");

            eventArgs = null;
            eventAwaiter.Reset();

            smartCollection.AddSorted("Test8");
            eventAwaiter.WaitOne(1000);
            Assert.AreEqual(3, smartCollection.Count);
            Assert.IsNotNull(eventArgs);

            Assert.IsTrue(smartCollection.IndexOf("Test8") == 1);
        }
    }
}
