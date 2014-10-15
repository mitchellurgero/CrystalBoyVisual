Imports System
Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Security
Imports System.Windows.Forms

Namespace CrystalBoy.Emulator
    Friend Class NativeMethods
        ' Methods
        <SuppressUnmanagedCodeSecurity, DllImport("user32.dll", ExactSpelling:=True)> _
        Public Shared Function GetAsyncKeyState(ByVal vKey As Keys) As UInt16
        End Function

        <SuppressUnmanagedCodeSecurity, DllImport("user32.dll", CharSet:=CharSet.Auto)> _
        Public Shared Function PeekMessage(<Out> ByRef msg As Message, ByVal hWnd As IntPtr, ByVal messageFilterMin As UInt32, ByVal messageFilterMax As UInt32, ByVal flags As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function


        ' Nested Types
        <StructLayout(LayoutKind.Sequential)> _
        Public Structure Message
            Public hWnd As IntPtr
            Public msg As UInt32
            Public wParam As IntPtr
            Public lParam As IntPtr
            Public time As UInt32
            Public p As Point
        End Structure
    End Class
End Namespace

