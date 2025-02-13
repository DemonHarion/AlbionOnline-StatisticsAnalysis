﻿using System;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.ViewModels;
using StatisticsAnalysisTool.Views;
using System.Linq;
using System.Windows;

namespace StatisticsAnalysisTool.UserControls;

/// <summary>
/// Interaction logic for DungeonControl.xaml
/// </summary>
public partial class DungeonControl
{
    public DungeonControl()
    {
        InitializeComponent();
    }

    public void ResetDungeons()
    {
        var dialog = new DialogWindow(LanguageController.Translation("RESET_DUNGEON_TRACKER"), LanguageController.Translation("SURE_YOU_WANT_TO_RESET_DUNGEON_TRACKER"));
        var dialogResult = dialog.ShowDialog();

        var vm = (MainWindowViewModel)DataContext;

        if (dialogResult is true)
        {
            vm?.TrackingController.DungeonController.ResetDungeons();
            _ = vm?.TrackingController.DungeonController.SetOrUpdateDungeonsDataUiAsync();
        }
    }

    public void ResetTodaysDungeons()
    {
        var dialog = new DialogWindow(LanguageController.Translation("RESET_TODAYS_DUNGEONS"), LanguageController.Translation("SURE_YOU_WANT_TO_RESET_DUNGEONS"));
        var dialogResult = dialog.ShowDialog();

        var vm = (MainWindowViewModel)DataContext;

        if (dialogResult is true)
        {
            vm?.TrackingController.DungeonController.ResetDungeonsByDateAscending(DateTime.UtcNow.Date);
            _ = vm?.TrackingController.DungeonController.SetOrUpdateDungeonsDataUiAsync();
        }
    }

    public void DeleteSelectedDungeons()
    {
        var dialog = new DialogWindow(LanguageController.Translation("DELETE_SELECTED_DUNGEONS"), LanguageController.Translation("SURE_YOU_WANT_TO_DELETE_SELECTED_DUNGEONS"));
        var dialogResult = dialog.ShowDialog();

        var vm = (MainWindowViewModel)DataContext;

        if (dialogResult is true)
        {
            var selectedDungeons = vm?.DungeonBindings?.TrackingDungeons.Where(x => x.IsSelectedForDeletion ?? false).Select(x => x.DungeonHash);
            _ = vm?.TrackingController.DungeonController.RemoveDungeonByHashAsync(selectedDungeons);
        }
    }

    public void DeleteZeroFameDungeons()
    {
        var dialog = new DialogWindow(LanguageController.Translation("DELETE_ZERO_FAME_DUNGEONS"), LanguageController.Translation("SURE_YOU_WANT_TO_DELETE_ZERO_FAME_DUNGEONS"));
        var dialogResult = dialog.ShowDialog();

        var vm = (MainWindowViewModel)DataContext;

        if (dialogResult is true)
        {
            vm?.TrackingController.DungeonController.DeleteDungeonsWithZeroFame();
            _ = vm?.TrackingController.DungeonController.SetOrUpdateDungeonsDataUiAsync();
        }
    }

    #region Ui events

    private void BtnDungeonTrackingReset_Click(object sender, RoutedEventArgs e)
    {
        ResetDungeons();
    }

    private void BtnResetTodaysDungeons_Click(object sender, RoutedEventArgs e)
    {
        ResetTodaysDungeons();
    }

    private void BtnDeleteZeroFameDungeons_Click(object sender, RoutedEventArgs e)
    {
        DeleteZeroFameDungeons();
    }

    private void BtnDeleteSelectedDungeons_Click(object sender, RoutedEventArgs e)
    {
        DeleteSelectedDungeons();
    }

    #endregion
}