using System.ComponentModel;

namespace FluentTerminal.Models.Enums
{
    public enum Command
    {
        ToggleWindow,
        NextTab,
        PreviousTab,
        NewTab,
        ConfigurableNewTab,

        [Description("New SSH tab")]
        NewSshTab,

        [Description("Change tab title")]
        ChangeTabTitle,

        CloseTab,
        NewWindow,
        ConfigurableNewWindow,
        ShowSettings,
        Copy,
        Paste,
        PasteWithoutNewlines,
        Search,
        ToggleFullScreen,
        SelectAll,
        Clear,
        SwitchToTerm1,
        SwitchToTerm2,
        SwitchToTerm3,
        SwitchToTerm4,
        SwitchToTerm5,
        SwitchToTerm6,
        SwitchToTerm7,
        SwitchToTerm8,
        SwitchToTerm9
    }
}