// GMP Calc is free software, you may use/copy/modify as you decide.
//
// Provided by IntegriTech.ca. 2013/01/17
// Written by Colin Lamarre, colin@integritech.ca
//
// Many Thanks to GMP, MPIR and MPFR teams! Incredible!
//
// Libraries used: 
//
//	  VS 2010
//	
//	  MPIR v2.6.0 http://www.mpir.org/mpir-2.6.0.tar.bz2
//
//	  MPFR v3.1.1 http://www.mpfr.org/mpfr-current/mpfr-3.1.1.tar.bz2
//	
//	  MPFR in .NET http://mpfr.codeplex.com/
//

using Math.Mpfr.Native;
using System;
using System.Collections;
using System.Text;

namespace GMPCalc
{
    public class Calc
    {
        public bool noerr = true;
        private string errmsg = "";
        private ArrayList digits = new ArrayList(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', 'E' });
        private ArrayList plusminus = new ArrayList(new char[] { '+', '-' });
        private ArrayList pmmd = new ArrayList(new char[] { '+', '-', '*', '/' });

        uint precision;
        string printprec;
        string calcprec;

        private void error(string cal, ref int i)
        {
            string s = "";

            if (noerr)
            {
                for (int j = i - 5; j < i + 15; j++)
                {
                    try
                    {
                        s += cal[j];
                    }
                    catch { }
                }
                errmsg = s;
                noerr = false;
            }
            i = cal.Length;
        }

        private bool clean(ref string toupper)
        {
            bool result;
            int l = 0;
            int r = 0;
            string s;
            StringBuilder t = new StringBuilder();

            toupper = toupper.Trim().Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(",", "");

            noerr = true;

            int len = toupper.Length;

            for (int i = 0; i < len; i++)
            {
                if (toupper[i] != ' ')
                {
                    t.Append(Char.ToUpper(toupper[i]));
                    if (toupper[i] == '(')
                        l++;
                    else if (toupper[i] == ')')
                        r++;
                }
            }

            if (r != l)
            {
                errmsg = "Unbalanced brackets!";
                noerr = result = false;
            }
            else
            {
                s = t.ToString();
                if (s == "")
                    toupper = "0";
                else
                    toupper = s;
                result = true;
            }
            return result;
        }

        private bool subxyz(ref string rcal, string x, string y, string z)
        {
            char[] xyz = new char[] { 'X', 'Y', 'Z', ')' };
            char[] mulchrs = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', '(', 'X', 'Y', 'Z' };

            char[] mulchrsrev = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', ')' };
            char[] xyzrev = new char[] { 'X', 'Y', 'Z', 'S', 'C', 'T', 'A', 'L', 'Q', '(' };

            char[] noteuler = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.' };
            char[] noteuler2 = new char[] { '+', '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

            ArrayList digs = new ArrayList(noteuler2);
            ArrayList nums = new ArrayList(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' });

            string s;

            s = rcal.Replace("EXP(", "QQQ("); // don't replace that X

            s = s.Replace("X", "(" + x + ")");
            s = s.Replace("Y", "(" + y + ")");
            s = s.Replace("Z", "(" + z + ")");

            bool ret = clean(ref s);

            s = s.Replace("EXP(", "QQQ(");
            s = s.Replace("SEC(", "Q@@(");  // handle Euler's e
            s = s.Replace("SECH(", "Q$$(");

            if (ret)
            {
                // handle Euler's e

                StringBuilder eb = new StringBuilder();
                bool hit = false;

                for (int k = 0; k < s.Length; k++)
                {   // handle weird things like 3e4e5 
                    if (hit)
                    {
                        eb.Append('W');
                        while (k < s.Length - 1 && digs.Contains(s[++k]))
                            eb.Append(s[k]);

                        if (!digs.Contains(s[k]))
                            eb.Append(s[k]);
                    }
                    else
                        eb.Append(s[k]);

                    hit = false;

                    if (k < s.Length - 2)
                    {
                        string tmp = s.Substring(k, 3);

                        for (int i = 0; i < noteuler.Length && !hit; i++)
                            for (int j = 0; j < noteuler2.Length && !hit; j++)
                                if (tmp == noteuler[i].ToString() + "E" + noteuler2[j].ToString())
                                {
                                    bool hit1 = false;

                                    if (plusminus.Contains(noteuler2[j]))
                                    {
                                        if (k < s.Length - 3 && nums.Contains(s[k + 3]))
                                            hit1 = true;
                                    }
                                    else
                                        hit1 = true;

                                    if (hit1)
                                    {  // if we find decimal point it's not exponential notation

                                        int k2 = (plusminus.Contains(noteuler2[j]) ? k + 4 : k + 3);

                                        while (k2 < s.Length && nums.Contains(s[k2]))
                                            k2++;

                                        if (k2 == s.Length || s[k2] != '.')
                                            hit = true;
                                    }
                                }
                    }
                }

                s = eb.ToString();

                if (s.IndexOf('E') != -1)
                {
                    mpfr_t e = new mpfr_t();
                    mpfr_lib.mpfr_init2(e, precision);
                    setgmp(ref e, "1");
                    mpfr_lib.mpfr_exp(e, e, mpfr_rnd_t.MPFR_RNDN);
                    string es = fstr(ref e);
                    mpfr_lib.mpfr_clear(e);

                    s = s.Replace("E", "(" + es + ")");
                }

                s = s.Replace("W", "E");

                if (s.IndexOf("PI") != -1)
                {
                    mpfr_t pi = new mpfr_t();
                    mpfr_lib.mpfr_init2(pi, precision);
                    mpfr_lib.mpfr_const_pi(pi, mpfr_rnd_t.MPFR_RNDN);
                    string pis = fstr(ref pi);
                    mpfr_lib.mpfr_clear(pi);

                    s = s.Replace("PI", "(" + pis + ")");
                }


                // Handle xy, 2x, x2, x(2 + 1), etc, insert *

                for (int i = 0; i < xyz.Length; i++)
                    for (int j = 0; j < mulchrs.Length; j++)
                    {
                        s = s.Replace(xyz[i].ToString() + mulchrs[j].ToString(), xyz[i].ToString() + "*" + mulchrs[j].ToString());
                    }

                for (int i = 0; i < mulchrsrev.Length; i++)
                    for (int j = 0; j < xyzrev.Length; j++)
                    {
                        s = s.Replace(mulchrsrev[i].ToString() + xyzrev[j].ToString(), mulchrsrev[i].ToString() + "*" + xyzrev[j].ToString());
                    }

                s = s.Replace("LOG2*(", "LOG2(");
                s = s.Replace("LOG10*(", "LOG10(");
                s = s.Replace(")(", ")*(");
            }

            s = s.Replace("QQQ(", "EXP(");
            s = s.Replace("Q@@(", "SEC(");
            s = s.Replace("Q$$(", "SECH(");

            rcal = s;

            return ret;
        }

        private bool fnoerr()
        {
            if (Globals.cancelled)
                noerr = false;

            return noerr;
        }

        private string fstr(ref mpfr_t x)
        {
            return mpfr_lib.asprintf("%." + calcprec + "RG", x);
        }

        private string fprn(ref mpfr_t x)
        {
            return mpfr_lib.asprintf("%." + printprec + "RG", x);
        }

        private uint fvalui(string s)
        {
            uint result;

            try
            {
                double d = Convert.ToDouble(s);
                d = System.Math.Round(d, 0, MidpointRounding.AwayFromZero);
                result = Convert.ToUInt32(d);
            }
            catch
            {
                result = 0;
                errmsg = "Invalid factorial!";
                noerr = false;
            }

            return result;
        }

        private string prevnum(ref string temp, int i)
        {
            string result;
            int oldi = i;

            try
            {
                while ((i >= 0) && digits.Contains(temp[i]) || ((i > 0) && (temp[i - 1] == 'E') && plusminus.Contains(temp[i])))
                    i--;

                if ((i >= 0) && plusminus.Contains(temp[i]) && ((i == 0) || pmmd.Contains(temp[i - 1])))
                    i--;
            }
            catch { }

            result = temp.Substring(i + 1, oldi - i);
            temp = temp.Remove(i + 1, oldi - i);
            return result;
        }

        private int signs(ref string cal, ref int i)
        {
            int sign = 1;

            do
            {
                if (cal[i] == '-')
                {
                    sign = sign * -1;
                    i++;
                }
                else if (cal[i] == '+')
                {
                    i++;
                }
            } while (plusminus.Contains(cal[i]));

            return sign;
        }

        private string nextnum(ref string cal, ref int i)
        {
            int sign;

            sign = signs(ref cal, ref i);

            int start = i;

            int len = cal.Length;

            while ((i < len) && digits.Contains(cal[i]))
            {
                i++;

                if ((cal[i - 1] == 'E') && plusminus.Contains(cal[i]))
                    i++;
            }

            string result = cal.Substring(start, i - start);
            setsign(ref result, sign);

            return result;
        }

        private string getbrackets(ref string cal, ref int i)
        {
            int count = 1;
            int start = i + 1;
            string t;

            do
            {
                i++;
                if (cal[i] == '(')
                    count++;
                else if (cal[i] == ')')
                    count--;
            } while ((cal[i] != ')') || (count != 0));

            t = cal.Substring(start, i - start);
            i++;

            return t;
        }

        private void setgmp(ref mpfr_t a, string astr)
        {
            mpfr_lib.set_str(a, astr, mpfr_rnd_t.MPFR_RNDN);
        }

        private void set2gmp(ref mpfr_t a, ref mpfr_t b, string astr, string bstr)
        {
            mpfr_lib.set_str(a, astr, mpfr_rnd_t.MPFR_RNDN);
            mpfr_lib.set_str(b, bstr, mpfr_rnd_t.MPFR_RNDN);
        }

        private string doadd(ref string temp)
        {
            int i = 0;
            string tot_s;

            if (!fnoerr())
                return "0";

            mpfr_t tot = new mpfr_t();
            mpfr_t b = new mpfr_t();

            mpfr_lib.mpfr_init2(tot, precision);
            mpfr_lib.mpfr_init2(b, precision);

            setgmp(ref tot, nextnum(ref temp, ref i));

            int len = temp.Length;

            while (++i < len)
            {
                try
                {
                    switch (temp[i - 1])
                    {
                        case '+':
                            setgmp(ref b, nextnum(ref temp, ref i));
                            mpfr_lib.mpfr_add(tot, tot, b, mpfr_rnd_t.MPFR_RNDN);
                            break;

                        case '-':
                            setgmp(ref b, nextnum(ref temp, ref i));
                            mpfr_lib.mpfr_sub(tot, tot, b, mpfr_rnd_t.MPFR_RNDN);
                            break;
                    }
                }
                catch { }
            }

            tot_s = fstr(ref tot);

            mpfr_lib.mpfr_clears(tot, b, null);

            return tot_s;
        }

        private void setsign(ref string cal, int sign)
        {
            if (sign == -1)
            {
                if (cal[0] == '-')
                    cal = cal.Remove(0, 1);
                else
                    cal = cal.Insert(0, "-");
            }
        }

        private string domuls(string cal)
        {
            if (!fnoerr())
                return "0";

            int i = 0;
            int sign;
            string temp = "";
            string s;
            int start;
            int len = cal.Length;
            bool hasplusminus = false;

            mpfr_t a = new mpfr_t();
            mpfr_t b = new mpfr_t();
            mpfr_t dst = new mpfr_t();

            mpfr_lib.mpfr_init2(a, precision);
            mpfr_lib.mpfr_init2(b, precision);
            mpfr_lib.mpfr_init2(dst, precision);

            do
            {
                switch (cal[i])
                {
                    case '+':
                    case '-':
                        temp = temp + cal[i++];
                        hasplusminus = true;
                        break;

                    case '*':
                        i++;
                        sign = signs(ref cal, ref i);
                        if (digits.Contains(cal[i]))
                        {
                            set2gmp(ref a, ref b, prevnum(ref temp, temp.Length - 1), nextnum(ref cal, ref i));
                            mpfr_lib.mpfr_mul(dst, a, b, mpfr_rnd_t.MPFR_RNDN);
                            s = fstr(ref dst);
                            setsign(ref s, sign);
                            temp = temp + s;
                        }
                        else if (cal[i] == '(')
                        {
                            set2gmp(ref a, ref b, prevnum(ref temp, temp.Length - 1), domuls(getbrackets(ref cal, ref i)));
                            mpfr_lib.mpfr_mul(dst, a, b, mpfr_rnd_t.MPFR_RNDN);
                            s = fstr(ref dst);
                            setsign(ref s, sign);
                            temp = temp + s;
                        }
                        else
                        {
                            error(cal, ref i);
                        }
                        break;

                    case '/':
                        i++;
                        sign = signs(ref cal, ref i);
                        if (digits.Contains(cal[i]))
                        {
                            set2gmp(ref a, ref b, prevnum(ref temp, temp.Length - 1), nextnum(ref cal, ref i));
                            mpfr_lib.mpfr_div(dst, a, b, mpfr_rnd_t.MPFR_RNDN);
                            s = fstr(ref dst);
                            setsign(ref s, sign);
                            temp = temp + s;
                        }
                        else if (cal[i] == '(')
                        {
                            set2gmp(ref a, ref b, prevnum(ref temp, temp.Length - 1), domuls(getbrackets(ref cal, ref i)));
                            mpfr_lib.mpfr_div(dst, a, b, mpfr_rnd_t.MPFR_RNDN);
                            s = fstr(ref dst);
                            setsign(ref s, sign);
                            temp = temp + s;
                        }
                        else
                        {
                            error(cal, ref i);
                        }
                        break;

                    case '%':
                        i++;
                        sign = signs(ref cal, ref i);
                        if (digits.Contains(cal[i]))
                        {  // negative of op2 ignored, Derive does it differently??
                            set2gmp(ref a, ref b, prevnum(ref temp, temp.Length - 1), nextnum(ref cal, ref i));
                            mpfr_lib.mpfr_fmod(dst, a, b, mpfr_rnd_t.MPFR_RNDN);
                            s = fstr(ref dst);
                            temp = temp + s;
                        }
                        else if (cal[i] == '(')
                        {
                            set2gmp(ref a, ref b, prevnum(ref temp, temp.Length - 1), domuls(getbrackets(ref cal, ref i)));
                            mpfr_lib.mpfr_fmod(dst, a, b, mpfr_rnd_t.MPFR_RNDN);
                            s = fstr(ref dst);
                            temp = temp + s;
                        }
                        else
                        {
                            error(cal, ref i);
                        }
                        break;

                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '.':
                        start = i;
                        while ((i < len) && digits.Contains(cal[i]))
                        {
                            i++;

                            if ((cal[i - 1] == 'E') && plusminus.Contains(cal[i]))
                                i++;
                        }
                        temp = temp + cal.Substring(start, i - start);
                        break;

                    case '(':
                        temp = temp + domuls(getbrackets(ref cal, ref i));
                        break;

                    default:
                        error(cal, ref i);
                        break;
                }

            } while (i < len && fnoerr());

            if (hasplusminus)
                temp = doadd(ref temp);

            mpfr_lib.mpfr_clears(a, b, dst, null);

            return temp;
        }

        private int fcnt(ref string cal, ref int i)
        {
            int j = 0;

            while (cal[i] == '!')
            {
                j++;
                i--;
            }

            i++;
            cal = cal.Remove(i, j);
            return j;
        }

        private string getprev(ref string cal, ref int i)
        {
            string result;
            int oldi;
            int count;

            i--;
            oldi = i;

            if (cal[i] != ')')
            {
                try
                {
                    while ((i >= 0) && digits.Contains(cal[i]) || ((i > 0) && (cal[i - 1] == 'E') && plusminus.Contains(cal[i])))
                        i--;

                    if ((i >= 0) && plusminus.Contains(cal[i]) && ((i == 0) || pmmd.Contains(cal[i - 1])))
                        i--;
                }
                catch { }

                result = cal.Substring(i + 1, oldi - i);
                cal = cal.Remove(i + 1, oldi - i);
            }
            else
            {
                count = 1;
                while (i >= 0 && ((cal[i] != '(') || (count != 0)))
                {
                    i--;
                    try
                    {
                        if (cal[i] == ')')
                            count++;
                        else if (cal[i] == '(')
                            count--;
                    }
                    catch { }
                }

                result = domuls(dopowers(cal.Substring(i + 1, oldi - i - 1)));
                int j = i;
                if (i < 0)
                    j = 0;
                cal = cal.Remove(j, oldi - j + 1);
                i--;
            }

            return result;
        }

        private string getnext(ref string cal, int i)
        {
            string result;
            int oldi;
            int sign;
            int count;
            int start;

            oldi = i;
            i++;
            sign = signs(ref cal, ref i);

            start = i;

            int len = cal.Length;

            if (cal[i] != '(')
            {
                while ((i < len) && digits.Contains(cal[i]))
                {
                    i++;

                    if ((cal[i - 1] == 'E') && plusminus.Contains(cal[i]))
                        i++;
                }

                result = cal.Substring(start, i - start);
                setsign(ref result, sign);

                cal = cal.Remove(oldi, i - oldi);
            }
            else
            {
                count = 1;
                start = i + 1;

                do
                {
                    i++;
                    if (cal[i] == '(')
                        count++;
                    else if (cal[i] == ')')
                        count--;
                } while (cal[i] != ')' || count != 0);

                result = cal.Substring(start, i - start);

                result = domuls(dopowers(result));
                setsign(ref result, sign);

                cal = cal.Remove(oldi, i - oldi + 1);
            }

            return result;
        }

        private string dopowers(string cal)
        {
            if (!fnoerr())
                return "0";

            int i;
            int c;
            string x;
            string f;

            i = cal.Length - 1;

            if (i == -1)
                return "0";

            mpfr_t a = new mpfr_t();
            mpfr_t b = new mpfr_t();
            mpfr_t dst = new mpfr_t();

            mpfr_lib.mpfr_init2(a, precision);
            mpfr_lib.mpfr_init2(b, precision);
            mpfr_lib.mpfr_init2(dst, precision);

            do
            {
                switch (cal[i])
                {
                    case '^':
                        x = getnext(ref cal, i);
                        if (cal[i - 1] == '!')
                        {
                            i--;
                            c = fcnt(ref cal, ref i);
                            f = getprev(ref cal, ref i);

                            for (int j = 1; j <= c && fnoerr(); j++)
                            {
                                mpfr_lib.mpfr_fac_ui(a, fvalui(f), mpfr_rnd_t.MPFR_RNDN);
                                f = mpfr_lib.asprintf("%d", a);
                            }

                            set2gmp(ref a, ref b, x, f);

                            mpfr_lib.mpfr_pow(dst, b, a, mpfr_rnd_t.MPFR_RNDN);

                            cal = cal.Insert(i + 1, fstr(ref dst));
                        }
                        else
                        {
                            string z = getprev(ref cal, ref i);
                            int j = i;
                            if (i < -1)
                                j = -1;

                            set2gmp(ref a, ref b, x, z);

                            mpfr_lib.mpfr_pow(dst, b, a, mpfr_rnd_t.MPFR_RNDN);

                            cal = cal.Insert(j + 1, fstr(ref dst));

                        }
                        break;

                    case '!':
                        c = fcnt(ref cal, ref i);
                        f = getprev(ref cal, ref i);
                        for (int j = 1; j <= c && fnoerr(); j++)
                        {
                            mpfr_lib.mpfr_fac_ui(a, fvalui(f), mpfr_rnd_t.MPFR_RNDN);
                            f = fstr(ref a);
                        }
                        cal = cal.Insert(i + 1, f);
                        break;

                    default:
                        i--;
                        break;
                }

            } while (i >= 0 && fnoerr());

            mpfr_lib.mpfr_clears(a, b, dst, null);

            return cal;
        }

        private string dobrackets(ref string cal, ref int i)
        {
            return domuls(dopowers(dofuncs(getbrackets(ref cal, ref i))));
        }

        private bool checkfunc(ref string cal, ref int i, ref StringBuilder temp, ref mpfr_t a, ref mpfr_t dst)
        {
            try
            {
                int count = 5;
                if (i + 5 >= cal.Length)
                    count = cal.Length - i - 1;
                int bracket = cal.IndexOf('(', i + 1, count);

                if (bracket == -1)
                    return false;

                string func = cal.Substring(i, bracket - i);

                Globals.mpfr_delegate matchfunc = Globals.funcs[func];
             
                i += func.Length;

                setgmp(ref a, dobrackets(ref cal, ref i));
                matchfunc(dst, a, mpfr_rnd_t.MPFR_RNDN);
                temp.Append(fstr(ref dst));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return false;
        }

        private string dofuncs(string cal)
        {
            if (!noerr)
                return "0";

            int i = 0;
            int start;
            string n3;
            StringBuilder temp = new StringBuilder();

            mpfr_t a = new mpfr_t();
            mpfr_t dst = new mpfr_t();

            mpfr_lib.mpfr_init2(a, precision);
            mpfr_lib.mpfr_init2(dst, precision);

            int len = cal.Length;

            try
            {
                do
                {
                    switch (cal[i])
                    {
                        case '+':
                        case '-':
                        case '*':
                        case '/':
                        case '(':
                        case ')':
                        case '^':
                        case '!':
                        case '%':
                            temp.Append(cal[i++]);
                            break;

                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                        case '.':
                            start = i;
                            while ((i < len) && digits.Contains(cal[i]))
                            {
                                i++;

                                if ((cal[i - 1] == 'E') && plusminus.Contains(cal[i]))
                                    i++;
                            }
                            temp.Append(cal.Substring(start, i - start));
                            break;

                        default:
                            if (!checkfunc(ref cal, ref i, ref temp, ref a, ref dst))
                                error(cal, ref i);
                            break;
                    }

                } while (i < len && noerr);
            }
            catch
            {
                error(cal, ref i);
            }

            mpfr_lib.mpfr_clears(a, dst, null);

            return temp.ToString();
        }
        
        public string calc(string rcal, string x, string y, string z, string cp, string pp, bool dopretty)
        {
            string answer = "0";

            try
            {
                double BITS_PER_DIGIT = 3.32192809488736234787;

                calcprec = cp;
                precision = (uint)(Convert.ToDouble(cp) * BITS_PER_DIGIT + 16.0);
                printprec = pp;

                if (clean(ref rcal) && subxyz(ref rcal, x, y, z))
                {
                    answer = domuls(dopowers(dofuncs(rcal)));
                    //int err = gmp_lib.gmp_errno;

                    if (noerr)
                    {
                        mpfr_t a = new mpfr_t();
                        mpfr_lib.mpfr_init2(a, precision);
                        setgmp(ref a, answer);
                        answer = fprn(ref a);
                        if (dopretty)
                            GMPCalc.utils.pretty(ref answer);
                        mpfr_lib.mpfr_clear(a);
                    }

                    mpfr_lib.mpfr_free_cache();
                }

            }
            catch (Exception ex)
            {
                errmsg = ex.ToString();
                noerr = false;
            }

            if (!noerr)
                answer = errmsg;

            if (Globals.cancelled)
            {
                answer = "Cancelled";
                Globals.cancelled = false;
            }

            return answer;

        }

        public string calc(string rcal)
        {
            return calc(rcal, "1", "1", "1", "10005", "10000", true);
        }
    }
}