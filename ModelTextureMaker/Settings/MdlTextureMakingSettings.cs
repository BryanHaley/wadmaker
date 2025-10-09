using Shared.FileSystem;
using Shared;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;
using System.Text.RegularExpressions;

namespace ModelTextureMaker.Settings
{
    /// <summary>
    /// A collection of texture settings rules, coming from a 'modeltexturemaker.config' file.
    /// <para>
    /// Rules are put on separate lines, starting with a filename (which can include wildcards: *) and followed by one or more texture settings.
    /// Empty lines and lines starting with // are ignored. When multiple rules match a filename, settings defined in more specific rules will take priority.
    /// </para>
    /// </summary>
    class MdlTextureMakingSettings
    {
        const string ConfigFilename = "modeltexturemaker.config";


        class Rule
        {
            public int Order { get; }
            public string NamePattern { get; }
            public MdlTextureSettings TextureSettings { get; }

            public Rule(int order, string namePattern, MdlTextureSettings textureSettings)
            {
                Order = order;
                NamePattern = namePattern;
                TextureSettings = textureSettings;
            }
        }


        private Dictionary<string, Rule[]> _exactRules = new();
        private List<(Regex, Rule[])> _wildcardRules = new();


        /// <summary>
        /// Returns information about the given file: the file hash, texture settings and the time when the file or its settings were last modified.
        /// Texture settings can come from multiple config file entries, with more specific name patterns (without wildcards) taking priority over less specific ones (with wildcards).
        /// </summary>
        public MdlTextureSourceFileInfo GetTextureSourceFileInfo(string path)
        {
            var fileHash = FileHash.FromFile(path, out var fileSize);

            // Later rules override settings defined by earlier rules:
            var textureSettings = new MdlTextureSettings();
            foreach (var rule in GetMatchingRules(Path.GetFileName(path)))
                textureSettings.OverrideWith(rule.TextureSettings);

            // Filename settings take priority over config file settings:
            var filenameSettings = GetTextureSettingsFromFilename(path);
            textureSettings.OverrideWith(filenameSettings);

            var lastWriteTime = new System.IO.FileInfo(path).LastWriteTimeUtc;
            return new MdlTextureSourceFileInfo(path, fileSize, fileHash, lastWriteTime, textureSettings);
        }

        /// <summary>
        /// Returns the texture name for the given file path.
        /// This is the first part of the filename, up to the first dot (.).
        /// </summary>
        public static string GetTextureName(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);

            var dotIndex = name.IndexOf('.');
            if (dotIndex >= 0)
                name = name.Substring(0, dotIndex);

            return name.ToLowerInvariant();
        }


        // Returns all rules that match the given filename, based on order of appearance:
        private IEnumerable<Rule> GetMatchingRules(string filename)
        {
            filename = filename.ToLowerInvariant();
            var textureName = GetTextureName(filename);

            var matchingRules = new List<Rule>();
            foreach ((var regex, var wildcardRules) in _wildcardRules)
            {
                if (regex.IsMatch(filename))
                    matchingRules.AddRange(wildcardRules);
            }

            if (_exactRules.TryGetValue(filename, out var ruleList) || _exactRules.TryGetValue(textureName, out ruleList))
                matchingRules.AddRange(ruleList);

            return matchingRules.OrderBy(rule => rule.Order);
        }


        private MdlTextureMakingSettings(IEnumerable<Rule> rules)
        {
            foreach (var group in rules.GroupBy(rule => rule.NamePattern))
            {
                if (group.Key.Contains('*'))
                    _wildcardRules.Add((MakeNamePatternRegex(group.Key), group.ToArray()));
                else
                    _exactRules[group.Key] = group.ToArray();
            }
        }

        private static Regex MakeNamePatternRegex(string namePattern)
        {
            var regex = Regex.Replace(namePattern, @"\\\*|\*|\\|[^\*\\]*", match =>
            {
                switch (match.Value)
                {
                    case @"*": return ".*";                     // A wildcard can be anything (including empty)
                    case @"\*": return Regex.Escape("*");       // A literal * must be escaped (\*)
                    default: return Regex.Escape(match.Value);  // There are no other special characters
                }
            });
            return new Regex(regex);
        }


        /// <summary>
        /// Reads texture settings from the modeltexturemaker.config file in the given folder, if it exists.
        /// </summary>
        public static MdlTextureMakingSettings Load(string folder)
        {
            // First read the global rules (modeltexturemaker.config in ModelTextureMaker.exe's directory):
            var globalConfigFilePath = Path.Combine(AppContext.BaseDirectory, ConfigFilename);
            var rules = new List<Rule>();
            if (File.Exists(globalConfigFilePath))
            {
                foreach (var line in File.ReadAllLines(globalConfigFilePath))
                    AddRule(ParseRuleLine(line, rules.Count));
            }

            // Then read the specified directory's current rules (modeltexturemaker.config):
            var configFilePath = Path.Combine(folder, ConfigFilename);
            if (File.Exists(configFilePath))
            {
                // NOTE: Local rules take precedence over global ones.
                foreach (var line in File.ReadAllLines(configFilePath))
                    AddRule(ParseRuleLine(line, rules.Count));
            }

            return new MdlTextureMakingSettings(rules);


            void AddRule(Rule? rule)
            {
                if (rule is not null)
                    rules.Add(rule);
            }
        }

        public static bool IsConfigurationFile(string path) => Path.GetFileName(path) == ConfigFilename;


        // Filename settings:
        public static MdlTextureSettings GetTextureSettingsFromFilename(string filename)
        {
            // NOTE: It's possible to have duplicate or conflicting settings in a filename, such as "test.color1 32.color2 32.png".
            //       We'll just let later segments override earlier segments.

            var settings = new MdlTextureSettings();
            foreach (var segment in Path.GetFileNameWithoutExtension(filename)
                .Split('.')
                .Skip(1)
                .Select(segment => segment.Trim().ToLowerInvariant()))
            {
                if (TryParseColorMask(segment, out var colorMask, out var colorCount))
                {
                    settings.ColorMask = colorMask;
                    settings.ColorCount = colorCount;
                }
                else if (segment == "portrait")
                {
                    settings.IsModelPortrait = true;
                }
            }
            return settings;
        }

        public static string InsertTextureSettingsIntoFilename(string filename, MdlTextureSettings settings)
        {
            var extension = Path.GetExtension(filename);
            var sb = new StringBuilder();

            if (settings.ColorMask != null)
            {
                if (ModelTextureName.IsDmBase(Path.GetFileName(filename)))
                {
                    if (settings.ColorMask != ColorMask.Main)
                        sb.Append($".color{(int)settings.ColorMask}");
                }
                else
                {
                    if (settings.ColorMask == ColorMask.Main)
                    {
                        sb.Append($".main {settings.ColorCount}");
                    }
                    else
                    {
                        sb.Append($".color{(int)settings.ColorMask}");
                        if (settings.ColorCount != null)
                            sb.Append($" {settings.ColorCount}");
                    }
                }
            }

            if (settings.IsModelPortrait == true && (settings.ColorMask ?? ColorMask.Main) == ColorMask.Main)
                sb.Append(".portrait");

            return Path.ChangeExtension(filename, sb.ToString() + extension);
        }


        private static bool TryParseColorMask(string str, out ColorMask colorMask, out int? colorCount)
        {
            var match = Regex.Match(str, @"^color(?<mask>[1-2])(?:\s+(?<count>\d{1,3}))?$");
            if (match.Success)
            {
                colorMask = (ColorMask)int.Parse(match.Groups["mask"].Value);
                colorCount = match.Groups["count"].Success ? int.Parse(match.Groups["count"].Value) : null;
                return true;
            }
            else
            {
                var mainMatch = Regex.Match(str, @"^main(?:\s+(?<count>\d{1,3}))?$");
                if (mainMatch.Success)
                {
                    colorMask = ColorMask.Main;
                    colorCount = mainMatch.Groups["count"].Success ? int.Parse(mainMatch.Groups["count"].Value) : null;
                    return true;
                }
                else
                {
                    colorMask = ColorMask.Main;
                    colorCount = null;
                    return false;
                }
            }
        }


        #region Parsing/serialization

        const string IgnoreKey = "ignore";
        const string ColorMaskKey = "color-mask";
        const string ColorCountKey = "color-count";
        const string IsModelPortraitKey = "is-model-portrait";
        const string PreservePaletteKey = "preserve-palette";
        const string DitheringAlgorithmKey = "dithering";
        const string DitherScaleKey = "dither-scale";
        const string TransparencyThresholdKey = "transparency-threshold";
        const string TransparencyColorKey = "transparency-color";
        const string ConverterKey = "converter";
        const string ConverterArgumentsKey = "arguments";


        private static Rule? ParseRuleLine(string line, int order)
        {
            var tokens = GetTokens(line).ToArray();
            if (tokens.Length == 0 || IsComment(tokens[0]))
                return null;

            var i = 0;
            var namePattern = Path.GetFileName(tokens[i++]).ToLowerInvariant();
            var textureSettings = new MdlTextureSettings();
            while (i < tokens.Length)
            {
                var token = tokens[i++];
                if (IsComment(token))
                    break;

                switch (token.ToLowerInvariant())
                {
                    case IgnoreKey:
                        RequireToken(":");
                        textureSettings.Ignore = ParseToken(bool.Parse);
                        break;

                    case ColorMaskKey:
                        RequireToken(":");
                        textureSettings.ColorMask = ParseToken(Serialization.ReadColorMask, "remap color mask");
                        break;

                    case ColorCountKey:
                        RequireToken(":");
                        textureSettings.ColorCount = ParseToken(int.Parse, "remap color count"); ;
                        break;

                    case IsModelPortraitKey:
                        RequireToken(":");
                        textureSettings.IsModelPortrait = ParseToken(bool.Parse);
                        break;

                    case PreservePaletteKey:
                        RequireToken(":");
                        textureSettings.PreservePalette = ParseToken(bool.Parse);
                        break;

                    case DitheringAlgorithmKey:
                        RequireToken(":");
                        textureSettings.DitheringAlgorithm = ParseToken(Serialization.ReadDitheringAlgorithm, "dithering algorithm");
                        break;

                    case DitherScaleKey:
                        RequireToken(":");
                        textureSettings.DitherScale = ParseToken(float.Parse, "dither scale");
                        break;

                    case TransparencyThresholdKey:
                        RequireToken(":");
                        textureSettings.TransparencyThreshold = ParseToken(byte.Parse, "transparency threshold");
                        break;

                    case TransparencyColorKey:
                        RequireToken(":");
                        textureSettings.TransparencyColor = new Rgba32(ParseToken(byte.Parse), ParseToken(byte.Parse), ParseToken(byte.Parse));
                        break;

                    case ConverterKey:
                        RequireToken(":");
                        textureSettings.Converter = ParseToken(s => s, "converter command string");
                        break;

                    case ConverterArgumentsKey:
                        RequireToken(":");
                        textureSettings.ConverterArguments = ParseToken(s => s, "converter arguments string");
                        ExternalConversion.ThrowIfArgumentsAreInvalid(textureSettings.ConverterArguments);
                        break;

                    default:
                        throw new InvalidDataException($"Unknown setting: '{token}'.");
                }
            }
            return new Rule(order, namePattern, textureSettings);


            void RequireToken(string value)
            {
                if (i >= tokens.Length) throw new InvalidDataException($"Expected a '{value}', but found end of line.");
                if (tokens[i++] != value) throw new InvalidDataException($"Expected a '{value}', but found '{tokens[i - 1]}'.");
            }

            T ParseToken<T>(Func<string, T> parse, string? label = null)
            {
                if (i >= tokens.Length)
                    throw new InvalidDataException($"Expected a {label ?? typeof(T).ToString()}, but found end of line.");

                try
                {
                    return parse(tokens[i++]);
                }
                catch (Exception)
                {
                    throw new InvalidDataException($"Expected a {label ?? typeof(T).ToString()}, but found '{tokens[i - 1]}'.");
                }
            }
        }

        private static IEnumerable<string> GetTokens(string line)
        {
            var start = 0;
            var isString = false;
            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (isString)
                {
                    if (c == '\'' && line[i - 1] != '\\')
                    {
                        yield return Token(i).Replace(@"\'", "'");
                        start = i + 1;
                        isString = false;
                    }
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (i > start) yield return Token(i);
                    start = i + 1;
                }
                else if (c == ':')
                {
                    if (i > start) yield return Token(i);
                    yield return ":";
                    start = i + 1;
                }
                else if (c == '/' && i > start && line[i - 1] == '/')
                {
                    if (i - 1 > start) yield return Token(i - 1);
                    start = i - 1;
                    yield return Token(line.Length);
                    yield break;
                }
                else if (c == '\'')
                {
                    if (i > start) yield return Token(i);
                    start = i + 1;
                    isString = true;
                }
            }

            if (isString) throw new InvalidDataException($"Expected a ' but found end of line.");
            if (start < line.Length) yield return Token(line.Length);

            string Token(int end) => line.Substring(start, end - start);
        }

        private static bool IsComment(string token) => token?.StartsWith("//") == true;

        #endregion
    }
}
