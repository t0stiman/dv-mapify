#if UNITY_EDITOR
using System.Collections.Generic;
using Mapify.Editor.Utils;
using UnityEngine;

namespace Mapify.Editor
{
    public static class MapRenderer
    {
        private const int TEXTURE_SIZE = 2048;

        public static void RenderMapFromTerrain(Terrain[] terrains, MapInfo mapInfo)
        {
            Texture2D combinedHeightmap = CreateHeightmap(terrains, mapInfo);
            Texture2D scaledHeightmap = Resize(combinedHeightmap, TEXTURE_SIZE, TEXTURE_SIZE);
            mapInfo.mapTextureSerialized = scaledHeightmap.EncodeToJPG();
            mapInfo.mapTextureSize = new[] { scaledHeightmap.width, scaledHeightmap.height };
        }

        private static Texture2D CreateHeightmap(IReadOnlyList<Terrain> terrains, MapInfo mapInfo)
        {
            int terrainCount = terrains.Count;

            int heightmapResolution = terrains[0].terrainData.heightmapResolution;
            int terrainWidth = heightmapResolution - 1;
            int totalWidth = terrainWidth * Mathf.CeilToInt(Mathf.Sqrt(terrainCount));

            // Texture2D combinedTexture = new Texture2D(totalWidth, totalWidth, TextureFormat.RGBA32, false); // OLD

            // The above line results in a texture large enough to crash the exporter on a large map,
            // at least without turning down terrain tile heightmap resolution.
            // Let's try something different. - Cvetka

            int combinationSize = 16384; // per-side size of combinedTexture, now a fixed value
            
            // Find how small the tiles need to be to fit into a combinationSize width texture.
            int maxTileSize = Mathf.CeilToInt(combinationSize / Mathf.CeilToInt(Mathf.Sqrt(terrainCount)));
            //Debug.Log($"maxTileSize: {maxTileSize} ({combinationSize} / terrain width in tiles, rounded down???)");

            // Find the largest power of 2 smaller than (or equal to) that for actual tile texture size target
            int maxPermissibleRes = Mathf.FloorToInt(Mathf.Pow(2, Mathf.Floor(Mathf.Log(maxTileSize, 2))));
            //Debug.Log($"maxPermissibleRes: {maxPermissibleRes} (size that tile heightmaps will attempt to be resized to)");

            // how wide will combinedTexture actually be?
            int scaledWidth = maxPermissibleRes * Mathf.CeilToInt(Mathf.Sqrt(terrainCount));
            //Debug.Log($"scaledWidth: {scaledWidth} (px size of combinedTexture)");

            // and NOW declare this
            Texture2D combinedTexture = new Texture2D(scaledWidth, scaledWidth, TextureFormat.RGBA32, false);
            //Debug.Log($"Verify that combinedTexure got iniialized to the correct size: {combinedTexture.height}");

            // these are unchanged, but now measure in maxPermissibleRes instead of terrainWidth
            int currentX = 0;
            int currentY = 0;

            // colors[] needs to still be the size (px count) of the tile heightmap so this is fine
            Color[] colors = new Color[terrainWidth * terrainWidth]; // do not change


            for (int i = 0; i < terrainCount; i++)
            {
                // I need a new texture to store the tile heightmap data into, per tile
                Texture2D tempTexture = new Texture2D(terrainWidth, terrainWidth, TextureFormat.RGBA32, false);

                // this block checks the heightmap of the tile pixel by pixel, figures out the correct map color at that
                // spot, and saves that color into the array colors[] - The Worst Bitmap Format(TM).
                // Anyway, sure, none of this changes.
                Terrain terrain = terrains[i];
                float terrainY = terrain.transform.position.y;
                TerrainData terrainData = terrain.terrainData;
                float terrainHeight = terrainData.size.y;
                float[,] heightmapData = terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);

                for (int y = 0; y < terrainWidth; y++)
                    for (int x = 0; x < terrainWidth; x++)
                    {
                        float height = heightmapData[y, x];
                        float worldHeight = terrainY + height * terrainHeight;
                        float waterLevel = worldHeight / mapInfo.waterLevel;
                        float terrainLevel = worldHeight / (terrainY + terrainHeight);
                        Color color = worldHeight <= mapInfo.waterLevel
                            ? mapInfo.waterColor.Evaluate(float.IsNaN(waterLevel) ? 0.0f : waterLevel)
                            : mapInfo.terrainColor.Evaluate(float.IsNaN(terrainLevel) ? 0.0f : terrainLevel);
                        colors[y * terrainWidth + x] = color;
                    }

                // OLD line:
                // combinedTexture.SetPixels(currentX, currentY, terrainWidth, terrainWidth, colors);

                // lay the heightmap colors into the tempTexture
                // and don't forget to Apply() it, because Graphics.Blit runs on the GPU, not the CPU
                tempTexture.SetPixels(0, 0, terrainWidth, terrainWidth, colors);
                tempTexture.Apply();

                // Now use a RenderTexture to scale the image down to match maxPermissibleRes
                RenderTexture rtt = RenderTexture.GetTemporary(maxPermissibleRes, maxPermissibleRes, 0);
                Graphics.Blit(tempTexture, rtt, new Material(Shader.Find("Hidden/BlitCopy")));
                //Debug.Log($"RenderTexture rtt: {rtt.width}");
                Texture2D tempScaledTexture = new Texture2D(maxPermissibleRes, maxPermissibleRes, TextureFormat.RGBA32, false);
                tempScaledTexture.ReadPixels(new Rect(0, 0, maxPermissibleRes, maxPermissibleRes), 0, 0);
                RenderTexture.ReleaseTemporary(rtt);


                // now copy THAT image into where it goes within combinedTexture.
                Graphics.CopyTexture(tempScaledTexture, 0, 0, 0, 0, maxPermissibleRes, maxPermissibleRes, combinedTexture, 0, 0, currentX, currentY);

                //Debug.Log($"Tile iteration: i = {i}, currentX = {currentX}, currentY = {currentY}," +
                //    $"tempTexture.width = {tempTexture.width}, first pixel {tempTexture.GetPixel(0,0)}," +
                //    $"tempScaledTexture.width = {tempScaledTexture.width}, first pixel {tempScaledTexture.GetPixel(0,0)}");

                // The rest of this block steps to the next terrain tile; basically unchanged, but
                // now runs on maxPermissibleRes instead of terrainWidth, and scaledWidth instead of totalWidth.
                currentX += maxPermissibleRes;
                if (currentX + maxPermissibleRes <= scaledWidth)
                    continue;
                currentX = 0;
                currentY += maxPermissibleRes;
            }

            //Debug.Log($"end of CreateHeightmap; combinedTexture first pixel: {combinedTexture.GetPixel(0,0)}");

            // end of Cvetka's edits

            combinedTexture.Apply();

            return combinedTexture;
        }

        private static Texture2D Resize(Texture2D source, int width, int height)
        {
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0);
            Graphics.Blit(source, rt, new Material(Shader.Find("Hidden/BlitCopy")));

            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply();

            RenderTexture.ReleaseTemporary(rt);
            return result;
        }

        public static void RenderMapFromImage(MapInfo mapInfo)
        {
            Texture2D scaledMapImage = Resize(mapInfo.fixedMapImage, TEXTURE_SIZE, TEXTURE_SIZE);
            mapInfo.mapTextureSerialized = scaledMapImage.EncodeToJPG();
            mapInfo.mapTextureSize = new[] { scaledMapImage.width, scaledMapImage.height };
        }
    }
}
#endif
