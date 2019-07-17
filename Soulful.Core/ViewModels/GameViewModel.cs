﻿using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using Soulful.Core.Model;
using Soulful.Core.Net;
using System;
using System.Collections.ObjectModel;

namespace Soulful.Core.ViewModels
{
    public class GameViewModel : Base.ViewModelBase<string>
    {
        #region Fields

        private readonly INetClientService _client;
        private string _playerName;

        private ObservableCollection<int> _whiteCards;
        private int _blackCard;

        #endregion

        #region Properties

        public ObservableCollection<int> WhiteCards
        {
            get => _whiteCards;
            set => SetProperty(ref _whiteCards, value);
        }

        public int BlackCard
        {
            get => _blackCard;
            set => SetProperty(ref _blackCard, value);
        }

        #endregion

        #region Commands

        public IMvxCommand NavigateBackCommand => new MvxCommand(NavigateBack);

        #endregion

        public GameViewModel(IMvxNavigationService navigationService, INetClientService client)
            : base(navigationService)
        {
            _client = client;
            _client.GameEvent += OnGameEvent;
            _client.DisconnectedFromServer += (_, __) => NavigationService.Navigate<HomeViewModel>();

            _whiteCards = new ObservableCollection<int>();

            if (!_client.IsRunning)
                NavigationService.Navigate<HomeViewModel>();
        }

        private void OnGameEvent(object sender, GameKeyPackage e)
        {
            switch (e.Key)
            {
                case GameKey.SendWhiteCards:
                    while (!e.Data.EndOfData)
                        EOMT(() => WhiteCards.Add(e.Data.GetInt()));
                    break;
                case GameKey.SendBlackCard:
                    EOMT(() => BlackCard = e.Data.GetInt());
                    break;
                case GameKey.InitiateCzar:
                    _whiteCards.Clear();
                    break;
            }
        }

        public override void Prepare(string parameter)
        {
            _playerName = parameter;
        }

        private async void NavigateBack()
        {
            _client.Stop();

            INetServerService server = Mvx.IoCProvider.Resolve<INetServerService>();
            if (server.IsRunning)
            {
                while (_client.IsRunning)
                {
                    await System.Threading.Tasks.Task.Delay(NetHelpers.POLL_DELAY).ConfigureAwait(false);
                }
                server.Stop();
            }

            await NavigationService.Navigate<HomeViewModel>().ConfigureAwait(false);
        }

        /// <summary>
        /// Provides a syntatic shortcut <see cref="AsyncDispatcher.ExecuteOnMainThreadAsync"/>
        /// </summary>
        /// <param name="action">The action to execute</param>
        private void EOMT(Action action) => AsyncDispatcher.ExecuteOnMainThreadAsync(action);
    }
}
