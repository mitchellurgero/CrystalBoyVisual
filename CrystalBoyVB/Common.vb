Imports CrystalBoy.Emulator.Properties
Imports System
Imports System.Drawing

Namespace CrystalBoy.Emulator
    Friend Class Common
        ' Methods
        Public Shared Sub ClearBitmap(ByVal bitmap As Bitmap)
            Dim graphics As Graphics = Graphics.FromImage(bitmap)
            graphics.FillRectangle(Brushes.White, 0, 0, bitmap.Width, bitmap.Height)
            graphics.Dispose
        End Sub

        Public Shared Function FormatSize(ByVal size As Integer) As String
            Dim oneByteFormat As String
            Dim num As Integer = size
            Dim num2 As Integer = 0
            Dim num3 As Integer = 1
            If (size < 0) Then
                Throw New ArgumentOutOfRangeException("size")
            End If
            Do While (num >= &H400)
                num = (num >> 10)
                num2 += 1
            Loop
            Select Case num2
                Case 0
                    If (size <> 1) Then
                        oneByteFormat = Resources.ByteFormat
                        Exit Select
                    End If
                    oneByteFormat = Resources.OneByteFormat
                    Exit Select
                Case 1
                    oneByteFormat = Resources.KiloByteFormat
                    num3 = &H400
                    Exit Select
                Case 2
                    oneByteFormat = Resources.MegaByteFormat
                    num3 = &H100000
                    Exit Select
                Case Else
                    oneByteFormat = Resources.GigaByteFormat
                    num3 = &H40000000
                    Exit Select
            End Select
            If (num2 = 0) Then
                Return String.Format(Resources.Culture, oneByteFormat, New Object() { size })
            End If
            Return String.Format(Resources.Culture, oneByteFormat, New Object() { (CDbl(size) / CDbl(num3)) })
        End Function

    End Class
End Namespace

