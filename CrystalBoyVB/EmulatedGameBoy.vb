<DesignerCategory("Component")> _
Friend NotInheritable Class EmulatedGameBoy
    Implements IComponent, IClockManager, IDisposable
    ' Events
    Public Custom Event AfterReset As EventHandler
    Public Custom Event BorderChanged As EventHandler
        AddHandler(ByVal value As EventHandler)
            AddHandler Me.bus.BorderChanged, value
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
            RemoveHandler Me.bus.BorderChanged, value
        End RemoveHandler
    End Event
    Public Custom Event Break As EventHandler
    Public Custom Event Disposed As EventHandler
    Public Custom Event EmulationStatusChanged As EventHandler
    Public Custom Event NewFrame As EventHandler
        AddHandler(ByVal value As EventHandler)
            AddHandler Me.bus.FrameDone, value
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
            RemoveHandler Me.bus.FrameDone, value
        End RemoveHandler
    End Event
    Public Custom Event Paused As EventHandler
    Public Custom Event RomChanged As EventHandler

        ' Methods
    Public Sub New()
        Me.New(Nothing)
    End Sub

    Public Sub New(ByVal container As IContainer)
        Me.bus = New GameBoyMemoryBus
        Me.frameStopwatch = New Stopwatch
        Me.frameRateStopwatch = New Stopwatch
        AddHandler Me.bus.EmulationStarted, New EventHandler(AddressOf Me.OnEmulationStarted)
        AddHandler Me.bus.EmulationStopped, New EventHandler(AddressOf Me.OnEmulationStopped)
        Me.bus.ClockManager = Me
        AddHandler Me.bus.ReadKeys, New EventHandler(Of ReadKeysEventArgs)(AddressOf Me.OnReadKeys)
        Me.emulationStatus = If(Me.bus.UseBootRom, EmulationStatus.Paused, EmulationStatus.Stopped)
        If (Not container Is Nothing) Then
            container.Add(Me)
        End If
    End Sub

    Private Sub Reset() Implements IClockManager.Reset
        Me.lastFrameTime = 0
        Me.currentFrameTime = 0
        Me.frameRateStopwatch.Reset()
        Me.frameRateStopwatch.Start()
        Me.frameStopwatch.Reset()
        Me.frameStopwatch.Start()
    End Sub

    Private Sub Wait() Implements IClockManager.Wait
        If Me.enableFramerateLimiter Then
            Dim elapsedMilliseconds As Long = Me.frameStopwatch.ElapsedMilliseconds
            If (elapsedMilliseconds < &H11) Then
                If (elapsedMilliseconds < &H10) Then
                    Thread.Sleep(CInt((&H10 - CInt(elapsedMilliseconds))))
                End If
                Do While (Me.frameStopwatch.Elapsed.TotalMilliseconds < 16.666666666666668)
                Loop
            End If
        End If
        Me.lastFrameTime = Me.currentFrameTime
        Me.currentFrameTime = Me.frameRateStopwatch.Elapsed.TotalMilliseconds
        Me.frameStopwatch.Reset()
        Me.frameStopwatch.Start()
    End Sub

    Public Sub Dispose()
        If (Not Me.bus Is Nothing) Then
            Me.bus.Dispose()
            Me.bus = Nothing
            If (Not Me.Disposed Is Nothing) Then
                Me.Disposed.Invoke(Me, EventArgs.Empty)
            End If
        End If
    End Sub

    Private Function IsKeyDown(ByVal vKey As Keys) As Boolean
        Return ((NativeMethods.GetAsyncKeyState(vKey) And &H8000) <> 0)
    End Function

    Public Sub LoadRom(ByVal rom As MemoryBlock)
        Me.emulationStatus = EmulationStatus.Stopped
        Me.bus.LoadRom(rom)
        Me.emulationStatus = EmulationStatus.Paused
        Me.OnRomChanged(EventArgs.Empty)
    End Sub

    Public Sub NotifyPressedKeys(ByVal pressedKeys As GameBoyKeys)
        Me.bus.Joypads.NotifyPressedKeys(pressedKeys)
    End Sub

    Public Sub NotifyReleasedKeys(ByVal releasedKeys As GameBoyKeys)
        Me.bus.Joypads.NotifyReleasedKeys(releasedKeys)
    End Sub

    Private Sub OnAfterReset(ByVal e As EventArgs)
        Dim afterReset As EventHandler = Me.AfterReset
        If (Not afterReset Is Nothing) Then
            afterReset.Invoke(Me, e)
        End If
    End Sub

    Private Sub OnBreak(ByVal e As EventArgs)
        Dim break As EventHandler = Me.Break
        If (Not break Is Nothing) Then
            break.Invoke(Me, e)
        End If
    End Sub

    Private Sub OnEmulationStarted(ByVal sender As Object, ByVal e As EventArgs)
        Me.EmulationStatus = EmulationStatus.Running
    End Sub

    Private Sub OnEmulationStatusChanged(ByVal e As EventArgs)
        Dim emulationStatusChanged As EventHandler = Me.EmulationStatusChanged
        If (Not emulationStatusChanged Is Nothing) Then
            emulationStatusChanged.Invoke(Me, e)
        End If
    End Sub

    Private Sub OnEmulationStopped(ByVal sender As Object, ByVal e As EventArgs)
        Me.Pause((Not Me.IsDisposed AndAlso (Me.Processor.Status = ProcessorStatus.Running)))
    End Sub

    Private Sub OnPause(ByVal e As EventArgs)
        Dim paused As EventHandler = Me.Paused
        If (Not paused Is Nothing) Then
            paused.Invoke(Me, e)
        End If
    End Sub

    Private Sub OnReadKeys(ByVal sender As Object, ByVal e As ReadKeysEventArgs)
        If (e.JoypadIndex = 0) Then
            Me.bus.PressedKeys = Me.ReadKeys
        End If
    End Sub

    Private Sub OnRomChanged(ByVal e As EventArgs)
        Dim romChanged As EventHandler = Me.RomChanged
        If (Not romChanged Is Nothing) Then
            romChanged.Invoke(Me, e)
        End If
    End Sub

    Public Sub Pause()
        If Not Me.IsDisposed Then
            Me.bus.Stop()
        End If
    End Sub

    Private Sub Pause(ByVal breakpoint As Boolean)
        Me.EmulationStatus = EmulationStatus.Paused
        If breakpoint Then
            Me.OnBreak(EventArgs.Empty)
        Else
            Me.OnPause(EventArgs.Empty)
        End If
    End Sub

    Private Function ReadKeys() As GameBoyKeys
        Dim none As GameBoyKeys = GameBoyKeys.None
        If Me.IsKeyDown(Keys.Right) Then
            none = DirectCast(CByte((none Or (GameBoyKeys.None Or GameBoyKeys.Right))), GameBoyKeys)
        End If
        If Me.IsKeyDown(Keys.Left) Then
            none = DirectCast(CByte((none Or GameBoyKeys.Left)), GameBoyKeys)
        End If
        If Me.IsKeyDown(Keys.Up) Then
            none = DirectCast(CByte((none Or (GameBoyKeys.None Or GameBoyKeys.Up))), GameBoyKeys)
        End If
        If Me.IsKeyDown(Keys.Down) Then
            none = DirectCast(CByte((none Or GameBoyKeys.Down)), GameBoyKeys)
        End If
        If Me.IsKeyDown(Keys.X) Then
            none = DirectCast(CByte((none Or GameBoyKeys.A)), GameBoyKeys)
        End If
        If Me.IsKeyDown(Keys.Z) Then
            none = DirectCast(CByte((none Or GameBoyKeys.B)), GameBoyKeys)
        End If
        If Me.IsKeyDown(Keys.RShiftKey) Then
            none = DirectCast(CByte((none Or (GameBoyKeys.None Or GameBoyKeys.Select))), GameBoyKeys)
        End If
        If Me.IsKeyDown(Keys.Enter) Then
            none = DirectCast(CByte((none Or (GameBoyKeys.None Or GameBoyKeys.Start))), GameBoyKeys)
        End If
        Return none
    End Function

    Public Sub Reset()
        Me.Reset(Me.bus.HardwareType)
    End Sub

    Public Sub Reset(ByVal hardwareType As HardwareType)
        Me.bus.Reset(hardwareType)
        If ((Me.emulationStatus = EmulationStatus.Stopped) AndAlso Me.bus.UseBootRom) Then
            Me.EmulationStatus = EmulationStatus.Paused
        End If
        Me.OnAfterReset(EventArgs.Empty)
    End Sub

    Public Sub Run()
        Me.bus.Run()
    End Sub

    Public Sub RunFrame()
        If (Me.EmulationStatus = EmulationStatus.Paused) Then
            Me.RunFrameInternal()
        End If
    End Sub

    Private Sub RunFrameInternal()
        Me.bus.RunFrame()
    End Sub

    Public Sub [Step]()
        If (Me.EmulationStatus = EmulationStatus.Paused) Then
            Me.Bus.Step()
            Me.OnBreak(EventArgs.Empty)
        End If
    End Sub

    Public Sub UnloadRom()
        Me.emulationStatus = EmulationStatus.Stopped
        Me.bus.UnloadRom()
        Me.emulationStatus = If(Me.bus.UseBootRom, EmulationStatus.Paused, EmulationStatus.Stopped)
    End Sub


    ' Properties
    Public ReadOnly Property Bus As GameBoyMemoryBus
        Get
            Return Me.bus
        End Get
    End Property

    Property EmulationStatus As EmulationStatus
        Public Get
            Return Me.emulationStatus
        End Get
        Private Set(ByVal value As EmulationStatus)
            If (value <> Me.emulationStatus) Then
                Me.emulationStatus = value
                Me.OnEmulationStatusChanged(EventArgs.Empty)
            End If
        End Set
    End Property

    Public Property EnableFramerateLimiter As Boolean
        Get
            Return Me.enableFramerateLimiter
        End Get
        Set(ByVal value As Boolean)
            Me.enableFramerateLimiter = value
        End Set
    End Property

    Public ReadOnly Property ExternalRam As MemoryBlock
        Get
            Return Me.bus.ExternalRam
        End Get
    End Property

    Public ReadOnly Property FrameRate As Integer
        Get
            If (Me.emulationStatus = EmulationStatus.Running) Then
                Return CInt(Math.Round(CDbl((1000 / (Me.currentFrameTime - Me.lastFrameTime))), 0))
            End If
            Return 0
        End Get
    End Property

    Public ReadOnly Property HardwareType As HardwareType
        Get
            Return Me.bus.HardwareType
        End Get
    End Property

    Public ReadOnly Property HasCustomBorder As Boolean
        Get
            Return Me.bus.HasCustomBorder
        End Get
    End Property

    Public ReadOnly Property IsDisposed As Boolean
        Get
            Return (Me.bus Is Nothing)
        End Get
    End Property

    Public ReadOnly Property Mapper As Mapper
        Get
            Return Me.bus.Mapper
        End Get
    End Property

    Public ReadOnly Property PreciseFrameRate As Double
        Get
            If (Me.emulationStatus = EmulationStatus.Running) Then
                Return (1000 / (Me.currentFrameTime - Me.lastFrameTime))
            End If
            Return 0
        End Get
    End Property

    Public Property PressedKeys As GameBoyKeys
        Get
            Return Me.bus.PressedKeys
        End Get
        Set(ByVal value As GameBoyKeys)
            Me.bus.PressedKeys = value
        End Set
    End Property

    Public ReadOnly Property Processor As Processor
        Get
            Return Me.bus.Processor
        End Get
    End Property

    Public ReadOnly Property RomInformation As RomInformation
        Get
            Return Me.bus.RomInformation
        End Get
    End Property

    Public ReadOnly Property RomLoaded As Boolean
        Get
            Return Me.bus.RomLoaded
        End Get
    End Property

    Public Property Site As ISite
        Get
        Set(ByVal value As ISite)
    End Property
    Public Property TryUsingBootRom As Boolean
        Get
            Return Me.bus.TryUsingBootRom
        End Get
        Set(ByVal value As Boolean)
            Me.bus.TryUsingBootRom = value
        End Set
    End Property


    ' Fields
    Private bus As GameBoyMemoryBus
    Private currentFrameTime As Double
    Private emulationStatus As EmulationStatus
    Private enableFramerateLimiter As Boolean
    Private frameRateStopwatch As Stopwatch
    Private frameStopwatch As Stopwatch
    Private lastFrameTime As Double
End Class


