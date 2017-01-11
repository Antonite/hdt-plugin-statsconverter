﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight.Command;
using HDT.Plugins.Common.Controls.SlidePanels;
using HDT.Plugins.Common.Plugin;
using HDT.Plugins.Common.Providers;
using HDT.Plugins.Common.Services;
using HDT.Plugins.Common.Settings;
using HDT.Plugins.Common.Util;
using HDT.Plugins.StatsConverter.ViewModels;
using HDT.Plugins.StatsConverter.Views;

namespace HDT.Plugins.StatsConverter
{
	[Name("Stats Converter")]
	[Description("Import and export game statistics in different formats")]
	public class StatsConverter : PluginBase
	{
		public static readonly IUpdateService Updater;
		public static readonly ILoggingService Logger;
		public static readonly IDataRepository Data;
		public static readonly Settings Settings;

		private static readonly MainViewModel _mainViewModel;

		private MenuItem _statsMenuItem;

		public override MenuItem MenuItem
		{
			get { return _statsMenuItem; }
		}

		static StatsConverter()
		{
			// initialize services
			var resolver = Injector.Instance.Container;
			Updater = resolver.GetInstance<IUpdateService>();
			Logger = resolver.GetInstance<ILoggingService>();
			Data = resolver.GetInstance<IDataRepository>();
			// load settings
			var assembly = Assembly.GetExecutingAssembly();
			var resourceName = "HDT.Plugins.StatsConverter.Resources.Default.ini";
			Settings = new Settings(assembly.GetManifestResourceStream(resourceName), "StatsConverter");
			// persistent main view model
			_mainViewModel = new MainViewModel();
		}

		public StatsConverter()
			: base(Assembly.GetExecutingAssembly())
		{
		}

		public override async void OnLoad()
		{
			CreatePluginMenu();
			await UpdateCheck("StatsConverter", "hdt-plugin-statsconverter");
		}

		public override void OnUnload()
		{
			SlidePanelManager.DetachAll();
			CloseMainView();
		}

		public override string ButtonText
		{
			get { return "Open"; }
		}

		public override void OnButtonPress()
		{
			ShowMainView();
		}

		private void ShowMainView()
		{
			MainView view = null;
			// check for any open windows
			var open = Application.Current.Windows.OfType<MainView>();
			if (open.Count() == 1)
			{
				view = open.FirstOrDefault();
			}
			else
			{
				CloseMainView();
				// create view
				view = new MainView();
				view.DataContext = _mainViewModel;
			}
			view.Show();
			if (view.WindowState == WindowState.Minimized)
				view.WindowState = WindowState.Normal;
			view.Activate();
		}

		private void CloseMainView()
		{
			foreach (var view in Application.Current.Windows.OfType<MainView>())
				view.Close();
		}

		private void CreatePluginMenu()
		{
			PluginMenu pm = new PluginMenu("Stats Converter", IcoMoon.PieChart,
				new RelayCommand(() => ShowMainView()));
			_statsMenuItem = pm.Menu;
		}

		private async Task UpdateCheck(string name, string repo)
		{
			var uri = new Uri($"https://api.github.com/repos/andburn/{repo}/releases");
			Logger.Debug("update uri = " + uri);
			try
			{
				var latest = await Updater.CheckForUpdate(uri, Version);
				if (latest.HasUpdate)
				{
					Logger.Info($"Plugin Update available ({latest.Version})");
					SlidePanelManager
						.Notification("Plugin Update Available",
							$"[DOWNLOAD]({latest.DownloadUrl}) {name} v{latest.Version}",
							IcoMoon.Download3,
							() => Process.Start(latest.DownloadUrl))
						.AutoClose(10);
				}
			}
			catch (Exception e)
			{
				Logger.Error(e);
			}
		}
	}
}