//
//  Launchpad.Launcher.Interface.MainWindow.cs
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

using Gdk;
using Gtk;
using Stetic;
using Image = Gtk.Image;

namespace Launchpad.Launcher.Interface
{
	public sealed partial class MainWindow
	{
		private UIManager UIManager;

		private MenuBar MainMenuBar;
		private ActionGroup MainActionGroup;

		private Action MenuAction;

		private Action RepairGameAction;
		private Action ReinstallGameAction;

		private VBox MainVerticalBox;
		private HBox MainHorizontalBox;

		private VBox BrowserVerticalBox;

		private Alignment BrowserAlignment;
		private ScrolledWindow BrowserWindow;

		private Alignment BannerAlignment;
		private Image Banner;

		private Alignment IndicatorLabelAlignment;
		private Label IndicatorLabel;

		private HBox BottomHorizontalBox;

		private Alignment MainProgressBarAlignment;
		private ProgressBar MainProgressBar;

		private Alignment MainButtonAlignment;
		private Button MainButton;

		private void Build()
		{
			Gui.Initialize(this);

			CreateWidgets();
			BuildHierarchy();
			InitializeWidgets();
			BindEvents();

			this.Child?.ShowAll();
			Show();
		}

		private void CreateWidgets()
		{
			this.UIManager = new UIManager();

			this.MainActionGroup = new ActionGroup("Default");

			this.MenuAction = new Action("MenuAction", LocalizationCatalog.GetString("Menu"), null, null)
			{
				ShortLabel = LocalizationCatalog.GetString("Menu")
			};

			this.RepairGameAction = new Action
			(
				"RepairGameAction",
				LocalizationCatalog.GetString("Repair Game"),
				LocalizationCatalog.GetString("Starts a repair process for the installed game."),
				"gtk-refresh"
			)
			{
				ShortLabel = LocalizationCatalog.GetString("Repair Game")
			};

			this.ReinstallGameAction = new Action
			(
				"ReinstallGameAction",
				LocalizationCatalog.GetString("Reinstall Game"),
				LocalizationCatalog.GetString("Reinstalls the installed game."),
				"gtk-refresh"
			)
			{
				ShortLabel = LocalizationCatalog.GetString("Reinstall Game")
			};

			this.MainVerticalBox = new VBox
			{
				Name = "MainVerticalBox",
				Spacing = 6
			};

			this.UIManager.AddUiFromString
			(
				"<ui>" +
				"<menubar name='MainMenuBar'>" +
				"<menu name='MenuAction' action='MenuAction'>" +
				"<menuitem name='RepairGameAction' action='RepairGameAction'/>" +
				"<separator/>" +
				"<menuitem name='ReinstallGameAction' action='ReinstallGameAction'/>" +
				"</menu>" +
				"</menubar>" +
				"</ui>"
			);
			this.MainMenuBar = (MenuBar)this.UIManager.GetWidget("/MainMenuBar");

			this.MainHorizontalBox = new HBox
			{
				Name = "MainHorizontalBox",
				Spacing = 6,
				BorderWidth = 4
			};

			this.BrowserVerticalBox = new VBox
			{
				Name = "BrowserVerticalBox",
				Spacing = 6
			};

			this.BrowserAlignment = new Alignment(0.5F, 0.5F, 1F, 1F)
			{
				WidthRequest = 310,
				Name = "BrowserAlignment"
			};

			this.BrowserWindow = new ScrolledWindow
			{
				CanFocus = true,
				Name = "BrowserWindow",
				ShadowType = ShadowType.In
			};

			this.BannerAlignment = new Alignment(0.5F, 0.5F, 1F, 1F)
			{
				Name = "BannerAlignment"
			};

			this.Banner = new Image
			{
				WidthRequest = 450,
				HeightRequest = 300,
				Name = "Banner",
			};

			this.MainButtonAlignment = new Alignment(0.5F, 0.5F, 1F, 1F)
			{
				WidthRequest = 100,
				Name = "MainButtonAlignment"
			};

			this.MainButton = new Button
			{
				Sensitive = false,
				CanDefault = true,
				CanFocus = true,
				Name = "MainButton",
				UseUnderline = true,
				Label = LocalizationCatalog.GetString("Inactive")
			};

			this.IndicatorLabelAlignment = new Alignment(0.5F, 0.5F, 1F, 1F)
			{
				Name = "IndicatorLabelAlignment",
				LeftPadding = 6
			};

			this.IndicatorLabel = new Label
			{
				Name = "IndicatorLabel",
				Xalign = 0F,
				LabelProp = LocalizationCatalog.GetString("Idle")
			};

			this.BottomHorizontalBox = new HBox
			{
				Name = "BottomHorizontalBox",
				Spacing = 6,
				BorderWidth = 4
			};

			this.MainProgressBarAlignment = new Alignment(0.5F, 0.5F, 1F, 1F)
			{
				Name = "MainProgressBarAlignment"
			};

			this.MainProgressBar = new ProgressBar
			{
				Name = "MainProgressBar"
			};
		}

		private void BuildHierarchy()
		{
			this.MainActionGroup.Add(this.MenuAction, null);
			this.MainActionGroup.Add(this.RepairGameAction, null);
			this.MainActionGroup.Add(this.ReinstallGameAction, null);

			this.UIManager.InsertActionGroup(this.MainActionGroup, 0);
			AddAccelGroup(this.UIManager.AccelGroup);

			this.MainVerticalBox.PackStart(this.MainMenuBar);
			this.MainVerticalBox.PackStart(this.MainHorizontalBox);

			this.MainHorizontalBox.PackStart(this.BrowserVerticalBox);
			this.BrowserVerticalBox.PackStart(this.BrowserAlignment);
			this.BrowserAlignment.Add(this.BrowserWindow);

			this.MainHorizontalBox.PackEnd(this.BannerAlignment);
			this.BannerAlignment.Add(this.Banner);

			this.MainVerticalBox.PackStart(this.IndicatorLabelAlignment);
			this.IndicatorLabelAlignment.Add(this.IndicatorLabel);

			this.MainVerticalBox.PackEnd(this.BottomHorizontalBox);

			this.BottomHorizontalBox.PackStart(this.MainProgressBarAlignment);
			this.MainProgressBarAlignment.Add(this.MainProgressBar);

			this.BottomHorizontalBox.PackEnd(this.MainButtonAlignment);
			this.MainButtonAlignment.Add(this.MainButton);

			Add(this.MainVerticalBox);
		}

		private void InitializeWidgets()
		{
			this.Name = "Launchpad.Launcher.Interface.MainWindow";
			this.Title = LocalizationCatalog.GetString("Launchpad - {0}");

			this.Icon = Pixbuf.LoadFromResource("Launchpad.Launcher.Resources.Icon.ico");

			this.WindowPosition = (WindowPosition)4;
			this.DefaultWidth = 745;
			this.DefaultHeight = 415;
			this.Resizable = false;

			var w12 = (Box.BoxChild)this.BottomHorizontalBox[this.MainProgressBarAlignment];
			w12.Expand = true;

			var w17 = (Box.BoxChild)this.BottomHorizontalBox[this.MainButtonAlignment];
			w17.Expand = false;

			this.MainButton.HasDefault = true;
		}

		private void BindEvents()
		{
			this.DeleteEvent += OnDeleteEvent;
			this.RepairGameAction.Activated += OnRepairGameActionActivated;
			this.ReinstallGameAction.Activated += OnReinstallGameActionActivated;
			this.MainButton.Clicked += OnMainButtonClicked;
		}
	}
}
