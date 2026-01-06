namespace GovUK.Dfe.CoreLibs.Http.NoScriptDetection.Pixel
{
    internal sealed class TransparentPixelProvider : INoScriptPixelProvider
    {
        private static readonly byte[] Pixel =
            Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO0pF1sAAAAASUVORK5CYII="
            );

        public byte[] GetPixel() => Pixel;
    }
}
