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
using Launchpad.Common;
using Stetic;
using Image = Gtk.Image;

namespace Launchpad.Launcher.Interface
{
	public sealed partial class MainWindow
	{
		private UIManager UIManager;

		private ActionGroup MainActionGroup;

		private Action MenuAction;

		private Action RepairGameAction;
		private Action ReinstallGameAction;

		private VBox MainVerticalBox;

		private MenuBar MainMenuBar;

		private HBox MainHorizontalBox;

		private VBox BrowserContainer;

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
			// Widget Launchpad.Launcher.Interface.MainWindow
			this.UIManager = new UIManager();

			this.MainActionGroup = new ActionGroup("Default");

			this.MenuAction = new Action("MenuAction", LocalizationCatalog.GetString("Menu"), null, null)
			{
				ShortLabel = LocalizationCatalog.GetString("Menu")
			};

			this.RepairGameAction = new Action
			(
				"repairGameAction",
				LocalizationCatalog.GetString("Repair Game"),
				LocalizationCatalog.GetString("Starts a repair process for the installed game."),
				"gtk-refresh"
			)
			{
				ShortLabel = LocalizationCatalog.GetString("Repair Game")
			};

			this.ReinstallGameAction = new Action
			(
				"reinstallGameAction",
				LocalizationCatalog.GetString("Reinstall Game"),
				LocalizationCatalog.GetString("Reinstalls the installed game."),
				"gtk-refresh"
			)
			{
				ShortLabel = LocalizationCatalog.GetString("Reinstall Game")
			};

			// Container child Launchpad.Launcher.Interface.MainWindow.Gtk.Container+ContainerChild
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
				"<menuitem name='repairGameAction' action='repairGameAction'/>" +
				"<separator/>" +
				"<menuitem name='reinstallGameAction' action='reinstallGameAction'/>" +
				"</menu>" +
				"</menubar>" +
				"</ui>"
			);
			this.MainMenuBar = (MenuBar)this.UIManager.GetWidget("/MainMenuBar");

			// Container child MainVerticalBox.Gtk.Box+BoxChild
			this.MainHorizontalBox = new HBox
			{
				Name = "MainHorizontalBox",
				Spacing = 6,
				BorderWidth = 4
			};

			// Container child MainHorizontalBox.Gtk.Box+BoxChild
			this.BrowserContainer = new VBox
			{
				Name = "browserContainer",
				Spacing = 6
			};

			// Container child browserContainer.Gtk.Box+BoxChild
			this.BrowserAlignment = new Alignment(0.5F, 0.5F, 1F, 1F)
			{
				WidthRequest = 310,
				Name = "alignment2"
			};

			// Container child alignment2.Gtk.Container+ContainerChild
			this.BrowserWindow = new ScrolledWindow
			{
				CanFocus = true,
				Name = "browserWindow",
				ShadowType = ShadowType.In
			};

			// Container child MainHorizontalBox.Gtk.Box+BoxChild
			this.BannerAlignment = new Alignment(0.5F, 0.5F, 1F, 1F)
			{
				Name = "alignment5"
			};

			// Container child alignment5.Gtk.Container+ContainerChild
			this.Banner = new Image
			{
				WidthRequest = 450,
				HeightRequest = 300,
				Name = "gameBanner",
			};

			// Container child HBox4.Gtk.Box+BoxChild
			this.MainButtonAlignment = new Alignment(0.5F, 0.5F, 1F, 1F)
			{
				WidthRequest = 100,
				Name = "alignment3"
			};

			// Container child alignment3.Gtk.Container+ContainerChild
			this.MainButton = new Button
			{
				Sensitive = false,
				CanDefault = true,
				CanFocus = true,
				Name = "primaryButton",
				UseUnderline = true,
				Label = LocalizationCatalog.GetString("Inactive")
			};

			// Container child MainVerticalBox.Gtk.Box+BoxChild
			this.IndicatorLabelAlignment = new Alignment(0.5F, 0.5F, 1F, 1F)
			{
				Name = "alignment1",
				LeftPadding = 6
			};

			// Container child alignment1.Gtk.Container+ContainerChild
			this.IndicatorLabel = new Label
			{
				Name = "IndicatorLabel",
				Xalign = 0F,
				LabelProp = LocalizationCatalog.GetString("Idle")
			};

			// Container child MainVerticalBox.Gtk.Box+BoxChild
			this.BottomHorizontalBox = new HBox
			{
				Name = "BottomHorizontalBox",
				Spacing = 6,
				BorderWidth = 4
			};

			// Container child BottomHorizontalBox.Gtk.Box+BoxChild
			this.MainProgressBarAlignment = new Alignment(0.5F, 0.5F, 1F, 1F)
			{
				Name = "alignment4"
			};

			// Container child alignment4.Gtk.Container+ContainerChild
			this.MainProgressBar = new ProgressBar
			{
				Name = "mainProgressBar"
			};
		}

		private void BuildHierarchy()
		{
			this.MainActionGroup.Add(this.MenuAction, null);
			this.MainActionGroup.Add(this.RepairGameAction, null);
			this.MainActionGroup.Add(this.ReinstallGameAction, null);

			this.UIManager.InsertActionGroup(this.MainActionGroup, 0);
			AddAccelGroup(this.UIManager.AccelGroup);

			this.MainVerticalBox.Add(this.MainMenuBar);

			this.BrowserAlignment.Add(this.BrowserWindow);
			this.BrowserContainer.Add(this.BrowserAlignment);

			this.MainHorizontalBox.Add(this.BrowserContainer);

			this.BannerAlignment.Add(this.Banner);
			this.MainHorizontalBox.Add(this.BannerAlignment);

			this.MainVerticalBox.Add(this.MainHorizontalBox);

			this.IndicatorLabelAlignment.Add(this.IndicatorLabel);
			this.MainVerticalBox.Add(this.IndicatorLabelAlignment);

			this.MainProgressBarAlignment.Add(this.MainProgressBar);
			this.BottomHorizontalBox.Add(this.MainProgressBarAlignment);

			this.MainButtonAlignment.Add(this.MainButton);
			this.BottomHorizontalBox.Add(this.MainButtonAlignment);

			this.MainVerticalBox.Add(this.BottomHorizontalBox);

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

			this.MainMenuBar.Name = "MainMenuBar";

			var w2 = (Box.BoxChild)this.MainVerticalBox[this.MainMenuBar];
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;

			var w4 = (Box.BoxChild)this.BrowserContainer[this.BrowserAlignment];
			w4.Position = 0;

			var w5 = (Box.BoxChild)this.MainHorizontalBox[this.BrowserContainer];
			w5.Position = 0;
			w5.Expand = false;

			var w7 = (Box.BoxChild)this.MainHorizontalBox[this.BannerAlignment];
			w7.Position = 1;

			var w8 = (Box.BoxChild)this.MainVerticalBox[this.MainHorizontalBox];
			w8.Position = 1;
			w8.Expand = false;
			w8.Fill = false;

			var w10 = (Box.BoxChild)this.MainVerticalBox[this.IndicatorLabelAlignment];
			w10.Position = 2;
			w10.Expand = false;
			w10.Fill = false;

			var w16 = (Box.BoxChild)this.MainVerticalBox[this.BottomHorizontalBox];
			w16.Position = 3;
			w16.Expand = true;
			w16.Fill = false;

			var w12 = (Box.BoxChild)this.BottomHorizontalBox[this.MainProgressBarAlignment];
			w12.Position = 0;
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
