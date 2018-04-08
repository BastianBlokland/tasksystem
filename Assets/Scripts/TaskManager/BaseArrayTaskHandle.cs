using System;
using System.Threading;

namespace Tasks
{
	public abstract class BaseArrayTaskHandle : ITaskHandle, ITaskExecutor
	{
		private readonly int length;
		private readonly TaskRunner runner;

		private bool isScheduled;
		private volatile bool isComplete;
		private int tasksLeft;

		public BaseArrayTaskHandle(int length, TaskRunner runner)
		{
			this.length = length;
			this.runner = runner;
		}

		public void Schedule(int batchSize)
		{
			if(isScheduled)
				throw new Exception("[BatchTaskHandle] Allready scheduled");

			tasksLeft = length;
			int startOffset = batchSize - 1;
			for (int i = 0; i < length; i += batchSize)
			{
				int start = i;
				int end = start + startOffset;
				runner.Schedule(this, start, end >= length ? (length - 1) : end);
			}
			isScheduled = true;
		}

		public void Join()
		{
			if(!isScheduled)
				throw new Exception("[BatchTaskHandle] Has not been scheduled yet");

			while(!isComplete)
				runner.Help();
		}

		//----> RUNNING ON SEPARATE THREAD
		public void ExecuteElement(int index)
		{
			try { ExecuteTask(index); }
			catch(Exception) { }

			if(Interlocked.Decrement(ref tasksLeft) == 0)
				isComplete = true;
		}
		//----> RUNNING ON SEPARATE THREAD

		protected abstract void ExecuteTask(int index);
	}
}