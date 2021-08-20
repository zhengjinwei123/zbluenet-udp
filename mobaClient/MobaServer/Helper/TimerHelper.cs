using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class TimerHelper
{
	private static readonly long epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

	private static long ClientNow() {
		return (DateTime.UtcNow.Ticks - epoch) / 10000; // 毫秒级
	}

	//秒级别
	public static long ClientNowSeconds()
	{
		return (DateTime.UtcNow.Ticks - epoch) / 10000000;//得到秒级别
	}

	public static long Now() {
		return ClientNow();
	}
}