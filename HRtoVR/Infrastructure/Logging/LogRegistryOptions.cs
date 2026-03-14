using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Platform;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Types;
using TextMateSharp.Themes;

namespace HRtoVR.Infrastructure.Logging;

public class LogRegistryOptions : TextMateSharp.Registry.IRegistryOptions {
    private readonly TextMateSharp.Registry.IRegistryOptions _defaultOptions;
    private readonly IRawGrammar _logGrammar;

    public LogRegistryOptions(ThemeName themeName) {
        _defaultOptions = new RegistryOptions(themeName);
        _logGrammar = LoadLogGrammar();
    }

    public IRawTheme GetTheme(string scopeName) => _defaultOptions.GetTheme(scopeName);

    public IRawGrammar GetGrammar(string scopeName) {
        if (scopeName == "source.log")
            return _logGrammar;

        return _defaultOptions.GetGrammar(scopeName);
    }

    public ICollection<string> GetInjections(string scopeName) => _defaultOptions.GetInjections(scopeName);

    public IRawTheme GetDefaultTheme() => _defaultOptions.GetDefaultTheme();

    private IRawGrammar LoadLogGrammar() {
        using var stream = AssetLoader.Open(new Uri("avares://HRtoVR/Assets/log.tmLanguage.json"));
        using var reader = new StreamReader(stream);
        return TextMateSharp.Internal.Grammars.Reader.GrammarReader.ReadGrammarSync(reader);
    }
}