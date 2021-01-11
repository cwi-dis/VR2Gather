using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace VRT.UserRepresentation.TVM.Utils
{
	public enum TimeGuardStatus
	{
		Unknown,
		Aborted,
		Success
	}
	public class TimeGuard
	{
		private Stopwatch m_SW = new Stopwatch();
		private float m_GuardTime = 3000f;
		private TimeGuardStatus m_Status = TimeGuardStatus.Unknown;

		public TimeGuard(float guardTime)
		{
			m_GuardTime = guardTime;
		}

		public void Trigger(bool restart)
		{
			if (!m_SW.IsRunning)
			{
				m_SW.Start();
				m_Status = TimeGuardStatus.Unknown;
			}
			else if (restart)
			{
				m_SW.Stop();
				m_SW.Reset();
				m_SW.Start();
				m_Status = TimeGuardStatus.Unknown;
			}

		}

		public void Trigger()
		{
			Trigger(false);
		}

		public void Abort()
		{
			if (m_SW.IsRunning)
			{
				m_SW.Stop();
				m_SW.Reset();
			}
			m_Status = TimeGuardStatus.Aborted;
		}

		public TimeGuardStatus Update()
		{
			if (!IsGuarded && m_Status == TimeGuardStatus.Unknown)
			{
				m_Status = TimeGuardStatus.Success;
			}
			return m_Status;
		}

		private bool IsGuarded
		{
			get
			{
				return m_SW.IsRunning && m_SW.ElapsedMilliseconds < m_GuardTime;
			}
		}

		public TimeGuardStatus Status
		{
			get
			{
				return m_Status;
			}
		}

		public float GuardTime
		{
			get
			{
				return m_GuardTime;
			}
			set
			{
				if (value >= 0.0f)
					m_GuardTime = value;
			}
		}
	}
}
