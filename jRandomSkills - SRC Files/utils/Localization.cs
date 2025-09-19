using CounterStrikeSharp.API.Modules.Utils;
using Newtonsoft.Json;

namespace jRandomSkills.src.utils
{
    public static class Localization
    {
        private static readonly string path = Path.Combine(jRandomSkills.Instance.ModuleDirectory, "lang");
        private static readonly Dictionary<string, Dictionary<string, string>> _translations = [];
        private static string langCode = "pl";

        public static void Load()
        {
            _translations.Clear();
            SetLangCode();
            LoadAllLanguages();
        }

        private static void SetLangCode()
        {
            langCode = Config.LoadedConfig.Settings.LangCode;
        }

        private static void LoadAllLanguages()
        {
            if (!Directory.Exists(path))
                return;

            foreach (var file in Directory.GetFiles(path, "*.json"))
                LoadLanguage(file);
        }

        private static void LoadLanguage(string langPath)
        {
            if (!File.Exists(langPath))
                return;

            var code = Path.GetFileNameWithoutExtension(langPath);
            var jsonText = File.ReadAllText(langPath);
            var translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonText);

            if (translations != null)
                _translations[code] = translations;
        }

        public static string GetTranslation(string key, params object[] args)
        {
            if (_translations.TryGetValue(langCode, out var langDict) && langDict.TryGetValue(key, out var translation))
                if (args.Length != 0 && args[0].ToString() == "welcome")
                    return translation;
                else
                {
                    string output = string.Format(translation, args).Replace("CHATCOLORS.RED", ChatColors.Red.ToString());
                    return output;
                }

            return key;
        }
    }
}
