using System;
using System.Net;
using System.Threading;
using Java2Dotnet.Spider.Redial.RedialManager;

namespace Java2Dotnet.Spider.Redial.AtomicExecutor
{
	public class RedisAtomicExecutor : IAtomicExecutor
	{
		private readonly RedisRedialManager _redisRedialManager;

		public static string HostName { get; set; } = Dns.GetHostName();

		public void Execute(string name, Action action)
		{
			WaitforRedial.WaitforRedialFinish();
			string setKey = GetSetKey();
			string fieldKey = GetFieldKey(name);

			try
			{
				_redisRedialManager.Redis.HashSet(setKey, fieldKey, DateTime.Now.ToString("yyyy-MM-dd hh:mm"));
				action();
			}
			finally
			{
				_redisRedialManager.Redis.HashDelete(setKey, fieldKey);
			}
		}

		public void Execute(string name, Action<object> action, object obj)
		{
			WaitforRedial.WaitforRedialFinish();
			string setKey = GetSetKey();
			string fieldKey = GetFieldKey(name);

			try
			{
				_redisRedialManager.Redis.HashSet(setKey, fieldKey, DateTime.Now.ToString("yyyy-MM-dd hh:mm"));
				action(obj);
			}
			finally
			{
				_redisRedialManager.Redis.HashDelete(setKey, fieldKey);
			}
		}

		public T Execute<T>(string name, Func<object, T> func, object obj)
		{
			WaitforRedial.WaitforRedialFinish();
			string setKey = GetSetKey();
			string fieldKey = GetFieldKey(name);

			try
			{
				//Db.SetAdd(setKey,fieldKey);
				_redisRedialManager.Redis.HashSet(setKey, fieldKey, DateTime.Now.ToString("yyyy-MM-dd hh:mm"));
				return func(obj);
			}
			finally
			{
				_redisRedialManager.Redis.HashDelete(setKey, fieldKey);
			}
		}

		public T Execute<T>(string name, Func<T> func)
		{
			WaitforRedial.WaitforRedialFinish();
			string setKey = GetSetKey();
			string fieldKey = GetFieldKey(name);

			try
			{
				_redisRedialManager.Redis.HashSet(setKey, fieldKey, DateTime.Now.ToString("yyyy-MM-dd hh:mm"));
				return func();
			}
			finally
			{
				_redisRedialManager.Redis.HashDelete(setKey, fieldKey);
			}
		}

		public void WaitAtomicAction()
		{
			while (true)
			{
				ClearTimeoutAction();
				if (_redisRedialManager.Redis.HashLength(GetSetKey()) <= 1)
				{
					break;
				}

				Thread.Sleep(1);
			}
		}

		private void ClearTimeoutAction()
		{
#if RELEASE
			try
			{
#endif
			foreach (var entry in _redisRedialManager.Redis.HashGetAll(GetSetKey()))
			{
				string key = entry.Key;
				string value = entry.Value;

				DateTime dt = DateTime.Parse(value);
				var minutes = (DateTime.Now - dt).TotalMinutes;

				if (minutes > 5)
				{
					_redisRedialManager.Redis.HashDelete(GetSetKey(), key);
				}
			}
#if RELEASE
			}
			catch (Exception)
			{
				
				throw;
			}
#endif
		}

		public static string GetSetKey()
		{
			return HostName;
		}

		private string GetFieldKey(string identity)
		{
			return identity + "_" + Guid.NewGuid().ToString("N");
		}

		public IWaitforRedial WaitforRedial { get; }

		public RedisAtomicExecutor(RedisRedialManager waitforRedial)
		{
			if (waitforRedial == null)
			{
				throw new RedialException("IWaitforRedial can't be null.");
			}
			WaitforRedial = waitforRedial;
			_redisRedialManager = waitforRedial;
		}
	}
}