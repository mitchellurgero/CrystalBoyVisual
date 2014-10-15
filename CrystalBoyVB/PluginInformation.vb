Imports System
Imports System.ComponentModel
Imports System.Runtime.InteropServices

Namespace CrystalBoy.Emulator
    <StructLayout(LayoutKind.Sequential)> _
    Friend Structure PluginInformation
        Public ReadOnly Type As Type
        Public ReadOnly DisplayName As String
        Public ReadOnly Description As String
        Friend Sub New(ByVal type As Type)
            Dim customAttributes As DisplayNameAttribute() = TryCast(type.GetCustomAttributes(GetType(DisplayNameAttribute), False),DisplayNameAttribute())
            Dim attributeArray2 As DescriptionAttribute() = TryCast(type.GetCustomAttributes(GetType(DescriptionAttribute), False),DescriptionAttribute())
            Me.Type = type
            Me.DisplayName = String.Intern(If((customAttributes.Length > 0), customAttributes(0).DisplayName, type.Name))
            Me.Description = If((attributeArray2.Length > 0), attributeArray2(0).Description, Nothing)
        End Sub

        Friend Sub New(ByVal type As Type, ByVal displayName As String, ByVal description As String)
            Me.Type = type
            Me.DisplayName = displayName
            Me.Description = description
        End Sub
    End Structure
End Namespace

