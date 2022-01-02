using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace KsWare.RunCalc {

	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {

		public App() {
			var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
			if (!args.Any()) {
				DefaultAction();
				Environment.Exit(0);
			}

			//TODO handle commandline args

			if ( new[]{"/?","-?","/help","-help"}.Contains(args.FirstOrDefault())) {
				//TODO Show Help
				return;
			}

			bool reuse = false;

			foreach (var arg in args) {
				var parts = arg.Split(new []{':', '='}, 2);
				switch (parts[0].ToLowerInvariant()) {
					case "reuse": reuse = GetBool(parts.Skip(1)?.FirstOrDefault(), false);break;
				}
			}

			DefaultAction();
		}

		private static bool GetBool(string? value, bool defaultValue) {
			switch ((value??"").ToLowerInvariant()) {
				case "false":case "off":case "0": return false;
				case "true": case "on": case "1": return true;
				default: return defaultValue;
			}
		}

		private void DefaultAction() {
			SetLayout(0);
			// ActivateExistingOrCreateNew(); 
			KillAllAndCreateNew();
		}

		private void KillAllAndCreateNew() {
			var processes = Process.GetProcessesByName("Calculator").ToArray();
			foreach (var process in processes) {
				process.Kill();
			}
			Process.Start("calc.exe");
		}

		private void ActivateExistingOrCreateNew() {
			var process = Process.GetProcessesByName("Calculator").FirstOrDefault();
			if (process != null) {
				var hWnd = process.MainWindowHandle; // always zero
				if (hWnd == IntPtr.Zero) hWnd = EnumerateProcessWindowHandles(process.Id).FirstOrDefault();

				if (hWnd != IntPtr.Zero) {
					SetForegroundWindow(hWnd); //TODO no focus
					var flags = SetWindowPosFlags.ShowWindow | SetWindowPosFlags.IgnoreMove | SetWindowPosFlags.IgnoreResize;
					SetWindowPos(hWnd, (IntPtr)(-1), 0, 0, 0, 0, flags); // no effect
				}
			}
			else Process.Start("calc.exe");
		}

		private bool EnumWindowCallback(IntPtr hWnd, IntPtr lParam) {
			_windows.Add(hWnd);
			var sb = new StringBuilder();
			GetWindowText(hWnd, sb, 50);
			var windowText = sb.ToString();
			sb.Clear();
			GetClassName(hWnd, sb, 100);
			var windowClass = sb.ToString();

			GetWindowThreadProcessId(hWnd, out var processId);

			Debug.WriteLine($"{windowText} [{windowClass}] - ${processId}");
			return true; // continue enum
		}

		private static void SetLayout(int layout) {
			var calcReg = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Calc", true);
			if (calcReg == null) return;
			//TODO key not existing in Window 10
			calcReg.SetValue("layout", layout);
			//'0=Scientific
			//'1=Standard
			//'2=Developer
			//'3=statistic
		}

		private static string GetWindowText(IntPtr hWnd) {
			var sb = new StringBuilder();
			GetWindowText(hWnd, sb, 50);
			return sb.ToString();
		}
		private static string GetClassName(IntPtr hWnd) {
			var sb = new StringBuilder();
			GetClassName(hWnd, sb, 256);
			return sb.ToString();
		}

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
		private static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

		public delegate bool CallBackPtr(IntPtr hwnd, IntPtr lParam);
		private static CallBackPtr callBackPtr;
		private List<IntPtr> _windows;

		[DllImport("user32.dll")]
		private static extern int EnumWindows(CallBackPtr callPtr, int lparam);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("user32.dll", SetLastError = false)]
		private static extern IntPtr GetDesktopWindow();

		[DllImport("user32.dll")]
		private static extern bool EnumThreadWindows(int dwThreadId, CallBackPtr lpfn, IntPtr lParam);

		private static IEnumerable<IntPtr> EnumerateProcessWindowHandles(int processId) {
			var handles = new List<IntPtr>();

			foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
				EnumThreadWindows(thread.Id, (hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);

			return handles;
		}

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr SetActiveWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

		[Flags]
		private enum SetWindowPosFlags : uint {
			/// <summary>If the calling thread and the thread that owns the window are attached to different input queues,
			/// the system posts the request to the thread that owns the window. This prevents the calling thread from
			/// blocking its execution while other threads process the request.</summary>
			/// <remarks>SWP_ASYNCWINDOWPOS</remarks>
			AsynchronousWindowPosition = 0x4000,
			/// <summary>Prevents generation of the WM_SYNCPAINT message.</summary>
			/// <remarks>SWP_DEFERERASE</remarks>
			DeferErase = 0x2000,
			/// <summary>Draws a frame (defined in the window's class description) around the window.</summary>
			/// <remarks>SWP_DRAWFRAME</remarks>
			DrawFrame = 0x0020,
			/// <summary>Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to
			/// the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE
			/// is sent only when the window's size is being changed.</summary>
			/// <remarks>SWP_FRAMECHANGED</remarks>
			FrameChanged = 0x0020,
			/// <summary>Hides the window.</summary>
			/// <remarks>SWP_HIDEWINDOW</remarks>
			HideWindow = 0x0080,
			/// <summary>Does not activate the window. If this flag is not set, the window is activated and moved to the
			/// top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter
			/// parameter).</summary>
			/// <remarks>SWP_NOACTIVATE</remarks>
			DoNotActivate = 0x0010,
			/// <summary>Discards the entire contents of the client area. If this flag is not specified, the valid
			/// contents of the client area are saved and copied back into the client area after the window is sized or
			/// repositioned.</summary>
			/// <remarks>SWP_NOCOPYBITS</remarks>
			DoNotCopyBits = 0x0100,
			/// <summary>Retains the current position (ignores X and Y parameters).</summary>
			/// <remarks>SWP_NOMOVE</remarks>
			IgnoreMove = 0x0002,
			/// <summary>Does not change the owner window's position in the Z order.</summary>
			/// <remarks>SWP_NOOWNERZORDER</remarks>
			DoNotChangeOwnerZOrder = 0x0200,
			/// <summary>Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to
			/// the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent
			/// window uncovered as a result of the window being moved. When this flag is set, the application must
			/// explicitly invalidate or redraw any parts of the window and parent window that need redrawing.</summary>
			/// <remarks>SWP_NOREDRAW</remarks>
			DoNotRedraw = 0x0008,
			/// <summary>Same as the SWP_NOOWNERZORDER flag.</summary>
			/// <remarks>SWP_NOREPOSITION</remarks>
			DoNotReposition = 0x0200,
			/// <summary>Prevents the window from receiving the WM_WINDOWPOSCHANGING message.</summary>
			/// <remarks>SWP_NOSENDCHANGING</remarks>
			DoNotSendChangingEvent = 0x0400,
			/// <summary>Retains the current size (ignores the cx and cy parameters).</summary>
			/// <remarks>SWP_NOSIZE</remarks>
			IgnoreResize = 0x0001,
			/// <summary>Retains the current Z order (ignores the hWndInsertAfter parameter).</summary>
			/// <remarks>SWP_NOZORDER</remarks>
			IgnoreZOrder = 0x0004,
			/// <summary>Displays the window.</summary>
			/// <remarks>SWP_SHOWWINDOW</remarks>
			ShowWindow = 0x0040,
		}
	}
}
