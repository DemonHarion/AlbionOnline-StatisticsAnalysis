﻿using log4net;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Common.UserSettings;
using StatisticsAnalysisTool.Properties;
using StatisticsAnalysisTool.Trade.Mails;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace StatisticsAnalysisTool.Trade;

public class TradeController
{
    private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

    private readonly MainWindowViewModel _mainWindowViewModel;
    private int _tradeCounter;

    public TradeController(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;

        if (_mainWindowViewModel?.TradeMonitoringBindings?.Trades != null)
        {
            _mainWindowViewModel.TradeMonitoringBindings.Trades.CollectionChanged += OnCollectionChanged;
        }
    }

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        _mainWindowViewModel?.TradeMonitoringBindings?.TradeStatsObject.SetTradeStats(_mainWindowViewModel?.TradeMonitoringBindings?.Trades);
    }

    public async void AddTradeToBindingCollection(Trade trade)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _mainWindowViewModel?.TradeMonitoringBindings?.Trades.Add(trade);
            _mainWindowViewModel?.TradeMonitoringBindings?.TradeCollectionView?.Refresh();
        });
    }

    public async Task RemoveTradesByIdsAsync(IEnumerable<long> ids)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            foreach (var trade in _mainWindowViewModel?.TradeMonitoringBindings?.Trades?.ToList().Where(x => ids.Contains(x.Id)) ?? new List<Trade>())
            {
                _mainWindowViewModel?.TradeMonitoringBindings?.Trades?.Remove(trade);
            }
            _mainWindowViewModel?.TradeMonitoringBindings?.TradeStatsObject?.SetTradeStats(_mainWindowViewModel?.TradeMonitoringBindings?.TradeCollectionView?.Cast<Trade>().ToList());

            _mainWindowViewModel?.TradeMonitoringBindings?.UpdateTotalTradesUi(null, null);
            _mainWindowViewModel?.TradeMonitoringBindings?.UpdateCurrentTradesUi(null, null);
        });
    }

    public async Task RemoveTradesByDaysInSettingsAsync()
    {
        var deleteAfterDays = SettingsController.CurrentSettings?.DeleteTradesOlderThanSpecifiedDays ?? 0;
        if (deleteAfterDays <= 0)
        {
            return;
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            foreach (var mail in _mainWindowViewModel?.TradeMonitoringBindings?.Trades?.ToList()
                         .Where(x => x?.Timestamp.AddDays(deleteAfterDays) < DateTime.UtcNow)!)
            {
                _mainWindowViewModel?.TradeMonitoringBindings?.Trades?.Remove(mail);
            }
            _mainWindowViewModel?.TradeMonitoringBindings?.TradeStatsObject?.SetTradeStats(_mainWindowViewModel?.TradeMonitoringBindings?.TradeCollectionView?.Cast<Trade>().ToList());

            _mainWindowViewModel?.TradeMonitoringBindings?.UpdateTotalTradesUi(null, null);
            _mainWindowViewModel?.TradeMonitoringBindings?.UpdateCurrentTradesUi(null, null);
        });
    }

    #region Save / Load data

    public async Task LoadFromFileAsync()
    {
        FileController.TransferFileIfExistFromOldPathToUserDataDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.TradesFileName));

        var tradesFromOldMails = GetOldMails();

        var tradeDtos = await FileController.LoadAsync<List<TradeDto>>(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.UserDataDirectoryName, Settings.Default.TradesFileName));
        var trades = tradeDtos.Select(TradeMapping.Mapping).ToList();

        trades.AddRange(await tradesFromOldMails);

        await SetTradesToBindings(trades);
    }

    public async Task SaveInFileAsync()
    {
        DirectoryController.CreateDirectoryWhenNotExists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.UserDataDirectoryName));
        await FileController.SaveAsync(_mainWindowViewModel.TradeMonitoringBindings?.Trades?.Select(TradeMapping.Mapping),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.UserDataDirectoryName, Settings.Default.TradesFileName));
        DeleteMailsJson();
    }

    public async Task SaveInFileAfterExceedingLimit(int limit)
    {
        if (++_tradeCounter < limit)
        {
            return;
        }

        if (_mainWindowViewModel?.TradeMonitoringBindings?.Trades == null)
        {
            return;
        }

        var tradeMonitoringBindingsTrade = _mainWindowViewModel.TradeMonitoringBindings.Trades;
        var tradeDtos = tradeMonitoringBindingsTrade?.Select(TradeMapping.Mapping).ToList();

        if (tradeDtos == null)
        {
            return;
        }

        DirectoryController.CreateDirectoryWhenNotExists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.UserDataDirectoryName));
        await FileController.SaveAsync(tradeDtos,
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.UserDataDirectoryName, Settings.Default.TradesFileName));
        _tradeCounter = 0;
    }

    private async Task SetTradesToBindings(IEnumerable<Trade> trades)
    {
        await Dispatcher.CurrentDispatcher.InvokeAsync(() =>
        {
            var enumerable = trades as Trade[] ?? trades.ToArray();
            _mainWindowViewModel?.TradeMonitoringBindings?.Trades?.AddRange(enumerable.AsEnumerable());
            _mainWindowViewModel?.TradeMonitoringBindings?.TradeCollectionView?.Refresh();
            _mainWindowViewModel?.TradeMonitoringBindings?.TradeStatsObject?.SetTradeStats(enumerable);
        }, DispatcherPriority.Background, CancellationToken.None);
    }

    private static async Task<IEnumerable<Trade>> GetOldMails()
    {
        if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.MailsFileName)))
        {
            return new List<Trade>();
        }

        return ConvertOldMailsToTrade(await FileController.LoadAsync<List<MailOld>>($"{AppDomain.CurrentDomain.BaseDirectory}{Settings.Default.MailsFileName}"));
    }

    [Obsolete("Can be deleted after july 2023")]
    private static void DeleteMailsJson()
    {
        try
        {
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.MailsFileName)))
            {
                File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.MailsFileName));
            }
        }
        catch (Exception e)
        {
            ConsoleManager.WriteLineForError(MethodBase.GetCurrentMethod()?.DeclaringType, e);
            Log.Error(MethodBase.GetCurrentMethod()?.DeclaringType, e);
        }
    }

    [Obsolete("Can be deleted after july 2023")]
    private static IEnumerable<Trade> ConvertOldMailsToTrade(IEnumerable<MailOld> mails)
    {
        return mails.Select(mail => new Trade()
        {
            Id = mail.MailId,
            Type = TradeType.Mail,
            Ticks = mail.Tick,
            ClusterIndex = mail.ClusterIndex,
            MailContent = mail.MailContent,
            MailTypeText = mail.MailTypeText,
            Guid = mail.Guid
        })
            .ToList();
    }

    #endregion
}