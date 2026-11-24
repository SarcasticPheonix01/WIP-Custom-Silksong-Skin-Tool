// WARNING
// This is very rough - please do not expect it to work perfectly
// I have not published this to be used right at the moment
// I just want to be transparent with my work and get suggestions if given

#:package ParquetSharp@19.0.0
#:package SkiaSharp@3.116.1

using System.IO;
using ParquetSharp;
using SkiaSharp;

static SequenceInfo GetAtlasInfo(int atlasNum, int sequence)
{
	// Replace with your file path
	string filePath = @"C:\FILEPATH\info.parquet";
	using var file = new ParquetFileReader(filePath);

	for (int rg = 0; rg < file.FileMetaData.NumRowGroups; rg++)
	{
		using var rowGroup = file.RowGroup(rg);

		var atlasNums = rowGroup.Column(0).LogicalReader<int>().ReadAll((int)rowGroup.MetaData.NumRows);
		if (atlasNums[0] != atlasNum) continue;

		var sequences = rowGroup.Column(1).LogicalReader<int>().ReadAll((int)rowGroup.MetaData.NumRows);
		int idx = Array.IndexOf(sequences, sequence);
		if (idx == -1) continue;

		var headNums  = rowGroup.Column(2).LogicalReader<int>().ReadAll((int)rowGroup.MetaData.NumRows);
		var angles    = rowGroup.Column(3).LogicalReader<int>().ReadAll((int)rowGroup.MetaData.NumRows);
		var xPoss     = rowGroup.Column(4).LogicalReader<int>().ReadAll((int)rowGroup.MetaData.NumRows);
		var yPoss     = rowGroup.Column(5).LogicalReader<int>().ReadAll((int)rowGroup.MetaData.NumRows);
		var xOffsets  = rowGroup.Column(6).LogicalReader<int>().ReadAll((int)rowGroup.MetaData.NumRows);
		var yOffsets  = rowGroup.Column(7).LogicalReader<int>().ReadAll((int)rowGroup.MetaData.NumRows);

		return new SequenceInfo(headNums[idx], angles[idx], xPoss[idx], yPoss[idx], xOffsets[idx], yOffsets[idx]);
	}

	throw new Exception($"No data found for atlasNum={atlasNum}, sequence={sequence}");
}

static void UpdateDestination(string destName)
{
	
	var spriteElements = destName.Split('-');
	int atlasNum = int.Parse(spriteElements[0]);
	int seqNum = int.Parse(spriteElements[1]);
	int idNum = int.Parse(spriteElements[2]);
	string atlasStr = spriteElements[0];
	string seqStr = spriteElements[1];
	var atlasInfo = GetAtlasInfo(atlasNum, seqNum);
	
	string atlasPath = @"C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight Silksong\" +
				@"Hollow Knight Silksong_Data\Mods\Customizer\DefaultHornetAtlases\Knight";
	
	string animationPath = Directory.EnumerateDirectories(atlasPath, $"{atlasStr}*")
						.Single();
	string spritePath = Directory.EnumerateFiles(animationPath, $"{destName}*")
                        .Single();
	
	// dest - distance
	// src - source
	// replace with your file path
	using SKBitmap srcBitmap = SKBitmap.Decode(@"C:\FILEPATH\CustomHeads\CustomHead" + atlasInfo.HeadNum + @".png");
	using SKBitmap ogBitmap = SKBitmap.Decode(@"C:\FILEPATH\OgHeads\OgHead" + atlasInfo.HeadNum + @".png");
	using SKBitmap destBitmap = SKBitmap.Decode(spritePath);

	int xPos = atlasInfo.XPos + atlasInfo.XOffset;
	int yPos = atlasInfo.YPos + atlasInfo.YOffset;

	//Get ogBitmap ready for removal of sprite headshape
	using var ogBitmapExp = new SKBitmap(destBitmap.Width, destBitmap.Height);
	using (var ogCanvas = new SKCanvas(ogBitmapExp))
	{
		ogCanvas.Clear(SKColors.Transparent);
		ogCanvas.DrawImage(SKImage.FromBitmap(ogBitmap), xPos, yPos);
	}


	// Save just the custom head, positioned in the proper place in space
	using var srcBitmapExp = new SKBitmap(destBitmap.Width, destBitmap.Height);
	using (var srcCanvas = new SKCanvas(srcBitmapExp))
	{
		srcCanvas.Clear(SKColors.Transparent);
		srcCanvas.DrawImage(SKImage.FromBitmap(srcBitmap), xPos, yPos);
	}
	
	using var image = SKImage.FromBitmap(srcBitmapExp);
	using var data = image.Encode(SKEncodedImageFormat.Png, 100);
	using var stream = File.Create(destName + "-Head.png");
	data.SaveTo(stream);


	RemoveSimilarities(destBitmap, ogBitmapExp);

	using var destCanvas = new SKCanvas(destBitmap);
	using var paint = new SKPaint { BlendMode = SKBlendMode.SrcOver };
	destCanvas.DrawImage(SKImage.FromBitmap(srcBitmap), xPos, yPos, SKSamplingOptions.Default, paint);
	destCanvas.Flush();

	// Save the custom head that's pasted onto the body
	using var destImage = SKImage.FromBitmap(destBitmap);
	using var destData = destImage.Encode(SKEncodedImageFormat.Png, 100);
	using var destStream = File.Create(destName + "-Custom.png");
	destData.SaveTo(destStream);
}

// Erases pixels from dest where og has any visible pixels
static void RemoveSimilarities(SKBitmap dest, SKBitmap og)
{
	if (dest.Width != og.Width || dest.Height != og.Height)
		throw new ArgumentException("Bitmaps must have the same dimensions.");

	for (int y = 0; y < dest.Height; y++)
		for (int x = 0; x < dest.Width; x++)
			if (og.GetPixel(x, y).Alpha > 0)
				dest.SetPixel(x, y, SKColors.Transparent);
	dest.NotifyPixelsChanged();
}

// Test
UpdateDestination("001-05-879");

record SequenceInfo(int HeadNum, int Angle, int XPos, int YPos, int XOffset, int YOffset);
