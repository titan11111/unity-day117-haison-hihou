// 素材の取り込み設定：立ち絵・背景はスプライト、声はモノラル圧縮
using UnityEditor; using UnityEngine;
namespace Day117.EditorTools
{
    class D117Import : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/D117/Resources/")) return;
            var ti = (TextureImporter)assetImporter;
            if (assetPath.Contains("/Dungeon/"))
            {
                ti.textureType = TextureImporterType.Default; ti.wrapMode = TextureWrapMode.Repeat; ti.mipmapEnabled = true; ti.maxTextureSize = 512;
                ti.textureCompression = TextureImporterCompression.Compressed; Web(ti, TextureImporterFormat.ASTC_8x8, 512); return;
            }
            if (!(assetPath.Contains("/Chars/") || assetPath.Contains("/BG/"))) return;
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true; ti.mipmapEnabled = false; ti.maxTextureSize = assetPath.Contains("/BG/") ? 1024 : 512;
            ti.textureCompression = TextureImporterCompression.Compressed;
            // iPhoneのメモリ対策：WebGLだけASTCのブロックを大きく（立ち絵6x6・背景8x8）
            Web(ti, assetPath.Contains("/BG/") ? TextureImporterFormat.ASTC_8x8 : TextureImporterFormat.ASTC_6x6, ti.maxTextureSize);
        }
        static void Web(TextureImporter ti, TextureImporterFormat f, int max)
        {
            var w = ti.GetPlatformTextureSettings("WebGL"); w.overridden = true; w.format = f; w.maxTextureSize = max; ti.SetPlatformTextureSettings(w);
        }
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/D117/Resources/Voice/")) return;
            var ai = (AudioImporter)assetImporter; ai.forceToMono = true;
            var s = ai.defaultSampleSettings; s.loadType = AudioClipLoadType.CompressedInMemory; s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = .2f; s.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate; s.sampleRateOverride = 16000; ai.defaultSampleSettings = s;
        }
    }
}
