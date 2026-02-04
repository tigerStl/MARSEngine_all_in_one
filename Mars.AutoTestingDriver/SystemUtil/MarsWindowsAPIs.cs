
using Microsoft.Win32.SafeHandles;
#if !_unitTest
//using OpenCvSharp;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;


#if _NET4
using System.Threading.Tasks;
using System.Windows.Markup;
using static Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs;
#endif
#if _EngineDriver
namespace MarsEnginer.windowsWrapper.SystemUtil
#else
#if _withMessageNamespace
namespace Mars.message.windowsWrapper.SystemUtil
#else
namespace Mars.windowsWrapper.SystemUtil
#endif
#endif
{

    public enum IMAGE_FILE_HEADER
    {
        IMAGE_FILE_MACHINE_UNKNOWN = 0x0,
        IMAGE_FILE_MACHINE_TARGET_HOST = 0x1,
        IMAGE_FILE_MACHINE_I386 = 0x014c,// Intel 386.
        IMAGE_FILE_MACHINE_R3000 = 0x0162,// MIPS little-endian, 0x160 big-endian
        IMAGE_FILE_MACHINE_R4000 = 0x0166,// MIPS little-endian
        IMAGE_FILE_MACHINE_R10000 = 0x0168,// MIPS little-endian
        IMAGE_FILE_MACHINE_WCEMIPSV2 = 0x0169,// MIPS little-endian WCE v2
        IMAGE_FILE_MACHINE_ALPHA = 0x0184,// Alpha_AXP
        IMAGE_FILE_MACHINE_SH3 = 0x01a2,// SH3 little-endian
        IMAGE_FILE_MACHINE_SH3DSP = 0x01a3,
        IMAGE_FILE_MACHINE_SH3E = 0x01a4,// SH3E little-endian
        IMAGE_FILE_MACHINE_SH4 = 0x01a6,// SH4 little-endian
        IMAGE_FILE_MACHINE_SH5 = 0x01a8,// SH5
        IMAGE_FILE_MACHINE_ARM = 0x01c0,// ARM Little-Endian
        IMAGE_FILE_MACHINE_THUMB = 0x01c2,// ARM Thumb/Thumb-2 Little-Endian
        IMAGE_FILE_MACHINE_ARMNT = 0x01c4,// ARM Thumb-2 Little-Endian
        IMAGE_FILE_MACHINE_AM33 = 0x01d3,
        IMAGE_FILE_MACHINE_POWERPC = 0x01F0,// IBM PowerPC Little-Endian
        IMAGE_FILE_MACHINE_POWERPCFP = 0x01f1,
        IMAGE_FILE_MACHINE_IA64 = 0x0200,// Intel 64
        IMAGE_FILE_MACHINE_MIPS16 = 0x0266,// MIPS
        IMAGE_FILE_MACHINE_ALPHA64 = 0x0284,// ALPHA64
        IMAGE_FILE_MACHINE_MIPSFPU = 0x0366,// MIPS
        IMAGE_FILE_MACHINE_MIPSFPU16 = 0x0466,// MIPS
        IMAGE_FILE_MACHINE_AXP64 = IMAGE_FILE_MACHINE_ALPHA64,
        IMAGE_FILE_MACHINE_TRICORE = 0x0520,// Infineon
        IMAGE_FILE_MACHINE_CEF = 0x0CEF,
        IMAGE_FILE_MACHINE_EBC = 0x0EBC,// EFI Byte Code
        IMAGE_FILE_MACHINE_AMD64 = 0x8664,// AMD64 (K8)
        IMAGE_FILE_MACHINE_M32R = 0x9041,// M32R little-endian
        IMAGE_FILE_MACHINE_ARM64 = 0xAA64,// ARM64 Little-Endian
        IMAGE_FILE_MACHINE_CEE = 0xC0EE
    }

    public enum SystemMetric
    {
        SM_CXSCREEN = 0,  // 0x00
        SM_CYSCREEN = 1,  // 0x01
        SM_CXVSCROLL = 2,  // 0x02
        SM_CYHSCROLL = 3,  // 0x03
        SM_CYCAPTION = 4,  // 0x04
        SM_CXBORDER = 5,  // 0x05
        SM_CYBORDER = 6,  // 0x06
        SM_CXDLGFRAME = 7,  // 0x07
        SM_CXFIXEDFRAME = 7,  // 0x07
        SM_CYDLGFRAME = 8,  // 0x08
        SM_CYFIXEDFRAME = 8,  // 0x08
        SM_CYVTHUMB = 9,  // 0x09
        SM_CXHTHUMB = 10, // 0x0A
        SM_CXICON = 11, // 0x0B
        SM_CYICON = 12, // 0x0C
        SM_CXCURSOR = 13, // 0x0D
        SM_CYCURSOR = 14, // 0x0E
        SM_CYMENU = 15, // 0x0F
        SM_CXFULLSCREEN = 16, // 0x10
        SM_CYFULLSCREEN = 17, // 0x11
        SM_CYKANJIWINDOW = 18, // 0x12
        SM_MOUSEPRESENT = 19, // 0x13
        SM_CYVSCROLL = 20, // 0x14
        SM_CXHSCROLL = 21, // 0x15
        SM_DEBUG = 22, // 0x16
        SM_SWAPBUTTON = 23, // 0x17
        SM_CXMIN = 28, // 0x1C
        SM_CYMIN = 29, // 0x1D
        SM_CXSIZE = 30, // 0x1E
        SM_CYSIZE = 31, // 0x1F
        SM_CXSIZEFRAME = 32, // 0x20
        SM_CXFRAME = 32, // 0x20
        SM_CYSIZEFRAME = 33, // 0x21
        SM_CYFRAME = 33, // 0x21
        SM_CXMINTRACK = 34, // 0x22
        SM_CYMINTRACK = 35, // 0x23
        SM_CXDOUBLECLK = 36, // 0x24
        SM_CYDOUBLECLK = 37, // 0x25
        SM_CXICONSPACING = 38, // 0x26
        SM_CYICONSPACING = 39, // 0x27
        SM_MENUDROPALIGNMENT = 40, // 0x28
        SM_PENWINDOWS = 41, // 0x29
        SM_DBCSENABLED = 42, // 0x2A
        SM_CMOUSEBUTTONS = 43, // 0x2B
        SM_SECURE = 44, // 0x2C
        SM_CXEDGE = 45, // 0x2D
        SM_CYEDGE = 46, // 0x2E
        SM_CXMINSPACING = 47, // 0x2F
        SM_CYMINSPACING = 48, // 0x30
        SM_CXSMICON = 49, // 0x31
        SM_CYSMICON = 50, // 0x32
        SM_CYSMCAPTION = 51, // 0x33
        SM_CXSMSIZE = 52, // 0x34
        SM_CYSMSIZE = 53, // 0x35
        SM_CXMENUSIZE = 54, // 0x36
        SM_CYMENUSIZE = 55, // 0x37
        SM_ARRANGE = 56, // 0x38
        SM_CXMINIMIZED = 57, // 0x39
        SM_CYMINIMIZED = 58, // 0x3A
        SM_CXMAXTRACK = 59, // 0x3B
        SM_CYMAXTRACK = 60, // 0x3C
        SM_CXMAXIMIZED = 61, // 0x3D
        SM_CYMAXIMIZED = 62, // 0x3E
        SM_NETWORK = 63, // 0x3F
        SM_CLEANBOOT = 67, // 0x43
        SM_CXDRAG = 68, // 0x44
        SM_CYDRAG = 69, // 0x45
        SM_SHOWSOUNDS = 70, // 0x46
        SM_CXMENUCHECK = 71, // 0x47
        SM_CYMENUCHECK = 72, // 0x48
        SM_SLOWMACHINE = 73, // 0x49
        SM_MIDEASTENABLED = 74, // 0x4A
        SM_MOUSEWHEELPRESENT = 75, // 0x4B
        SM_XVIRTUALSCREEN = 76, // 0x4C
        SM_YVIRTUALSCREEN = 77, // 0x4D
        SM_CXVIRTUALSCREEN = 78, // 0x4E
        SM_CYVIRTUALSCREEN = 79, // 0x4F
        SM_CMONITORS = 80, // 0x50
        SM_SAMEDISPLAYFORMAT = 81, // 0x51
        SM_IMMENABLED = 82, // 0x52
        SM_CXFOCUSBORDER = 83, // 0x53
        SM_CYFOCUSBORDER = 84, // 0x54
        SM_TABLETPC = 86, // 0x56
        SM_MEDIACENTER = 87, // 0x57
        SM_STARTER = 88, // 0x58
        SM_SERVERR2 = 89, // 0x59
        SM_MOUSEHORIZONTALWHEELPRESENT = 91, // 0x5B
        SM_CXPADDEDBORDER = 92, // 0x5C
        SM_DIGITIZER = 94, // 0x5E
        SM_MAXIMUMTOUCHES = 95, // 0x5F

        SM_REMOTESESSION = 0x1000, // 0x1000
        SM_SHUTTINGDOWN = 0x2000, // 0x2000
        SM_REMOTECONTROL = 0x2001, // 0x2001


        SM_CONVERTIBLESLATEMODE = 0x2003,
        SM_SYSTEMDOCKED = 0x2004,
    }
    public delegate bool EnumThreadDelegate(IntPtr hwnd, IntPtr lParam);

    /// <summary>
    /// Windows Messages
    /// Defined in winuser.h from Windows SDK v6.1
    /// Documentation pulled from MSDN.
    /// </summary>
    public enum WM : uint
    {
        /// <summary>
        /// The WM_NULL message performs no operation. An application sends the WM_NULL message if it wants to post a message that the recipient window will ignore.
        /// </summary>
        NULL = 0x0000,
        /// <summary>
        /// The WM_CREATE message is sent when an application requests that a window be created by calling the CreateWindowEx or CreateWindow function. (The message is sent before the function returns.) The window procedure of the new window receives this message after the window is created, but before the window becomes visible.
        /// </summary>
        CREATE = 0x0001,
        /// <summary>
        /// The WM_DESTROY message is sent when a window is being destroyed. It is sent to the window procedure of the window being destroyed after the window is removed from the screen. 
        /// This message is sent first to the window being destroyed and then to the child windows (if any) as they are destroyed. During the processing of the message, it can be assumed that all child windows still exist.
        /// /// </summary>
        DESTROY = 0x0002,
        /// <summary>
        /// The WM_MOVE message is sent after a window has been moved. 
        /// </summary>
        MOVE = 0x0003,
        /// <summary>
        /// The WM_SIZE message is sent to a window after its size has changed.
        /// </summary>
        SIZE = 0x0005,
        /// <summary>
        /// The WM_ACTIVATE message is sent to both the window being activated and the window being deactivated. If the windows use the same input queue, the message is sent synchronously, first to the window procedure of the top-level window being deactivated, then to the window procedure of the top-level window being activated. If the windows use different input queues, the message is sent asynchronously, so the window is activated immediately. 
        /// </summary>
        ACTIVATE = 0x0006,
        /// <summary>
        /// The WM_SETFOCUS message is sent to a window after it has gained the keyboard focus. 
        /// </summary>
        SETFOCUS = 0x0007,
        /// <summary>
        /// The WM_KILLFOCUS message is sent to a window immediately before it loses the keyboard focus. 
        /// </summary>
        KILLFOCUS = 0x0008,
        /// <summary>
        /// The WM_ENABLE message is sent when an application changes the enabled state of a window. It is sent to the window whose enabled state is changing. This message is sent before the EnableWindow function returns, but after the enabled state (WS_DISABLED style bit) of the window has changed. 
        /// </summary>
        ENABLE = 0x000A,
        /// <summary>
        /// An application sends the WM_SETREDRAW message to a window to allow changes in that window to be redrawn or to prevent changes in that window from being redrawn. 
        /// </summary>
        SETREDRAW = 0x000B,
        /// <summary>
        /// An application sends a WM_SETTEXT message to set the text of a window. 
        /// </summary>
        SETTEXT = 0x000C,
        /// <summary>
        /// An application sends a WM_GETTEXT message to copy the text that corresponds to a window into a buffer provided by the caller. 
        /// </summary>
        GETTEXT = 0x000D,
        /// <summary>
        /// An application sends a WM_GETTEXTLENGTH message to determine the length, in characters, of the text associated with a window. 
        /// </summary>
        GETTEXTLENGTH = 0x000E,
        /// <summary>
        /// The WM_PAINT message is sent when the system or another application makes a request to paint a portion of an application's window. The message is sent when the UpdateWindow or RedrawWindow function is called, or by the DispatchMessage function when the application obtains a WM_PAINT message by using the GetMessage or PeekMessage function. 
        /// </summary>
        PAINT = 0x000F,
        /// <summary>
        /// The WM_CLOSE message is sent as a signal that a window or an application should terminate.
        /// </summary>
        CLOSE = 0x0010,
        /// <summary>
        /// The WM_QUERYENDSESSION message is sent when the user chooses to end the session or when an application calls one of the system shutdown functions. If any application returns zero, the session is not ended. The system stops sending WM_QUERYENDSESSION messages as soon as one application returns zero.
        /// After processing this message, the system sends the WM_ENDSESSION message with the wParam parameter set to the results of the WM_QUERYENDSESSION message.
        /// </summary>
        QUERYENDSESSION = 0x0011,
        /// <summary>
        /// The WM_QUERYOPEN message is sent to an icon when the user requests that the window be restored to its previous size and position.
        /// </summary>
        QUERYOPEN = 0x0013,
        /// <summary>
        /// The WM_ENDSESSION message is sent to an application after the system processes the results of the WM_QUERYENDSESSION message. The WM_ENDSESSION message informs the application whether the session is ending.
        /// </summary>
        ENDSESSION = 0x0016,
        /// <summary>
        /// The WM_QUIT message indicates a request to terminate an application and is generated when the application calls the PostQuitMessage function. It causes the GetMessage function to return zero.
        /// </summary>
        QUIT = 0x0012,
        /// <summary>
        /// The WM_ERASEBKGND message is sent when the window background must be erased (for example, when a window is resized). The message is sent to prepare an invalidated portion of a window for painting. 
        /// </summary>
        ERASEBKGND = 0x0014,
        /// <summary>
        /// This message is sent to all top-level windows when a change is made to a system color setting. 
        /// </summary>
        SYSCOLORCHANGE = 0x0015,
        /// <summary>
        /// The WM_SHOWWINDOW message is sent to a window when the window is about to be hidden or shown.
        /// </summary>
        SHOWWINDOW = 0x0018,
        /// <summary>
        /// An application sends the WM_WININICHANGE message to all top-level windows after making a change to the WIN.INI file. The SystemParametersInfo function sends this message after an application uses the function to change a setting in WIN.INI.
        /// Note  The WM_WININICHANGE message is provided only for compatibility with earlier versions of the system. Applications should use the WM_SETTINGCHANGE message.
        /// </summary>
        WININICHANGE = 0x001A,
        /// <summary>
        /// An application sends the WM_WININICHANGE message to all top-level windows after making a change to the WIN.INI file. The SystemParametersInfo function sends this message after an application uses the function to change a setting in WIN.INI.
        /// Note  The WM_WININICHANGE message is provided only for compatibility with earlier versions of the system. Applications should use the WM_SETTINGCHANGE message.
        /// </summary>
        SETTINGCHANGE = WININICHANGE,
        /// <summary>
        /// The WM_DEVMODECHANGE message is sent to all top-level windows whenever the user changes device-mode settings. 
        /// </summary>
        DEVMODECHANGE = 0x001B,
        /// <summary>
        /// The WM_ACTIVATEAPP message is sent when a window belonging to a different application than the active window is about to be activated. The message is sent to the application whose window is being activated and to the application whose window is being deactivated.
        /// </summary>
        ACTIVATEAPP = 0x001C,
        /// <summary>
        /// An application sends the WM_FONTCHANGE message to all top-level windows in the system after changing the pool of font resources. 
        /// </summary>
        FONTCHANGE = 0x001D,
        /// <summary>
        /// A message that is sent whenever there is a change in the system time.
        /// </summary>
        TIMECHANGE = 0x001E,
        /// <summary>
        /// The WM_CANCELMODE message is sent to cancel certain modes, such as mouse capture. For example, the system sends this message to the active window when a dialog box or message box is displayed. Certain functions also send this message explicitly to the specified window regardless of whether it is the active window. For example, the EnableWindow function sends this message when disabling the specified window.
        /// </summary>
        CANCELMODE = 0x001F,
        /// <summary>
        /// The WM_SETCURSOR message is sent to a window if the mouse causes the cursor to move within a window and mouse input is not captured. 
        /// </summary>
        SETCURSOR = 0x0020,
        /// <summary>
        /// The WM_MOUSEACTIVATE message is sent when the cursor is in an inactive window and the user presses a mouse button. The parent window receives this message only if the child window passes it to the DefWindowProc function.
        /// </summary>
        MOUSEACTIVATE = 0x0021,
        /// <summary>
        /// The WM_CHILDACTIVATE message is sent to a child window when the user clicks the window's title bar or when the window is activated, moved, or sized.
        /// </summary>
        CHILDACTIVATE = 0x0022,
        /// <summary>
        /// The WM_QUEUESYNC message is sent by a computer-based training (CBT) application to separate user-input messages from other messages sent through the WH_JOURNALPLAYBACK Hook procedure. 
        /// </summary>
        QUEUESYNC = 0x0023,
        /// <summary>
        /// The WM_GETMINMAXINFO message is sent to a window when the size or position of the window is about to change. An application can use this message to override the window's default maximized size and position, or its default minimum or maximum tracking size. 
        /// </summary>
        GETMINMAXINFO = 0x0024,
        /// <summary>
        /// Windows NT 3.51 and earlier: The WM_PAINTICON message is sent to a minimized window when the icon is to be painted. This message is not sent by newer versions of Microsoft Windows, except in unusual circumstances explained in the Remarks.
        /// </summary>
        PAINTICON = 0x0026,
        /// <summary>
        /// Windows NT 3.51 and earlier: The WM_ICONERASEBKGND message is sent to a minimized window when the background of the icon must be filled before painting the icon. A window receives this message only if a class icon is defined for the window; otherwise, WM_ERASEBKGND is sent. This message is not sent by newer versions of Windows.
        /// </summary>
        ICONERASEBKGND = 0x0027,
        /// <summary>
        /// The WM_NEXTDLGCTL message is sent to a dialog box procedure to set the keyboard focus to a different control in the dialog box. 
        /// </summary>
        NEXTDLGCTL = 0x0028,
        /// <summary>
        /// The WM_SPOOLERSTATUS message is sent from Print Manager whenever a job is added to or removed from the Print Manager queue. 
        /// </summary>
        SPOOLERSTATUS = 0x002A,
        /// <summary>
        /// The WM_DRAWITEM message is sent to the parent window of an owner-drawn button, combo box, list box, or menu when a visual aspect of the button, combo box, list box, or menu has changed.
        /// </summary>
        DRAWITEM = 0x002B,
        /// <summary>
        /// The WM_MEASUREITEM message is sent to the owner window of a combo box, list box, list view control, or menu item when the control or menu is created.
        /// </summary>
        MEASUREITEM = 0x002C,
        /// <summary>
        /// Sent to the owner of a list box or combo box when the list box or combo box is destroyed or when items are removed by the LB_DELETESTRING, LB_RESETCONTENT, CB_DELETESTRING, or CB_RESETCONTENT message. The system sends a WM_DELETEITEM message for each deleted item. The system sends the WM_DELETEITEM message for any deleted list box or combo box item with nonzero item data.
        /// </summary>
        DELETEITEM = 0x002D,
        /// <summary>
        /// Sent by a list box with the LBS_WANTKEYBOARDINPUT style to its owner in response to a WM_KEYDOWN message. 
        /// </summary>
        VKEYTOITEM = 0x002E,
        /// <summary>
        /// Sent by a list box with the LBS_WANTKEYBOARDINPUT style to its owner in response to a WM_CHAR message. 
        /// </summary>
        CHARTOITEM = 0x002F,
        /// <summary>
        /// An application sends a WM_SETFONT message to specify the font that a control is to use when drawing text. 
        /// </summary>
        SETFONT = 0x0030,
        /// <summary>
        /// An application sends a WM_GETFONT message to a control to retrieve the font with which the control is currently drawing its text. 
        /// </summary>
        GETFONT = 0x0031,
        /// <summary>
        /// An application sends a WM_SETHOTKEY message to a window to associate a hot key with the window. When the user presses the hot key, the system activates the window. 
        /// </summary>
        SETHOTKEY = 0x0032,
        /// <summary>
        /// An application sends a WM_GETHOTKEY message to determine the hot key associated with a window. 
        /// </summary>
        GETHOTKEY = 0x0033,
        /// <summary>
        /// The WM_QUERYDRAGICON message is sent to a minimized (iconic) window. The window is about to be dragged by the user but does not have an icon defined for its class. An application can return a handle to an icon or cursor. The system displays this cursor or icon while the user drags the icon.
        /// </summary>
        QUERYDRAGICON = 0x0037,
        /// <summary>
        /// The system sends the WM_COMPAREITEM message to determine the relative position of a new item in the sorted list of an owner-drawn combo box or list box. Whenever the application adds a new item, the system sends this message to the owner of a combo box or list box created with the CBS_SORT or LBS_SORT style. 
        /// </summary>
        COMPAREITEM = 0x0039,
        /// <summary>
        /// Active Accessibility sends the WM_GETOBJECT message to obtain information about an accessible object contained in a server application. 
        /// Applications never send this message directly. It is sent only by Active Accessibility in response to calls to AccessibleObjectFromPoint, AccessibleObjectFromEvent, or AccessibleObjectFromWindow. However, server applications handle this message. 
        /// </summary>
        GETOBJECT = 0x003D,
        /// <summary>
        /// The WM_COMPACTING message is sent to all top-level windows when the system detects more than 12.5 percent of system time over a 30- to 60-second interval is being spent compacting memory. This indicates that system memory is low.
        /// </summary>
        COMPACTING = 0x0041,
        /// <summary>
        /// WM_COMMNOTIFY is Obsolete for Win32-Based Applications
        /// </summary>
        [Obsolete]
        COMMNOTIFY = 0x0044,
        /// <summary>
        /// The WM_WINDOWPOSCHANGING message is sent to a window whose size, position, or place in the Z order is about to change as a result of a call to the SetWindowPos function or another window-management function.
        /// </summary>
        WINDOWPOSCHANGING = 0x0046,
        /// <summary>
        /// The WM_WINDOWPOSCHANGED message is sent to a window whose size, position, or place in the Z order has changed as a result of a call to the SetWindowPos function or another window-management function.
        /// </summary>
        WINDOWPOSCHANGED = 0x0047,
        /// <summary>
        /// Notifies applications that the system, typically a battery-powered personal computer, is about to enter a suspended mode.
        /// Use: POWERBROADCAST
        /// </summary>
        [Obsolete]
        POWER = 0x0048,
        /// <summary>
        /// An application sends the WM_COPYDATA message to pass data to another application. 
        /// </summary>
        COPYDATA = 0x004A,
        /// <summary>
        /// The WM_CANCELJOURNAL message is posted to an application when a user cancels the application's journaling activities. The message is posted with a NULL window handle. 
        /// </summary>
        CANCELJOURNAL = 0x004B,
        /// <summary>
        /// Sent by a common control to its parent window when an event has occurred or the control requires some information. 
        /// </summary>
        NOTIFY = 0x004E,
        /// <summary>
        /// The WM_INPUTLANGCHANGEREQUEST message is posted to the window with the focus when the user chooses a new input language, either with the hotkey (specified in the Keyboard control panel application) or from the indicator on the system taskbar. An application can accept the change by passing the message to the DefWindowProc function or reject the change (and prevent it from taking place) by returning immediately. 
        /// </summary>
        INPUTLANGCHANGEREQUEST = 0x0050,
        /// <summary>
        /// The WM_INPUTLANGCHANGE message is sent to the topmost affected window after an application's input language has been changed. You should make any application-specific settings and pass the message to the DefWindowProc function, which passes the message to all first-level child windows. These child windows can pass the message to DefWindowProc to have it pass the message to their child windows, and so on. 
        /// </summary>
        INPUTLANGCHANGE = 0x0051,
        /// <summary>
        /// Sent to an application that has initiated a training card with Microsoft Windows Help. The message informs the application when the user clicks an authorable button. An application initiates a training card by specifying the HELP_TCARD command in a call to the WinHelp function.
        /// </summary>
        TCARD = 0x0052,
        /// <summary>
        /// Indicates that the user pressed the F1 key. If a menu is active when F1 is pressed, WM_HELP is sent to the window associated with the menu; otherwise, WM_HELP is sent to the window that has the keyboard focus. If no window has the keyboard focus, WM_HELP is sent to the currently active window. 
        /// </summary>
        HELP = 0x0053,
        /// <summary>
        /// The WM_USERCHANGED message is sent to all windows after the user has logged on or off. When the user logs on or off, the system updates the user-specific settings. The system sends this message immediately after updating the settings.
        /// </summary>
        USERCHANGED = 0x0054,
        /// <summary>
        /// Determines if a window accepts ANSI or Unicode structures in the WM_NOTIFY notification message. WM_NOTIFYFORMAT messages are sent from a common control to its parent window and from the parent window to the common control.
        /// </summary>
        NOTIFYFORMAT = 0x0055,
        /// <summary>
        /// The WM_CONTEXTMENU message notifies a window that the user clicked the right mouse button (right-clicked) in the window.
        /// </summary>
        CONTEXTMENU = 0x007B,
        /// <summary>
        /// The WM_STYLECHANGING message is sent to a window when the SetWindowLong function is about to change one or more of the window's styles.
        /// </summary>
        STYLECHANGING = 0x007C,
        /// <summary>
        /// The WM_STYLECHANGED message is sent to a window after the SetWindowLong function has changed one or more of the window's styles
        /// </summary>
        STYLECHANGED = 0x007D,
        /// <summary>
        /// The WM_DISPLAYCHANGE message is sent to all windows when the display resolution has changed.
        /// </summary>
        DISPLAYCHANGE = 0x007E,
        /// <summary>
        /// The WM_GETICON message is sent to a window to retrieve a handle to the large or small icon associated with a window. The system displays the large icon in the ALT+TAB dialog, and the small icon in the window caption. 
        /// </summary>
        GETICON = 0x007F,
        /// <summary>
        /// An application sends the WM_SETICON message to associate a new large or small icon with a window. The system displays the large icon in the ALT+TAB dialog box, and the small icon in the window caption. 
        /// </summary>
        SETICON = 0x0080,
        /// <summary>
        /// The WM_NCCREATE message is sent prior to the WM_CREATE message when a window is first created.
        /// </summary>
        NCCREATE = 0x0081,
        /// <summary>
        /// The WM_NCDESTROY message informs a window that its nonclient area is being destroyed. The DestroyWindow function sends the WM_NCDESTROY message to the window following the WM_DESTROY message. WM_DESTROY is used to free the allocated memory object associated with the window. 
        /// The WM_NCDESTROY message is sent after the child windows have been destroyed. In contrast, WM_DESTROY is sent before the child windows are destroyed.
        /// </summary>
        NCDESTROY = 0x0082,
        /// <summary>
        /// The WM_NCCALCSIZE message is sent when the size and position of a window's client area must be calculated. By processing this message, an application can control the content of the window's client area when the size or position of the window changes.
        /// </summary>
        NCCALCSIZE = 0x0083,
        /// <summary>
        /// The WM_NCHITTEST message is sent to a window when the cursor moves, or when a mouse button is pressed or released. If the mouse is not captured, the message is sent to the window beneath the cursor. Otherwise, the message is sent to the window that has captured the mouse.
        /// </summary>
        NCHITTEST = 0x0084,
        /// <summary>
        /// The WM_NCPAINT message is sent to a window when its frame must be painted. 
        /// </summary>
        NCPAINT = 0x0085,
        /// <summary>
        /// The WM_NCACTIVATE message is sent to a window when its nonclient area needs to be changed to indicate an active or inactive state.
        /// </summary>
        NCACTIVATE = 0x0086,
        /// <summary>
        /// The WM_GETDLGCODE message is sent to the window procedure associated with a control. By default, the system handles all keyboard input to the control; the system interprets certain types of keyboard input as dialog box navigation keys. To override this default behavior, the control can respond to the WM_GETDLGCODE message to indicate the types of input it wants to process itself.
        /// </summary>
        GETDLGCODE = 0x0087,
        /// <summary>
        /// The WM_SYNCPAINT message is used to synchronize painting while avoiding linking independent GUI threads.
        /// </summary>
        SYNCPAINT = 0x0088,
        /// <summary>
        /// The WM_NCMOUSEMOVE message is posted to a window when the cursor is moved within the nonclient area of the window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCMOUSEMOVE = 0x00A0,
        /// <summary>
        /// The WM_NCLBUTTONDOWN message is posted when the user presses the left mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCLBUTTONDOWN = 0x00A1,
        /// <summary>
        /// The WM_NCLBUTTONUP message is posted when the user releases the left mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCLBUTTONUP = 0x00A2,
        /// <summary>
        /// The WM_NCLBUTTONDBLCLK message is posted when the user double-clicks the left mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCLBUTTONDBLCLK = 0x00A3,
        /// <summary>
        /// The WM_NCRBUTTONDOWN message is posted when the user presses the right mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCRBUTTONDOWN = 0x00A4,
        /// <summary>
        /// The WM_NCRBUTTONUP message is posted when the user releases the right mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCRBUTTONUP = 0x00A5,
        /// <summary>
        /// The WM_NCRBUTTONDBLCLK message is posted when the user double-clicks the right mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCRBUTTONDBLCLK = 0x00A6,
        /// <summary>
        /// The WM_NCMBUTTONDOWN message is posted when the user presses the middle mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCMBUTTONDOWN = 0x00A7,
        /// <summary>
        /// The WM_NCMBUTTONUP message is posted when the user releases the middle mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCMBUTTONUP = 0x00A8,
        /// <summary>
        /// The WM_NCMBUTTONDBLCLK message is posted when the user double-clicks the middle mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCMBUTTONDBLCLK = 0x00A9,
        /// <summary>
        /// The WM_NCXBUTTONDOWN message is posted when the user presses the first or second X button while the cursor is in the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCXBUTTONDOWN = 0x00AB,
        /// <summary>
        /// The WM_NCXBUTTONUP message is posted when the user releases the first or second X button while the cursor is in the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCXBUTTONUP = 0x00AC,
        /// <summary>
        /// The WM_NCXBUTTONDBLCLK message is posted when the user double-clicks the first or second X button while the cursor is in the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCXBUTTONDBLCLK = 0x00AD,
        /// <summary>
        /// The WM_INPUT_DEVICE_CHANGE message is sent to the window that registered to receive raw input. A window receives this message through its WindowProc function.
        /// </summary>
        INPUT_DEVICE_CHANGE = 0x00FE,
        /// <summary>
        /// The WM_INPUT message is sent to the window that is getting raw input. 
        /// </summary>
        INPUT = 0x00FF,
        /// <summary>
        /// This message filters for keyboard messages.
        /// </summary>
        KEYFIRST = 0x0100,
        /// <summary>
        /// The WM_KEYDOWN message is posted to the window with the keyboard focus when a nonsystem key is pressed. A nonsystem key is a key that is pressed when the ALT key is not pressed. 
        /// </summary>
        KEYDOWN = 0x0100,
        /// <summary>
        /// The WM_KEYUP message is posted to the window with the keyboard focus when a nonsystem key is released. A nonsystem key is a key that is pressed when the ALT key is not pressed, or a keyboard key that is pressed when a window has the keyboard focus. 
        /// </summary>
        KEYUP = 0x0101,
        /// <summary>
        /// The WM_CHAR message is posted to the window with the keyboard focus when a WM_KEYDOWN message is translated by the TranslateMessage function. The WM_CHAR message contains the character code of the key that was pressed. 
        /// </summary>
        CHAR = 0x0102,
        /// <summary>
        /// The WM_DEADCHAR message is posted to the window with the keyboard focus when a WM_KEYUP message is translated by the TranslateMessage function. WM_DEADCHAR specifies a character code generated by a dead key. A dead key is a key that generates a character, such as the umlaut (double-dot), that is combined with another character to form a composite character. For example, the umlaut-O character (Ö) is generated by typing the dead key for the umlaut character, and then typing the O key. 
        /// </summary>
        DEADCHAR = 0x0103,
        /// <summary>
        /// The WM_SYSKEYDOWN message is posted to the window with the keyboard focus when the user presses the F10 key (which activates the menu bar) or holds down the ALT key and then presses another key. It also occurs when no window currently has the keyboard focus; in this case, the WM_SYSKEYDOWN message is sent to the active window. The window that receives the message can distinguish between these two contexts by checking the context code in the lParam parameter. 
        /// </summary>
        SYSKEYDOWN = 0x0104,
        /// <summary>
        /// The WM_SYSKEYUP message is posted to the window with the keyboard focus when the user releases a key that was pressed while the ALT key was held down. It also occurs when no window currently has the keyboard focus; in this case, the WM_SYSKEYUP message is sent to the active window. The window that receives the message can distinguish between these two contexts by checking the context code in the lParam parameter. 
        /// </summary>
        SYSKEYUP = 0x0105,
        /// <summary>
        /// The WM_SYSCHAR message is posted to the window with the keyboard focus when a WM_SYSKEYDOWN message is translated by the TranslateMessage function. It specifies the character code of a system character key — that is, a character key that is pressed while the ALT key is down. 
        /// </summary>
        SYSCHAR = 0x0106,
        /// <summary>
        /// The WM_SYSDEADCHAR message is sent to the window with the keyboard focus when a WM_SYSKEYDOWN message is translated by the TranslateMessage function. WM_SYSDEADCHAR specifies the character code of a system dead key — that is, a dead key that is pressed while holding down the ALT key. 
        /// </summary>
        SYSDEADCHAR = 0x0107,
        /// <summary>
        /// The WM_UNICHAR message is posted to the window with the keyboard focus when a WM_KEYDOWN message is translated by the TranslateMessage function. The WM_UNICHAR message contains the character code of the key that was pressed. 
        /// The WM_UNICHAR message is equivalent to WM_CHAR, but it uses Unicode Transformation Format (UTF)-32, whereas WM_CHAR uses UTF-16. It is designed to send or post Unicode characters to ANSI windows and it can can handle Unicode Supplementary Plane characters.
        /// </summary>
        UNICHAR = 0x0109,
        /// <summary>
        /// This message filters for keyboard messages.
        /// </summary>
        KEYLAST = 0x0108,
        /// <summary>
        /// Sent immediately before the IME generates the composition string as a result of a keystroke. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_STARTCOMPOSITION = 0x010D,
        /// <summary>
        /// Sent to an application when the IME ends composition. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_ENDCOMPOSITION = 0x010E,
        /// <summary>
        /// Sent to an application when the IME changes composition status as a result of a keystroke. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_COMPOSITION = 0x010F,
        IME_KEYLAST = 0x010F,
        /// <summary>
        /// The WM_INITDIALOG message is sent to the dialog box procedure immediately before a dialog box is displayed. Dialog box procedures typically use this message to initialize controls and carry out any other initialization tasks that affect the appearance of the dialog box. 
        /// </summary>
        INITDIALOG = 0x0110,
        /// <summary>
        /// The WM_COMMAND message is sent when the user selects a command item from a menu, when a control sends a notification message to its parent window, or when an accelerator keystroke is translated. 
        /// </summary>
        COMMAND = 0x0111,
        /// <summary>
        /// A window receives this message when the user chooses a command from the Window menu, clicks the maximize button, minimize button, restore button, close button, or moves the form. You can stop the form from moving by filtering this out.
        /// </summary>
        SYSCOMMAND = 0x0112,
        /// <summary>
        /// The WM_TIMER message is posted to the installing thread's message queue when a timer expires. The message is posted by the GetMessage or PeekMessage function. 
        /// </summary>
        TIMER = 0x0113,
        /// <summary>
        /// The WM_HSCROLL message is sent to a window when a scroll event occurs in the window's standard horizontal scroll bar. This message is also sent to the owner of a horizontal scroll bar control when a scroll event occurs in the control. 
        /// </summary>
        HSCROLL = 0x0114,
        /// <summary>
        /// The WM_VSCROLL message is sent to a window when a scroll event occurs in the window's standard vertical scroll bar. This message is also sent to the owner of a vertical scroll bar control when a scroll event occurs in the control. 
        /// </summary>
        VSCROLL = 0x0115,
        /// <summary>
        /// The WM_INITMENU message is sent when a menu is about to become active. It occurs when the user clicks an item on the menu bar or presses a menu key. This allows the application to modify the menu before it is displayed. 
        /// </summary>
        INITMENU = 0x0116,
        /// <summary>
        /// The WM_INITMENUPOPUP message is sent when a drop-down menu or submenu is about to become active. This allows an application to modify the menu before it is displayed, without changing the entire menu. 
        /// </summary>
        INITMENUPOPUP = 0x0117,
        /// <summary>
        /// The WM_MENUSELECT message is sent to a menu's owner window when the user selects a menu item. 
        /// </summary>
        MENUSELECT = 0x011F,
        /// <summary>
        /// The WM_MENUCHAR message is sent when a menu is active and the user presses a key that does not correspond to any mnemonic or accelerator key. This message is sent to the window that owns the menu. 
        /// </summary>
        MENUCHAR = 0x0120,
        /// <summary>
        /// The WM_ENTERIDLE message is sent to the owner window of a modal dialog box or menu that is entering an idle state. A modal dialog box or menu enters an idle state when no messages are waiting in its queue after it has processed one or more previous messages. 
        /// </summary>
        ENTERIDLE = 0x0121,
        /// <summary>
        /// The WM_MENURBUTTONUP message is sent when the user releases the right mouse button while the cursor is on a menu item. 
        /// </summary>
        MENURBUTTONUP = 0x0122,
        /// <summary>
        /// The WM_MENUDRAG message is sent to the owner of a drag-and-drop menu when the user drags a menu item. 
        /// </summary>
        MENUDRAG = 0x0123,
        /// <summary>
        /// The WM_MENUGETOBJECT message is sent to the owner of a drag-and-drop menu when the mouse cursor enters a menu item or moves from the center of the item to the top or bottom of the item. 
        /// </summary>
        MENUGETOBJECT = 0x0124,
        /// <summary>
        /// The WM_UNINITMENUPOPUP message is sent when a drop-down menu or submenu has been destroyed. 
        /// </summary>
        UNINITMENUPOPUP = 0x0125,
        /// <summary>
        /// The WM_MENUCOMMAND message is sent when the user makes a selection from a menu. 
        /// </summary>
        MENUCOMMAND = 0x0126,
        /// <summary>
        /// An application sends the WM_CHANGEUISTATE message to indicate that the user interface (UI) state should be changed.
        /// </summary>
        CHANGEUISTATE = 0x0127,
        /// <summary>
        /// An application sends the WM_UPDATEUISTATE message to change the user interface (UI) state for the specified window and all its child windows.
        /// </summary>
        UPDATEUISTATE = 0x0128,
        /// <summary>
        /// An application sends the WM_QUERYUISTATE message to retrieve the user interface (UI) state for a window.
        /// </summary>
        QUERYUISTATE = 0x0129,
        /// <summary>
        /// The WM_CTLCOLORMSGBOX message is sent to the owner window of a message box before Windows draws the message box. By responding to this message, the owner window can set the text and background colors of the message box by using the given display device context handle. 
        /// </summary>
        CTLCOLORMSGBOX = 0x0132,
        /// <summary>
        /// An edit control that is not read-only or disabled sends the WM_CTLCOLOREDIT message to its parent window when the control is about to be drawn. By responding to this message, the parent window can use the specified device context handle to set the text and background colors of the edit control. 
        /// </summary>
        CTLCOLOREDIT = 0x0133,
        /// <summary>
        /// Sent to the parent window of a list box before the system draws the list box. By responding to this message, the parent window can set the text and background colors of the list box by using the specified display device context handle. 
        /// </summary>
        CTLCOLORLISTBOX = 0x0134,
        /// <summary>
        /// The WM_CTLCOLORBTN message is sent to the parent window of a button before drawing the button. The parent window can change the button's text and background colors. However, only owner-drawn buttons respond to the parent window processing this message. 
        /// </summary>
        CTLCOLORBTN = 0x0135,
        /// <summary>
        /// The WM_CTLCOLORDLG message is sent to a dialog box before the system draws the dialog box. By responding to this message, the dialog box can set its text and background colors using the specified display device context handle. 
        /// </summary>
        CTLCOLORDLG = 0x0136,
        /// <summary>
        /// The WM_CTLCOLORSCROLLBAR message is sent to the parent window of a scroll bar control when the control is about to be drawn. By responding to this message, the parent window can use the display context handle to set the background color of the scroll bar control. 
        /// </summary>
        CTLCOLORSCROLLBAR = 0x0137,
        /// <summary>
        /// A static control, or an edit control that is read-only or disabled, sends the WM_CTLCOLORSTATIC message to its parent window when the control is about to be drawn. By responding to this message, the parent window can use the specified device context handle to set the text and background colors of the static control. 
        /// </summary>
        CTLCOLORSTATIC = 0x0138,
        /// <summary>
        /// Use WM_MOUSEFIRST to specify the first mouse message. Use the PeekMessage() Function.
        /// </summary>
        MOUSEFIRST = 0x0200,
        /// <summary>
        /// The WM_MOUSEMOVE message is posted to a window when the cursor moves. If the mouse is not captured, the message is posted to the window that contains the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        MOUSEMOVE = 0x0200,
        /// <summary>
        /// The WM_LBUTTONDOWN message is posted when the user presses the left mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        LBUTTONDOWN = 0x0201,
        /// <summary>
        /// The WM_LBUTTONUP message is posted when the user releases the left mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        LBUTTONUP = 0x0202,
        /// <summary>
        /// The WM_LBUTTONDBLCLK message is posted when the user double-clicks the left mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        LBUTTONDBLCLK = 0x0203,
        /// <summary>
        /// The WM_RBUTTONDOWN message is posted when the user presses the right mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        RBUTTONDOWN = 0x0204,
        /// <summary>
        /// The WM_RBUTTONUP message is posted when the user releases the right mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        RBUTTONUP = 0x0205,
        /// <summary>
        /// The WM_RBUTTONDBLCLK message is posted when the user double-clicks the right mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        RBUTTONDBLCLK = 0x0206,
        /// <summary>
        /// The WM_MBUTTONDOWN message is posted when the user presses the middle mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        MBUTTONDOWN = 0x0207,
        /// <summary>
        /// The WM_MBUTTONUP message is posted when the user releases the middle mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        MBUTTONUP = 0x0208,
        /// <summary>
        /// The WM_MBUTTONDBLCLK message is posted when the user double-clicks the middle mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        MBUTTONDBLCLK = 0x0209,
        /// <summary>
        /// The WM_MOUSEWHEEL message is sent to the focus window when the mouse wheel is rotated. The DefWindowProc function propagates the message to the window's parent. There should be no internal forwarding of the message, since DefWindowProc propagates it up the parent chain until it finds a window that processes it.
        /// </summary>
        MOUSEWHEEL = 0x020A,
        /// <summary>
        /// The WM_XBUTTONDOWN message is posted when the user presses the first or second X button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse. 
        /// </summary>
        XBUTTONDOWN = 0x020B,
        /// <summary>
        /// The WM_XBUTTONUP message is posted when the user releases the first or second X button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        XBUTTONUP = 0x020C,
        /// <summary>
        /// The WM_XBUTTONDBLCLK message is posted when the user double-clicks the first or second X button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        XBUTTONDBLCLK = 0x020D,
        /// <summary>
        /// The WM_MOUSEHWHEEL message is sent to the focus window when the mouse's horizontal scroll wheel is tilted or rotated. The DefWindowProc function propagates the message to the window's parent. There should be no internal forwarding of the message, since DefWindowProc propagates it up the parent chain until it finds a window that processes it.
        /// </summary>
        MOUSEHWHEEL = 0x020E,
        /// <summary>
        /// Use WM_MOUSELAST to specify the last mouse message. Used with PeekMessage() Function.
        /// </summary>
        MOUSELAST = 0x020E,
        /// <summary>
        /// The WM_PARENTNOTIFY message is sent to the parent of a child window when the child window is created or destroyed, or when the user clicks a mouse button while the cursor is over the child window. When the child window is being created, the system sends WM_PARENTNOTIFY just before the CreateWindow or CreateWindowEx function that creates the window returns. When the child window is being destroyed, the system sends the message before any processing to destroy the window takes place.
        /// </summary>
        PARENTNOTIFY = 0x0210,
        /// <summary>
        /// The WM_ENTERMENULOOP message informs an application's main window procedure that a menu modal loop has been entered. 
        /// </summary>
        ENTERMENULOOP = 0x0211,
        /// <summary>
        /// The WM_EXITMENULOOP message informs an application's main window procedure that a menu modal loop has been exited. 
        /// </summary>
        EXITMENULOOP = 0x0212,
        /// <summary>
        /// The WM_NEXTMENU message is sent to an application when the right or left arrow key is used to switch between the menu bar and the system menu. 
        /// </summary>
        NEXTMENU = 0x0213,
        /// <summary>
        /// The WM_SIZING message is sent to a window that the user is resizing. By processing this message, an application can monitor the size and position of the drag rectangle and, if needed, change its size or position. 
        /// </summary>
        SIZING = 0x0214,
        /// <summary>
        /// The WM_CAPTURECHANGED message is sent to the window that is losing the mouse capture.
        /// </summary>
        CAPTURECHANGED = 0x0215,
        /// <summary>
        /// The WM_MOVING message is sent to a window that the user is moving. By processing this message, an application can monitor the position of the drag rectangle and, if needed, change its position.
        /// </summary>
        MOVING = 0x0216,
        /// <summary>
        /// Notifies applications that a power-management event has occurred.
        /// </summary>
        POWERBROADCAST = 0x0218,
        /// <summary>
        /// Notifies an application of a change to the hardware configuration of a device or the computer.
        /// </summary>
        DEVICECHANGE = 0x0219,
        /// <summary>
        /// An application sends the WM_MDICREATE message to a multiple-document interface (MDI) client window to create an MDI child window. 
        /// </summary>
        MDICREATE = 0x0220,
        /// <summary>
        /// An application sends the WM_MDIDESTROY message to a multiple-document interface (MDI) client window to close an MDI child window. 
        /// </summary>
        MDIDESTROY = 0x0221,
        /// <summary>
        /// An application sends the WM_MDIACTIVATE message to a multiple-document interface (MDI) client window to instruct the client window to activate a different MDI child window. 
        /// </summary>
        MDIACTIVATE = 0x0222,
        /// <summary>
        /// An application sends the WM_MDIRESTORE message to a multiple-document interface (MDI) client window to restore an MDI child window from maximized or minimized size. 
        /// </summary>
        MDIRESTORE = 0x0223,
        /// <summary>
        /// An application sends the WM_MDINEXT message to a multiple-document interface (MDI) client window to activate the next or previous child window. 
        /// </summary>
        MDINEXT = 0x0224,
        /// <summary>
        /// An application sends the WM_MDIMAXIMIZE message to a multiple-document interface (MDI) client window to maximize an MDI child window. The system resizes the child window to make its client area fill the client window. The system places the child window's window menu icon in the rightmost position of the frame window's menu bar, and places the child window's restore icon in the leftmost position. The system also appends the title bar text of the child window to that of the frame window. 
        /// </summary>
        MDIMAXIMIZE = 0x0225,
        /// <summary>
        /// An application sends the WM_MDITILE message to a multiple-document interface (MDI) client window to arrange all of its MDI child windows in a tile format. 
        /// </summary>
        MDITILE = 0x0226,
        /// <summary>
        /// An application sends the WM_MDICASCADE message to a multiple-document interface (MDI) client window to arrange all its child windows in a cascade format. 
        /// </summary>
        MDICASCADE = 0x0227,
        /// <summary>
        /// An application sends the WM_MDIICONARRANGE message to a multiple-document interface (MDI) client window to arrange all minimized MDI child windows. It does not affect child windows that are not minimized. 
        /// </summary>
        MDIICONARRANGE = 0x0228,
        /// <summary>
        /// An application sends the WM_MDIGETACTIVE message to a multiple-document interface (MDI) client window to retrieve the handle to the active MDI child window. 
        /// </summary>
        MDIGETACTIVE = 0x0229,
        /// <summary>
        /// An application sends the WM_MDISETMENU message to a multiple-document interface (MDI) client window to replace the entire menu of an MDI frame window, to replace the window menu of the frame window, or both. 
        /// </summary>
        MDISETMENU = 0x0230,
        /// <summary>
        /// The WM_ENTERSIZEMOVE message is sent one time to a window after it enters the moving or sizing modal loop. The window enters the moving or sizing modal loop when the user clicks the window's title bar or sizing border, or when the window passes the WM_SYSCOMMAND message to the DefWindowProc function and the wParam parameter of the message specifies the SC_MOVE or SC_SIZE value. The operation is complete when DefWindowProc returns. 
        /// The system sends the WM_ENTERSIZEMOVE message regardless of whether the dragging of full windows is enabled.
        /// </summary>
        ENTERSIZEMOVE = 0x0231,
        /// <summary>
        /// The WM_EXITSIZEMOVE message is sent one time to a window, after it has exited the moving or sizing modal loop. The window enters the moving or sizing modal loop when the user clicks the window's title bar or sizing border, or when the window passes the WM_SYSCOMMAND message to the DefWindowProc function and the wParam parameter of the message specifies the SC_MOVE or SC_SIZE value. The operation is complete when DefWindowProc returns. 
        /// </summary>
        EXITSIZEMOVE = 0x0232,
        /// <summary>
        /// Sent when the user drops a file on the window of an application that has registered itself as a recipient of dropped files.
        /// </summary>
        DROPFILES = 0x0233,
        /// <summary>
        /// An application sends the WM_MDIREFRESHMENU message to a multiple-document interface (MDI) client window to refresh the window menu of the MDI frame window. 
        /// </summary>
        MDIREFRESHMENU = 0x0234,
        /// <summary>
        /// Sent to an application when a window is activated. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_SETCONTEXT = 0x0281,
        /// <summary>
        /// Sent to an application to notify it of changes to the IME window. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_NOTIFY = 0x0282,
        /// <summary>
        /// Sent by an application to direct the IME window to carry out the requested command. The application uses this message to control the IME window that it has created. To send this message, the application calls the SendMessage function with the following parameters.
        /// </summary>
        IME_CONTROL = 0x0283,
        /// <summary>
        /// Sent to an application when the IME window finds no space to extend the area for the composition window. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_COMPOSITIONFULL = 0x0284,
        /// <summary>
        /// Sent to an application when the operating system is about to change the current IME. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_SELECT = 0x0285,
        /// <summary>
        /// Sent to an application when the IME gets a character of the conversion result. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_CHAR = 0x0286,
        /// <summary>
        /// Sent to an application to provide commands and request information. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_REQUEST = 0x0288,
        /// <summary>
        /// Sent to an application by the IME to notify the application of a key press and to keep message order. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_KEYDOWN = 0x0290,
        /// <summary>
        /// Sent to an application by the IME to notify the application of a key release and to keep message order. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_KEYUP = 0x0291,
        /// <summary>
        /// The WM_MOUSEHOVER message is posted to a window when the cursor hovers over the client area of the window for the period of time specified in a prior call to TrackMouseEvent.
        /// </summary>
        MOUSEHOVER = 0x02A1,
        /// <summary>
        /// The WM_MOUSELEAVE message is posted to a window when the cursor leaves the client area of the window specified in a prior call to TrackMouseEvent.
        /// </summary>
        MOUSELEAVE = 0x02A3,
        /// <summary>
        /// The WM_NCMOUSEHOVER message is posted to a window when the cursor hovers over the nonclient area of the window for the period of time specified in a prior call to TrackMouseEvent.
        /// </summary>
        NCMOUSEHOVER = 0x02A0,
        /// <summary>
        /// The WM_NCMOUSELEAVE message is posted to a window when the cursor leaves the nonclient area of the window specified in a prior call to TrackMouseEvent.
        /// </summary>
        NCMOUSELEAVE = 0x02A2,
        /// <summary>
        /// The WM_WTSSESSION_CHANGE message notifies applications of changes in session state.
        /// </summary>
        WTSSESSION_CHANGE = 0x02B1,
        TABLET_FIRST = 0x02c0,
        TABLET_LAST = 0x02df,
        /// <summary>
        /// An application sends a WM_CUT message to an edit control or combo box to delete (cut) the current selection, if any, in the edit control and copy the deleted text to the clipboard in CF_TEXT format. 
        /// </summary>
        CUT = 0x0300,
        /// <summary>
        /// An application sends the WM_COPY message to an edit control or combo box to copy the current selection to the clipboard in CF_TEXT format. 
        /// </summary>
        COPY = 0x0301,
        /// <summary>
        /// An application sends a WM_PASTE message to an edit control or combo box to copy the current content of the clipboard to the edit control at the current caret position. Data is inserted only if the clipboard contains data in CF_TEXT format. 
        /// </summary>
        PASTE = 0x0302,
        /// <summary>
        /// An application sends a WM_CLEAR message to an edit control or combo box to delete (clear) the current selection, if any, from the edit control. 
        /// </summary>
        CLEAR = 0x0303,
        /// <summary>
        /// An application sends a WM_UNDO message to an edit control to undo the last operation. When this message is sent to an edit control, the previously deleted text is restored or the previously added text is deleted.
        /// </summary>
        UNDO = 0x0304,
        /// <summary>
        /// The WM_RENDERFORMAT message is sent to the clipboard owner if it has delayed rendering a specific clipboard format and if an application has requested data in that format. The clipboard owner must render data in the specified format and place it on the clipboard by calling the SetClipboardData function. 
        /// </summary>
        RENDERFORMAT = 0x0305,
        /// <summary>
        /// The WM_RENDERALLFORMATS message is sent to the clipboard owner before it is destroyed, if the clipboard owner has delayed rendering one or more clipboard formats. For the content of the clipboard to remain available to other applications, the clipboard owner must render data in all the formats it is capable of generating, and place the data on the clipboard by calling the SetClipboardData function. 
        /// </summary>
        RENDERALLFORMATS = 0x0306,
        /// <summary>
        /// The WM_DESTROYCLIPBOARD message is sent to the clipboard owner when a call to the EmptyClipboard function empties the clipboard. 
        /// </summary>
        DESTROYCLIPBOARD = 0x0307,
        /// <summary>
        /// The WM_DRAWCLIPBOARD message is sent to the first window in the clipboard viewer chain when the content of the clipboard changes. This enables a clipboard viewer window to display the new content of the clipboard. 
        /// </summary>
        DRAWCLIPBOARD = 0x0308,
        /// <summary>
        /// The WM_PAINTCLIPBOARD message is sent to the clipboard owner by a clipboard viewer window when the clipboard contains data in the CF_OWNERDISPLAY format and the clipboard viewer's client area needs repainting. 
        /// </summary>
        PAINTCLIPBOARD = 0x0309,
        /// <summary>
        /// The WM_VSCROLLCLIPBOARD message is sent to the clipboard owner by a clipboard viewer window when the clipboard contains data in the CF_OWNERDISPLAY format and an event occurs in the clipboard viewer's vertical scroll bar. The owner should scroll the clipboard image and update the scroll bar values. 
        /// </summary>
        VSCROLLCLIPBOARD = 0x030A,
        /// <summary>
        /// The WM_SIZECLIPBOARD message is sent to the clipboard owner by a clipboard viewer window when the clipboard contains data in the CF_OWNERDISPLAY format and the clipboard viewer's client area has changed size. 
        /// </summary>
        SIZECLIPBOARD = 0x030B,
        /// <summary>
        /// The WM_ASKCBFORMATNAME message is sent to the clipboard owner by a clipboard viewer window to request the name of a CF_OWNERDISPLAY clipboard format.
        /// </summary>
        ASKCBFORMATNAME = 0x030C,
        /// <summary>
        /// The WM_CHANGECBCHAIN message is sent to the first window in the clipboard viewer chain when a window is being removed from the chain. 
        /// </summary>
        CHANGECBCHAIN = 0x030D,
        /// <summary>
        /// The WM_HSCROLLCLIPBOARD message is sent to the clipboard owner by a clipboard viewer window. This occurs when the clipboard contains data in the CF_OWNERDISPLAY format and an event occurs in the clipboard viewer's horizontal scroll bar. The owner should scroll the clipboard image and update the scroll bar values. 
        /// </summary>
        HSCROLLCLIPBOARD = 0x030E,
        /// <summary>
        /// This message informs a window that it is about to receive the keyboard focus, giving the window the opportunity to realize its logical palette when it receives the focus. 
        /// </summary>
        QUERYNEWPALETTE = 0x030F,
        /// <summary>
        /// The WM_PALETTEISCHANGING message informs applications that an application is going to realize its logical palette. 
        /// </summary>
        PALETTEISCHANGING = 0x0310,
        /// <summary>
        /// This message is sent by the OS to all top-level and overlapped windows after the window with the keyboard focus realizes its logical palette. 
        /// This message enables windows that do not have the keyboard focus to realize their logical palettes and update their client areas.
        /// </summary>
        PALETTECHANGED = 0x0311,
        /// <summary>
        /// The WM_HOTKEY message is posted when the user presses a hot key registered by the RegisterHotKey function. The message is placed at the top of the message queue associated with the thread that registered the hot key. 
        /// </summary>
        HOTKEY = 0x0312,
        /// <summary>
        /// The WM_PRINT message is sent to a window to request that it draw itself in the specified device context, most commonly in a printer device context.
        /// </summary>
        PRINT = 0x0317,
        /// <summary>
        /// The WM_PRINTCLIENT message is sent to a window to request that it draw its client area in the specified device context, most commonly in a printer device context.
        /// </summary>
        PRINTCLIENT = 0x0318,
        /// <summary>
        /// The WM_APPCOMMAND message notifies a window that the user generated an application command event, for example, by clicking an application command button using the mouse or typing an application command key on the keyboard.
        /// </summary>
        APPCOMMAND = 0x0319,
        /// <summary>
        /// The WM_THEMECHANGED message is broadcast to every window following a theme change event. Examples of theme change events are the activation of a theme, the deactivation of a theme, or a transition from one theme to another.
        /// </summary>
        THEMECHANGED = 0x031A,
        /// <summary>
        /// Sent when the contents of the clipboard have changed.
        /// </summary>
        CLIPBOARDUPDATE = 0x031D,
        /// <summary>
        /// The system will send a window the WM_DWMCOMPOSITIONCHANGED message to indicate that the availability of desktop composition has changed.
        /// </summary>
        DWMCOMPOSITIONCHANGED = 0x031E,
        /// <summary>
        /// WM_DWMNCRENDERINGCHANGED is called when the non-client area rendering status of a window has changed. Only windows that have set the flag DWM_BLURBEHIND.fTransitionOnMaximized to true will get this message. 
        /// </summary>
        DWMNCRENDERINGCHANGED = 0x031F,
        /// <summary>
        /// Sent to all top-level windows when the colorization color has changed. 
        /// </summary>
        DWMCOLORIZATIONCOLORCHANGED = 0x0320,
        /// <summary>
        /// WM_DWMWINDOWMAXIMIZEDCHANGE will let you know when a DWM composed window is maximized. You also have to register for this message as well. You'd have other windowd go opaque when this message is sent.
        /// </summary>
        DWMWINDOWMAXIMIZEDCHANGE = 0x0321,
        /// <summary>
        /// Sent to request extended title bar information. A window receives this message through its WindowProc function.
        /// </summary>
        GETTITLEBARINFOEX = 0x033F,
        HANDHELDFIRST = 0x0358,
        HANDHELDLAST = 0x035F,
        AFXFIRST = 0x0360,
        AFXLAST = 0x037F,
        PENWINFIRST = 0x0380,
        PENWINLAST = 0x038F,
        /// <summary>
        /// The WM_APP constant is used by applications to help define private messages, usually of the form WM_APP+X, where X is an integer value. 
        /// </summary>
        APP = 0x8000,
        /// <summary>
        /// The WM_USER constant is used by applications to help define private messages for use by private window classes, usually of the form WM_USER+X, where X is an integer value. 
        /// </summary>
        USER = 0x0400,

        /// <summary>
        /// An application sends the WM_CPL_LAUNCH message to Windows Control Panel to request that a Control Panel application be started. 
        /// </summary>
        CPL_LAUNCH = USER + 0x1000,
        /// <summary>
        /// The WM_CPL_LAUNCHED message is sent when a Control Panel application, started by the WM_CPL_LAUNCH message, has closed. The WM_CPL_LAUNCHED message is sent to the window identified by the wParam parameter of the WM_CPL_LAUNCH message that started the application. 
        /// </summary>
        CPL_LAUNCHED = USER + 0x1001,
        /// <summary>
        /// WM_SYSTIMER is a well-known yet still undocumented message. Windows uses WM_SYSTIMER for internal actions like scrolling.
        /// </summary>
        SYSTIMER = 0x118,

        /// <summary>
        /// The accessibility state has changed.
        /// </summary>
        HSHELL_ACCESSIBILITYSTATE = 11,
        /// <summary>
        /// The shell should activate its main window.
        /// </summary>
        HSHELL_ACTIVATESHELLWINDOW = 3,
        /// <summary>
        /// The user completed an input event (for example, pressed an application command button on the mouse or an application command key on the keyboard), and the application did not handle the WM_APPCOMMAND message generated by that input.
        /// If the Shell procedure handles the WM_COMMAND message, it should not call CallNextHookEx. See the Return Value section for more information.
        /// </summary>
        HSHELL_APPCOMMAND = 12,
        /// <summary>
        /// A window is being minimized or maximized. The system needs the coordinates of the minimized rectangle for the window.
        /// </summary>
        HSHELL_GETMINRECT = 5,
        /// <summary>
        /// Keyboard language was changed or a new keyboard layout was loaded.
        /// </summary>
        HSHELL_LANGUAGE = 8,
        /// <summary>
        /// The title of a window in the task bar has been redrawn.
        /// </summary>
        HSHELL_REDRAW = 6,
        /// <summary>
        /// The user has selected the task list. A shell application that provides a task list should return TRUE to prevent Windows from starting its task list.
        /// </summary>
        HSHELL_TASKMAN = 7,
        /// <summary>
        /// A top-level, unowned window has been created. The window exists when the system calls this hook.
        /// </summary>
        HSHELL_WINDOWCREATED = 1,
        /// <summary>
        /// A top-level, unowned window is about to be destroyed. The window still exists when the system calls this hook.
        /// </summary>
        HSHELL_WINDOWDESTROYED = 2,
        /// <summary>
        /// The activation has changed to a different top-level, unowned window.
        /// </summary>
        HSHELL_WINDOWACTIVATED = 4,
        /// <summary>
        /// A top-level window is being replaced. The window exists when the system calls this hook.
        /// </summary>
        HSHELL_WINDOWREPLACED = 13
    }

    public enum VirtualKeyStates : int
    {
        VK_LBUTTON = 0x01,
        VK_RBUTTON = 0x02,
        VK_CANCEL = 0x03,
        VK_MBUTTON = 0x04,
        //
        VK_XBUTTON1 = 0x05,
        VK_XBUTTON2 = 0x06,
        //
        VK_BACK = 0x08,
        VK_TAB = 0x09,
        //
        VK_CLEAR = 0x0C,
        VK_RETURN = 0x0D,
        //
        VK_SHIFT = 0x10,
        VK_CONTROL = 0x11,
        VK_MENU = 0x12,
        VK_PAUSE = 0x13,
        VK_CAPITAL = 0x14,
        //
        VK_KANA = 0x15,
        VK_HANGEUL = 0x15,  /* old name - should be here for compatibility */
        VK_HANGUL = 0x15,
        VK_JUNJA = 0x17,
        VK_FINAL = 0x18,
        VK_HANJA = 0x19,
        VK_KANJI = 0x19,
        //
        VK_ESCAPE = 0x1B,
        //
        VK_CONVERT = 0x1C,
        VK_NONCONVERT = 0x1D,
        VK_ACCEPT = 0x1E,
        VK_MODECHANGE = 0x1F,
        //
        VK_SPACE = 0x20,
        VK_PRIOR = 0x21,
        VK_NEXT = 0x22,
        VK_END = 0x23,
        VK_HOME = 0x24,
        VK_LEFT = 0x25,
        VK_UP = 0x26,
        VK_RIGHT = 0x27,
        VK_DOWN = 0x28,
        VK_SELECT = 0x29,
        VK_PRINT = 0x2A,
        VK_EXECUTE = 0x2B,
        VK_SNAPSHOT = 0x2C,
        VK_INSERT = 0x2D,
        VK_DELETE = 0x2E,
        VK_HELP = 0x2F,
        //
        VK_LWIN = 0x5B,
        VK_RWIN = 0x5C,
        VK_APPS = 0x5D,
        //
        VK_SLEEP = 0x5F,
        //
        VK_NUMPAD0 = 0x60,
        VK_NUMPAD1 = 0x61,
        VK_NUMPAD2 = 0x62,
        VK_NUMPAD3 = 0x63,
        VK_NUMPAD4 = 0x64,
        VK_NUMPAD5 = 0x65,
        VK_NUMPAD6 = 0x66,
        VK_NUMPAD7 = 0x67,
        VK_NUMPAD8 = 0x68,
        VK_NUMPAD9 = 0x69,
        VK_MULTIPLY = 0x6A,
        VK_ADD = 0x6B,
        VK_SEPARATOR = 0x6C,
        VK_SUBTRACT = 0x6D,
        VK_DECIMAL = 0x6E,
        VK_DIVIDE = 0x6F,
        VK_F1 = 0x70,
        VK_F2 = 0x71,
        VK_F3 = 0x72,
        VK_F4 = 0x73,
        VK_F5 = 0x74,
        VK_F6 = 0x75,
        VK_F7 = 0x76,
        VK_F8 = 0x77,
        VK_F9 = 0x78,
        VK_F10 = 0x79,
        VK_F11 = 0x7A,
        VK_F12 = 0x7B,
        VK_F13 = 0x7C,
        VK_F14 = 0x7D,
        VK_F15 = 0x7E,
        VK_F16 = 0x7F,
        VK_F17 = 0x80,
        VK_F18 = 0x81,
        VK_F19 = 0x82,
        VK_F20 = 0x83,
        VK_F21 = 0x84,
        VK_F22 = 0x85,
        VK_F23 = 0x86,
        VK_F24 = 0x87,
        //
        VK_NUMLOCK = 0x90,
        VK_SCROLL = 0x91,
        //
        VK_OEM_NEC_EQUAL = 0x92,   // '=' key on numpad
                                   //
        VK_OEM_FJ_JISHO = 0x92,   // 'Dictionary' key
        VK_OEM_FJ_MASSHOU = 0x93,   // 'Unregister word' key
        VK_OEM_FJ_TOUROKU = 0x94,   // 'Register word' key
        VK_OEM_FJ_LOYA = 0x95,   // 'Left OYAYUBI' key
        VK_OEM_FJ_ROYA = 0x96,   // 'Right OYAYUBI' key
                                 //
        VK_LSHIFT = 0xA0,
        VK_RSHIFT = 0xA1,
        VK_LCONTROL = 0xA2,
        VK_RCONTROL = 0xA3,
        VK_LMENU = 0xA4,
        VK_RMENU = 0xA5,
        //
        VK_BROWSER_BACK = 0xA6,
        VK_BROWSER_FORWARD = 0xA7,
        VK_BROWSER_REFRESH = 0xA8,
        VK_BROWSER_STOP = 0xA9,
        VK_BROWSER_SEARCH = 0xAA,
        VK_BROWSER_FAVORITES = 0xAB,
        VK_BROWSER_HOME = 0xAC,
        //
        VK_VOLUME_MUTE = 0xAD,
        VK_VOLUME_DOWN = 0xAE,
        VK_VOLUME_UP = 0xAF,
        VK_MEDIA_NEXT_TRACK = 0xB0,
        VK_MEDIA_PREV_TRACK = 0xB1,
        VK_MEDIA_STOP = 0xB2,
        VK_MEDIA_PLAY_PAUSE = 0xB3,
        VK_LAUNCH_MAIL = 0xB4,
        VK_LAUNCH_MEDIA_SELECT = 0xB5,
        VK_LAUNCH_APP1 = 0xB6,
        VK_LAUNCH_APP2 = 0xB7,
        //
        VK_OEM_1 = 0xBA,   // ';:' for US
        VK_OEM_PLUS = 0xBB,   // '+' any country
        VK_OEM_COMMA = 0xBC,   // ',' any country
        VK_OEM_MINUS = 0xBD,   // '-' any country
        VK_OEM_PERIOD = 0xBE,   // '.' any country
        VK_OEM_2 = 0xBF,   // '/?' for US
        VK_OEM_3 = 0xC0,   // '`~' for US
                           //
        VK_OEM_4 = 0xDB,  //  '[{' for US
        VK_OEM_5 = 0xDC,  //  '\|' for US
        VK_OEM_6 = 0xDD,  //  ']}' for US
        VK_OEM_7 = 0xDE,  //  ''"' for US
        VK_OEM_8 = 0xDF,
        //
        VK_OEM_AX = 0xE1,  //  'AX' key on Japanese AX kbd
        VK_OEM_102 = 0xE2,  //  "<>" or "\|" on RT 102-key kbd.
        VK_ICO_HELP = 0xE3,  //  Help key on ICO
        VK_ICO_00 = 0xE4,  //  00 key on ICO
                           //
        VK_PROCESSKEY = 0xE5,
        //
        VK_ICO_CLEAR = 0xE6,
        //
        VK_PACKET = 0xE7,
        //
        VK_OEM_RESET = 0xE9,
        VK_OEM_JUMP = 0xEA,
        VK_OEM_PA1 = 0xEB,
        VK_OEM_PA2 = 0xEC,
        VK_OEM_PA3 = 0xED,
        VK_OEM_WSCTRL = 0xEE,
        VK_OEM_CUSEL = 0xEF,
        VK_OEM_ATTN = 0xF0,
        VK_OEM_FINISH = 0xF1,
        VK_OEM_COPY = 0xF2,
        VK_OEM_AUTO = 0xF3,
        VK_OEM_ENLW = 0xF4,
        VK_OEM_BACKTAB = 0xF5,
        //
        VK_ATTN = 0xF6,
        VK_CRSEL = 0xF7,
        VK_EXSEL = 0xF8,
        VK_EREOF = 0xF9,
        VK_PLAY = 0xFA,
        VK_ZOOM = 0xFB,
        VK_NONAME = 0xFC,
        VK_PA1 = 0xFD,
        VK_OEM_CLEAR = 0xFE
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }

#if _marsRef
    public sealed class MarsWindowsAPIs
#else
    public sealed class MarsWindowsAPIs
#endif
    {
        public const string cnst_system_dialog_windows_className = "#32770";


        public const int SMTO_ABORTIFHUNG = 2;
        public const int SMTO_NORMAL = 0;
        public const int SMTO_NOTIMEOUTIFNOTHUNG = 0x8;
        public const int SMTO_BLOCK = 1;
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(HandleRef hWnd, int msg, IntPtr wParam, IntPtr lParam, int flags, int timeout, out IntPtr pdwResult);

        /// <summary>
        /// 必须保留这个函数的入口。如果使用handleref, 在某些程序中会出错。
        /// </summary>
        /// <param name="windowHandle"></param>
        /// <param name="Msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="flags"></param>
        /// <param name="timeout"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(IntPtr windowHandle, uint Msg, IntPtr wParam, IntPtr lParam, int flags, uint timeout, out IntPtr result);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetDlgCtrlID(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        //[StructLayout(LayoutKind.Sequential)]
        public struct WNDCLASSEX
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public int style;
            public IntPtr lpfnWndProc; // not WndProc
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;

            //Use this function to make a new one with cbSize already filled in.
            //For example:
            //var WndClss = WNDCLASSEX.Build()
            public static WNDCLASSEX Build()
            {
                var nw = new WNDCLASSEX();
                nw.cbSize = Marshal.SizeOf(typeof(WNDCLASSEX));
                return nw;
            }
        }
        [DebuggerDisplay("{" + nameof(szModule) + "}")]
        [StructLayout(LayoutKind.Sequential)]
        public struct MODULEENTRY32
        {
            public uint dwSize;
            public uint th32ModuleID;
            public uint th32ProcessID;
            public uint GlblcntUsage;
            public uint ProccntUsage;
            public readonly IntPtr modBaseAddr;
            public uint modBaseSize;
            public readonly IntPtr hModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExePath;
        }


        public enum SBFlags : uint
        {
            SB_HORZ = 0,
            SB_VERT = 1,
            SB_CTL = 2,
            SB_BOTH = 3
        }
        public enum SBOrientation : int
        {
            SB_HORZ = 0x0,
            SB_VERT = 0x1,
            SB_CTL = 0x2,
            SB_BOTH = 0x3
        }
        public enum ScrollInfoMask : uint
        {
            SIF_RANGE = 0x1,
            SIF_PAGE = 0x2,
            SIF_POS = 0x4,
            SIF_DISABLENOSCROLL = 0x8,
            SIF_TRACKPOS = 0x10,
            SIF_ALL = (SIF_RANGE | SIF_PAGE | SIF_POS | SIF_TRACKPOS),
        }
        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct SCROLLINFO
        {
            public uint cbSize;
            public uint fMask;
            public int nMin;
            public int nMax;
            public uint nPage;
            public int nPos;
            public int nTrackPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }


        public abstract class WindowStyles
        {
            public const uint WS_OVERLAPPED = 0x00000000;
            public const uint WS_POPUP = 0x80000000;
            public const uint WS_CHILD = 0x40000000;
            public const uint WS_MINIMIZE = 0x20000000;
            public const uint WS_VISIBLE = 0x10000000;
            public const uint WS_DISABLED = 0x08000000;
            public const uint WS_CLIPSIBLINGS = 0x04000000;
            public const uint WS_CLIPCHILDREN = 0x02000000;
            public const uint WS_MAXIMIZE = 0x01000000;
            public const uint WS_CAPTION = 0x00C00000;     /* WS_BORDER | WS_DLGFRAME  */
            public const uint WS_BORDER = 0x00800000;
            public const uint WS_DLGFRAME = 0x00400000;
            public const uint WS_VSCROLL = 0x00200000;
            public const uint WS_HSCROLL = 0x00100000;
            public const uint WS_SYSMENU = 0x00080000;
            public const uint WS_THICKFRAME = 0x00040000;
            public const uint WS_GROUP = 0x00020000;
            public const uint WS_TABSTOP = 0x00010000;

            public const uint WS_MINIMIZEBOX = 0x00020000;
            public const uint WS_MAXIMIZEBOX = 0x00010000;

            public const uint WS_TILED = WS_OVERLAPPED;
            public const uint WS_ICONIC = WS_MINIMIZE;
            public const uint WS_SIZEBOX = WS_THICKFRAME;
            public const uint WS_TILEDWINDOW = WS_OVERLAPPEDWINDOW;

            // Common Window Styles

            public const uint WS_OVERLAPPEDWINDOW =
                (WS_OVERLAPPED |
                  WS_CAPTION |
                  WS_SYSMENU |
                  WS_THICKFRAME |
                  WS_MINIMIZEBOX |
                  WS_MAXIMIZEBOX);

            public const uint WS_POPUPWINDOW =
                (WS_POPUP |
                  WS_BORDER |
                  WS_SYSMENU);

            public const uint WS_CHILDWINDOW = WS_CHILD;

            //Extended Window Styles

            public const uint WS_EX_DLGMODALFRAME = 0x00000001;
            public const uint WS_EX_NOPARENTNOTIFY = 0x00000004;
            public const uint WS_EX_TOPMOST = 0x00000008;
            public const uint WS_EX_ACCEPTFILES = 0x00000010;
            public const uint WS_EX_TRANSPARENT = 0x00000020;

            //#if(WINVER >= 0x0400)
            public const uint WS_EX_MDICHILD = 0x00000040;
            public const uint WS_EX_TOOLWINDOW = 0x00000080;
            public const uint WS_EX_WINDOWEDGE = 0x00000100;
            public const uint WS_EX_CLIENTEDGE = 0x00000200;
            public const uint WS_EX_CONTEXTHELP = 0x00000400;

            public const uint WS_EX_RIGHT = 0x00001000;
            public const uint WS_EX_LEFT = 0x00000000;
            public const uint WS_EX_RTLREADING = 0x00002000;
            public const uint WS_EX_LTRREADING = 0x00000000;
            public const uint WS_EX_LEFTSCROLLBAR = 0x00004000;
            public const uint WS_EX_RIGHTSCROLLBAR = 0x00000000;

            public const uint WS_EX_CONTROLPARENT = 0x00010000;
            public const uint WS_EX_STATICEDGE = 0x00020000;
            public const uint WS_EX_APPWINDOW = 0x00040000;

            public const uint WS_EX_OVERLAPPEDWINDOW = (WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE);
            public const uint WS_EX_PALETTEWINDOW = (WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST);
            //#endif /* WINVER >= 0x0400 */

            //#if(_WIN32_WINNT >= 0x0500)
            public const uint WS_EX_LAYERED = 0x00080000;
            //#endif /* _WIN32_WINNT >= 0x0500 */

            //#if(WINVER >= 0x0500)
            public const uint WS_EX_NOINHERITLAYOUT = 0x00100000; // Disable inheritence of mirroring by children
            public const uint WS_EX_LAYOUTRTL = 0x00400000; // Right to left mirroring
                                                            //#endif /* WINVER >= 0x0500 */

            //#if(_WIN32_WINNT >= 0x0500)
            public const uint WS_EX_COMPOSITED = 0x02000000;
            public const uint WS_EX_NOACTIVATE = 0x08000000;
            //#endif /* _WIN32_WINNT >= 0x0500 */
        }
        public class SearchData
        {
            // You can put any dicks or Doms in here...
            public string Wndclass;
            public string Title;
            public IntPtr hWnd;
        }

        public enum GetWindowType : uint
        {
            /// <summary>
            /// The retrieved handle identifies the window of the same type that is highest in the Z order.
            /// <para/>
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            GW_HWNDFIRST = 0,
            /// <summary>
            /// The retrieved handle identifies the window of the same type that is lowest in the Z order.
            /// <para />
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            GW_HWNDLAST = 1,
            /// <summary>
            /// The retrieved handle identifies the window below the specified window in the Z order.
            /// <para />
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            GW_HWNDNEXT = 2,
            /// <summary>
            /// The retrieved handle identifies the window above the specified window in the Z order.
            /// <para />
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            GW_HWNDPREV = 3,
            /// <summary>
            /// The retrieved handle identifies the specified window's owner window, if any.
            /// </summary>
            GW_OWNER = 4,
            /// <summary>
            /// The retrieved handle identifies the child window at the top of the Z order,
            /// if the specified window is a parent window; otherwise, the retrieved handle is NULL.
            /// The function examines only child windows of the specified window. It does not examine descendant windows.
            /// </summary>
            GW_CHILD = 5,
            /// <summary>
            /// The retrieved handle identifies the enabled popup window owned by the specified window (the
            /// search uses the first such window found using GW_HWNDNEXT); otherwise, if there are no enabled
            /// popup windows, the retrieved handle is that of the specified window.
            /// </summary>
            GW_ENABLEDPOPUP = 6
        }

        // BOOL WINAPI IsWindowVisible(
        //  _In_ HWND hWnd
        //);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowEnabled(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetCapture(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FlashWindowEx(ref FLASHWINFO pwfi);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, GetWindowType uCmd);

        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
        public delegate bool EnumWindowsProcSearch(IntPtr hwnd, [MarshalAsAttribute(UnmanagedType.Struct)] ref SearchData data);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProcSearch lpEnumFunc, [MarshalAsAttribute(UnmanagedType.Struct)] ref SearchData data);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumThreadWindows(uint dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetTopWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern Boolean GetClassInfoEx(IntPtr hInstance, String lpClassName, ref WNDCLASSEX lpWndClass);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);


        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        public const int WS_EX_APPWINDOW = 0x40000;
        public const int GWL_EXSTYLE      = -0x14;
        public const int GWL_STYLE        = -16;
        public const int GWL_HINSTANCE    = -6;
        public const int GWL_HWNDPARENT   = -8;
        public const int GWL_ID           = -12;
        public const int GWL_USERDATA     = -21;
        public const int GWL_WNDPROC      = -4;
        [DllImport("User32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("User32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

   
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(SystemMetric smIndex);

        [DllImport("user32.dll")]
        public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
#pragma warning disable 649
        public struct KEYBDINPUT
        {
            public short wVk;

            public short wScan;

            public int dwFlags;

            public int time;

            public IntPtr dwExtraInfo;
        }

        [System.Runtime.InteropServices.StructLayout(LayoutKind.Explicit)]
        public struct INPUT
        {
            [FieldOffset(0)]
            public UInt32 Type;
            [FieldOffset(4)]
            public MOUSEKEYBDHARDWAREINPUT Data;
            [FieldOffset(4)]
            public KEYBDINPUT ki;          
            
            //[FieldOffset(4)]
            //public HARDWAREINPUT hi;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetFocus();

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        [StructLayout(LayoutKind.Explicit)]
        public struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(
                                          IntPtr hWnd,
                                          int uMsg,
                                          IntPtr wParam,
                                          ref TITLEBARINFOEX lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, StringBuilder lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, ref IntPtr lParam);
        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, ref TCITEM lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, ref RECT lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        public const int KEY_PRESSED = 0x8000;
        [DllImport("user32.dll")]
        public static extern short GetKeyState(VirtualKeyStates nVirtKey);


        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("user32.dll")]
        public static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);


        [Flags()]
        public enum DeviceContextValues : uint
        {
            /// <summary>DCX_WINDOW: Returns a DC that corresponds to the window rectangle rather 
            /// than the client rectangle.</summary>
            Window = 0x00000001,
            /// <summary>DCX_CACHE: Returns a DC from the cache, rather than the OWNDC or CLASSDC 
            /// window. Essentially overrides CS_OWNDC and CS_CLASSDC.</summary>
            Cache = 0x00000002,
            /// <summary>DCX_NORESETATTRS: Does not reset the attributes of this DC to the 
            /// default attributes when this DC is released.</summary>
            NoResetAttrs = 0x00000004,
            /// <summary>DCX_CLIPCHILDREN: Excludes the visible regions of all child windows 
            /// below the window identified by hWnd.</summary>
            ClipChildren = 0x00000008,
            /// <summary>DCX_CLIPSIBLINGS: Excludes the visible regions of all sibling windows 
            /// above the window identified by hWnd.</summary>
            ClipSiblings = 0x00000010,
            /// <summary>DCX_PARENTCLIP: Uses the visible region of the parent window. The 
            /// parent's WS_CLIPCHILDREN and CS_PARENTDC style bits are ignored. The origin is 
            /// set to the upper-left corner of the window identified by hWnd.</summary>
            ParentClip = 0x00000020,
            /// <summary>DCX_EXCLUDERGN: The clipping region identified by hrgnClip is excluded 
            /// from the visible region of the returned DC.</summary>
            ExcludeRgn = 0x00000040,
            /// <summary>DCX_INTERSECTRGN: The clipping region identified by hrgnClip is 
            /// intersected with the visible region of the returned DC.</summary>
            IntersectRgn = 0x00000080,
            /// <summary>DCX_EXCLUDEUPDATE: Unknown...Undocumented</summary>
            ExcludeUpdate = 0x00000100,
            /// <summary>DCX_INTERSECTUPDATE: Unknown...Undocumented</summary>
            IntersectUpdate = 0x00000200,
            /// <summary>DCX_LOCKWINDOWUPDATE: Allows drawing even if there is a LockWindowUpdate 
            /// call in effect that would otherwise exclude this window. Used for drawing during 
            /// tracking.</summary>
            LockWindowUpdate = 0x00000400,
            /// <summary>DCX_USESTYLE: Undocumented, something related to WM_NCPAINT message.</summary>
            UseStyle = 0x00010000,
            /// <summary>DCX_VALIDATE When specified with DCX_INTERSECTUPDATE, causes the DC to 
            /// be completely validated. Using this function with both DCX_INTERSECTUPDATE and 
            /// DCX_VALIDATE is identical to using the BeginPaint function.</summary>
            Validate = 0x00200000,
        }
        [DllImport("user32.dll")]
        public static extern IntPtr GetDCEx(IntPtr hWnd, IntPtr hrgnClip, DeviceContextValues flags);
        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = false)]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(ref POINT pt);
        #region gdi32


        [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC", SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC([In] IntPtr hdc);
        /// <summary>
        ///        Creates a bitmap compatible with the device that is associated with the specified device context.
        /// </summary>
        /// <param name="hdc">A handle to a device context.</param>
        /// <param name="nWidth">The bitmap width, in pixels.</param>
        /// <param name="nHeight">The bitmap height, in pixels.</param>
        /// <returns>If the function succeeds, the return value is a handle to the compatible bitmap (DDB). If the function fails, the return value is <see cref="System.IntPtr.Zero"/>.</returns>
        [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleBitmap")]
        public static extern IntPtr CreateCompatibleBitmap([In] IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        public static extern IntPtr GetStockObject(StockObjects fnObject);
        public enum StockObjects
        {
            WHITE_BRUSH = 0,
            LTGRAY_BRUSH = 1,
            GRAY_BRUSH = 2,
            DKGRAY_BRUSH = 3,
            BLACK_BRUSH = 4,
            NULL_BRUSH = 5,
            HOLLOW_BRUSH = NULL_BRUSH,
            WHITE_PEN = 6,
            BLACK_PEN = 7,
            NULL_PEN = 8,
            OEM_FIXED_FONT = 10,
            ANSI_FIXED_FONT = 11,
            ANSI_VAR_FONT = 12,
            SYSTEM_FONT = 13,
            DEVICE_DEFAULT_FONT = 14,
            DEFAULT_PALETTE = 15,
            SYSTEM_FIXED_FONT = 16,
            DEFAULT_GUI_FONT = 17,
            DC_BRUSH = 18,
            DC_PEN = 19,
        }


        /// <summary>
        ///    Performs a bit-block transfer of the color data corresponding to a
        ///    rectangle of pixels from the specified source device context into
        ///    a destination device context.
        /// </summary>
        /// <param name="hdc">Handle to the destination device context.</param>
        /// <param name="nXDest">The leftmost x-coordinate of the destination rectangle (in pixels).</param>
        /// <param name="nYDest">The topmost y-coordinate of the destination rectangle (in pixels).</param>
        /// <param name="nWidth">The width of the source and destination rectangles (in pixels).</param>
        /// <param name="nHeight">The height of the source and the destination rectangles (in pixels).</param>
        /// <param name="hdcSrc">Handle to the source device context.</param>
        /// <param name="nXSrc">The leftmost x-coordinate of the source rectangle (in pixels).</param>
        /// <param name="nYSrc">The topmost y-coordinate of the source rectangle (in pixels).</param>
        /// <param name="dwRop">A raster-operation code.</param>
        /// <returns>
        ///    <c>true</c> if the operation succeedes, <c>false</c> otherwise. To get extended error information, call <see cref="System.Runtime.InteropServices.Marshal.GetLastWin32Error"/>.
        /// </returns>
        [DllImport("gdi32.dll", EntryPoint = "BitBlt", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BitBlt([In] IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, [In] IntPtr hdcSrc, int nXSrc, int nYSrc, TernaryRasterOperations dwRop);


        /// <summary>
        ///     Specifies a raster-operation code. These codes define how the color data for the
        ///     source rectangle is to be combined with the color data for the destination
        ///     rectangle to achieve the final color.
        /// </summary>
        public enum TernaryRasterOperations : uint
        {
            /// <summary>dest = source</summary>
            SRCCOPY = 0x00CC0020,
            /// <summary>dest = source OR dest</summary>
            SRCPAINT = 0x00EE0086,
            /// <summary>dest = source AND dest</summary>
            SRCAND = 0x008800C6,
            /// <summary>dest = source XOR dest</summary>
            SRCINVERT = 0x00660046,
            /// <summary>dest = source AND (NOT dest)</summary>
            SRCERASE = 0x00440328,
            /// <summary>dest = (NOT source)</summary>
            NOTSRCCOPY = 0x00330008,
            /// <summary>dest = (NOT src) AND (NOT dest)</summary>
            NOTSRCERASE = 0x001100A6,
            /// <summary>dest = (source AND pattern)</summary>
            MERGECOPY = 0x00C000CA,
            /// <summary>dest = (NOT source) OR dest</summary>
            MERGEPAINT = 0x00BB0226,
            /// <summary>dest = pattern</summary>
            PATCOPY = 0x00F00021,
            /// <summary>dest = DPSnoo</summary>
            PATPAINT = 0x00FB0A09,
            /// <summary>dest = pattern XOR dest</summary>
            PATINVERT = 0x005A0049,
            /// <summary>dest = (NOT dest)</summary>
            DSTINVERT = 0x00550009,
            /// <summary>dest = BLACK</summary>
            BLACKNESS = 0x00000042,
            /// <summary>dest = WHITE</summary>
            WHITENESS = 0x00FF0062,
            /// <summary>
            /// Capture window as seen on screen.  This includes layered windows 
            /// such as WPF windows with AllowsTransparency="true"
            /// </summary>
            CAPTUREBLT = 0x40000000
        }


        #endregion gdi32
        public struct MOUSEINPUT
        {
            public Int32 X;
            public Int32 Y;
            public UInt32 MouseData;
            public UInt32 Flags;
            public UInt32 Time;
            public IntPtr ExtraInfo;
        }

        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        private const uint MOUSEEVENTF_HWHEEL = 0x01000;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_MOVE_NOCOALESCE = 0x2000;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;


        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern short VkKeyScan(char ch);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("kernel32.dll")]
        public static extern bool AttachConsole(int dwProcessId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetScrollInfo(IntPtr hwnd, int fnBar, ref SCROLLINFO lpsi);
        [DllImport("user32.dll")]
        public static extern int SetScrollInfo(IntPtr hwnd, int fnBar, [In] ref SCROLLINFO lpsi, bool fRedraw);

        public const int WM_GETTITLEBARINFOEX = 0x033F;
        public const int CCHILDREN_TITLEBAR = 5;

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        };

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
             uint processAccess,
             bool bInheritHandle,
             uint processId
        );
        

        public static IntPtr OpenProcess(Process proc, ProcessAccessFlags flags)
        {
            return OpenProcess((uint)flags, false, (uint)proc.Id);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TITLEBARINFOEX
        {
            public int cbSize;
            public Rectangle rcTitleBar;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = CCHILDREN_TITLEBAR + 1)]
            public int[] rgstate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = CCHILDREN_TITLEBAR + 1)]
            public Rectangle[] rgrect;
        }

        /// <summary>
        ///   The XFORM structure specifies a world-space to page-space transformation.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct XFORM
        {
            public float eM11;
            public float eM12;
            public float eM21;
            public float eM22;
            public float eDx;
            public float eDy;

            public XFORM(float eM11, float eM12, float eM21, float eM22, float eDx, float eDy)
            {
                this.eM11 = eM11;
                this.eM12 = eM12;
                this.eM21 = eM21;
                this.eM22 = eM22;
                this.eDx = eDx;
                this.eDy = eDy;
            }

            /// <summary>
            ///   Allows implicit converstion to a managed transformation matrix.
            /// </summary>
            public static implicit operator System.Drawing.Drawing2D.Matrix(XFORM xf)
            {
                return new System.Drawing.Drawing2D.Matrix(xf.eM11, xf.eM12, xf.eM21, xf.eM22, xf.eDx, xf.eDy);
            }

            /// <summary>
            ///   Allows implicit converstion from a managed transformation matrix.
            /// </summary>
            public static implicit operator XFORM(System.Drawing.Drawing2D.Matrix m)
            {
                float[] elems = m.Elements;
                return new XFORM(elems[0], elems[1], elems[2], elems[3], elems[4], elems[5]);
            }
        }

        /// <summary>
        /// The WindowFromPoint function does not retrieve a handle to a hidden or disabled window, 
        /// even if the point is within the window. An application should use the ChildWindowFromPoint 
        /// function for a nonrestrictive search.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(System.Drawing.Point p);

        public enum GetAncestorFlags
        {
            /// <summary>
            /// Retrieves the parent window. This does not include the owner, as it does with the GetParent function.
            /// </summary>
            GetParent = 1,
            /// <summary>
            /// Retrieves the root window by walking the chain of parent windows.
            /// </summary>
            GetRoot = 2,
            /// <summary>
            /// Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent.
            /// </summary>
            GetRootOwner = 3
        }
        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flags);

        public const short SWP_NOMOVE = 0X2;
        public const short SWP_NOSIZE = 1;
        public const short SWP_NOZORDER = 0X4;
        public const int SWP_SHOWWINDOW = 0x0040;

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cX, int cY, uint uFlags);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hHandle);

        [Flags]
        public enum SnapshotFlags : uint
        {
            HeapList = 0x00000001,
            Process = 0x00000002,
            Thread = 0x00000004,
            Module = 0x00000008,
            Module32 = 0x00000010,
            Inherit = 0x80000000,
            All = 0x0000001F
        }


        public static IntPtr BuildKeyLParam(bool isKeyUp, int scancode, int repeatCount = 1, bool isExtended = false)
        {
            uint lParam = 0;
            lParam |= (uint)(repeatCount & 0xFFFF);          // repeat count (low 16)
            lParam |= (uint)((scancode & 0xFF) << 16);       // scan code
            if (isExtended) lParam |= 1u << 24;              // extended-key flag
            if (!isKeyUp)
            {
                // keydown: previous state = 0, transition = 0
            }
            else
            {
                lParam |= 1u << 30;                          // previous key state
                lParam |= 1u << 31;                          // transition state (key up)
            }
            return new IntPtr((long)lParam);
        }

        public class ToolHelpHandle : SafeHandleZeroOrMinusOneIsInvalid
        {

            private ToolHelpHandle()
                : base(true)
            {
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            protected override bool ReleaseHandle()
            {
                return CloseHandle(this.handle);
            }
        }

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public static extern bool IsWow64Process2(
            IntPtr process,
            out ushort processMachine,
            out ushort nativeMachine
        );

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern ushort GlobalAddAtom(string lpString);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern ushort GlobalDeleteAtom(ushort nAtom);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern ushort GlobalFindAtom(string lpString);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern ToolHelpHandle CreateToolhelp32Snapshot(SnapshotFlags dwFlags, int th32ProcessID);
        [DllImport("kernel32.dll")]
        public static extern bool Module32First(ToolHelpHandle hSnapshot, ref MODULEENTRY32 lpme);

        [DllImport("kernel32.dll")]
        public static extern bool Module32Next(ToolHelpHandle hSnapshot, ref MODULEENTRY32 lpme);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);
#if gdienable
        [DllImport("gdi32.dll", EntryPoint = "SetROP2", CallingConvention = CallingConvention.StdCall)]
        internal extern static int SetROP2(IntPtr hdc, int fnDrawMode);

        [DllImport("gdi32.dll", EntryPoint = "MoveToEx", CallingConvention = CallingConvention.StdCall)]
        internal extern static bool MoveToEx(IntPtr hdc, int x, int y, IntPtr lpPoint);

        [DllImport("gdi32.dll", EntryPoint = "LineTo", CallingConvention = CallingConvention.StdCall)]
        internal extern static bool LineTo(IntPtr hdc, int x, int y);

        [DllImport("gdi32.dll")]
        public static extern bool Rectangle(IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreatePen(PenStyle fnPenStyle, int nWidth, uint crColor);
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateSolidBrush(uint crColor);

        [DllImport("gdi32.dll")]
        public static extern bool SetWorldTransform(IntPtr hdc, [In] ref XFORM lpXform);




        [DllImport("gdi32.dll")]
        public static extern int SetGraphicsMode(IntPtr hdc, int iMode);
#endif
        public enum BinaryRasterOperations
        {
            R2_BLACK = 1,
            R2_NOTMERGEPEN = 2,
            R2_MASKNOTPEN = 3,
            R2_NOTCOPYPEN = 4,
            R2_MASKPENNOT = 5,
            R2_NOT = 6,
            R2_XORPEN = 7,
            R2_NOTMASKPEN = 8,
            R2_MASKPEN = 9,
            R2_NOTXORPEN = 10,
            R2_NOP = 11,
            R2_MERGENOTPEN = 12,
            R2_COPYPEN = 13,
            R2_MERGEPENNOT = 14,
            R2_MERGEPEN = 15,
            R2_WHITE = 16
        }

        public enum PenStyle : int
        {
            PS_SOLID = 0, //The pen is solid.
            PS_DASH = 1, //The pen is dashed.
            PS_DOT = 2, //The pen is dotted.
            PS_DASHDOT = 3, //The pen has alternating dashes and dots.
            PS_DASHDOTDOT = 4, //The pen has alternating dashes and double dots.
            PS_NULL = 5, //The pen is invisible.
            PS_INSIDEFRAME = 6,// Normally when the edge is drawn, it’s centred on the outer edge meaning that half the width of the pen is drawn
                               // outside the shape’s edge, half is inside the shape’s edge. When PS_INSIDEFRAME is specified the edge is drawn 
                               //completely inside the outer edge of the shape.
            PS_USERSTYLE = 7,
            PS_ALTERNATE = 8,
            PS_STYLE_MASK = 0x0000000F,

            PS_ENDCAP_ROUND = 0x00000000,
            PS_ENDCAP_SQUARE = 0x00000100,
            PS_ENDCAP_FLAT = 0x00000200,
            PS_ENDCAP_MASK = 0x00000F00,

            PS_JOIN_ROUND = 0x00000000,
            PS_JOIN_BEVEL = 0x00001000,
            PS_JOIN_MITER = 0x00002000,
            PS_JOIN_MASK = 0x0000F000,

            PS_COSMETIC = 0x00000000,
            PS_GEOMETRIC = 0x00010000,
            PS_TYPE_MASK = 0x000F0000
        };

        public enum GraphicsMode : int
        {
            GM_COMPATIBLE = 1,
            GM_ADVANCED = 2,
        }

        #region tab相关的
        public const int TCM_GETITEMW = 0x133C;
        public const int TCIF_TEXT = 0x0001;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct TCITEM
        {
            public uint mask;
            public IntPtr dwState;
            public IntPtr dwStateMask;
            public IntPtr pszText;
            public int cchTextMax;
            public int iImage;
            public IntPtr lParam;
        }
        #endregion
    }

    public enum ShowWindowCommands
    {
        SW_HIDE = 0,
        SW_SHOWNORMAL = 1,
        SW_NORMAL = 1,

        SW_SHOWMINIMIZED = 2,
        SW_SHOWMAXIMIZED = 3,
        SW_MAXIMIZE = 3,
        SW_SHOWNOACTIVATE = 4,
        SW_SHOW = 5,
        SW_MINIMIZE = 6,
        SW_SHOWMINNOACTIVE = 7,
        SW_SHOWNA = 8,
        SW_RESTORE = 9,
        SW_SHOWDEFAULT = 10,
    }

    public class ModalChecker
    {
        public static Boolean IsWaitingForUserInput(String processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
                throw new Exception("No process found matching the search criteria");
            if (processes.Length > 1)
                throw new Exception("More than one process found matching the search criteria");
            // for thread safety
            ModalChecker checker = new ModalChecker(processes[0]);
            return checker.WaitingForUserInput;
        }

        public static Boolean IsWaitingForUserInput(int pid)
        {
            Process p = Process.GetProcessById(pid);
            ModalChecker checker = new ModalChecker(p);
            return checker.WaitingForUserInput;
        }

        public static Boolean IsWaitingForUserInput(Process p,ref IntPtr dialogHdl)
        {
            ModalChecker checker = new ModalChecker(p);
            Boolean isWaiting = checker.WaitingForUserInput;
            dialogHdl = checker.targetWnd;
            return isWaiting;
        }


        #region Native Windows Stuff
        private const int WS_EX_DLGMODALFRAME = 0x00000001;
        private const int GWL_EXSTYLE = (-20);        
        #endregion

        // The process we want the info from
        private Process _process;
        private Boolean _waiting;
        private IntPtr targetWnd;

        private ModalChecker(Process process)
        {
            _process = process;
            _waiting = false; //default
        }

        private Boolean WaitingForUserInput
        {
            get
            {
                MarsWindowsAPIs.EnumWindows(new MarsWindowsAPIs.EnumWindowsProc(this.WindowEnum), IntPtr.Zero);
                return _waiting;
            }
        }

        private bool WindowEnum(IntPtr hWnd, IntPtr lParam)
        {
            targetWnd = hWnd;
            if (hWnd == _process.MainWindowHandle)
            {
                return true;
            }
            int processId;
            MarsWindowsAPIs.GetWindowThreadProcessId(hWnd, out processId);
            if (processId != _process.Id)
            {
                return true;
            }
            int style = MarsWindowsAPIs.GetWindowLong(hWnd, GWL_EXSTYLE);

            // 获得owner
            IntPtr hdlOwner = MarsWindowsAPIs.GetWindow(hWnd, MarsWindowsAPIs.GetWindowType.GW_OWNER);
            if ((hdlOwner != IntPtr.Zero))
            {
                int ownerLong = MarsWindowsAPIs.GetWindowLong(hdlOwner, MarsWindowsAPIs.GWL_STYLE);
                if ((ownerLong & MarsWindowsAPIs.WindowStyles.WS_DISABLED) != 0)
                {
                    _waiting = true;
                    return false;
                }
            }
            if ((style & WS_EX_DLGMODALFRAME) != 0)
            {
                _waiting = true;
                return false; // stop searching further
            }
            return true;
        }
    }

   

#if _marsRef
    public sealed class MarsWindowsAPIsExtend
#else
    public sealed class MarsWindowsAPIsExtend
#endif
    {
        public static string FixFolderName(string strName)
        {
            string accntName = strName;
            accntName = accntName.Replace("*", "-");
            accntName = accntName.Replace("\\", "-");
            accntName = accntName.Replace(":", "-");
            /// \ : * " < > | ？
            accntName = accntName.Replace(">", "-");
            accntName = accntName.Replace("<", "-");
            accntName = accntName.Replace("|", "-");
            accntName = accntName.Replace("?", "-");
            accntName = accntName.Replace("\"", "-");
            return accntName; 
        }

        public static void WaitForCurrentProcessResponse(int waitSeconds, Process p2Check=null)
        {
            Process p = p2Check??Process.GetCurrentProcess();
            long n = DateTime.Now.Ticks, pre=n;
            if (!p.Responding) return;
            while (((pre - n) / TimeSpan.TicksPerSecond) < waitSeconds)
            {
                TimeSpan initialTime = p.TotalProcessorTime;
                Thread.Sleep(100); // wait for some time
                TimeSpan currentTime = p.TotalProcessorTime;

                if (initialTime != currentTime)
                {
                    continue;
                    //Console.WriteLine("Process is busy");
                }
                else
                {
                    return;
                    //Console.WriteLine("Process is idle");
                }

                //if (!p.Responding) return;
                //Thread.Sleep(100);                
                //pre = DateTime.Now.Ticks;
            }
        }
        public static bool WaitForControlHandlerCreate(System.Windows.Forms.Control c, int waitSeconds=5)
        {
            if (c== null) return true;
            long n = DateTime.Now.Ticks, pre = n;
            if (c.IsHandleCreated) return true;
            while (((pre - n) / TimeSpan.TicksPerSecond) < waitSeconds)
            {
                if (c.IsHandleCreated) return true;
                Thread.Sleep(100);
                pre = DateTime.Now.Ticks;
            }
            return false;
        }

        public static List<IntPtr> EnumerateProcessWindowHandles(int processId)
        {
            var handles = new List<IntPtr>();

            foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
                MarsWindowsAPIs.EnumThreadWindows((uint)thread.Id,
                    (hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);

            return handles;
        }


        public static IEnumerable<MarsWindowsAPIs.MODULEENTRY32> GetModules(int processId)
        {
            var me32 = default(MarsWindowsAPIs.MODULEENTRY32);
            var hModuleSnap = MarsWindowsAPIs.CreateToolhelp32Snapshot(MarsWindowsAPIs.SnapshotFlags.Module | MarsWindowsAPIs.SnapshotFlags.Module32, processId);

            if (hModuleSnap.IsInvalid)
            {
                yield break;
            }

            using (hModuleSnap)
            {
                me32.dwSize = (uint)Marshal.SizeOf(me32);

                if (MarsWindowsAPIs.Module32First(hModuleSnap, ref me32))
                {
                    do
                    {
                        yield return me32;
                    }
                    while (MarsWindowsAPIs.Module32Next(hModuleSnap, ref me32));
                }
            }
        }
        public static void ShowWindowInTaskbar(IntPtr pMainWindow)
        {
            MarsWindowsAPIs.SetWindowLong(pMainWindow, MarsWindowsAPIs.GWL_EXSTYLE, MarsWindowsAPIs.GetWindowLong(pMainWindow, MarsWindowsAPIs.GWL_EXSTYLE) | MarsWindowsAPIs.WS_EX_APPWINDOW);

            MarsWindowsAPIs.ShowWindow(pMainWindow, (int)ShowWindowCommands.SW_HIDE);
            MarsWindowsAPIs.ShowWindow(pMainWindow, (int)ShowWindowCommands.SW_SHOW);
        }
        public static void HideWindowFromTaskbar(IntPtr pMainWindow)
        {
            MarsWindowsAPIs.SetWindowLong(pMainWindow, MarsWindowsAPIs.GWL_EXSTYLE, MarsWindowsAPIs.GetWindowLong(pMainWindow, MarsWindowsAPIs.GWL_EXSTYLE) & MarsWindowsAPIs.WS_EX_APPWINDOW);

            MarsWindowsAPIs.ShowWindow(pMainWindow, (int)ShowWindowCommands.SW_HIDE);
            MarsWindowsAPIs.ShowWindow(pMainWindow, (int)ShowWindowCommands.SW_SHOW);
        }

        private static bool IsWin64Emulator(Process process)
        {
            if ((Environment.OSVersion.Version.Major > 5)
                || ((Environment.OSVersion.Version.Major == 5) && (Environment.OSVersion.Version.Minor >= 1)))
            {
                bool retVal;

                return MarsWindowsAPIs.IsWow64Process(process.Handle, out retVal) && retVal;
            }

            return false; // not on 64-bit Windows Emulator
        }

        public static bool getBits(IntPtr procHdl, ref bool windowsIs32Bit, ref bool isWOW64, ref bool processIs32Bit)
        {
            ushort ProcessMachine;
            ushort NativeMachine;
            //try
            //{
            //if(!MarsS)
                if (!MarsWindowsAPIs.IsWow64Process2(procHdl, out ProcessMachine, out NativeMachine))
                {
                    Console.WriteLine("Error with getlasterror code:" + MarsWindowsAPIs.GetLastError());
                    return false;
                }
            //}catch(Exception e)
            //{
            //    Console.WriteLine($"\t{e.Message}\r\n{e.StackTrace}");
            //    return false;
            //}
            if (ProcessMachine == (ushort)IMAGE_FILE_HEADER.IMAGE_FILE_MACHINE_UNKNOWN)
            {
                isWOW64 = false;
                if ((NativeMachine == (ushort)IMAGE_FILE_HEADER.IMAGE_FILE_MACHINE_IA64)
                    || (NativeMachine == (ushort)IMAGE_FILE_HEADER.IMAGE_FILE_MACHINE_AMD64)
                    || (NativeMachine == (ushort)IMAGE_FILE_HEADER.IMAGE_FILE_MACHINE_ARM64)
                    )
                {
                    windowsIs32Bit = false;
                    processIs32Bit = false;

                    return true;
                }

                if ((NativeMachine == (ushort)IMAGE_FILE_HEADER.IMAGE_FILE_MACHINE_I386)
                    || (NativeMachine == (ushort)IMAGE_FILE_HEADER.IMAGE_FILE_MACHINE_ARM))
                {
                    windowsIs32Bit = true;
                    processIs32Bit = true;

                    return true;
                }
            }
            windowsIs32Bit = false;
            isWOW64 = true;
            processIs32Bit = true;

            return true;
        }


        public static bool IsProcess32(IntPtr procHdl)
        {
            //bool windowsIs32Bit = false;
            //bool isWOW64 = false;
            bool processIs32Bit = false;

            ///for ifc, the windows 2016 doesn't now the iswow64process
            /// 
            //bool isOk = getBits(procHdl, ref windowsIs32Bit, ref isWOW64, ref processIs32Bit);
            //return isOk ? processIs32Bit: false ;
            //processIs32Bit = IsWin64Emulator(procHdl);
            return !Environment.Is64BitOperatingSystem || IsWow64Process(procHdl);

            //return processIs32Bit;
        }

        private static bool IsWow64Process(IntPtr phandle)
        {
            bool isWow64 = false;
            if (MarsWindowsAPIs.IsWow64Process(phandle, out isWow64))
            {
                if (isWow64)
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("The process is not running in WOW64 mode.");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Failed to determine the process's WOW64 status.");
                return false;
            }
        }

        public static bool IsWin64Emulator(IntPtr pHandle)
        {
            if ((Environment.OSVersion.Version.Major > 5)
                || ((Environment.OSVersion.Version.Major == 5) && (Environment.OSVersion.Version.Minor >= 1)))
            {
                bool retVal;
                
                return MarsWindowsAPIs.IsWow64Process(pHandle, out retVal) && retVal;
            }

            return false; // not on 64-bit Windows Emulator
        }

        public static bool IsWin64Emulator(string strProcessId, ref bool isOk, ref string strError, ref IntPtr hwnd, ref Process p)
        {
            Process[] arrp = Process.GetProcessesByName(strProcessId);
            if ((arrp == null) || (arrp.Length <= 0))
            {
                strError = string.Format("No such process [{0}] found", strProcessId);
                isOk = false;
                return false;
            }
            isOk = true;
            hwnd = arrp[0].MainWindowHandle;
            p = arrp[0];
            return IsWin64Emulator(arrp[0]);
        }

        public static bool IsLeftMousePressed()
        {
            return (MarsWindowsAPIs.GetKeyState(VirtualKeyStates.VK_LBUTTON) & 0x80) != 0;
        }

        public static bool IsProcessMainWindowLoaded(Process pToCheck, ref string strError, int idefaultWaitseconds = 30)
        {
            ///判断process的主window是否已经显示
            /// 
            if (pToCheck == null)
            {
                strError = "Process parameter is null";
                return true;
            }

            int iWaitedSeconds = 0;
            while (iWaitedSeconds <= idefaultWaitseconds)
            {
                try
                {
                    if (pToCheck.MainWindowHandle == null)
                    {
                        iWaitedSeconds += 3;
                        Thread.Sleep(3000);
                        continue;
                    }
                    if (MarsWindowsAPIs.IsWindowVisible(pToCheck.MainWindowHandle))
                    {
                        strError = string.Format("\tCheck Process [{0}] Mainwindow Visible takes [{1}] seconds", pToCheck.ProcessName, iWaitedSeconds);
                        return true;
                    }
                    iWaitedSeconds += 2;
                    Thread.Sleep(2000);
                }
                catch (Exception)
                {
                    iWaitedSeconds += 1;
                    Thread.Sleep(1000);
                    continue;
                }
            }
            strError = string.Format("\tApp waited for [{0}] Seconds, but no Main window is visible [{1}]", iWaitedSeconds, pToCheck.ProcessName);
            return false;
        }

        public static bool FlashWindowByHandle(IntPtr hwnd, uint iFlashTimes = 3)
        {
            FLASHWINFO fInfo = new FLASHWINFO();

            fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
            fInfo.hwnd = hwnd;
            fInfo.dwFlags = (uint)FlashWindow.FLASHW_ALL;
            fInfo.uCount = iFlashTimes;
            fInfo.dwTimeout = 0;

            return MarsWindowsAPIs.FlashWindowEx(ref fInfo);
        }

        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_SHOWMAXIMIZED = 3;

        public static bool MaximizeWidow(IntPtr hwnd)
        {
            MarsWindowsAPIs.SetActiveWindow(hwnd);
            MarsWindowsAPIs.ShowWindowAsync(hwnd, SW_SHOWMAXIMIZED);
            return true;
        }
        public const int MOUSE_MOVE = 0x01;
        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;
        public const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        public const int MOUSEEVENTF_RIGHTUP = 0x10;
        public const int MOUSEEVENTF_ABSOLUTE = 0x8000; //标示是否采用绝对坐标
        //This simulates a left mouse click
        public static void LeftMouseClick(int xpos, int ypos)
        {
            //Mars.Inter.MQCenter.simpleLog.MarsLoggerSimple.Info("LeftMouseClick", $"{xpos}-{ypos}");
            //MarsWindowsAPIs.SetCursorPos(xpos-10, ypos-10);
            //Thread.Sleep(350);
            //MarsWindowsAPIs.mouse_event(MOUSE_MOVE | MOUSEEVENTF_ABSOLUTE, xpos, ypos , 0, 0);
            MarsWindowsAPIs.SetCursorPos(xpos, ypos);
            //POINT pt = new POINT(0, 0);
            //MarsWindowsAPIs.GetCursorPos(ref pt);
            //MarsWindowsAPIs.mouse_event(MOUSE_MOVE | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            //Thread.Sleep(300);
            //MarsWindowsAPIs.mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 1, 0);
            //MarsWindowsAPIs.mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            Thread.Sleep(50);
            //MarsWindowsAPIs.mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 1, 0);
            //MarsWindowsAPIs.mouse_event(MOUSEEVENTF_LEFTUP   | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            MarsWindowsAPIs.mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            Thread.Sleep(50);
            MarsWindowsAPIs.mouse_event( MOUSEEVENTF_LEFTUP  | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            //MarsWindowsAPIs.mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            //MarsWindowsAPIs.mouse_event(MOUSE_MOVE | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            //MarsWindowsAPIs.mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            //MarsWindowsAPIs.mouse_event(MOUSEEVENTF_LEFTDOWN , 0, 0, 0, 0);
            //Thread.Sleep(150);
            //MarsWindowsAPIs.mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            //MarsWindowsAPIs.mouse_event(MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, pt.X, pt.Y, 0, 0);
            //MarsWindowsAPIs.mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, pt.X, pt.Y, 0, 0);
            //MarsWindowsAPIs.mouse_event(MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
        }

        public static void LeftMouseDblClick(int xpos, int ypos)
        {
            MarsWindowsAPIs.SetCursorPos(xpos, ypos);
            Thread.Sleep(50);
            MarsWindowsAPIs.mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            Thread.Sleep(30);
            MarsWindowsAPIs.mouse_event(MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            Thread.Sleep(80);
            MarsWindowsAPIs.mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            Thread.Sleep(30);
            MarsWindowsAPIs.mouse_event(MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            Thread.Sleep(120);
        }
        public static void MoveMouse(int x, int y)
        {
            MarsWindowsAPIs.mouse_event(MOUSE_MOVE | MOUSEEVENTF_ABSOLUTE, x, y, 0, 0);
            MarsWindowsAPIs.SetCursorPos(x, y);
        }

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(SystemMetric smIndex);

        static int CalculateAbsoluteCoordinateX(int x)
        {
            return (x * 65536) / GetSystemMetrics(SystemMetric.SM_CXSCREEN);
        }

        static int CalculateAbsoluteCoordinateY(int y)
        {
            return (y * 65536) / GetSystemMetrics(SystemMetric.SM_CYSCREEN);
        }        

        public static System.Windows.Forms.Control FromScreenPoint(System.Drawing.Point pt, ref string strError, 
            ref string strStack)
        {
            try
            {
                IntPtr hdl = windowsWrapper.SystemUtil.MarsWindowsAPIs.WindowFromPoint(pt);
                if (hdl == IntPtr.Zero)
                {
                    strError = "No Handle find from Point ";
                    strStack = Environment.StackTrace;
                    return null;
                }
                System.Windows.Forms.Control c = System.Windows.Forms.Control.FromHandle(hdl);
                if (c == null)
                {
                    strError = "Can't retrieve control from handle";
                    strStack = Environment.StackTrace;
                    return null;
                }
                return c;
            }
            catch (Exception e)
            {
                strError = e.Message;
                strStack = e.StackTrace;
                return null;
            }
        }

        public static void LeftMouseClick(System.Windows.Forms.Control c, E_ClickPosition ePos, System.Drawing.Point attchedOffset = default(System.Drawing.Point))
        {
            if (c == null) return;
            System.Drawing.Rectangle rct = c.Bounds;
            System.Windows.Forms.Control cp = c.Parent;
            if (cp == null) return;
            System.Drawing.Point pt;
            int xOff = 0, yOff = 0;
            if (!attchedOffset.Equals(default(System.Drawing.Point)))
            {
                xOff = attchedOffset.X;
                yOff = attchedOffset.Y;
            }
            switch (ePos)
            {
                case E_ClickPosition.e_Center:
                    pt = cp.PointToScreen(
                            new System.Drawing.Point(
                                rct.Location.X + rct.Width / 2 + xOff, rct.Location.Y + rct.Height / 2 + yOff));
                    break;
                case E_ClickPosition.e_LeftBegin:
                    pt = cp.PointToScreen(
                            new System.Drawing.Point(
                                c.ClientRectangle.Location.X + 1 + xOff,
                                c.ClientRectangle.Location.Y + 1 + yOff));
                    break;
                default:
                    pt = cp.PointToScreen(
                            new System.Drawing.Point(
                                c.ClientRectangle.Location.X + c.ClientRectangle.Width - 1 + xOff,
                                c.ClientRectangle.Location.Y + c.ClientRectangle.Height - 1 + yOff));
                    break;
            }
            LeftMouseClick(pt.X, pt.Y);
        }

        public static void RightMouseClick(int xpos, int ypos)
        {
            //MarsWindowsAPIs.SetCursorPos(xpos, ypos);
            //Thread.Sleep(50);
            ////MarsWindowsAPIs.mouse_event(MOUSEEVENTF_RIGHTDOWN, xpos, ypos, 1, 0);
            ////Thread.Sleep(100);
            ////MarsWindowsAPIs.mouse_event(MOUSEEVENTF_RIGHTUP, xpos, ypos, 1, 0);
            //MarsWindowsAPIs.mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP | MOUSEEVENTF_ABSOLUTE, 0, 0, 0, 0);

            
            MarsWindowsAPIs.SetCursorPos(xpos, ypos);            
            Thread.Sleep(50);            
            MarsWindowsAPIs.mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            Thread.Sleep(50);
            MarsWindowsAPIs.mouse_event(MOUSEEVENTF_RIGHTUP | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            Thread.Sleep(150);
        }

        private class mouseevntforsendinput
        {
            public const int INPUT_MOUSE = 0;

            public const int MOUSEEVENTF_MOVE = 0x01;
            public const int MouseEventLeftDown = 0x02;
            public const int MouseEventLeftUp = 0x04;
            public const int MouseEventRightDown = 0x08;
            public const int MouseEventRightUp = 0x10;
            public const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        }

        public static void moveMouseBySendInput(int x, int y)
        {
            double fScreenWidth = MarsWindowsAPIs.GetSystemMetrics(SystemMetric.SM_CXSCREEN) - 1;
            double fScreenHeight = MarsWindowsAPIs.GetSystemMetrics(SystemMetric.SM_CYSCREEN) - 1;
            double fx = x * (65535.0f / fScreenWidth);
            double fy = y * (65535.0f / fScreenHeight);
            MarsWindowsAPIs.INPUT Input = new MarsWindowsAPIs.INPUT();
            Input.Type = mouseevntforsendinput.INPUT_MOUSE;
            Input.Data.Mouse.Flags = mouseevntforsendinput.MOUSEEVENTF_MOVE | mouseevntforsendinput.MOUSEEVENTF_ABSOLUTE;
            Input.Data.Mouse.X = x;
            Input.Data.Mouse.Y = y;
            MarsWindowsAPIs.SendInput(1, new MarsWindowsAPIs.INPUT[] { Input }, Marshal.SizeOf(Input));
        }

        public static void RightMouseClickBySendInput(int x, int y)
        {
            moveMouseBySendInput(x, y);

            MarsWindowsAPIs.INPUT[] arrInput = new MarsWindowsAPIs.INPUT[2];

            arrInput[0] = new MarsWindowsAPIs.INPUT();
            arrInput[0].Type = mouseevntforsendinput.INPUT_MOUSE;
            arrInput[0].Data.Mouse.Flags = mouseevntforsendinput.MouseEventRightDown;

            arrInput[1] = new MarsWindowsAPIs.INPUT();
            arrInput[1].Type = mouseevntforsendinput.INPUT_MOUSE;
            arrInput[1].Data.Mouse.Flags = mouseevntforsendinput.MouseEventRightUp;

            MarsWindowsAPIs.SendInput(2, arrInput, Marshal.SizeOf(arrInput[0]));
        }

        public static void LeftMouseClickBySendInput(int x, int y)
        {
            moveMouseBySendInput(x, y);

            MarsWindowsAPIs.INPUT[] arrInput = new MarsWindowsAPIs.INPUT[2];

            arrInput[0] = new MarsWindowsAPIs.INPUT();
            arrInput[0].Type = mouseevntforsendinput.INPUT_MOUSE;
            arrInput[0].Data.Mouse.Flags = mouseevntforsendinput.MouseEventLeftDown;

            arrInput[1] = new MarsWindowsAPIs.INPUT();
            arrInput[1].Type = mouseevntforsendinput.INPUT_MOUSE;
            arrInput[1].Data.Mouse.Flags = mouseevntforsendinput.MouseEventLeftUp;

            MarsWindowsAPIs.SendInput(2, arrInput, Marshal.SizeOf(arrInput[0]));
        }



        public static string Dic2String(Dictionary<string, string> source, string keyvalueSeperate, string squenceSepearte)
        {
            if (source == null) return "";
#if _NET4
            var paires = source.Select(p => string.Format("{0}{1}{2}", p.Key, keyvalueSeperate, p.Value));
#else
            var paires = source.Select(p => string.Format("{0}{1}{2}", p.Key, keyvalueSeperate, p.Value)).ToArray();
#endif
            return string.Join(squenceSepearte, paires);
        }

        public static string Dic2String(Dictionary<string, string> source)
        {
            return Dic2String(source, ":", ";");
        }


        public static bool RegularTest(string strPartern, string strValue)
        {
            if (strValue == null) return false;
            try
            {
                RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace;
                string strOldPrtrn = strPartern;
                string strNewPWithSpace = strOldPrtrn.Replace(" ", @"\s");
                while (string.Compare(strOldPrtrn, strNewPWithSpace) != 0)
                {
                    strOldPrtrn = strNewPWithSpace;
                    strNewPWithSpace = strOldPrtrn.Replace(" ", @"\s");
                }
                strNewPWithSpace = strNewPWithSpace.Replace("#", @"\#");
                //if (((strNewPWithSpace[0]>='a')&&(strNewPWithSpace[0]<='z'))
                //    ||(strNewPWithSpace[0]>='A')&&(strNewPWithSpace[0]<='Z'))
                //{
                //    //strNewPWithSpace = "^" + strNewPWithSpace;
                //}
                return Regex.IsMatch(strValue, strNewPWithSpace, options);
            }
            catch
            {
                return false;
            }
        }

        public static bool ApplicationIsActivated()
        {
            var activatedHandle = MarsWindowsAPIs.GetForegroundWindow();
            if (activatedHandle == IntPtr.Zero)
            {
                return false;       // No window is currently activated
            }

            var procId = Process.GetCurrentProcess().Id;
            int activeProcId;
            MarsWindowsAPIs.GetWindowThreadProcessId(activatedHandle, out activeProcId);

            return activeProcId == procId;
        }

    

        public static bool IsWindowsActived(IntPtr hwnd)
        {
            var activatedHandle = MarsWindowsAPIs.GetForegroundWindow();
            return hwnd == activatedHandle;
        }

        public static MarsWindowsAPIs.TITLEBARINFOEX GetTitleBarInfoEx(IntPtr hWnd)
        {
            // Create and initialize the structure
            MarsWindowsAPIs.TITLEBARINFOEX tbi = new MarsWindowsAPIs.TITLEBARINFOEX();
            tbi.cbSize = Marshal.SizeOf(typeof(MarsWindowsAPIs.TITLEBARINFOEX));

            // Send the WM_GETTITLEBARINFOEX message
            MarsWindowsAPIs.SendMessage(hWnd, MarsWindowsAPIs.WM_GETTITLEBARINFOEX, IntPtr.Zero, ref tbi);

            // Return the filled-in structure
            return tbi;
        }


        public static List<KeyValuePair<IntPtr, string>> GetWindows()
        {
            List<KeyValuePair<IntPtr, string>> lstResult = new List<KeyValuePair<IntPtr, string>>();
            MarsWindowsAPIs.EnumWindows(delegate (IntPtr hwnd, IntPtr lParam)
            {
                StringBuilder sb = new StringBuilder(256);
                MarsWindowsAPIs.GetClassName(hwnd, sb, 255);
                lstResult.Add(new KeyValuePair<IntPtr, string>(hwnd, sb.ToString()));
                return true;
            }, IntPtr.Zero);
            return lstResult;
        }

        public static List<IntPtr> GetWindows(int pId)
        {
            List<IntPtr> lstWnd = new List<IntPtr>();
            MarsWindowsAPIs.EnumWindows(delegate (IntPtr hwnd, IntPtr lParam)
            {
                int tmpPid;
                MarsWindowsAPIs.GetWindowThreadProcessId(hwnd, out tmpPid);
                if (tmpPid == pId)
                {
                    lstWnd.Add(hwnd);
                    
                }
                return true;
            }, IntPtr.Zero);
            return lstWnd;
        }

        public static List<IntPtr> GetWindows(string strClassName, int PID)
        {
            List<IntPtr> lstWnd = new List<IntPtr>();
            //public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
            MarsWindowsAPIs.EnumWindows(delegate (IntPtr hwnd, IntPtr lParam)
            {
                int tmpPid;
                MarsWindowsAPIs.GetWindowThreadProcessId(hwnd, out tmpPid);
                if (tmpPid == PID)
                {
                    StringBuilder sb = new StringBuilder(256);
                    int iLen = MarsWindowsAPIs.GetClassName(hwnd, sb, 255);
                    if (string.Compare(strClassName, sb.ToString(), true) == 0)
                    {
                        lstWnd.Add(hwnd);
                    }
                }
                return true;
            }, IntPtr.Zero);
            return lstWnd;
        }

        public static List<IntPtr> GetChildWindows(IntPtr parent)
        {
            List<IntPtr> result = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(result);
            try
            {
                MarsWindowsAPIs.EnumWindowsProc childProc = new MarsWindowsAPIs.EnumWindowsProc(EnumWindow);
                MarsWindowsAPIs.EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }
            return result;
        }

        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            List<IntPtr> list = gch.Target as List<IntPtr>;
            if (list == null)
            {
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            }
            list.Add(handle);
            //  You can modify this to check to see if you want to cancel the operation, then return a null here
            return true;
        }

        public static IntPtr SearchForWindow(string wndclass, string title)
        {
            MarsWindowsAPIs.SearchData sd = new MarsWindowsAPIs.SearchData { Wndclass = wndclass, Title = title };
            MarsWindowsAPIs.EnumWindows(new MarsWindowsAPIs.EnumWindowsProcSearch(EnumProc), ref sd);
            return sd.hWnd;
        }

        public static bool SetFoucsByMessage(IntPtr hWnd, ref string strError)
        {
            IntPtr lPara = new IntPtr(0);
            try
            {
                MarsWindowsAPIs.SendMessage(hWnd, (int)WM.SETFOCUS, 0, ref lPara);
                return true;
            }
            catch (Exception e)
            {
                strError = string.Format("Exception:{0} stacktrace:{1}", e.Message, e.StackTrace);
                return false;
            }

        }

        public static bool EnumProc(IntPtr hWnd, ref MarsWindowsAPIs.SearchData data)
        {
            // Check classname and title 
            // This is different from FindWindow() in that the code below allows partial matches
            StringBuilder sb = new StringBuilder(1024);
            MarsWindowsAPIs.GetClassName(hWnd, sb, sb.Capacity);
            if (sb.ToString().StartsWith(data.Wndclass))
            {
                sb = new StringBuilder(1024);
                MarsWindowsAPIs.GetWindowText(hWnd, sb, sb.Capacity);
                if (sb.ToString().StartsWith(data.Title))
                {
                    data.hWnd = hWnd;
                    return false;    // Found the wnd, halt enumeration
                }
            }
            return true;
        }
        public static void DrawARectangleOnDesk(System.Drawing.Rectangle rectTarget, bool isRemove = false)
        {
            IntPtr desktopPtr = IntPtr.Zero;
            try
            {
                desktopPtr = MarsWindowsAPIs.GetDC(IntPtr.Zero);
                Graphics g = Graphics.FromHdc(desktopPtr);

                SolidBrush b = new SolidBrush(Color.White);
                g.FillRectangle(b, rectTarget);

                g.Dispose();

            }
            finally
            {
                try
                {
                    MarsWindowsAPIs.ReleaseDC(IntPtr.Zero, desktopPtr);
                }
                catch (Exception)
                {
                }

            }
        }


        public class Module
        {
            public Module(string moduleName, IntPtr baseAddress, uint size, string strPath)
            {
                this.ModuleName = moduleName;
                this.BaseAddress = baseAddress;
                this.Size = size;
                this.ModulePath = strPath;
            }

            public string ModuleName { get; set; }
            public IntPtr BaseAddress { get; set; }
            public uint Size { get; set; }
            public string ModulePath { get; set; }
        }

        public class Native
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct ModuleInformation
            {
                public IntPtr lpBaseOfDll;
                public uint SizeOfImage;
                public IntPtr EntryPoint;
            }

            public enum ModuleFilter
            {
                ListModulesDefault = 0x0,
                ListModules32Bit = 0x01,
                ListModules64Bit = 0x02,
                ListModulesAll = 0x03,
            }

            [DllImport("psapi.dll")]
            public static extern bool EnumProcessModulesEx(IntPtr hProcess, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U4)][In][Out] IntPtr[] lphModule, int cb, [MarshalAs(UnmanagedType.U4)] out int lpcbNeeded, uint dwFilterFlag);

            [DllImport("psapi.dll")]
            public static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, [In][MarshalAs(UnmanagedType.U4)] uint nSize);

            [DllImport("psapi.dll", SetLastError = true)]
            public static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, out ModuleInformation lpmodinfo, uint cb);
        }


        public static List<Module> CollectModules(int pid)
        {
            Process p = Process.GetProcessById(pid);
            return CollectModules(p);
        }

        public const int PROCESS_VM_READ = (0x0010);
        public const int PROCESS_QUERY_INFORMATION = (0x0400);
        public const int PROCESS_QUERY_LIMITED_INFORMATION = (0x1000);

        public static List<Module> CollectModules(Process process)
        {
            List<Module> collectedModules = new List<Module>();

            IntPtr[] modulePointers = new IntPtr[0];
            int bytesNeeded = 0;

            // Determine number of modules
            if (!Native.EnumProcessModulesEx(process.Handle, modulePointers, 0, out bytesNeeded, (uint)Native.ModuleFilter.ListModulesAll))
            {
                return collectedModules;
            }
            IntPtr hProcess = MarsWindowsAPIs.OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, (uint)process.Id);

            int totalNumberofModules = bytesNeeded / IntPtr.Size;
            modulePointers = new IntPtr[totalNumberofModules];

            // Collect modules from the process
            if (Native.EnumProcessModulesEx(hProcess, //process.Handle, 
                modulePointers, bytesNeeded, out bytesNeeded, (uint)Native.ModuleFilter.ListModulesAll))
            {
                for (int index = 0; index < totalNumberofModules; index++)
                {
                    StringBuilder moduleFilePath = new StringBuilder(1024);
                    Native.GetModuleFileNameEx(process.Handle, modulePointers[index], moduleFilePath, (uint)(moduleFilePath.Capacity));

                    string moduleName = Path.GetFileName(moduleFilePath.ToString());
                    Native.ModuleInformation moduleInformation = new Native.ModuleInformation();
                    Native.GetModuleInformation(process.Handle, modulePointers[index], out moduleInformation, (uint)(IntPtr.Size * (modulePointers.Length)));

                    // Convert to a normalized module and add it to our list
                    Module module = new Module(moduleName, moduleInformation.lpBaseOfDll, moduleInformation.SizeOfImage, moduleFilePath.ToString());
                    collectedModules.Add(module);
                }
            }

            return collectedModules;
        }

        public static void RepeatSendVK(IntPtr hwnd, int k, int times = 20)
        {
            IntPtr tmp = new IntPtr();
            for (int i = 0; i < times; i++)
            {
                MarsWindowsAPIs.SendMessage(hwnd, (int)WM.CHAR, (int)VirtualKeyStates.VK_DELETE, ref tmp);
            }
        }

        public static void SimulateInputString(string sText)
        {
            char[] cText = sText.ToCharArray();
            foreach (char c in cText)
            {
                MarsWindowsAPIs.INPUT[] input = new MarsWindowsAPIs.INPUT[2];
                if (c >= 0 && c < 256)
                {
                    short num = MarsWindowsAPIs.VkKeyScan(c);
                    if (num != -1)
                    {
                        bool shift = (num >> 8 & 1) != 0;
                        if ((MarsWindowsAPIs.GetKeyState(VirtualKeyStates.VK_CAPITAL) & 1) != 0 && ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')))
                        {
                            shift = !shift;
                        }
                        if (shift)
                        {
                            input[0].Type = 1;
                            input[0].ki.wVk = 16; // Shift
                            input[0].ki.dwFlags = 0; // Down
                            MarsWindowsAPIs.SendInput(1u, input, Marshal.SizeOf((object)default(MarsWindowsAPIs.INPUT)));
                        }
                        input[0].Type = 1;
                        input[0].ki.wVk = (short)(num & 0xFF);
                        input[1].Type = 1;
                        input[1].ki.wVk = (short)(num & 0xFF);
                        input[1].ki.dwFlags = 2; // Up
                        MarsWindowsAPIs.SendInput(2u, input, Marshal.SizeOf((object)default(MarsWindowsAPIs.INPUT)));
                        if (shift)
                        {
                            input[0].Type = 1;
                            input[0].ki.wVk = 16;
                            input[0].ki.dwFlags = 2; // Up
                            MarsWindowsAPIs.SendInput(1u, input, Marshal.SizeOf((object)default(MarsWindowsAPIs.INPUT)));
                        }
                        continue;
                    }
                }
                input[0].Type = 1;
                input[0].ki.wVk = 0;
                input[0].ki.wScan = (short)c;
                input[0].ki.dwFlags = 4; // KEYEVENTF_UNICODE Down
                input[0].ki.time = 0;
                input[0].ki.dwExtraInfo = IntPtr.Zero;
                input[1].Type = 1;
                input[1].ki.wVk = 0;
                input[1].ki.wScan = (short)c;
                input[1].ki.dwFlags = 6; // KEYEVENTF_UNICODE Up
                input[1].ki.time = 0;
                input[1].ki.dwExtraInfo = IntPtr.Zero;
                MarsWindowsAPIs.SendInput(2u, input, Marshal.SizeOf((object)default(MarsWindowsAPIs.INPUT)));
            }
        }

        public static void SendStringByWM_CHAR(IntPtr hwnd, string text)
        {
            if (hwnd == IntPtr.Zero || string.IsNullOrEmpty(text))
                return;

            foreach (char c in text)
            {
                IntPtr tmp = new IntPtr();
                MarsWindowsAPIs.SendMessage(hwnd, (int)WM.CHAR, (int)c, ref tmp);
            }
        }

        
    }

#if _marsRef
    [StructLayout(LayoutKind.Sequential)]
    public struct FLASHWINFO
#else
    [StructLayout(LayoutKind.Sequential)]
    public struct FLASHWINFO
#endif
    {
        public UInt32 cbSize;
        public IntPtr hwnd;
        public UInt32 dwFlags;
        public UInt32 uCount;
        public UInt32 dwTimeout;
    }
#if _marsRef
    public enum FlashWindow : uint
#else
    public enum FlashWindow : uint
#endif
    {
        /// <summary>
        /// Stop flashing. The system restores the window to its original state. 
        /// </summary>    
        FLASHW_STOP = 0,

        /// <summary>
        /// Flash the window caption 
        /// </summary>
        FLASHW_CAPTION = 1,

        /// <summary>
        /// Flash the taskbar button. 
        /// </summary>
        FLASHW_TRAY = 2,

        /// <summary>
        /// Flash both the window caption and taskbar button.
        /// This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags. 
        /// </summary>
        FLASHW_ALL = 3,

        /// <summary>
        /// Flash continuously, until the FLASHW_STOP flag is set.
        /// </summary>
        FLASHW_TIMER = 4,

        /// <summary>
        /// Flash continuously until the window comes to the foreground. 
        /// </summary>
        FLASHW_TIMERNOFG = 12
    }


    public enum E_ClickPosition
    {
        e_Center,
        e_LeftBegin,
        e_RightEnd
    }
#if gdienable
    public static class XorDrawing
    {

        private static IntPtr BeginDraw(System.Drawing.Bitmap bmp, System.Drawing.Graphics graphics, int x1, int y1, int x2, int y2, bool dash, out int oldRop, out IntPtr img, out IntPtr oldpen)
        {
            var gHdc = graphics.GetHdc();
            var hdc = MarsWindowsAPIs.CreateCompatibleDC(gHdc);
            graphics.ReleaseHdc(hdc);

            img = bmp.GetHbitmap();
            MarsWindowsAPIs.SelectObject(hdc, img);

            oldpen = IntPtr.Zero;
            if (dash)
            {
                var pen = MarsWindowsAPIs.CreatePen(MarsWindowsAPIs.PenStyle.PS_DASH, 1, 0);
                oldpen = MarsWindowsAPIs.SelectObject(hdc, pen);
            }
            oldRop = MarsWindowsAPIs.SetROP2(hdc, (int)MarsWindowsAPIs.BinaryRasterOperations.R2_NOTXORPEN); // Switch to inverted mode. (XOR)

            MarsWindowsAPIs.SetGraphicsMode(hdc, (int)MarsWindowsAPIs.GraphicsMode.GM_ADVANCED);
            MarsWindowsAPIs.XFORM transform = graphics.Transform;
            MarsWindowsAPIs.SetWorldTransform(hdc, ref transform);

            return hdc;
        }

        private static IntPtr BeginDrawOnDesk(int x1, int y1, int x2, int y2, bool dash, out int oldRop, out IntPtr oldpen)
        {
            var gHdc = MarsWindowsAPIs.GetDC(IntPtr.Zero);
            var hdc = MarsWindowsAPIs.CreateCompatibleDC(gHdc);

            oldpen = IntPtr.Zero;
            if (dash)
            {
                var pen = MarsWindowsAPIs.CreatePen(MarsWindowsAPIs.PenStyle.PS_DASH, 1, 0);
                oldpen = MarsWindowsAPIs.SelectObject(hdc, pen);
            }
            oldRop = MarsWindowsAPIs.SetROP2(hdc, (int)MarsWindowsAPIs.BinaryRasterOperations.R2_NOTXORPEN); // Switch to inverted mode. (XOR)

            MarsWindowsAPIs.SetGraphicsMode(hdc, (int)MarsWindowsAPIs.GraphicsMode.GM_ADVANCED);
            //MarsWindowsAPIs.XFORM transform = graphics.Transform;
            //MarsWindowsAPIs.SetWorldTransform(hdc, ref transform);

            return hdc;
        }


        private static void FinishDraw(System.Drawing.Bitmap bmp, System.Drawing.Graphics graphics, IntPtr hdc, IntPtr oldpen, int oldRop, IntPtr img, bool dash)
        {
            MarsWindowsAPIs.SetROP2(hdc, oldRop);

            var transform = graphics.Transform;
            graphics.ResetTransform(); //in case there is transform
            var outBmp = System.Drawing.Image.FromHbitmap(img);
            //CopyChannel(bmp, outBmp, ChannelARGB.Alpha, ChannelARGB.Alpha);
            graphics.Clear(Color.Transparent);
            graphics.DrawImage(outBmp, 0, 0); //draw the xored image on the bitmap
            graphics.Transform = transform;

            if (dash) MarsWindowsAPIs.DeleteObject(MarsWindowsAPIs.SelectObject(hdc, oldpen)); //delete new pen (switch to oldpen)
            MarsWindowsAPIs.DeleteObject(img); // Delete the GDI bitmap (important).
            MarsWindowsAPIs.DeleteObject(hdc);
        }

        public static void DrawXorLine(this System.Drawing.Graphics graphics, System.Drawing.Bitmap bmp, int x1, int y1, int x2, int y2, bool dash = true)
        {
            int oldRop;
            IntPtr oldpen, img;
            var hdc = BeginDraw(bmp, graphics, x1, y1, x2, y2, dash, out oldRop, out img, out oldpen);

            MarsWindowsAPIs.MoveToEx(hdc, x1, y1, IntPtr.Zero);
            MarsWindowsAPIs.LineTo(hdc, x2, y2);

            FinishDraw(bmp, graphics, hdc, oldpen, oldRop, img, dash);
        }

        public static void DrawXorRectangle(this System.Drawing.Graphics graphics, System.Drawing.Bitmap bmp, int x1, int y1, int x2, int y2, bool dash = true)
        {
            int oldRop;
            IntPtr oldpen, img;
            var hdc = BeginDraw(bmp, graphics, x1, y1, x2, y2, dash, out oldRop, out img, out oldpen);

            MarsWindowsAPIs.MoveToEx(hdc, x1, y1, IntPtr.Zero); //clockwise
            MarsWindowsAPIs.LineTo(hdc, x2, y1);
            MarsWindowsAPIs.LineTo(hdc, x2, y2);
            MarsWindowsAPIs.LineTo(hdc, x1, y2);
            MarsWindowsAPIs.LineTo(hdc, x1, y1);

            FinishDraw(bmp, graphics, hdc, oldpen, oldRop, img, dash);
        }

        public static bool DrawXorRectangleOnDeskTop(MarsWindowsAPIs.RECT lpRect, ref string strError,
            int iTimes = 4, int iInterTime = 100, bool isErease = true)
        {
            Console.WriteLine("draw xor");
            IntPtr hdcForDeskTop = IntPtr.Zero;
            IntPtr oldPenHandle = IntPtr.Zero;
            IntPtr currentPenHandle = IntPtr.Zero;
            IntPtr currentBrush = IntPtr.Zero;
            IntPtr oldBrush = IntPtr.Zero;
            try
            {
                Console.WriteLine($"times:{iTimes}, isErease:{isErease} {lpRect.Left}/{lpRect.Top}");

                hdcForDeskTop = MarsWindowsAPIs.GetDC(IntPtr.Zero);
                int iLineWidth = 3;
                int iLeftX = lpRect.Left - iLineWidth, iRight = lpRect.Right+ iLineWidth;
                int iTop = lpRect.Top - iLineWidth, iBottom = lpRect.Bottom+ iLineWidth;
                MarsWindowsAPIs.SetROP2(hdcForDeskTop, (int)MarsWindowsAPIs.BinaryRasterOperations.R2_XORPEN);

                //MarsWindowsAPIs.SetROP2(hdcForDeskTop, (int)MarsWindowsAPIs.BinaryRasterOperations.R2_NOT);
                currentPenHandle = MarsWindowsAPIs.CreatePen(MarsWindowsAPIs.PenStyle.PS_DASH, iLineWidth, 
                    (uint)ColorTranslator.ToWin32(Color.Red)^(uint)ColorTranslator.ToWin32(Color.LightGray));
                oldPenHandle = MarsWindowsAPIs.SelectObject(hdcForDeskTop, currentPenHandle);
                currentBrush = MarsWindowsAPIs.GetStockObject(MarsWindowsAPIs.StockObjects.NULL_BRUSH);
                oldBrush = MarsWindowsAPIs.SelectObject(hdcForDeskTop, currentBrush);

                for (int i = 0; i < iTimes; i++)
                {
                   
                    MarsWindowsAPIs.MoveToEx(hdcForDeskTop, iLeftX, iTop, IntPtr.Zero);
                   
                    Console.WriteLine($"draw time {i + 1}");
                    MarsWindowsAPIs.Rectangle(hdcForDeskTop, iLeftX, iTop,
                        iRight, //lpRect.Right, 
                        iBottom //lpRect.Bottom
                        );
                    Task.Delay(iInterTime).GetAwaiter().GetResult();
                   
                    //if ((i == iTimes - 1) && (isErease))
                    //{
                        Console.WriteLine($"draw xor time {i + 1}");
                        MarsWindowsAPIs.MoveToEx(hdcForDeskTop, iLeftX, iTop, IntPtr.Zero);
                        MarsWindowsAPIs.Rectangle(hdcForDeskTop, iLeftX, iTop, 
                            iRight, //lpRect.Right, 
                            iBottom //lpRect.Bottom
                            );
                    //}
                    //Thread.Sleep(iInterTime);
                    Task.Delay(iInterTime).GetAwaiter().GetResult();
                }
                return true;
            }
            catch (Exception e)
            {
                strError = string.Format("Exception:[{0}] stack Trace:[{1}]", e.Message, e.StackTrace);
                return false;
            }
            finally
            {
                MarsWindowsAPIs.SelectObject(hdcForDeskTop, oldPenHandle);
                MarsWindowsAPIs.SelectObject(hdcForDeskTop, oldBrush);
                MarsWindowsAPIs.DeleteObject(currentPenHandle);
                MarsWindowsAPIs.DeleteObject(currentBrush);
                MarsWindowsAPIs.ReleaseDC(IntPtr.Zero, hdcForDeskTop);
            }

        }
    }
#endif

    /// <summary>
    /// this data should stored in t_registed_apps' EXTRAREQUIREMENT
    /// this one could be expand
    /// </summary>
    public class TestStepErrorCheckSetting
    {
        public string type { get; set; } = "STEP_ERROR_CHECK";
        public bool autoError { get; set; }
        public object normal { get; set; }
        public List<string> errorColor { get; set; }
    }

    [Flags]
    public enum MARSSupportedProcessType
    {
        Mars_noneSupport                = 0x00,
        Mars_dotNet                     = 0x01,   // .NET Framework
        Mars_dotNet_Core                = 0x02,   // .NET Core
        Mars_dotNet_Infragistics_frame  = 0x04,   // Infragistics for .NET Framework
        Mars_dotNet_Infragistics_Core   = 0x08,   // Infragistics for .NET Core
        Mars_dotNet_wpf_Core            = 0x10,   // WPF for .NET Core
        Mars_dotNet_wpf_frame           = 0x20,   // WPF for .NET Framework
        Mars_QT487                      = 0x40,   // QT
        Mars_Java                       = 0x80,   // Java
        Mars_Web                        = 0x100,  // Web
        Mars_Standard_CPlusPlus         = 0x200
    }

    public class MarsProcessModule
    {

        public static bool IsMfcApp(List<MarsWindowsAPIsExtend.Module> modules)
        {            
            bool hasMfcDll = modules.Any(m =>
                m.ModuleName.StartsWith("mfc", StringComparison.OrdinalIgnoreCase) &&
                m.ModuleName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));

            /// 不一定加载了
            //var handles = MarsWindowsAPIsExtend.EnumerateProcessWindowHandles(process.Id);
            //foreach (var hwnd in handles)
            //{
            //    string className = GetWindowClassName(hwnd);
            //    if (className.StartsWith("Afx:", StringComparison.OrdinalIgnoreCase) && hasMfcDll)
            //        return true;
            //}
            return hasMfcDll;
        }

        // IEnumerable<MarsWindowsAPIs.MODULEENTRY32> 
        public static MARSSupportedProcessType GetTargetTypeFromProcessModule(List<MarsWindowsAPIsExtend.Module> modules,ref string strVersion)
        {
            if (modules == null) return MARSSupportedProcessType.Mars_noneSupport;
            var pJava = (from m in modules
                         where m.ModuleName.Equals("java.dll", StringComparison.OrdinalIgnoreCase)
                         select m).FirstOrDefault();
            if (pJava!=null)
            {
                //java 
                //獲得java的版本
                FileVersionInfo fv = FileVersionInfo.GetVersionInfo(pJava.ModulePath);
                strVersion = fv.FileVersion;
                return MARSSupportedProcessType.Mars_Java;
            }

            MARSSupportedProcessType result = MARSSupportedProcessType.Mars_noneSupport;
            /// 首先判断是否存在mfc等代码
            /// 
            if (IsMfcApp(modules))
            {
                result |= MARSSupportedProcessType.Mars_Standard_CPlusPlus;
            }

            /// 如果系统中加载了clr.dll这是.net framework, 如果加载了coreclr.dll这是.net core
            /// 如果加载了PresentationCore + PresentationFramework，这是wpf
            /// 
            var moduleNames = modules.Where(p => !(string.IsNullOrEmpty(p.ModuleName)));
            var clrDll = moduleNames.FirstOrDefault(p=>p.ModuleName.Equals("clr.dll", StringComparison.OrdinalIgnoreCase));
            if (clrDll != null)
            {
                /// 说明是.net frameword
                /// 
                var wpfInfo = moduleNames.FirstOrDefault(p => 
                       p.ModuleName.Equals("PresentationFramework.dll", StringComparison.OrdinalIgnoreCase)
                    || p.ModuleName.Equals("PresentationFramework.ni.dll", StringComparison.OrdinalIgnoreCase));
                if (wpfInfo != null)
                {
                    return result | MARSSupportedProcessType.Mars_dotNet_wpf_frame;
                }
                var infragistics = moduleNames.FirstOrDefault(p=>p.ModuleName.IndexOf("INFRAGISTICS", StringComparison.OrdinalIgnoreCase)>=0);
                if (infragistics != null)
                    return result | MARSSupportedProcessType.Mars_dotNet_Infragistics_frame;
                return result | MARSSupportedProcessType.Mars_dotNet;
            }
            var coreclrDll = moduleNames.FirstOrDefault(p => 
                   p.ModuleName.Equals("coreclr.dll", StringComparison.OrdinalIgnoreCase)
                || p.ModuleName.Equals("coreclr.ni.dll", StringComparison.OrdinalIgnoreCase));
            if (coreclrDll != null)
            {
                /// 说明是.net core
                /// 
                var wpfInfo = moduleNames.FirstOrDefault(p => p.ModuleName.Equals("PresentationFramework.dll", StringComparison.OrdinalIgnoreCase));
                if (wpfInfo != null)
                {
                    return MARSSupportedProcessType.Mars_dotNet_wpf_Core;
                }
                var infragistics = moduleNames.FirstOrDefault(p => p.ModuleName.IndexOf("INFRAGISTICS", StringComparison.OrdinalIgnoreCase) >= 0);
                if (infragistics != null)
                    return MARSSupportedProcessType.Mars_dotNet_Infragistics_Core;
                return MARSSupportedProcessType.Mars_dotNet_Core;
            }

            /// 这里存在问题
            /// MARSengine的.net framework的程序会加载PresentationFramework.dll,但是.net core的wpf程序也会加载这个dll
            /// 会让测试系统误以为是wpf，所以需要判断系统是否存在MARS的包
            var pnetCoreWpf = (from m in modules
                            where (m.ModuleName.Equals("PresentationFramework.dll", StringComparison.OrdinalIgnoreCase)
                            ||(m.ModuleName.Equals("PresentationFramework.dll", StringComparison.OrdinalIgnoreCase))
                            ||(m.ModuleName.StartsWith("PresentationNative_", StringComparison.OrdinalIgnoreCase))
                            )
                            && m.ModuleName.StartsWith("wpfgfx_co",StringComparison.OrdinalIgnoreCase)
                            && (m.ModuleName.IndexOf("ManagedInjector", StringComparison.OrdinalIgnoreCase)>=0)
                            && (m.ModuleName.IndexOf("Mars.Inter.MQCenter", StringComparison.OrdinalIgnoreCase) >= 0)
                            select m
                           ).FirstOrDefault();
            if (pnetCoreWpf != null)
            {
                return result | MARSSupportedProcessType.Mars_dotNet_wpf_Core;
            }
            /// Mars引擎会把这几个包加载，所以，第二次运行在这里会误以为wpf的程序。所以，目前取消这块
            /// 
            
            var pnetWpf = (from m in modules
                        where (m.ModuleName.Equals("PresentationFramework.dll", StringComparison.OrdinalIgnoreCase)
                        || (m.ModuleName.Equals("PresentationFramework.dll", StringComparison.OrdinalIgnoreCase)
                        || (m.ModuleName.StartsWith("PresentationNative_",StringComparison.OrdinalIgnoreCase)))                        
                        ) 
                        && (m.ModuleName.IndexOf("ManagedInjector", StringComparison.OrdinalIgnoreCase) >= 0)
                        && (m.ModuleName.IndexOf("Mars.Inter.MQCenter", StringComparison.OrdinalIgnoreCase) >= 0)
                        select m
                        ).FirstOrDefault();
            if (pnetWpf != null)
            {

                return result | MARSSupportedProcessType.Mars_dotNet_wpf_frame;
            }
            
            var pQT = (from q in modules
                       where q.ModuleName.ToUpper().IndexOf("QTCORE") >= 0
                       select q).FirstOrDefault();
            if (pQT!=null)
            {
                FileVersionInfo fv = FileVersionInfo.GetVersionInfo(pQT.ModulePath);
                strVersion = fv.FileVersion;
                return result | MARSSupportedProcessType.Mars_QT487;
            }

            return result;
        }

        
    }



}
