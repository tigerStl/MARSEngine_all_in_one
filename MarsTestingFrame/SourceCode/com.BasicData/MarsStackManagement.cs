using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace com.BasicData
{
    public class MarsQueueManagement<T>
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsQueueManagement<T>));

        public static MarsQueueManagement<T> GetInstance()
        {
            return new MarsQueueManagement<T>();
        }

        private MarsQueueManagement()
        { }

        private Queue<T> currentQueue;
        private string accessLock = "accessLock";
        public Queue<T> CurrentQueue
        {
            get { return currentQueue; }
            set { currentQueue = value; }
        }

        public void Add(T obj)
        {
            if (currentQueue == null)
                currentQueue = new Queue<T>();
            try
            {
                Monitor.Enter(accessLock);
                currentQueue.Enqueue(obj);
            }
            catch (Exception e)
            {
                Logger.Error("Add",string.Format("Exception:[{0}]",e.Message),e);
            }
            finally
            {
                Monitor.Exit(accessLock);
            }
        }

        public T Peek()
        {
            if (currentQueue == null)
                currentQueue = new Queue<T>();
            try
            {
                Monitor.Enter(accessLock);
                if (currentQueue.Count <= 0) return default(T);
                return currentQueue.Dequeue();
            }
            catch (Exception e)
            {
                Logger.Error("Add", string.Format("Exception:[{0}]", e.Message), e);
                return default(T);
            }
            finally
            {
                Monitor.Exit(accessLock);
            }
        }

        public int GetCount()
        {
            return currentQueue == null ? 0 : currentQueue.Count;
        }

        public void CleanQueue()
        {
            if (currentQueue == null)
                currentQueue = new Queue<T>();
            try
            {
                Monitor.Enter(accessLock);
                currentQueue.Clear();
            }
            catch (Exception e)
            {
                Logger.Error("Add", string.Format("Exception:[{0}]", e.Message), e);
            }
            finally
            {
                Monitor.Exit(accessLock);
            }
        }
    }


    public class MarsStackManagement<T>
    {
        public static MarsStackManagement<T> GetInstance()
        {
            return new MarsStackManagement<T>();
        }

        private MarsStackManagement()
        {

        }

        private Stack<T> currentStack;
        private string accessLock = "accessLock";
        public Stack<T> CurrentStack
        {
            get { return currentStack; }
            set { currentStack = value; }
        }

        private int maxStackSize = 1000;
        public int MaxStackSize
        {
            get { return maxStackSize; }
            set { maxStackSize = value; }
        }

        public void Push(IList<T> lstObj)
        {
            if (currentStack == null)
                currentStack = new Stack<T>();
            try
            {
                Monitor.Enter(accessLock);

                while (currentStack.Count >= maxStackSize)
                {
                    currentStack.Pop();
                }
                if (lstObj == null) return;
                foreach(var itm in lstObj)
                    currentStack.Push(itm);
            }
            finally
            {
                Monitor.Exit(accessLock);
            }
        }
        public void Push(T obj)
        {
            if (currentStack == null)
                currentStack = new Stack<T>();
            try
            {
                Monitor.Enter(accessLock);
                
                while(currentStack.Count>=maxStackSize)
                {
                    currentStack.Pop();
                }
                currentStack.Push(obj);                
            }
            finally
            {
                Monitor.Exit(accessLock);
            }
        }

        public T Pop()
        {
            
            if (currentStack == null)
                currentStack = new Stack<T>();
            try
            {
                Monitor.Enter(accessLock);
                if (this.currentStack.Count == 0) return default(T);
                return this.currentStack.Pop();
            }
            finally
            {
                Monitor.Exit(accessLock);
            }
        }

        internal void clean()
        {
            try
            {
                Monitor.Enter(accessLock);
                if (this.currentStack==null)
                    currentStack = new Stack<T>();
                if (this.currentStack.Count == 0) return ;
                this.currentStack.Clear();
            }
            finally
            {
                Monitor.Exit(accessLock);
            }
        }
    }
}
