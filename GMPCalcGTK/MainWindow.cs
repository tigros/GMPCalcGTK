using System;
using Gtk;
using System.Collections;
using GMPCalc;
using System.Runtime.Remoting.Messaging;

public partial class MainWindow : Gtk.Window
{
	string gAnswer = "";
	DateTime starttime;
	DateTime endtime;

	delegate string CalcDelegate(string pcal, string x, string y, string z, string cp, string pp, bool dopretty);

	private void SelectAll()
	{
		var start = textview1.Buffer.GetIterAtOffset(0);

		var end = textview1.Buffer.GetIterAtOffset(0);
		end.ForwardToEnd();

		textview1.Buffer.SelectRange(start, end);
	}

	public MainWindow() : base(Gtk.WindowType.Toplevel)
	{
		Build();

		SelectAll();
	}

	private void MessageBox(string msg)
	{
		Console.WriteLine(msg);
		/*
			MessageDialog md = new MessageDialog(null,
				DialogFlags.DestroyWithParent, MessageType.Info,
				ButtonsType.Close, msg);
			md.Run();
			md.Destroy();
			*/
	}

	protected void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		Application.Quit();
		a.RetVal = true;
	}

	private string RunCalc(string rcal, string x, string y, string z, string cp, string pp, bool dopretty)
	{
		Calc c = new Calc();
		string answer = "0";

		try
		{
			answer = c.calc(rcal, x, y, z, cp, pp, dopretty);

			if (!c.noerr)
				answer = (answer != "Cancelled" ? "Error: " + answer : answer);
		}
		catch (Exception ex)
		{
			answer = "Error: " + ex.ToString();
		}

		return answer;
	}

	void CalcCallback(IAsyncResult asyncRes)
	{
		endtime = DateTime.Now;

		AsyncResult ares = (AsyncResult)asyncRes;
		CalcDelegate delg = (CalcDelegate)ares.AsyncDelegate;
		string answer = delg.EndInvoke(asyncRes);

		gAnswer = answer;
	}

	private bool timerTick()
	{
		if (gAnswer != "")
		{
            textview2.Buffer.Text = gAnswer;
			gAnswer = "";

			TimeSpan ts = endtime - starttime;
			timertext.Text = ts.TotalMilliseconds.ToString() + " ms";
			button4.Label = "=";
			return false;
		}

		return true;
	}

	private bool validinput()
	{
		if (xtext.Buffer.Text.Trim() == "")
			xtext.Buffer.Text = "1";

		if (ytext.Buffer.Text.Trim() == "")
			ytext.Buffer.Text = "1";

		if (ztext.Buffer.Text.Trim() == "")
			ztext.Buffer.Text = "1";

		uint x;

		if (!UInt32.TryParse(calcprec.Buffer.Text, out x))
		{
			textview2.Buffer.Text = "Invalid Calc precision!";
			return false;
		}

		if (!UInt32.TryParse(printprec.Buffer.Text, out x))
		{
			textview2.Buffer.Text = "Invalid Print precision!";
			return false;
		}

		return true;
	}

	protected void apress(object sender, EventArgs e)
	{		
		if (!validinput())
			return;
		
		if (button4.Label == "Cancel")
		{
			button4.Label = "Pending";
			Globals.cancelled = true;
			return;
		}
		else if (button4.Label == "Pending")
		{
			button4.Label = "Cancel";
			Globals.cancelled = false;
			return;
		}

		GLib.Timeout.Add(100, new GLib.TimeoutHandler(timerTick));
	
		CalcDelegate cd = RunCalc;

		button4.Label = "Cancel";

        starttime = DateTime.Now;

        IAsyncResult asyncRes = cd.BeginInvoke(textview1.Buffer.Text, xtext.Buffer.Text, ytext.Buffer.Text, ztext.Buffer.Text, 
			calcprec.Buffer.Text, printprec.Buffer.Text, digitgrp.Active, new AsyncCallback(CalcCallback), null);

	}

	protected void clearbuttonclicked (object sender, EventArgs e)
	{
		textview1.Buffer.Text = "";
		textview2.Buffer.Text = "";
	}

	protected void digitgrpchanged (object sender, EventArgs e)
	{
		if (digitgrp.Active)
		{
			textview2.Buffer.Text = GMPCalc.utils.pretty(textview2.Buffer.Text);
		}
		else
		{
			textview2.Buffer.Text = textview2.Buffer.Text.Replace(",", "");
		}
	}

}
