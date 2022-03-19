Imports System.IO
Public Class PngImage
    Public PngColorTypeString() As String = {"Grayscale", "Reserved (1)", "RGB (TrueColor)", "Indexed Palette", "Grayscale with Alpha", "Reserved (5)", "RGB with Alpha", "Reserved (7)"}
    Public Structure PngChunkHeader
        Dim Length As UInteger
        Dim ChunkType As UInteger
        Dim CheckSum As UInteger
        Dim RawData() As Byte
    End Structure

    Public Enum PngPixelFormat
        Grayscale = 0
        TrueColorRGB = 2
        IndexedPalette = 3
        GrayscaleAlpha = 4
        RGBA = 6
    End Enum

    Public Structure PngChunkIHdr
        Dim Width As Integer
        Dim Height As Integer
        Dim BitDepth As Byte
        Dim ColorType As Byte
        Dim CompressionMethod As Byte
        Dim FilterMethod As Byte
        Dim InterlaceMethod As Byte
    End Structure

    Public Const PngSignature As Long = &HA1A0A0D474E5089

    Public Const PngChunkTypeIHDR As Integer = &H52444849
    Public Const PngChunkTypeIDAT As Integer = &H54414449
    Public Const PngChunkTypeIEND As Integer = &H444E4549
    Public Const PngChunkTypePLTE As Integer = &H45544C50

    Public NumberOfChunks As UInteger
    Dim ChunkList As ArrayList

    Sub New()
        NumberOfChunks = 0
        ChunkList = New ArrayList
    End Sub

    Public Sub AddChunk(ByVal Length As UInteger, ByVal ChunkType As UInteger, ByVal CheckSum As UInteger, ByRef RawData() As Byte)
        Dim NewChunk As PngChunkHeader
        NewChunk.Length = Length
        NewChunk.ChunkType = ChunkType
        NewChunk.CheckSum = CheckSum
        ReDim NewChunk.RawData(Length - 1)
        Array.Copy(RawData, NewChunk.RawData, Length)
        ChunkList.Add(NewChunk)
        NumberOfChunks += 1
    End Sub

    Public Function ValidateImage() As Boolean
        Dim IHdr As PngChunkHeader, IEnd As PngChunkHeader
        IHdr = ChunkList(0)
        IEnd = ChunkList(NumberOfChunks - 1)
        If IHdr.ChunkType <> PngChunkTypeIHDR Then Return False
        If IEnd.ChunkType <> PngChunkTypeIEND Then Return False
        ' FIXME: Verify the checksum for each chunks.
        Return True
    End Function

    Public Sub GetBasicMetadata(ByRef Metadata As PngChunkIHdr)
        Dim IHdr As PngChunkHeader = ChunkList(0)
        If IHdr.ChunkType <> PngChunkTypeIHDR Then Throw New Exception("The first chunk of this PNG is not IHDR!")
        Dim Buff(3) As Byte
        With Metadata
            Array.Copy(IHdr.RawData, 0, Buff, 0, 4)
            Array.Reverse(Buff)
            .Width = BitConverter.ToUInt32(Buff, 0)
            Array.Copy(IHdr.RawData, 4, Buff, 0, 4)
            Array.Reverse(Buff)
            .Height = BitConverter.ToUInt32(Buff, 0)
            .BitDepth = IHdr.RawData(8)
            .ColorType = IHdr.RawData(9)
            .CompressionMethod = IHdr.RawData(10)
            .FilterMethod = IHdr.RawData(11)
            .InterlaceMethod = IHdr.RawData(12)
        End With
    End Sub

    Public Sub Save(ByVal TargetStream As FileStream)
        TargetStream.Seek(0, SeekOrigin.Begin)
        Dim Buff(7) As Byte
        Dim i As UInteger
        Buff = BitConverter.GetBytes(PngSignature)
        TargetStream.Write(Buff, 0, 8)
        ReDim Buff(3)
        For Each Chunk As PngChunkHeader In ChunkList
            Buff = BitConverter.GetBytes(Chunk.Length)
            If BitConverter.IsLittleEndian Then Array.Reverse(Buff)
            TargetStream.Write(Buff, 0, 4)
            Buff = BitConverter.GetBytes(Chunk.ChunkType)
            TargetStream.Write(Buff, 0, 4)
            TargetStream.Write(Chunk.RawData, 0, Chunk.Length)
            Buff = BitConverter.GetBytes(Chunk.CheckSum)
            TargetStream.Write(Buff, 0, 4)
        Next
    End Sub
End Class
