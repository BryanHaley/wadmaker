using System.Text.RegularExpressions;

namespace Shared
{
    public static class ModelTextureName
    {
        private static Regex RemapTextureNameRegex = new Regex(@"^remap._(?<start1>\d{1,3})_(?<end1>\d{1,3})_(?<end2>\d{1,3})\.bmp$", RegexOptions.IgnoreCase);


        public static bool IsRemap(string textureName, out int color1Start, out int color1End, out int color2End)
        {
            if (string.Equals(textureName, "dm_base.bmp", StringComparison.InvariantCultureIgnoreCase))
            {
                color1Start = 160;
                color1End = 191;
                color2End = 223;
                return true;
            }

            var match = RemapTextureNameRegex.Match(textureName);
            if (match.Success)
            {
                color1Start = int.Parse(match.Groups["start1"].Value);
                color1End = int.Parse(match.Groups["end1"].Value);
                color2End = int.Parse(match.Groups["end2"].Value);
                return true;
            }
            else
            {
                color1Start = 0;
                color1End = 0;
                color2End = 0;
                return false;
            }
        }
    }
}
