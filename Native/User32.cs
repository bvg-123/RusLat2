using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RusLat2.Native
{
  using HWND = UInt32;

  /// <summary>
  /// Interop функции User32 и необходимые высокоуровневые API с их участием.
  /// </summary>
  public static class User32
  {
    const int WS_EX_TRANSPARENT = 0x00000020;
    const int GWL_EXSTYLE = (-20);

    [DllImport("user32.dll")]
    static extern int GetWindowLong (IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    static extern int SetWindowLong (IntPtr hwnd, int index, int newStyle);

    public static void SetWindowExTransparent (IntPtr hwnd)
    {
      var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
      SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
    }

    [DllImport("user32.dll")]
    static extern IntPtr GetActiveWindow ();

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId (IntPtr hWnd, IntPtr ProcessId);

    [DllImport("user32.dll")]
    static extern Int32 GetKeyboardLayout (uint idThread);

    public static int GetCurrentLang ()
    {
      return GetKeyboardLayout(GetWindowThreadProcessId(GetActiveWindow(), IntPtr.Zero));
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern int GetClassName (HWND hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    static extern bool PrintWindow (HWND hWnd, IntPtr hdcBlt, int nFlags);


    /// <summary>
    /// Делегат для отбора подходящих окон среди перебираемых.
    /// </summary>
    /// <param name="parentHwnd">Хэндл очередного окна.</param>
    /// <param name="hwnd">Хэндл очередного окна.</param>
    /// <param name="className">Название класса окна.</param>
    /// <param name="caption">Заголовок окна.</param>
    /// <returns>Возвращает true, если нужно продолжать перебор. Если перебор нужно прекратить, то необходимо вернуть false.</returns>
    public delegate bool CheckWindowFunc (HWND parentHwnd, HWND hWnd, string className, string caption);

    /// <summary>Перебирает все открытые окна, включая дочерние.</summary>
    public static void EnumerateWindows (CheckWindowFunc checkWindowFunc)
    {
      EnumWindows(delegate (HWND hWnd, int lParam)
      {
        bool @continue = false;
        string className;
        string caption;
        GetWindowInfo(hWnd, out className, out caption);
        try
        {
          @continue = checkWindowFunc(0, hWnd, className, caption);
          if (@continue)
          {
            if (DoEnumChildWindows(hWnd, checkWindowFunc) == false) @continue = false;
          }
        }
        catch
        {
        }
        return (@continue == true);
      }, 0);
    } // EnumerateWindows


    private static bool? DoEnumChildWindows (HWND hWnd, CheckWindowFunc checkWindowFunc)
    {
      bool? @continue = null;
      EnumChildWindows(hWnd, delegate (HWND hWndChild, int lParamChild)
      {
        string classNameChild;
        string captionChild;
        GetWindowInfo(hWndChild, out classNameChild, out captionChild);
        @continue = checkWindowFunc(hWnd, hWndChild, classNameChild, captionChild);
        if (@continue == true)
        {
          if (DoEnumChildWindows(hWndChild, checkWindowFunc) == false) @continue = false;
        }
        return (@continue == true);
      }, 0);
      return @continue;
    } // DoEnumChildWindows


    private static void GetWindowInfo (HWND hWnd, out string className, out string caption)
    {
      className = String.Empty;
      StringBuilder sbClassName = new StringBuilder(256);
      if (GetClassName(hWnd, sbClassName, sbClassName.Capacity) != 0) className = sbClassName.ToString();
      caption = String.Empty;
      int length = GetWindowTextLength(hWnd);
      if (length > 0)
      {
        StringBuilder sb = new StringBuilder(length);
        GetWindowText(hWnd, sb, length+1);
        caption = sb.ToString();
      }
    } // GetWindowInfo


    private delegate bool EnumWindowsProc (HWND hWnd, int lParam);

    [DllImport("USER32.DLL")]
    private static extern bool EnumWindows (EnumWindowsProc enumFunc, int lParam);

    [DllImport("USER32.DLL")]
    private static extern int GetWindowText (HWND hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("USER32.DLL")]
    private static extern int GetWindowTextLength (HWND hWnd);

    [DllImport("USER32.DLL")]
    private static extern bool IsWindowVisible (HWND hWnd);

    [DllImport("USER32.DLL")]
    private static extern IntPtr GetShellWindow ();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumChildWindows (HWND hwndParent, EnumWindowsProc lpEnumFunc, int lParam);


    public static System.Drawing.Rectangle GetWindowArea (HWND hwnd)
    {
      RECT rect;
      GetWindowRect(hwnd, out rect);
      System.Drawing.Rectangle result = new System.Drawing.Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);
      return result;
    } // GetWindowArea


    public static Bitmap PrintWindow (HWND hwnd)
    {
      RECT rc = GetWindowArea(hwnd);
      Bitmap bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
      Graphics gfxBmp = Graphics.FromImage(bmp);
      try
      {
        IntPtr hdcBitmap = gfxBmp.GetHdc();
        try
        {
          PrintWindow(hwnd, hdcBitmap, 0);
        }
        finally
        {
          gfxBmp.ReleaseHdc(hdcBitmap);
        }
      }
      finally
      {
        gfxBmp.Dispose();
      }
      return bmp;
    } // PrintWindow


    [DllImport("user32.dll")]
    private static extern bool GetClientRect (HWND hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool GetWindowRect (HWND hwnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
      public int Left, Top, Right, Bottom;

      public RECT (int left, int top, int right, int bottom)
      {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
      } // RECT


      public RECT (System.Drawing.Rectangle r) :this(r.Left, r.Top, r.Right, r.Bottom)
      {
      } // RECT


      public int X
      {
        get { return Left; }
        set { Right -= (Left - value); Left = value; }
      } // X


      public int Y
      {
        get { return Top; }
        set { Bottom -= (Top - value); Top = value; }
      } // Y

      public int Height
      {
        get { return Bottom - Top; }
        set { Bottom = value + Top; }
      } // Height


      public int Width
      {
        get { return Right - Left; }
        set { Right = value + Left; }
      } // Width


      public System.Drawing.Point Location
      {
        get { return new System.Drawing.Point(Left, Top); }
        set { X = value.X; Y = value.Y; }
      } // Location


      public System.Drawing.Size Size
      {
        get { return new System.Drawing.Size(Width, Height); }
        set { Width = value.Width; Height = value.Height; }
      } // Size


      public static implicit operator System.Drawing.Rectangle (RECT r)
      {
        return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
      } // operator System.Drawing.Rectangle


      public static implicit operator RECT (System.Drawing.Rectangle r)
      {
        return new RECT(r);
      } // operator RECT


      public static bool operator == (RECT r1, RECT r2)
      {
        return r1.Equals(r2);
      } // operator ==


      public static bool operator != (RECT r1, RECT r2)
      {
        return !r1.Equals(r2);
      } // operator !=


      public bool Equals (RECT r)
      {
        return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
      } // Equals


      public override bool Equals (object obj)
      {
        if (obj is RECT)
          return Equals((RECT)obj);
        else if (obj is System.Drawing.Rectangle)
          return Equals(new RECT((System.Drawing.Rectangle)obj));
        return false;
      } // Equals


      public override int GetHashCode ()
      {
        return ((System.Drawing.Rectangle)this).GetHashCode();
      } // GetHashCode


      public override string ToString ()
      {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
      } // ToString

    } // private struct RECT


  } // class User32


} // namespace RusLat2.Native
