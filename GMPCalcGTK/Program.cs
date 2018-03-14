using System;
using Gtk;

namespace GMPCalc
{
	public static class Globals
	{
		public static bool cancelled = false;
	}

	class MainClass
	{
		public static void Main(string[] args)
		{
			Application.Init();
			MainWindow win = new MainWindow();
			win.Show();
			Application.Run();
		}
	}
}
