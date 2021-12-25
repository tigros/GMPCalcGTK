using System.Collections.Generic;
using Gtk;
using Math.Mpfr.Native;

namespace GMPCalc
{
    public static class Globals
	{
		public static bool cancelled = false;
		public delegate int mpfr_delegate(mpfr_t rop, mpfr_t op, mpfr_rnd_t rnd);
		public static Dictionary<string, mpfr_delegate> funcs = new Dictionary<string, mpfr_delegate>();
	}

	class MainClass
	{
		private static void initfuncs()
		{
			Globals.funcs.Add("SIN", mpfr_lib.mpfr_sin);
			Globals.funcs.Add("SQRT", mpfr_lib.mpfr_sqrt);
			Globals.funcs.Add("SECH", mpfr_lib.mpfr_sech);
			Globals.funcs.Add("SINH", mpfr_lib.mpfr_sinh);
			Globals.funcs.Add("SEC", mpfr_lib.mpfr_sec);
			Globals.funcs.Add("COS", mpfr_lib.mpfr_cos);
			Globals.funcs.Add("COT", mpfr_lib.mpfr_cot);
			Globals.funcs.Add("CSC", mpfr_lib.mpfr_csc);
			Globals.funcs.Add("COSH", mpfr_lib.mpfr_cosh);
			Globals.funcs.Add("COTH", mpfr_lib.mpfr_coth);
			Globals.funcs.Add("CSCH", mpfr_lib.mpfr_csch);
			Globals.funcs.Add("TAN", mpfr_lib.mpfr_tan);
			Globals.funcs.Add("TANH", mpfr_lib.mpfr_tanh);
			Globals.funcs.Add("TRUNC", mpfr_lib.mpfr_trunc);
			Globals.funcs.Add("ATAN", mpfr_lib.mpfr_atan);
			Globals.funcs.Add("ACOSH", mpfr_lib.mpfr_acosh);
			Globals.funcs.Add("ASINH", mpfr_lib.mpfr_asinh);
			Globals.funcs.Add("ATANH", mpfr_lib.mpfr_atanh);
			Globals.funcs.Add("ACOS", mpfr_lib.mpfr_acos);
			Globals.funcs.Add("ASIN", mpfr_lib.mpfr_asin);
			Globals.funcs.Add("ABS", mpfr_lib.mpfr_abs);
			Globals.funcs.Add("LOG10", mpfr_lib.mpfr_log10);
			Globals.funcs.Add("LOG2", mpfr_lib.mpfr_log2);
			Globals.funcs.Add("LOG", mpfr_lib.mpfr_log);
			Globals.funcs.Add("LN", mpfr_lib.mpfr_log);
			Globals.funcs.Add("EXP", mpfr_lib.mpfr_exp);
			Globals.funcs.Add("ROUND", mpfr_lib.mpfr_round);
		}

		public static void Main(string[] args)
		{
			initfuncs();
			Application.Init();
			MainWindow win = new MainWindow();
			win.Show();
			Application.Run();
		}
	}
}
