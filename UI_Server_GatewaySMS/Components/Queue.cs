/*
 * Created by SharpDevelop.
 * User: SoporteSEM
 * Date: 04/04/2017
 * Time: 13:21
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace UI_Server_GatewaySMS
{
	
	public struct Message
	{
		public string numero;

		public string mensaje;
		
		
		public Message(string nro, string msj){
			numero=nro;
			mensaje=msj;
		}

		
	}
	
	public class BlockingQueue<T>
	{
		private bool closing;
		private readonly Queue<T> queue = new Queue<T>();

		public int Count
		{
			get
			{
				lock (queue)
				{
					return queue.Count;
				}
			}
		}

		public BlockingQueue()
		{
			lock (queue)
			{
				closing = false;
				Monitor.PulseAll(queue);
			}
		}

		public bool Enqueue(T item)
		{
			lock (queue)
			{
				if (closing || null == item)
				{
					return false;
				}

				queue.Enqueue(item);

				if (queue.Count == 1)
				{
					// wake up any blocked dequeue
					Monitor.PulseAll(queue);
				}

				return true;
			}
		}


		public void Close()
		{
			lock (queue)
			{
				if (!closing)
				{
					closing = true;
					queue.Clear();
					Monitor.PulseAll(queue);
				}
			}
		}


		public bool TryDequeue(out T value, int timeout = Timeout.Infinite)
		{
			lock (queue)
			{
				while (queue.Count == 0)
				{
					if (closing || (timeout < Timeout.Infinite) || !Monitor.Wait(queue, timeout))
					{
						MessageBox.Show(timeout.ToString());
						value = default(T);
						return false;
						
					}
				}

				value = queue.Dequeue();
				return true;
			}
		}

		public void Clear()
		{
			lock (queue)
			{
				queue.Clear();
				Monitor.Pulse(queue);
			}
		}
	}
}
