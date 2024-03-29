﻿//
//  MainWindow.UI.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Reflection;
using Gtk;
using Launchpad.Launcher.Utility;
using Microsoft.Extensions.DependencyInjection;
using UIElement = Gtk.Builder.ObjectAttribute;

// ReSharper disable UnassignedReadonlyField
#pragma warning disable 169
#pragma warning disable 649
#pragma warning disable 1591
#pragma warning disable SA1134 // Each attribute should be placed on its own line of code
#pragma warning disable SA1214 // Readonly fields should appear before non-readonly fields

namespace Launchpad.Launcher.Interface;

/// <summary>
/// Interface elements for the <see cref="MainWindow"/> widget.
/// </summary>
public partial class MainWindow
{
    [UIElement("MenuRepairItem")]
    private readonly ImageMenuItem _menuRepairItem = null!;

    [UIElement("MenuReinstallItem")]
    private readonly ImageMenuItem _menuReinstallItem = null!;

    [UIElement("MenuAboutItem")]
    private readonly ImageMenuItem _menuAboutItem = null!;

    [UIElement("ChangelogTextView")]
    private readonly TextView _changelogTextView = null!;

    [UIElement("BannerImage")]
    private readonly Image _bannerImage = null!;

    [UIElement("StatusLabel")]
    private readonly Label _statusLabel = null!;

    [UIElement("ProgressBar")]
    private readonly ProgressBar _mainProgressBar = null!;

    [UIElement("MainButton")]
    private readonly Button _mainButton = null!;

    /// <summary>
    /// Creates a new instance of the <see cref="MainWindow"/> class, loading its interface definition from file.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <returns>An instance of the main window widget.</returns>
    public static MainWindow Create(IServiceProvider services)
    {
        using var builder = new Builder
        (
            Assembly.GetExecutingAssembly(),
            "Launchpad.Launcher.Interface.Launchpad.glade",
            null
        );

        var window = ActivatorUtilities.CreateInstance<MainWindow>
        (
            services,
            builder,
            builder.GetObject("MainWindow").Handle
        );

        window.Icon = ResourceManager.ApplicationIcon;

        return window;
    }

    /// <summary>
    /// Binds UI-related events.
    /// </summary>
    private void BindUIEvents()
    {
        this.DeleteEvent += OnDeleteEvent;
        _menuReinstallItem.Activated += OnReinstallGameActionActivated;
        _menuRepairItem.Activated += OnMenuRepairItemActivated;
        _menuAboutItem.Activated += OnMenuAboutItemActivated;

        _mainButton.Clicked += OnMainButtonClicked;
    }

    /// <summary>
    /// Displays the about window to the user.
    /// </summary>
    /// <param name="sender">The sending object.</param>
    /// <param name="e">The event args.</param>
    private void OnMenuAboutItemActivated(object? sender, EventArgs e)
    {
        using var builder = new Builder(Assembly.GetExecutingAssembly(), "Launchpad.Launcher.Interface.Launchpad.glade", null);
        using var dialog = new AboutDialog(builder.GetObject("MainAboutDialog").Handle);
        dialog.Icon = ResourceManager.ApplicationIcon;
        dialog.Logo = ResourceManager.ApplicationIcon;
        dialog.Run();
    }

    /// <summary>
    /// Exits the application properly when the window is deleted.
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="a">The alpha component.</param>
    private static void OnDeleteEvent(object? sender, DeleteEventArgs a)
    {
        Application.Quit();
        a.RetVal = true;
    }
}
