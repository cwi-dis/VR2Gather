using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ftTextureProcessor : AssetPostprocessor
{
    public static Dictionary<string, Vector2> texSettings = new Dictionary<string, Vector2>();

    public const int TEX_LM = 0;
    public const int TEX_LMDEFAULT = 1;
    public const int TEX_MASK = 2;
    public const int TEX_DIR = 3;

    void OnPreprocessTexture()
    {
        TextureImporter importer = assetImporter as TextureImporter;
        Vector2 settings;

        if (!texSettings.TryGetValue(importer.assetPath, out settings)) return;

        importer.maxTextureSize = (int)settings.x;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;

        int texType = (int)settings.y;
        switch(texType)
        {
            case TEX_LM:
            {
                importer.textureType = TextureImporterType.Lightmap;
                break;
            }
            case TEX_LMDEFAULT:
            {
                importer.textureType = TextureImporterType.Default;
                break;
            }
            case TEX_MASK:
            {
                importer.textureType = TextureImporterType.Default;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                break;
            }
            case TEX_DIR:
            {
                importer.textureType = TextureImporterType.Default;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.sRGBTexture = false;
                break;
            }
        }
    }
}

