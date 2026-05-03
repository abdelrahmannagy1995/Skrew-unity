// ArabicSupportStub.cs
// Wraps the ArabicSupport library (MIT, Abdullah Konash) in the RTLTMPro namespace
// so that LocalizationManager.RtlProcessor finds it via reflection without the
// real RTLTMPro plugin installed.
//
// Source: https://github.com/Konash/arabic-support-unity (MIT License)

using System;
using System.Collections.Generic;
using System.Text;

// ── Public entry-point in the RTLTMPro namespace ─────────────────────────────
namespace RTLTMPro
{
    public static class RTLSupport
    {
        /// <summary>
        /// Shapes Arabic text into connected presentation forms and reverses
        /// the string for correct RTL display in TextMeshPro.
        /// </summary>
        public static string FixRTL(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return ArabicSupportInternal.ArabicFixer.Fix(input, false, true);
        }
    }
}

// ── Internal implementation (Abdullah Konash, MIT) ────────────────────────────
namespace ArabicSupportInternal
{
    public class ArabicFixer
    {
        public static string Fix(string str)                                    => Fix(str, false, true);
        public static string Fix(string str, bool showTashkeel, bool useHinduNumbers)
        {
            ArabicFixerTool.showTashkeel    = showTashkeel;
            ArabicFixerTool.useHinduNumbers = useHinduNumbers;

            if (str.Contains("\n")) str = str.Replace("\n", Environment.NewLine);

            if (str.Contains(Environment.NewLine))
            {
                string[] sep   = new[] { Environment.NewLine };
                string[] parts = str.Split(sep, StringSplitOptions.None);
                if (parts.Length <= 1) return ArabicFixerTool.FixLine(str);
                string result = ArabicFixerTool.FixLine(parts[0]);
                for (int i = 1; i < parts.Length; i++)
                    result += Environment.NewLine + ArabicFixerTool.FixLine(parts[i]);
                return result;
            }
            return ArabicFixerTool.FixLine(str);
        }
    }
}

// ── Enums ─────────────────────────────────────────────────────────────────────
internal enum IsolatedArabicLetters
{
    Hamza=0xFE80,Alef=0xFE8D,AlefHamza=0xFE83,WawHamza=0xFE85,AlefMaksoor=0xFE87,
    AlefMaksora=0xFBFC,HamzaNabera=0xFE89,Ba=0xFE8F,Ta=0xFE95,Tha2=0xFE99,
    Jeem=0xFE9D,H7aa=0xFEA1,Khaa2=0xFEA5,Dal=0xFEA9,Thal=0xFEAB,Ra2=0xFEAD,
    Zeen=0xFEAF,Seen=0xFEB1,Sheen=0xFEB5,S9a=0xFEB9,Dha=0xFEBD,T6a=0xFEC1,
    T6ha=0xFEC5,Ain=0xFEC9,Gain=0xFECD,Fa=0xFED1,Gaf=0xFED5,Kaf=0xFED9,
    Lam=0xFEDD,Meem=0xFEE1,Noon=0xFEE5,Ha=0xFEE9,Waw=0xFEED,Ya=0xFEF1,
    AlefMad=0xFE81,TaMarboota=0xFE93,PersianPe=0xFB56,PersianChe=0xFB7A,
    PersianZe=0xFB8A,PersianGaf=0xFB92,PersianGaf2=0xFB8E,PersianYeh=0xFBFC,
}

internal enum GeneralArabicLetters
{
    Hamza=0x0621,Alef=0x0627,AlefHamza=0x0623,WawHamza=0x0624,AlefMaksoor=0x0625,
    AlefMagsora=0x0649,HamzaNabera=0x0626,Ba=0x0628,Ta=0x062A,Tha2=0x062B,
    Jeem=0x062C,H7aa=0x062D,Khaa2=0x062E,Dal=0x062F,Thal=0x0630,Ra2=0x0631,
    Zeen=0x0632,Seen=0x0633,Sheen=0x0634,S9a=0x0635,Dha=0x0636,T6a=0x0637,
    T6ha=0x0638,Ain=0x0639,Gain=0x063A,Fa=0x0641,Gaf=0x0642,Kaf=0x0643,
    Lam=0x0644,Meem=0x0645,Noon=0x0646,Ha=0x0647,Waw=0x0648,Ya=0x064A,
    AlefMad=0x0622,TaMarboota=0x0629,PersianPe=0x067E,PersianChe=0x0686,
    PersianZe=0x0698,PersianGaf=0x06AF,PersianGaf2=0x06A9,PersianYeh=0x06CC,
}

internal struct ArabicMapping
{
    public int from, to;
    public ArabicMapping(int f, int t) { from=f; to=t; }
}

internal class ArabicTable
{
    private static ArabicMapping[] mapList;
    private static ArabicTable arabicMapper;

    private ArabicTable()
    {
        mapList = new ArabicMapping[] {
            new ArabicMapping((int)GeneralArabicLetters.Hamza,      (int)IsolatedArabicLetters.Hamza),
            new ArabicMapping((int)GeneralArabicLetters.Alef,       (int)IsolatedArabicLetters.Alef),
            new ArabicMapping((int)GeneralArabicLetters.AlefHamza,  (int)IsolatedArabicLetters.AlefHamza),
            new ArabicMapping((int)GeneralArabicLetters.WawHamza,   (int)IsolatedArabicLetters.WawHamza),
            new ArabicMapping((int)GeneralArabicLetters.AlefMaksoor,(int)IsolatedArabicLetters.AlefMaksoor),
            new ArabicMapping((int)GeneralArabicLetters.AlefMagsora,(int)IsolatedArabicLetters.AlefMaksora),
            new ArabicMapping((int)GeneralArabicLetters.HamzaNabera,(int)IsolatedArabicLetters.HamzaNabera),
            new ArabicMapping((int)GeneralArabicLetters.Ba,         (int)IsolatedArabicLetters.Ba),
            new ArabicMapping((int)GeneralArabicLetters.Ta,         (int)IsolatedArabicLetters.Ta),
            new ArabicMapping((int)GeneralArabicLetters.Tha2,       (int)IsolatedArabicLetters.Tha2),
            new ArabicMapping((int)GeneralArabicLetters.Jeem,       (int)IsolatedArabicLetters.Jeem),
            new ArabicMapping((int)GeneralArabicLetters.H7aa,       (int)IsolatedArabicLetters.H7aa),
            new ArabicMapping((int)GeneralArabicLetters.Khaa2,      (int)IsolatedArabicLetters.Khaa2),
            new ArabicMapping((int)GeneralArabicLetters.Dal,        (int)IsolatedArabicLetters.Dal),
            new ArabicMapping((int)GeneralArabicLetters.Thal,       (int)IsolatedArabicLetters.Thal),
            new ArabicMapping((int)GeneralArabicLetters.Ra2,        (int)IsolatedArabicLetters.Ra2),
            new ArabicMapping((int)GeneralArabicLetters.Zeen,       (int)IsolatedArabicLetters.Zeen),
            new ArabicMapping((int)GeneralArabicLetters.Seen,       (int)IsolatedArabicLetters.Seen),
            new ArabicMapping((int)GeneralArabicLetters.Sheen,      (int)IsolatedArabicLetters.Sheen),
            new ArabicMapping((int)GeneralArabicLetters.S9a,        (int)IsolatedArabicLetters.S9a),
            new ArabicMapping((int)GeneralArabicLetters.Dha,        (int)IsolatedArabicLetters.Dha),
            new ArabicMapping((int)GeneralArabicLetters.T6a,        (int)IsolatedArabicLetters.T6a),
            new ArabicMapping((int)GeneralArabicLetters.T6ha,       (int)IsolatedArabicLetters.T6ha),
            new ArabicMapping((int)GeneralArabicLetters.Ain,        (int)IsolatedArabicLetters.Ain),
            new ArabicMapping((int)GeneralArabicLetters.Gain,       (int)IsolatedArabicLetters.Gain),
            new ArabicMapping((int)GeneralArabicLetters.Fa,         (int)IsolatedArabicLetters.Fa),
            new ArabicMapping((int)GeneralArabicLetters.Gaf,        (int)IsolatedArabicLetters.Gaf),
            new ArabicMapping((int)GeneralArabicLetters.Kaf,        (int)IsolatedArabicLetters.Kaf),
            new ArabicMapping((int)GeneralArabicLetters.Lam,        (int)IsolatedArabicLetters.Lam),
            new ArabicMapping((int)GeneralArabicLetters.Meem,       (int)IsolatedArabicLetters.Meem),
            new ArabicMapping((int)GeneralArabicLetters.Noon,       (int)IsolatedArabicLetters.Noon),
            new ArabicMapping((int)GeneralArabicLetters.Ha,         (int)IsolatedArabicLetters.Ha),
            new ArabicMapping((int)GeneralArabicLetters.Waw,        (int)IsolatedArabicLetters.Waw),
            new ArabicMapping((int)GeneralArabicLetters.Ya,         (int)IsolatedArabicLetters.Ya),
            new ArabicMapping((int)GeneralArabicLetters.AlefMad,    (int)IsolatedArabicLetters.AlefMad),
            new ArabicMapping((int)GeneralArabicLetters.TaMarboota, (int)IsolatedArabicLetters.TaMarboota),
            new ArabicMapping((int)GeneralArabicLetters.PersianPe,  (int)IsolatedArabicLetters.PersianPe),
            new ArabicMapping((int)GeneralArabicLetters.PersianChe, (int)IsolatedArabicLetters.PersianChe),
            new ArabicMapping((int)GeneralArabicLetters.PersianZe,  (int)IsolatedArabicLetters.PersianZe),
            new ArabicMapping((int)GeneralArabicLetters.PersianGaf, (int)IsolatedArabicLetters.PersianGaf),
            new ArabicMapping((int)GeneralArabicLetters.PersianGaf2,(int)IsolatedArabicLetters.PersianGaf2),
            new ArabicMapping((int)GeneralArabicLetters.PersianYeh, (int)IsolatedArabicLetters.PersianYeh),
        };
    }

    internal static ArabicTable ArabicMapper
    {
        get { if (arabicMapper == null) arabicMapper = new ArabicTable(); return arabicMapper; }
    }

    internal int Convert(int c)
    {
        for (int i = 0; i < mapList.Length; i++)
            if (mapList[i].from == c) return mapList[i].to;
        return c;
    }
}

internal class TashkeelLocation
{
    public char tashkeel; public int position;
    public TashkeelLocation(char t, int p) { tashkeel=t; position=p; }
}

internal static class ArabicFixerTool
{
    internal static bool showTashkeel    = true;
    internal static bool combineTashkeel = true;
    internal static bool useHinduNumbers = false;
    private  static StringBuilder _sb   = new StringBuilder();

    internal static void RemoveTashkeel(ref string str, out List<TashkeelLocation> tl)
    {
        tl = new List<TashkeelLocation>();
        _sb.Clear(); _sb.EnsureCapacity(str.Length);
        int last = 0, idx = 0;

        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];
            bool isTashkeel = (c>=0x064B && c<=0x0653) || c==0xFC60||c==0xFC61||c==0xFC62;
            if (isTashkeel)
            {
                if (i-last>0) _sb.Append(str,last,i-last);
                last=i+1;
                // combine shadda
                if (combineTashkeel && idx>0)
                {
                    char prev=tl[idx-1].tashkeel;
                    if(c==(char)0x064E&&prev==(char)0x0651){tl[idx-1].tashkeel=(char)0xFC60;continue;}
                    if(c==(char)0x064F&&prev==(char)0x0651){tl[idx-1].tashkeel=(char)0xFC61;continue;}
                    if(c==(char)0x0650&&prev==(char)0x0651){tl[idx-1].tashkeel=(char)0xFC62;continue;}
                    if(c==(char)0x0651&&prev==(char)0x064E){tl[idx-1].tashkeel=(char)0xFC60;continue;}
                    if(c==(char)0x0651&&prev==(char)0x064F){tl[idx-1].tashkeel=(char)0xFC61;continue;}
                    if(c==(char)0x0651&&prev==(char)0x0650){tl[idx-1].tashkeel=(char)0xFC62;continue;}
                }
                tl.Add(new TashkeelLocation(c,i)); idx++;
            }
        }
        if (last!=0) { if(str.Length-last>0)_sb.Append(str,last,str.Length-last); str=_sb.ToString(); }
    }

    internal static void ReturnTashkeel(ref char[] letters, List<TashkeelLocation> tl)
    {
        Array.Resize(ref letters, letters.Length + tl.Count);
        for (int i=0;i<tl.Count;i++)
        {
            for (int j=letters.Length-1;j>tl[i].position;j--)
                letters[j]=letters[j-1];
            letters[tl[i].position]=tl[i].tashkeel;
        }
    }

    internal static string FixLine(string str)
    {
        List<TashkeelLocation> tl;
        RemoveTashkeel(ref str, out tl);

        char[] origin = new char[str.Length];
        char[] final2 = str.ToCharArray();
        for (int i=0;i<origin.Length;i++)
            origin[i]=(char)ArabicTable.ArabicMapper.Convert(str[i]);

        for (int i=0;i<origin.Length;i++)
        {
            bool skip=false;
            if (origin[i]==(char)IsolatedArabicLetters.Lam && i<origin.Length-1)
            {
                if      (origin[i+1]==(char)IsolatedArabicLetters.AlefMaksoor){origin[i]=(char)0xFEF7;final2[i+1]=(char)0xFFFF;skip=true;}
                else if (origin[i+1]==(char)IsolatedArabicLetters.Alef)       {origin[i]=(char)0xFEF9;final2[i+1]=(char)0xFFFF;skip=true;}
                else if (origin[i+1]==(char)IsolatedArabicLetters.AlefHamza)  {origin[i]=(char)0xFEF5;final2[i+1]=(char)0xFFFF;skip=true;}
                else if (origin[i+1]==(char)IsolatedArabicLetters.AlefMad)    {origin[i]=(char)0xFEF3;final2[i+1]=(char)0xFFFF;skip=true;}
            }
            if (!IsIgnored(origin[i]))
            {
                if      (IsMiddle  (origin,i)) final2[i]=(char)(origin[i]+3);
                else if (IsFinish  (origin,i)) final2[i]=(char)(origin[i]+1);
                else if (IsLeading (origin,i)) final2[i]=(char)(origin[i]+2);
            }
            if (skip) i++;
            if (useHinduNumbers) final2[i]=(char)ToHindu(origin[i],final2[i]);
        }

        if (showTashkeel && tl.Count>0) ReturnTashkeel(ref final2, tl);

        _sb.Clear(); _sb.EnsureCapacity(final2.Length);
        List<char> nums = null;

        for (int i=final2.Length-1;i>=0;i--)
        {
            char c=final2[i];
            bool isLatin=char.IsNumber(c)||char.IsLower(c)||char.IsUpper(c)||char.IsSymbol(c)||char.IsPunctuation(c);
            bool isSurrogate=(c>=(char)0xD800&&c<=(char)0xDBFF)||(c>=(char)0xDC00&&c<=(char)0xDFFF);

            if (isSurrogate) { if(nums==null)nums=new List<char>(); nums.Add(c); }
            else if (isLatin)
            {
                if(nums==null)nums=new List<char>();
                if      (c=='(')nums.Add(')');
                else if (c==')')nums.Add('(');
                else if (c=='<')nums.Add('>');
                else if (c=='>')nums.Add('<');
                else            nums.Add(c);
            }
            else
            {
                if(nums!=null&&nums.Count>0){for(int j=nums.Count-1;j>=0;j--)_sb.Append(nums[j]);nums.Clear();}
                if(c!=0xFFFF) _sb.Append(c);
            }
        }
        if(nums!=null&&nums.Count>0){for(int j=nums.Count-1;j>=0;j--)_sb.Append(nums[j]);}
        return _sb.ToString();
    }

    static int ToHindu(char orig, char fin)
    {
        if(orig>='0'&&orig<='9') return 0x0660+(orig-'0');
        return fin;
    }

    static bool IsIgnored(char c)
    {
        bool pf=(c<=(char)0xFEFF&&c>=(char)0xFE70);
        bool pe=c==(char)0xFB56||c==(char)0xFB7A||c==(char)0xFB8A||c==(char)0xFB92||c==(char)0xFB8E;
        bool ok=pf||pe||c==(char)0xFBFC;
        return char.IsPunctuation(c)||char.IsNumber(c)||char.IsLower(c)||char.IsUpper(c)||char.IsSymbol(c)||!ok||c==(char)0x061B;
    }

    static bool IsLeading(char[] l, int i)
    {
        bool prevOk = i==0||l[i-1]==' '||char.IsPunctuation(l[i-1])||l[i-1]=='>'||l[i-1]=='<'
            ||l[i-1]==(char)IsolatedArabicLetters.Alef  ||l[i-1]==(char)IsolatedArabicLetters.Dal
            ||l[i-1]==(char)IsolatedArabicLetters.Thal  ||l[i-1]==(char)IsolatedArabicLetters.Ra2
            ||l[i-1]==(char)IsolatedArabicLetters.Zeen  ||l[i-1]==(char)IsolatedArabicLetters.PersianZe
            ||l[i-1]==(char)IsolatedArabicLetters.Waw   ||l[i-1]==(char)IsolatedArabicLetters.AlefMad
            ||l[i-1]==(char)IsolatedArabicLetters.AlefHamza||l[i-1]==(char)IsolatedArabicLetters.Hamza
            ||l[i-1]==(char)IsolatedArabicLetters.AlefMaksoor||l[i-1]==(char)IsolatedArabicLetters.WawHamza;
        bool selfOk = l[i]!=' '&&l[i]!=(char)IsolatedArabicLetters.Dal&&l[i]!=(char)IsolatedArabicLetters.Thal
            &&l[i]!=(char)IsolatedArabicLetters.Ra2&&l[i]!=(char)IsolatedArabicLetters.Zeen
            &&l[i]!=(char)IsolatedArabicLetters.PersianZe&&l[i]!=(char)IsolatedArabicLetters.Alef
            &&l[i]!=(char)IsolatedArabicLetters.AlefHamza&&l[i]!=(char)IsolatedArabicLetters.AlefMaksoor
            &&l[i]!=(char)IsolatedArabicLetters.AlefMad&&l[i]!=(char)IsolatedArabicLetters.WawHamza
            &&l[i]!=(char)IsolatedArabicLetters.Waw&&l[i]!=(char)IsolatedArabicLetters.Hamza;
        bool nextOk = i<l.Length-1&&l[i+1]!=' '&&l[i+1]!='\n'&&l[i+1]!='\r'
            &&!char.IsPunctuation(l[i+1])&&!char.IsNumber(l[i+1])&&!char.IsSymbol(l[i+1])
            &&!char.IsLower(l[i+1])&&!char.IsUpper(l[i+1])&&l[i+1]!=(char)IsolatedArabicLetters.Hamza;
        return prevOk&&selfOk&&nextOk;
    }

    static bool IsFinish(char[] l, int i)
    {
        if(i==0) return false;
        bool prevOk = l[i-1]!=' '
            &&l[i-1]!=(char)IsolatedArabicLetters.Dal  &&l[i-1]!=(char)IsolatedArabicLetters.Thal
            &&l[i-1]!=(char)IsolatedArabicLetters.Ra2  &&l[i-1]!=(char)IsolatedArabicLetters.Zeen
            &&l[i-1]!=(char)IsolatedArabicLetters.PersianZe&&l[i-1]!=(char)IsolatedArabicLetters.Waw
            &&l[i-1]!=(char)IsolatedArabicLetters.Alef &&l[i-1]!=(char)IsolatedArabicLetters.AlefMad
            &&l[i-1]!=(char)IsolatedArabicLetters.AlefHamza&&l[i-1]!=(char)IsolatedArabicLetters.AlefMaksoor
            &&l[i-1]!=(char)IsolatedArabicLetters.WawHamza&&l[i-1]!=(char)IsolatedArabicLetters.Hamza
            &&!char.IsPunctuation(l[i-1])&&!char.IsSymbol(l[i-1])&&l[i-1]!='>'&&l[i-1]!='<';
        return prevOk && l[i]!=' ' && l[i]!=(char)IsolatedArabicLetters.Hamza;
    }

    static bool IsMiddle(char[] l, int i)
    {
        if(i==0||i>=l.Length-1) return false;
        bool selfOk = l[i]!=(char)IsolatedArabicLetters.Alef&&l[i]!=(char)IsolatedArabicLetters.Dal
            &&l[i]!=(char)IsolatedArabicLetters.Thal&&l[i]!=(char)IsolatedArabicLetters.Ra2
            &&l[i]!=(char)IsolatedArabicLetters.Zeen&&l[i]!=(char)IsolatedArabicLetters.PersianZe
            &&l[i]!=(char)IsolatedArabicLetters.Waw&&l[i]!=(char)IsolatedArabicLetters.AlefMad
            &&l[i]!=(char)IsolatedArabicLetters.AlefHamza&&l[i]!=(char)IsolatedArabicLetters.AlefMaksoor
            &&l[i]!=(char)IsolatedArabicLetters.WawHamza&&l[i]!=(char)IsolatedArabicLetters.Hamza;
        bool prevOk = l[i-1]!=(char)IsolatedArabicLetters.Alef&&l[i-1]!=(char)IsolatedArabicLetters.Dal
            &&l[i-1]!=(char)IsolatedArabicLetters.Thal&&l[i-1]!=(char)IsolatedArabicLetters.Ra2
            &&l[i-1]!=(char)IsolatedArabicLetters.Zeen&&l[i-1]!=(char)IsolatedArabicLetters.PersianZe
            &&l[i-1]!=(char)IsolatedArabicLetters.Waw&&l[i-1]!=(char)IsolatedArabicLetters.AlefMad
            &&l[i-1]!=(char)IsolatedArabicLetters.AlefHamza&&l[i-1]!=(char)IsolatedArabicLetters.AlefMaksoor
            &&l[i-1]!=(char)IsolatedArabicLetters.WawHamza&&l[i-1]!=(char)IsolatedArabicLetters.Hamza
            &&!char.IsPunctuation(l[i-1])&&l[i-1]!='>'&&l[i-1]!='<'&&l[i-1]!=' '&&l[i-1]!='*';
        bool nextOk = l[i+1]!=' '&&l[i+1]!='\r'&&l[i+1]!=(char)IsolatedArabicLetters.Hamza
            &&!char.IsNumber(l[i+1])&&!char.IsSymbol(l[i+1])&&!char.IsPunctuation(l[i+1]);
        return selfOk&&prevOk&&nextOk&&!char.IsPunctuation(l[i+1]);
    }
}
