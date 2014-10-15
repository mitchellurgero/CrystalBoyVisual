Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports CrystalBoy.Core
Imports CrystalBoy.Emulation
Imports CrystalBoy.Emulator.Properties
Imports System.Collections.Generic
Imports System.Reflection
Imports System.ComponentModel
Public Class Form1
    Private debuggerForm As DebuggerForm
    Private tileViewerForm As TileViewerForm
    Private mapViewerForm As MapViewerForm
    Private romInformationForm As RomInformationForm
    Private emulatedGameBoy As EmulatedGameBoy
    Private videoRenderer As VideoRenderer
    Private audioRenderer As AudioRenderer
    Private videoRendererMenuItemDictionary As Dictionary(Of Type, ToolStripMenuItem)
    Private audioRendererMenuItemDictionary As Dictionary(Of Type, ToolStripMenuItem)
    Private ramSaveWriter As BinaryWriter
    Private ramSaveReader As BinaryReader
    Private pausedTemporarily As Boolean
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub
    Private Sub LoadRom(fileName As String)
        Dim romFileInfo = New FileInfo(fileName)

        ' Open only existing rom files
        If Not romFileInfo.Exists Then
            Throw New FileNotFoundException()
        End If
        ' Limit the rom size to 4 Mb
        If romFileInfo.Length > 4 * 1024 * 1024 Then
            Throw New InvalidOperationException()
        End If

        emulatedGameBoy.LoadRom(MemoryUtility.ReadFile(romFileInfo))

        If emulatedGameBoy.RomInformation.HasRam AndAlso emulatedGameBoy.RomInformation.HasBattery Then
            Dim ramFileInfo = New FileInfo(Path.Combine(romFileInfo.DirectoryName, Path.GetFileNameWithoutExtension(romFileInfo.Name)) & ".sav")

            Dim ramSaveStream = ramFileInfo.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)
            ramSaveStream.SetLength(emulatedGameBoy.Mapper.SavedRamSize + (If(emulatedGameBoy.RomInformation.HasTimer, 48, 0)))
            ramSaveStream.Read(emulatedGameBoy.ExternalRam, 0, emulatedGameBoy.Mapper.SavedRamSize)
            ramSaveWriter = New BinaryWriter(ramSaveStream)

            If emulatedGameBoy.RomInformation.HasTimer Then
                Dim mbc3 = TryCast(emulatedGameBoy.Mapper, CrystalBoy.Emulation.Mappers.MemoryBankController3)

                If mbc3 IsNot Nothing Then
                    Dim rtcState = mbc3.RtcState
                    ramSaveReader = New BinaryReader(ramSaveStream)

                    rtcState.Frozen = True

                    rtcState.Seconds = CByte(ramSaveReader.ReadInt32())
                    rtcState.Minutes = CByte(ramSaveReader.ReadInt32())
                    rtcState.Hours = CByte(ramSaveReader.ReadInt32())
                    rtcState.Days = CShort(CByte(ramSaveReader.ReadInt32()) + (CByte(ramSaveReader.ReadInt32()) << 8))

                    rtcState.LatchedSeconds = CByte(ramSaveReader.ReadInt32())
                    rtcState.LatchedMinutes = CByte(ramSaveReader.ReadInt32())
                    rtcState.LatchedHours = CByte(ramSaveReader.ReadInt32())
                    rtcState.LatchedDays = CShort(CByte(ramSaveReader.ReadInt32()) + (CByte(ramSaveReader.ReadInt32()) << 8))

                    rtcState.DateTime = New DateTime(1970, 1, 1) + TimeSpan.FromSeconds(ramSaveReader.ReadInt64())

                    rtcState.Frozen = False
                End If
            End If

            emulatedGameBoy.Mapper.RamUpdated += Mapper_RamUpdated
        End If

        emulatedGameBoy.Run()
    End Sub
End Class
