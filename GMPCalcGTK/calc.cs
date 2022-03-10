// GMP Calc is free software, you may use/copy/modify as you decide.
//
// Provided by IntegriTech.ca. 2022/03/08
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
// Changes:
//    2022/03/08 Changed it to use StringBuilder instead of string in order to handle huge lists of numbers in reasonable time.
//    2021/12/22 Improved dofuncs. Added support for Intel i9.

using System;
using System.Collections;
using System.Text;
using Math.Mpfr.Native;

namespace GMPCalc
{
    public class Calc
    {
        public bool noerr = true;
        private string errmsg = "";
        private ArrayList digits = new ArrayList(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', 'E' });
        private ArrayList plusminus = new ArrayList(new char[] { '+', '-' });
        private ArrayList pmmd = new ArrayList(new char[] { '+', '-', '*', '/' });

        StringBuilder zerosb = new StringBuilder("0");
        uint precision;
        string printprec;
        string calcprec;

        private void error(ref StringBuilder cal, ref int i)
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

        private bool clean(ref StringBuilder toupper)
        {
            bool result;
            int l = 0;
            int r = 0;
            string s;

            toupper.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(",", "").Replace(" ", "");

            noerr = true;

            int len = toupper.Length;
            StringBuilder t = new StringBuilder(len);

            for (int i = 0; i < len; i++)
            {
                char c = toupper[i];
                t.Append(Char.ToUpper(c));
                if (c == '(')
                    l++;
                else if (c == ')')
                    r++;
            }

            if (r != l)
            {
                errmsg = "Unbalanced brackets!";
                noerr = result = false;
            }
            else
            {
                if (t.Length == 0)
                    toupper = zerosb;
                else
                    toupper = t;
                result = true;
            }
            return result;
        }

        private bool subxyz(ref StringBuilder rcal, string x, string y, string z)
        {
            char[] xyz = new char[] { 'X', 'Y', 'Z', ')' };
            char[] mulchrs = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', '(', 'X', 'Y', 'Z' };

            char[] mulchrsrev = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', ')' };
            char[] xyzrev = new char[] { 'X', 'Y', 'Z', 'S', 'C', 'T', 'A', 'L', 'Q', '(' };

            char[] noteuler = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.' };
            char[] noteuler2 = new char[] { '+', '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

            ArrayList digs = new ArrayList(noteuler2);
            ArrayList nums = new ArrayList(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' });

            rcal.Replace("EXP(", "QQQ("); // don't replace that X

            rcal.Replace("X", "(" + x + ")");
            rcal.Replace("Y", "(" + y + ")");
            rcal.Replace("Z", "(" + z + ")");

            bool ret = clean(ref rcal);

            if (!ret)
                return ret;

            rcal.Replace("EXP(", "QQQ(");
            rcal.Replace("SEC(", "Q@@(");  // handle Euler's e
            rcal.Replace("SECH(", "Q$$(");

            // handle Euler's e
            if (rcal.IndexOf('E') != -1)
            {
                int rcallen = rcal.Length;
                StringBuilder eb = new StringBuilder(rcallen);
                bool hit = false;

                for (int k = 0; k < rcallen; k++)
                {   // handle weird things like 3e4e5
                    if (hit)
                    {
                        eb.Append('W');
                        while (k < rcallen - 1 && digs.Contains(rcal[++k]))
                            eb.Append(rcal[k]);

                        if (!digs.Contains(rcal[k]))
                            eb.Append(rcal[k]);
                    }
                    else
                        eb.Append(rcal[k]);

                    hit = false;

                    if (k < rcallen - 2)
                    {
                        string tmp = rcal.ToString(k, 3);

                        for (int i = 0; i < noteuler.Length && !hit; i++)
                            for (int j = 0; j < noteuler2.Length && !hit; j++)
                                if (tmp == noteuler[i].ToString() + "E" + noteuler2[j].ToString())
                                {
                                    bool hit1 = false;

                                    if (plusminus.Contains(noteuler2[j]))
                                    {
                                        if (k < rcallen - 3 && nums.Contains(rcal[k + 3]))
                                            hit1 = true;
                                    }
                                    else
                                        hit1 = true;

                                    if (hit1)
                                    {  // if we find decimal point it's not exponential notation

                                        int k2 = (plusminus.Contains(noteuler2[j]) ? k + 4 : k + 3);

                                        while (k2 < rcallen && nums.Contains(rcal[k2]))
                                            k2++;

                                        if (k2 == rcallen || rcal[k2] != '.')
                                            hit = true;
                                    }
                                }
                    }
                }

                rcal = eb;

                if (rcal.IndexOf('E') != -1)
                {
                    mpfr_t e = new mpfr_t();
                    mpfr_lib.mpfr_init2(e, precision);
                    setgmp(ref e, "1");
                    mpfr_lib.mpfr_exp(e, e, mpfr_rnd_t.MPFR_RNDN);
                    string es = fstr(ref e);
                    mpfr_lib.mpfr_clear(e);

                    rcal.Replace("E", "(" + es + ")");
                }

                rcal.Replace('W', 'E');
            }

            if (rcal.IndexOf("PI") != -1)
            {
                mpfr_t pi = new mpfr_t();
                mpfr_lib.mpfr_init2(pi, precision);
                mpfr_lib.mpfr_const_pi(pi, mpfr_rnd_t.MPFR_RNDN);
                string pis = fstr(ref pi);
                mpfr_lib.mpfr_clear(pi);

                rcal.Replace("PI", "(" + pis + ")");
            }

            // Handle xy, 2x, x2, x(2 + 1), etc, insert *

            for (int i = 0; i < xyz.Length; i++)
                for (int j = 0; j < mulchrs.Length; j++)
                {
                    rcal.Replace(xyz[i].ToString() + mulchrs[j].ToString(), xyz[i].ToString() + "*" + mulchrs[j].ToString());
                }

            for (int i = 0; i < mulchrsrev.Length; i++)
                for (int j = 0; j < xyzrev.Length; j++)
                {
                    rcal.Replace(mulchrsrev[i].ToString() + xyzrev[j].ToString(), mulchrsrev[i].ToString() + "*" + xyzrev[j].ToString());
                }

            rcal.Replace("LOG2*(", "LOG2(");
            rcal.Replace("LOG10*(", "LOG10(");
            rcal.Replace(")(", ")*(");

            rcal.Replace("QQQ(", "EXP(");
            rcal.Replace("Q@@(", "SEC(");
            rcal.Replace("Q$$(", "SECH(");

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

        private StringBuilder fstrsb(ref mpfr_t x)
        {
            return new StringBuilder(mpfr_lib.asprintf("%." + calcprec + "RG", x));
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

        private StringBuilder prevnum(ref StringBuilder temp, int i)
        {
            StringBuilder result;
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
            temp.Remove(i + 1, oldi - i);
            return result;
        }

        private int signs(ref StringBuilder cal, ref int i)
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
                    i++;
            } while (plusminus.Contains(cal[i]));

            return sign;
        }

        private StringBuilder nextnum(ref StringBuilder cal, ref int i)
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

            StringBuilder result = cal.Substring(start, i - start);
            setsign(ref result, sign);

            return result;
        }

        private StringBuilder getbrackets(ref StringBuilder cal, ref int i)
        {
            int count = 1;
            int start = i + 1;
            StringBuilder t;

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

        private void setgmp(ref mpfr_t a, StringBuilder astr)
        {
            mpfr_lib.set_str(a, astr.ToString(), mpfr_rnd_t.MPFR_RNDN);
        }

        private void set2gmp(ref mpfr_t a, ref mpfr_t b, StringBuilder astr, StringBuilder bstr)
        {
            mpfr_lib.set_str(a, astr.ToString(), mpfr_rnd_t.MPFR_RNDN);

            mpfr_lib.set_str(b, bstr.ToString(), mpfr_rnd_t.MPFR_RNDN);
        }

        private void set2gmp(ref mpfr_t a, ref mpfr_t b, StringBuilder astr, string bstr)
        {
            mpfr_lib.set_str(a, astr.ToString(), mpfr_rnd_t.MPFR_RNDN);

            mpfr_lib.set_str(b, bstr, mpfr_rnd_t.MPFR_RNDN);
        }

        private StringBuilder doadd(ref StringBuilder temp)
        {
            int i = 0;
            StringBuilder tot_s;

            if (!fnoerr())
                return zerosb;

            mpfr_t tot = new mpfr_t();
            mpfr_t b = new mpfr_t();

            mpfr_lib.mpfr_init2(tot, precision);
            mpfr_lib.mpfr_init2(b, precision);

            setgmp(ref tot, nextnum(ref temp, ref i));

            int len = temp.Length;

            while (++i < len && fnoerr())
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

            tot_s = fstrsb(ref tot);

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

        private void setsign(ref StringBuilder cal, int sign)
        {
            if (sign == -1)
            {
                if (cal[0] == '-')
                    cal.Remove(0, 1);
                else
                    cal.Insert(0, "-");
            }
        }

        private StringBuilder domuls(StringBuilder cal)
        {
            if (!fnoerr())
                return zerosb;

            int i = 0;
            int sign;
            StringBuilder temp = new StringBuilder();
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
                        temp.Append(cal[i++]);
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
                            temp.Append(s);
                        }
                        else if (cal[i] == '(')
                        {
                            set2gmp(ref a, ref b, prevnum(ref temp, temp.Length - 1), domuls(getbrackets(ref cal, ref i)));
                            mpfr_lib.mpfr_mul(dst, a, b, mpfr_rnd_t.MPFR_RNDN);
                            s = fstr(ref dst);
                            setsign(ref s, sign);
                            temp.Append(s);
                        }
                        else
                        {
                            error(ref cal, ref i);
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
                            temp.Append(s);
                        }
                        else if (cal[i] == '(')
                        {
                            set2gmp(ref a, ref b, prevnum(ref temp, temp.Length - 1), domuls(getbrackets(ref cal, ref i)));
                            mpfr_lib.mpfr_div(dst, a, b, mpfr_rnd_t.MPFR_RNDN);
                            s = fstr(ref dst);
                            setsign(ref s, sign);
                            temp.Append(s);
                        }
                        else
                        {
                            error(ref cal, ref i);
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
                            temp.Append(s);
                        }
                        else if (cal[i] == '(')
                        {
                            set2gmp(ref a, ref b, prevnum(ref temp, temp.Length - 1), domuls(getbrackets(ref cal, ref i)));
                            mpfr_lib.mpfr_fmod(dst, a, b, mpfr_rnd_t.MPFR_RNDN);
                            s = fstr(ref dst);
                            temp.Append(s);
                        }
                        else
                        {
                            error(ref cal, ref i);
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
                        temp.Append(cal.ToString(start, i - start));
                        break;

                    case '(':
                        temp.Append(domuls(getbrackets(ref cal, ref i)).ToString());
                        break;

                    default:
                        error(ref cal, ref i);
                        break;
                }

            } while (i < len && fnoerr());

            if (hasplusminus)
                temp = doadd(ref temp);

            mpfr_lib.mpfr_clears(a, b, dst, null);

            return temp;
        }

        private int fcnt(ref StringBuilder cal, ref int i)
        {
            int j = 0;

            while (cal[i] == '!')
            {
                j++;
                i--;
            }

            i++;
            cal.Remove(i, j);
            return j;
        }

        private StringBuilder getprev(ref StringBuilder cal, ref int i)
        {
            StringBuilder result;
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
                cal.Remove(i + 1, oldi - i);
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
                cal.Remove(j, oldi - j + 1);
                i--;
            }

            return result;
        }

        private StringBuilder getnext(ref StringBuilder cal, int i)
        {
            StringBuilder result;
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

                cal.Remove(oldi, i - oldi);
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

                cal.Remove(oldi, i - oldi + 1);
            }

            return result;
        }

        private StringBuilder dopowers(StringBuilder cal)
        {
            if (!fnoerr())
                return zerosb;

            int i;
            int c;
            StringBuilder x;
            string f;

            i = cal.Length - 1;

            if (i == -1)
                return zerosb;

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
                            f = getprev(ref cal, ref i).ToString();

                            for (int j = 1; j <= c && fnoerr(); j++)
                            {
                                mpfr_lib.mpfr_fac_ui(a, fvalui(f), mpfr_rnd_t.MPFR_RNDN);
                                f = mpfr_lib.asprintf("%d", a);
                            }

                            set2gmp(ref a, ref b, x, f);

                            mpfr_lib.mpfr_pow(dst, b, a, mpfr_rnd_t.MPFR_RNDN);

                            cal.Insert(i + 1, fstr(ref dst));
                        }
                        else
                        {
                            StringBuilder z = getprev(ref cal, ref i);
                            int j = i;
                            if (i < -1)
                                j = -1;

                            set2gmp(ref a, ref b, x, z);

                            mpfr_lib.mpfr_pow(dst, b, a, mpfr_rnd_t.MPFR_RNDN);

                            cal.Insert(j + 1, fstr(ref dst));

                        }
                        break;

                    case '!':
                        c = fcnt(ref cal, ref i);
                        f = getprev(ref cal, ref i).ToString();
                        for (int j = 1; j <= c && fnoerr(); j++)
                        {
                            mpfr_lib.mpfr_fac_ui(a, fvalui(f), mpfr_rnd_t.MPFR_RNDN);
                            f = fstr(ref a);
                        }
                        cal.Insert(i + 1, f);
                        break;

                    default:
                        i--;
                        break;
                }

            } while (i >= 0 && fnoerr());

            mpfr_lib.mpfr_clears(a, b, dst, null);

            return cal;
        }

        private StringBuilder dobrackets(ref StringBuilder cal, ref int i)
        {
            return domuls(dopowers(dofuncs(getbrackets(ref cal, ref i))));
        }

        private bool checkfunc(ref StringBuilder cal, ref int i, ref StringBuilder temp, ref mpfr_t a, ref mpfr_t dst)
        {
            try
            {
                int count = 5;
                if (i + 5 >= cal.Length)
                    count = cal.Length - i - 1;

                string cals = cal.ToString(i + 1, count);
                int bracket = cals.IndexOf('(');

                if (bracket == -1)
                    return false;

                string func = cal[i] + cals.Substring(0, bracket);

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

        private StringBuilder dofuncs(StringBuilder cal)
        {
            if (!fnoerr())
                return zerosb;

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
                            temp.Append(cal.ToString(start, i - start));
                            break;

                        default:
                            if (!checkfunc(ref cal, ref i, ref temp, ref a, ref dst))
                                error(ref cal, ref i);
                            break;
                    }

                } while (i < len && fnoerr());
            }
            catch
            {
                error(ref cal, ref i);
            }

            mpfr_lib.mpfr_clears(a, dst, null);

            return temp;
        }
        
        public string calc(string rcal, string x, string y, string z, string cp, string pp, bool dopretty)
        {
            StringBuilder answer;
            string answerret = "";

            try
            {
                double BITS_PER_DIGIT = 3.32192809488736234787;

                calcprec = cp;
                precision = (uint)(Convert.ToDouble(cp) * BITS_PER_DIGIT + 16.0);
                printprec = pp;
                StringBuilder sb = new StringBuilder(rcal);

                if (clean(ref sb) && subxyz(ref sb, x, y, z))
                {

                    answer = domuls(dopowers(dofuncs(sb)));

                    if (noerr)
                    {
                        mpfr_t a = new mpfr_t();
                        mpfr_lib.mpfr_init2(a, precision);
                        setgmp(ref a, answer);
                        answerret = fprn(ref a);
                        if (dopretty)
                            GMPCalc.utils.pretty(ref answerret);
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
                answerret = errmsg;

            if (Globals.cancelled)
            {
                answerret = "Cancelled";
                Globals.cancelled = false;
            }

            return answerret;

        }

        public string calc(string rcal)
        {
            return calc(rcal, "1", "1", "1", "10005", "10000", true);
        }
    }

    public static class Extensions
    {
        public static StringBuilder Substring(this StringBuilder sb, int startindex, int count)
        {
            try
            {
                return new StringBuilder(sb.ToString(startindex, count));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return new StringBuilder();
        }

        public static int IndexOf(this StringBuilder sb, string value)
        {
            int index;
            int length = value.Length;
            int maxSearchLength = (sb.Length - length) + 1;

            for (int i = 0; i < maxSearchLength; i++)
            {
                if (sb[i] == value[0])
                {
                    index = 1;
                    while ((index < length) && (sb[i + index] == value[index]))
                        ++index;

                    if (index == length)
                        return i;
                }
            }

            return -1;
        }

        public static int IndexOf(this StringBuilder sb, char value)
        {
            int maxSearchLength = sb.Length;

            for (int i = 0; i < maxSearchLength; i++)
                if (sb[i] == value)
                    return i;

            return -1;
        }
    }
}