﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using HDT.Plugins.StatsConverter.Export;
using HDT.Plugins.StatsConverter.Import;
using HDT.Plugins.StatsConverter.Models;
using HDT.Plugins.StatsConverter.Services;
using HDT.Plugins.StatsConverter.Utilities;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Stats;
using Hearthstone_Deck_Tracker.Utility.Logging;
using MahApps.Metro.Controls.Dialogs;

using APICore = Hearthstone_Deck_Tracker.API.Core;

namespace HDT.Plugins.StatsConverter
{
	public class StatsConverter
	{
		private static readonly IStatsRepository _repo = new HDTStatsRepository();

		public static List<GameStats> Filter(StatsFilter filter)
		{
			return filter.Apply(GetStats());
		}

		public static async Task Export(IStatsExporter export, string filepath, List<GameStats> stats)
		{
			var controller = await APICore.MainWindow.ShowProgressAsync("Exporting stats", "Please Wait...");
			try
			{
				if (stats.Count <= 0)
					throw new Exception("No stats found");
				export.To(filepath, stats);
			}
			catch (Exception e)
			{
				Log.Error(e);
			}
			finally
			{
				await controller.CloseAsync();
			}
		}

		public static void Import(IStatsImporter import, string filename)
		{
			var games = import.From(filename);
			// for current import options, do nothing with result
		}

		private static List<DeckStats> GetStats()
		{
			return _repo.GetAllStats();
		}

		public static void ArenaExtras(string filename, List<GameStats> stats, Guid? deck, List<Deck> decks)
		{
			List<ArenaExtra> arenaRuns = null;
			if (deck == null)
				arenaRuns = decks
					.Where(x => x.IsArenaDeck && stats.Any(s => s.DeckId == x.DeckId))
					.Select(x => new ArenaExtra(x, stats))
					.OrderByDescending(x => x.LastPlayed).ToList();
			else
				arenaRuns = decks
					.Where(x => x.DeckId == deck && x.IsArenaDeck)
					.Select(x => new ArenaExtra(x, stats))
					.OrderByDescending(x => x.LastPlayed).ToList();

			var fn = filename.Replace(".csv", "-extra.csv");
			using (var writer = new StreamWriter(fn))
			using (var csv = new CsvWriter(writer))
			{
				csv.Configuration.RegisterClassMap<ArenaExtraMap>();
				csv.WriteHeader<ArenaExtra>();
				csv.WriteRecords(arenaRuns);
			}
		}
	}
}