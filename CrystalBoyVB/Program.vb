Imports CrystalBoy.Emulation
Imports CrystalBoy.Emulator.Properties
Imports System
Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Collections.Specialized
Imports System.IO
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Namespace CrystalBoy.Emulator
    Friend Class Program
        ' Methods
        Private Shared Sub FindPlugins(ByVal [assembly] As Assembly)
            Try 
                Dim type As Type
                For Each type In [assembly].GetTypes
                    Try 
                        If Not type.IsAbstract Then
                            Dim typeArray As Type() = If(type.IsGenericType, type.GetGenericArguments, Type.EmptyTypes)
                            If (typeArray.Length <= 1) Then
                                Dim baseType As Type = type
                                Dim type3 As Type = If(Not type.IsSubclassOf(GetType(AudioRenderer)), If(Not type.IsSubclassOf(GetType(VideoRenderer)), Nothing, GetType(VideoRenderer(Of ))), GetType(AudioRenderer(Of )))
                                If (Not type3 Is Nothing) Then
                                    Do While True
                                        baseType = type.BaseType
                                        If baseType.IsGenericType Then
                                            Dim genericTypeDefinition As Type = baseType.GetGenericTypeDefinition
                                            Dim genericArguments As Type() = baseType.GetGenericArguments
                                            If ((genericTypeDefinition Is type3) AndAlso (Program.IsGenericArgumentTypeSupported(genericArguments(0), Program.supportedRenderObjectTypes, type.IsGenericType, Not type.IsGenericType, (GenericParameterAttributes.DefaultConstructorConstraint Or GenericParameterAttributes.ReferenceTypeConstraint), GenericParameterAttributes.ReferenceTypeConstraint) AndAlso (Not type.GetConstructor(New Type() { genericArguments(0) }) Is Nothing))) Then
                                                Program.pluginList.Add(New PluginInformation(type))
                                                Continue Do
                                            End If
                                        ElseIf (baseType Is type3.BaseType) Then
                                            If Not (type.IsGenericType OrElse (type.GetConstructor(Type.EmptyTypes) Is Nothing)) Then
                                                Program.pluginList.Add(New PluginInformation(type))
                                            End If
                                            Continue Do
                                        End If
                                    Loop
                                End If
                            End If
                        End If
                    Catch exception As TypeLoadException
                        Console.Error.WriteLine(exception)
                        MessageBox.Show(exception.ToString, Resources.TypeLoadingErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                    End Try
                Next
            Catch exception2 As ReflectionTypeLoadException
                Console.Error.WriteLine(exception2)
                Dim exception3 As Exception
                For Each exception3 In exception2.LoaderExceptions
                    Console.Error.WriteLine(exception3)
                Next
                MessageBox.Show(exception2.ToString, Resources.TypeLoadingErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End Try
        End Sub

        Private Shared Function IsGenericArgumentTypeSupported(ByVal argumentType As Type, ByVal supportedTypes As Type(), ByVal Optional allowOpen As Boolean = False, ByVal Optional allowClosed As Boolean = True, ByVal Optional allowedParameterAttributes As GenericParameterAttributes = &H1F, ByVal Optional requiredParameterAttributes As GenericParameterAttributes = 0) As Boolean
            If argumentType.IsGenericParameter Then
                Return ((allowOpen AndAlso ((argumentType.GenericParameterAttributes And Not allowedParameterAttributes) = GenericParameterAttributes.None)) AndAlso ((argumentType.GenericParameterAttributes And requiredParameterAttributes) = requiredParameterAttributes))
            End If
            If allowClosed Then
                Dim type As Type
                For Each type In supportedTypes
                    If (argumentType Is type) Then
                        Return True
                    End If
                Next
            End If
            Return False
        End Function

        Private Shared Sub LoadPluginAssemblies()
            Dim strings As New StringCollection
            Dim flag As Boolean = False
            Dim str As String
            For Each str In Settings.Default.PluginAssemblies
                Try 
                    Dim assembly As Assembly = Assembly.LoadFrom(str)
                    Program.FindPlugins([assembly])
                    Program.pluginAssemblyList.Add([assembly])
                    strings.Add(str)
                Catch exception As FileNotFoundException
                    Console.Error.WriteLine(exception)
                    MessageBox.Show(String.Format(Resources.Culture, Resources.AssemblyNotFoundErrorMessage, New Object() { str }), Resources.AssemblyLoadErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                    flag = True
                Catch exception2 As BadImageFormatException
                    Console.Error.WriteLine(exception2)
                    MessageBox.Show(String.Format(Resources.Culture, Resources.AssemblyArchitectureErrorMessage, New Object() { str }), Resources.AssemblyLoadErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                    flag = True
                Catch exception3 As Exception
                    Console.Error.WriteLine(exception3)
                    MessageBox.Show(String.Format(Resources.Culture, Resources.AssemblyLoadErrorMessage, New Object() { str, exception3 }), Resources.AssemblyLoadErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                    flag = True
                End Try
            Next
            If flag Then
                Settings.Default.PluginAssemblies = strings
                Settings.Default.Save
            End If
        End Sub

        <STAThread> _
        Private Shared Sub Main()
            Dim newError As New StreamWriter(New ErrorStream(Console.OpenStandardError, "CrystalBoy.Emulator.log")) With { _
                .AutoFlush = True _
            }
            Console.SetError(newError)
            Program.SetProcessDPIAware
            Application.EnableVisualStyles
            Application.SetCompatibleTextRenderingDefault(False)
            ToolStripManager.VisualStylesEnabled = True
            ToolStripManager.RenderMode = ToolStripManagerRenderMode.Professional
            RuntimeHelpers.RunClassConstructor(GetType(LookupTables).TypeHandle)
            Program.FindPlugins(GetType(Program).Assembly)
            Program.LoadPluginAssemblies
            GC.Collect
            Application.Run(New MainForm)
        End Sub

        <DllImport("user32", SetLastError:=True)> _
        Private Shared Function SetProcessDPIAware() As Boolean
        End Function


        ' Fields
        Public Shared ReadOnly PluginAssemblyCollection As ReadOnlyCollection(Of Assembly) = New ReadOnlyCollection(Of Assembly)(Program.pluginAssemblyList)
        Private Shared ReadOnly pluginAssemblyList As List(Of Assembly) = New List(Of Assembly)
        Public Shared ReadOnly PluginCollection As ReadOnlyCollection(Of PluginInformation) = New ReadOnlyCollection(Of PluginInformation)(Program.pluginList)
        Private Shared ReadOnly pluginList As List(Of PluginInformation) = New List(Of PluginInformation)
        Private Shared ReadOnly supportedRenderObjectTypes As Type() = New Type() { GetType(Control), GetType(IWin32Window) }
        Private Shared ReadOnly supportedSampleTypes As Type() = New Type() { GetType(Short) }

        ' Nested Types
        Private NotInheritable Class ErrorStream
            Inherits Stream
            ' Methods
            Public Sub New(ByVal standardErrorStream As Stream, ByVal logFileName As String)
                Me.standardErrorStream = standardErrorStream
                Me.logFileName = If(Path.IsPathRooted(logFileName), logFileName, Path.Combine(Environment.CurrentDirectory, logFileName))
            End Sub

            Public Overrides Function BeginRead(ByVal buffer As Byte(), ByVal offset As Integer, ByVal count As Integer, ByVal callback As AsyncCallback, ByVal state As Object) As IAsyncResult
                Throw New NotSupportedException
            End Function

            Protected Overrides Sub Dispose(ByVal disposing As Boolean)
                If disposing Then
                    If (Not Me.standardErrorStream Is Nothing) Then
                        Me.standardErrorStream.Dispose
                    End If
                    Me.standardErrorStream = Nothing
                    If (Not Me.fileStream Is Nothing) Then
                        Me.fileStream.Close
                    End If
                    Me.fileStream = Nothing
                End If
                MyBase.Dispose(disposing)
            End Sub

            Public Overrides Sub Flush()
                If (Me.standardErrorStream Is Nothing) Then
                    Throw New ObjectDisposedException(MyBase.GetType.FullName)
                End If
                Me.standardErrorStream.Flush
                If (Not Me.fileStream Is Nothing) Then
                    Me.fileStream.Flush
                End If
            End Sub

            Public Overrides Function Read(ByVal buffer As Byte(), ByVal offset As Integer, ByVal count As Integer) As Integer
                Throw New NotSupportedException
            End Function

            Public Overrides Function ReadByte() As Integer
                Throw New NotSupportedException
            End Function

            Public Overrides Function Seek(ByVal offset As Long, ByVal origin As SeekOrigin) As Long
                Throw New NotSupportedException
            End Function

            Public Overrides Sub SetLength(ByVal value As Long)
                Throw New NotSupportedException
            End Sub

            Private Sub TryCreateFileStream()
                If Not Me.fileStreamCreationFailed Then
                    Try 
                        Me.fileStream = File.Create(Me.logFileName, &H1000, FileOptions.SequentialScan)
                    Catch exception As Exception
                        Me.fileStreamCreationFailed = True
                        Console.Error.WriteLine(exception)
                    End Try
                End If
            End Sub

            Public Overrides Sub Write(ByVal buffer As Byte(), ByVal offset As Integer, ByVal count As Integer)
                If (Me.standardErrorStream Is Nothing) Then
                    Throw New ObjectDisposedException(MyBase.GetType.FullName)
                End If
                Me.standardErrorStream.Write(buffer, offset, count)
                If (Me.fileStream Is Nothing) Then
                    Me.TryCreateFileStream
                End If
                If (Not Me.fileStream Is Nothing) Then
                    Try 
                        Me.fileStream.Write(buffer, offset, count)
                    Catch exception As IOException
                        Me.fileStreamCreationFailed = True
                        Try 
                            Me.fileStream.Close
                        Finally
                            Me.fileStream = Nothing
                        End Try
                        Console.Error.WriteLine(exception)
                    End Try
                End If
            End Sub


            ' Properties
            Public Overrides ReadOnly Property CanRead As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property CanSeek As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property CanWrite As Boolean
                Get
                    Return True
                End Get
            End Property

            Public Overrides ReadOnly Property Length As Long
                Get
                    If Me.fileStreamCreationFailed Then
                        Throw New NotSupportedException
                    End If
                    If (Me.standardErrorStream Is Nothing) Then
                        Throw New ObjectDisposedException(MyBase.GetType.FullName)
                    End If
                    Return If((Not Me.fileStream Is Nothing), Me.fileStream.Length, 0)
                End Get
            End Property

            Public Overrides Property Position As Long
                Get
                    Throw New NotSupportedException
                End Get
                Set(ByVal value As Long)
                    Throw New NotSupportedException
                End Set
            End Property


            ' Fields
            Private fileStream As Stream
            Private fileStreamCreationFailed As Boolean
            Private logFileName As String
            Private standardErrorStream As Stream
        End Class
    End Class
End Namespace

