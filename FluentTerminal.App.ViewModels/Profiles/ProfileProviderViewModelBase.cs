﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using FluentTerminal.App.Services;
using FluentTerminal.Models;
using FluentTerminal.Models.Enums;
using FluentTerminal.Models.Messages;
using GalaSoft.MvvmLight;

namespace FluentTerminal.App.ViewModels.Profiles
{
    /// <summary>
    /// Base class for all profile view model classes. It contains properties shared by all other view models
    /// (theme-related properties, line-ending translation, and WinPTY/ConPTY selection).
    /// </summary>
    public abstract class ProfileProviderViewModelBase : ViewModelBase
    {
        #region Fields

        protected readonly ISettingsService SettingsService;
        protected readonly IApplicationView ApplicationView;
        private readonly bool _strictProfileType;

        #endregion Fields

        #region Properties

        private ShellProfile _model;

        public ShellProfile Model
        {
            get => _model;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (ReferenceEquals(_model, value))
                {
                    return;
                }

                if (_strictProfileType && _model != null && _model.GetType() != value.GetType())
                {
                    throw new ArgumentException($"The value has to be of type {_model.GetType().Name}.");
                }

                _model = value;

                LoadFromProfile(value);
            }
        }

        public ObservableCollection<TabTheme> TabThemes { get; }

        public ObservableCollection<TerminalTheme> TerminalThemes { get; }

        public ObservableCollection<LineEndingStyle> LineEndingStyles { get; } =
            new ObservableCollection<LineEndingStyle>(Enum.GetValues(typeof(LineEndingStyle)).Cast<LineEndingStyle>());

        private LineEndingStyle _lineEndingTranslation;

        public LineEndingStyle LineEndingTranslation
        {
            get => _lineEndingTranslation;
            set => Set(ref _lineEndingTranslation, value);
        }

        private TabTheme _selectedTabTheme;

        public TabTheme SelectedTabTheme
        {
            get => _selectedTabTheme ?? (_selectedTabTheme = TabThemes.FirstOrDefault(t => t.Id.Equals(_tabThemeId)));
            set
            {
                if (value == null)
                {
                    // Ignore attempt setting null.
                    return;
                }

                // Ensure that it's from the list
                var theme = TabThemes.Contains(value)
                    ? value
                    : TabThemes.FirstOrDefault(t => t.Id == value.Id) ?? TabThemes.First();

                if (Set(ref _selectedTabTheme, theme))
                {
                    _tabThemeId = theme.Id;
                }
            }
        }

        private int _tabThemeId;

        public int TabThemeId
        {
            get => _tabThemeId;
            set
            {
                if (Set(ref _tabThemeId, value))
                {
                    SelectedTabTheme = TabThemes.FirstOrDefault(t => t.Id.Equals(value));
                }
            }
        }

        private TerminalTheme _selectedTerminalTheme;

        public TerminalTheme SelectedTerminalTheme
        {
            get => _selectedTerminalTheme ??
                   (_selectedTerminalTheme = TerminalThemes.FirstOrDefault(t => t.Id.Equals(_terminalThemeId)));
            set
            {
                TerminalTheme theme = value;

                if (theme == null)
                {
                    // How to handle attempt to setting null theme?
                    // 1) Throw an exception:
                    //throw new ArgumentNullException();
                    // 2) Ignore the attempt:
                    //return;
                    // 3) Use default theme (probably the best):
                    theme = TerminalThemes.First();
                }
                else if (!TerminalThemes.Contains(theme))
                {
                    // Ensure that it's from the list
                    theme = TerminalThemes.FirstOrDefault(t => t.Id.Equals(theme.Id)) ?? TerminalThemes.First();
                }

                if (Set(ref _selectedTerminalTheme, theme))
                {
                    _terminalThemeId = theme.Id;
                }
            }
        }

        private Guid _terminalThemeId;

        public Guid TerminalThemeId
        {
            get => _terminalThemeId;
            set
            {
                if (Set(ref _terminalThemeId, value))
                {
                    SelectedTerminalTheme = TerminalThemes.FirstOrDefault(t => t.Id.Equals(value));
                }
            }
        }

        public ObservableCollection<SessionType> SessionTypes { get; } =
            new ObservableCollection<SessionType>(Enum.GetValues(typeof(SessionType)).Cast<SessionType>());

        private SessionType _sessionType = SessionType.ConPty;

        public SessionType SessionType
        {
            get => _sessionType;
            set => Set(ref _sessionType, value);
        }

        #endregion Properties

        #region Constructor

        protected ProfileProviderViewModelBase(ISettingsService settingsService, IApplicationView applicationView,
            bool strictProfileType, ShellProfile original = null, bool isSshProfile = false)
        {
            SettingsService = settingsService;
            ApplicationView = applicationView;
            _strictProfileType = strictProfileType;

            if (original == null)
            {
                _model = isSshProfile ? new SshProfile() : new ShellProfile();
                _model.SessionType = ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7)
                    ? SessionType.ConPty
                    : SessionType.WinPty;
            }
            else
            {
                _model = original;
            }

            TabThemes = new ObservableCollection<TabTheme>(settingsService.GetTabThemes());

            SelectedTabTheme = TabThemes.First();

            TerminalThemes = new ObservableCollection<TerminalTheme>(settingsService.GetThemes());

            TerminalThemes.Insert(0, new TerminalTheme {Id = Guid.Empty, Name = "Default"});

            SelectedTerminalTheme = TerminalThemes.First();

            MessengerInstance.Register<ThemeAddedMessage>(this, OnThemeAdded);
            MessengerInstance.Register<ThemeDeletedMessage>(this, OnThemeDeleted);

            Initialize(Model);
        }

        #endregion Constructor

        #region Methods

        private void OnThemeDeleted(ThemeDeletedMessage message)
        {
            var theme = TerminalThemes.FirstOrDefault(t => t.Id.Equals(message.ThemeId));

            if (theme == null)
            {
                return;
            }

            ApplicationView.ExecuteOnUiThreadAsync(() =>
            {
                TerminalThemes.Remove(theme);

                if (_terminalThemeId.Equals(message.ThemeId))
                {
                    SelectedTerminalTheme = TerminalThemes.First();
                }
            }, CoreDispatcherPriority.Low, true);
        }

        private void OnThemeAdded(ThemeAddedMessage message)
        {
            ApplicationView.ExecuteOnUiThreadAsync(() => TerminalThemes.Add(message.Theme), CoreDispatcherPriority.Low);
        }

        private void Initialize(ShellProfile profile)
        {
            LineEndingTranslation = profile.LineEndingTranslation;
            SessionType = profile.SessionType;
            TerminalThemeId = profile.TerminalThemeId;
            TabThemeId = profile.TabThemeId;
        }

        protected virtual void LoadFromProfile(ShellProfile profile)
        {
            Initialize(profile);
        }

        protected virtual Task CopyToProfileAsync(ShellProfile profile)
        {
            profile.LineEndingTranslation = _lineEndingTranslation;
            profile.SessionType = _sessionType;
            profile.TerminalThemeId = _terminalThemeId;
            profile.TabThemeId = _tabThemeId;
            return Task.CompletedTask;
        }

        public virtual Task<string> ValidateAsync()
        {
            return Task.FromResult<string>(null);
        }

        /// <summary>
        /// Returns <c>true</c> if view model values aren't equal to the corresponding values of the
        /// underlying <see cref="ShellProfile"/>.
        /// </summary>
        public virtual bool HasChanges()
        {
            return Model.LineEndingTranslation != _lineEndingTranslation ||
                   Model.SessionType != _sessionType ||
                   !Model.TerminalThemeId.Equals(_terminalThemeId) ||
                   Model.TabThemeId != _tabThemeId;
        }

        /// <summary>
        /// Copies values from the view model to the underlying <see cref="ShellProfile"/>.
        /// </summary>
        /// <param name="acceptIfInvalid">Defines what will happen if the view model data is invalid (if
        /// <see cref="ValidateAsync"/> returns non-empty string). If this parameter is set to <c>true</c>, the
        /// data will be copied to the underlying <see cref="ShellProfile"/> anyway. If it's set to <c>false</c>,
        /// (default) the underlying <see cref="ShellProfile"/> won't be changed at all.</param>
        /// <returns><c>null</c> or empty string if the operation was successful (the data is valid), or an error
        /// message returned by <see cref="ValidateAsync"/> method.</returns>
        public async Task<string> AcceptChangesAsync(bool acceptIfInvalid = false)
        {
            var error = await ValidateAsync();

            if (acceptIfInvalid || string.IsNullOrEmpty(error))
            {
                await CopyToProfileAsync(Model);
            }

            return error;
        }

        public void RejectChanges()
        {
            ApplicationView.ExecuteOnUiThreadAsync(() => LoadFromProfile(Model));
        }

        #endregion Methods

        #region Links/shortcuts related

        private const string UseConPtyQueryStringName = "conpty";
        private const string LineEndingQueryStringName = "lineending";
        private const string TerminalThemeIdQueryStringName = "theme";
        private const string TabThemeIdQueryStringName = "tab";

        private const string ShortcutFileFormat = @"[{{000214A0-0000-0000-C000-000000000046}}]
Prop3=19,0
[InternetShortcut]
IDList=
URL={0}
";

        public static string GetShortcutFileContent(string url) => string.Format(ShortcutFileFormat, url);

        public static IEnumerable<Tuple<string, string>> ParseParams(string uriOpts, char separator) =>
            uriOpts.Split(separator).Select(ParseSshOptionFromUri).Where(p => p != null);

        private static Tuple<string, string> ParseSshOptionFromUri(string option)
        {
            string[] nv = option.Split('=');

            if (nv.Length != 2 || string.IsNullOrEmpty(nv[0]))
            {
                // For now simply ignore invalid options
                return null;
                //throw new FormatException($"Invalid SSH option '{option}'.");
            }

            return Tuple.Create(HttpUtility.UrlDecode(nv[0]), HttpUtility.UrlDecode(nv[1]));
        }

        /// <summary>
        /// Generates URL.
        /// </summary>
        /// <returns><see cref="Tuple"/> whose first item represents success. If the first item is <c>true</c>,
        /// the second contains the URL. If the first item is <c>false</c>, the second contains error message.</returns>
        public virtual Task<Tuple<bool, string>> GetUrlAsync() =>
            Task.FromResult(Tuple.Create(false, "Not supported."));

        public string GetBaseQueryString()
        {
            var queryString =
                $"{UseConPtyQueryStringName}={_sessionType}&{LineEndingQueryStringName}={_lineEndingTranslation}";

            if (_tabThemeId != TabThemes.First().Id)
            {
                queryString += $"&{TabThemeIdQueryStringName}={_tabThemeId:##########}";
            }

            if (!_terminalThemeId.Equals(TerminalThemes.First().Id))
            {
                queryString += $"&{TerminalThemeIdQueryStringName}={_terminalThemeId}";
            }

            return queryString;
        }

        public void LoadBaseFromQueryString(IList<Tuple<string, string>> queryStringParams)
        {
            if (queryStringParams == null)
            {
                return;
            }

            var keyValue = queryStringParams.FirstOrDefault(t =>
                UseConPtyQueryStringName.Equals(t.Item1, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(keyValue?.Item2) && Enum.TryParse(keyValue.Item2, true, out SessionType sessionType))
            {
                SessionType = sessionType;
            }

            keyValue = queryStringParams.FirstOrDefault(t =>
                LineEndingQueryStringName.Equals(t.Item1, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(keyValue?.Item2) && Enum.TryParse(keyValue.Item2, true, out LineEndingStyle lineEndingTranslation))
            {
                LineEndingTranslation = lineEndingTranslation;
            }

            keyValue = queryStringParams.FirstOrDefault(t =>
                TabThemeIdQueryStringName.Equals(t.Item1, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(keyValue?.Item2) && int.TryParse(keyValue.Item2, out int tabThemeId))
            {
                TabThemeId = tabThemeId;
            }

            keyValue = queryStringParams.FirstOrDefault(t =>
                TerminalThemeIdQueryStringName.Equals(t.Item1, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(keyValue?.Item2) && Guid.TryParse(keyValue.Item2, out Guid terminalThemeId))
            {
                TerminalThemeId = terminalThemeId;
            }
        }

        #endregion Links/shortcuts related
    }
}