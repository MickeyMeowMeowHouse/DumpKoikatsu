Imports System.IO
Imports System.Drawing
Imports System.Text
Public Class Form1
    Dim BodyPhoto As PngImage
    Dim IdPhoto As PngImage
    Dim Intersession() As Byte
    Dim Profile() As Byte
    Dim Gfx As Graphics

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        TreeView1.Nodes.Clear()
        Dim Fs As New FileStream(TextBox1.Text, FileMode.Open)
        TreeView1.Nodes.Add("Profile Size:" + Str(Fs.Length) + " bytes")
        ' Stage I: Run the analysis for first part of PNG File
        Dim Buff(7) As Byte
        Dim Signature As Long
        Fs.Read(Buff, 0, 8)
        Signature = BitConverter.ToInt64(Buff, 0)
        If Signature <> PngImage.PngSignature Then
            MsgBox("The file does not have PNG signature!", vbExclamation, "Error")
            GoTo EndDump
        End If
        Dim Length As UInteger, ChunkType As UInteger, CheckSum As UInteger, RawData() As Byte
        Dim Offset As Long = 8
        BodyPhoto = New PngImage
        IdPhoto = New PngImage
        ReDim Buff(3)
        With TreeView1.Nodes.Add("ID Photo").Nodes
            With .Add("Chunks").Nodes
                ' Traverse all chunks.
                Do
                    Dim ChunkTypeString As String
                    Fs.Read(Buff, 0, 4)
                    If BitConverter.IsLittleEndian Then Array.Reverse(Buff) ' The length member is stored as big-endian. Reverse the byte-order.
                    Length = BitConverter.ToUInt32(Buff, 0)
                    ReDim RawData(Length - 1)
                    Fs.Read(Buff, 0, 4)
                    ChunkType = BitConverter.ToUInt32(Buff, 0)
                    ChunkTypeString = Encoding.ASCII.GetString(Buff)
                    Fs.Read(RawData, 0, Length)
                    Fs.Read(Buff, 0, 4)
                    CheckSum = BitConverter.ToUInt32(Buff, 0)
                    IdPhoto.AddChunk(Length, ChunkType, CheckSum, RawData)
                    Offset += 8
                    With .Add("Chunk #" + CStr(IdPhoto.NumberOfChunks)).Nodes
                        .Add("Offset to Chunk Raw Data: 0x" + Hex(Offset))
                        .Add("Size of Chunk:" + Str(Length) + " bytes")
                        .Add("Chunk CRC: 0x" + Hex(CheckSum))
                        .Add("Type of Chunk: " + ChunkTypeString)
                    End With
                    Offset += Length + 4
                Loop Until ChunkType = PngImage.PngChunkTypeIEND
                If IdPhoto.ValidateImage() = False Then
                    MsgBox("Validation failed! This ID-Photo PNG does not seem valid!", vbExclamation, "Error")
                    GoTo EndDump
                End If
            End With
            Dim IdPhotoMetadata As PngImage.PngChunkIHdr
            IdPhoto.GetBasicMetadata(IdPhotoMetadata)
            .Add("Width:" + Str(IdPhotoMetadata.Width) + " Pixels")
            .Add("Height:" + Str(IdPhotoMetadata.Height) + " Pixels")
            .Add("Bit Depth:" + Str(IdPhotoMetadata.BitDepth))
            .Add("Pixel Format: " + IdPhoto.PngColorTypeString(IdPhotoMetadata.ColorType))
            Select Case IdPhotoMetadata.InterlaceMethod
                Case 0
                    .Add("No Interlace")
                Case 1
                    .Add("Adam7 Interlace")
                Case Else
                    .Add("Unknown Interlace")
            End Select
        End With
        ' Search the PNG signature by bruteforce
        ' For best performance, use more memory space than disk I/O.
        ReDim Buff(Fs.Length - Offset - 1)
        Fs.Read(Buff, 0, Buff.Length)
        Dim i As Integer = 0, EffectiveOffset As Integer = -1
        For i = 0 To Buff.Length - 1 Step 1
            Signature = BitConverter.ToInt64(Buff, i)
            If Signature = PngImage.PngSignature Then
                EffectiveOffset = i
                Exit For
            End If
        Next
        If EffectiveOffset = -1 Then
            MsgBox("The Body Photo is missing!", vbExclamation, "Error")
            GoTo EndDump
        End If
        With TreeView1.Nodes.Add("Intersession").Nodes
            .Add("Size:" + Str(EffectiveOffset) + " bytes").Tag = CStr(EffectiveOffset)
            .Add("Offset: 0x" + Hex(Offset)).Tag = CStr(Offset)
        End With
        ReDim Intersession(EffectiveOffset - 1)
        Array.Copy(Buff, Intersession, EffectiveOffset)
        Dim ProfileLength As Integer = Buff.Length
        With TreeView1.Nodes.Add("Body Photo").Nodes
            Dim CurrentOffset As Integer = EffectiveOffset + 8
            With .Add("Chunks").Nodes
                ' Traverse all chunks
                Do
                    Dim Tmp(3) As Byte
                    Dim ChunkTypeString As String
                    Array.Copy(Buff, CurrentOffset, Tmp, 0, 4)
                    Array.Reverse(Tmp)
                    Length = BitConverter.ToUInt32(Tmp, 0)
                    ChunkType = BitConverter.ToUInt32(Buff, CurrentOffset + 4)
                    ChunkTypeString = Encoding.ASCII.GetString(Buff, CurrentOffset + 4, 4)
                    ReDim RawData(Length - 1)
                    Array.Copy(Buff, CurrentOffset + 8, RawData, 0, Length)
                    CheckSum = BitConverter.ToUInt32(Buff, CurrentOffset + Length + 8)
                    BodyPhoto.AddChunk(Length, ChunkType, CheckSum, RawData)
                    With .Add("Chunk #" + CStr(BodyPhoto.NumberOfChunks)).Nodes
                        .Add("Offset to Chunk Raw Data: 0x" + Hex(CurrentOffset + 8))
                        .Add("Size of Chunk:" + Str(Length) + " bytes")
                        .Add("Chunk CRC: 0x" + Hex(CheckSum))
                        .Add("Type of Chunk: " + ChunkTypeString)
                    End With
                    CurrentOffset += Length + 12
                Loop Until ChunkType = PngImage.PngChunkTypeIEND
                If BodyPhoto.ValidateImage() = False Then
                    MsgBox("Validation failed! Body-Photo PNG does not seem valid!", vbExclamation, "Error")
                    GoTo EndDump
                End If
            End With
            Dim BodyPhotoMetadata As PngImage.PngChunkIHdr
            BodyPhoto.GetBasicMetadata(BodyPhotoMetadata)
            .Add("Width:" + Str(BodyPhotoMetadata.Width) + " Pixels")
            .Add("Height:" + Str(BodyPhotoMetadata.Height) + " Pixels")
            .Add("Bit Depth:" + Str(BodyPhotoMetadata.BitDepth))
            .Add("Pixel Format: " + BodyPhoto.PngColorTypeString(BodyPhotoMetadata.ColorType))
            Select Case BodyPhotoMetadata.InterlaceMethod
                Case 0
                    .Add("No Interlace")
                Case 1
                    .Add("Adam7 Interlace")
                Case Else
                    .Add("Unknown Interlace")
            End Select
            ProfileLength -= CurrentOffset
        End With
        With TreeView1.Nodes.Add("Profile").Nodes
            .Add("Size:" + Str(ProfileLength) + " bytes").Tag = CStr(ProfileLength)
            .Add("Offset: 0x" + Hex(Fs.Length - ProfileLength)).Tag = CStr(Fs.Length - ProfileLength)
        End With
        ReDim Profile(ProfileLength - 1)
        Array.Copy(Buff, Buff.Length - ProfileLength, Profile, 0, ProfileLength)
EndDump:
        Fs.Close()
    End Sub

    Private Sub TextBox1_DragDrop(ByVal sender As Object, ByVal e As System.Windows.Forms.DragEventArgs) Handles TextBox1.DragDrop
        Dim FilePaths() As String = e.Data.GetData(DataFormats.FileDrop)
        If FilePaths.Length > 1 Then MsgBox("Only one file is allowed at a time!", vbExclamation, "Error")
        TextBox1.Text = FilePaths(0)
    End Sub

    Private Sub TextBox1_DragEnter(ByVal sender As Object, ByVal e As System.Windows.Forms.DragEventArgs) Handles TextBox1.DragEnter
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then e.Effect = DragDropEffects.Copy
    End Sub

    Private Sub TreeView1_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles TreeView1.DoubleClick
        Dim ShowSave As Boolean = False, SaveBin As Boolean = False
        Dim Photo As PngImage = Nothing, BinBuff(1) As Byte
        If TreeView1.SelectedNode.Text = "ID Photo" Then
            SaveFileDialog1.Title = "Save the ID Photo..."
            SaveFileDialog1.FileName = "id_photo.png"
            Photo = IdPhoto
            ShowSave = True
        ElseIf TreeView1.SelectedNode.Text = "Body Photo" Then
            SaveFileDialog1.Title = "Save the Body Photo..."
            SaveFileDialog1.FileName = "body_photo.png"
            Photo = BodyPhoto
            ShowSave = True
        ElseIf TreeView1.SelectedNode.Text = "Intersession" Then
            SaveFileDialog1.Title = "Save the Intersession Binary..."
            SaveFileDialog1.FileName = "intersession.bin"
            BinBuff = Intersession
            SaveBin = True
        ElseIf TreeView1.SelectedNode.Text = "Profile" Then
            SaveFileDialog1.Title = "Save the Profile Binary..."
            SaveFileDialog1.FileName = "profile.bin"
            BinBuff = Profile
            SaveBin = True
        End If
        If ShowSave Then
            SaveFileDialog1.Filter = "Portable Network Graphics|*.png"
            If SaveFileDialog1.ShowDialog() = DialogResult.OK Then
                Dim Fs As New FileStream(SaveFileDialog1.FileName, FileMode.Create)
                Photo.Save(Fs)
                Fs.Close()
                PictureBox1.Image = Image.FromFile(SaveFileDialog1.FileName)
            End If
        ElseIf SaveBin Then
            SaveFileDialog1.Filter = "Binary File|*.bin"
            If SaveFileDialog1.ShowDialog() = Windows.Forms.DialogResult.OK Then
                Dim Fs As New FileStream(SaveFileDialog1.FileName, FileMode.Create)
                Fs.Write(BinBuff, 0, BinBuff.Length)
                Fs.Close()
            End If
        End If
    End Sub
End Class
