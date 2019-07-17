﻿using IntraMessaging;
using LiteNetLib;
using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using Soulful.Core.Model;
using Soulful.Core.Net;
using Soulful.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Soulful.Core.ViewModels
{
    public class StartGameViewModel : Base.ViewModelBase<string>
    {
        #region Constants

        public const double MIN_PLAYERS = 3;
        public const double MAX_PLAYERS = 100;

        #endregion

        #region Fields

        private readonly INetServerService _server;
        private readonly IIntraMessenger _messenger;

        private int _gamePin;
        private int _maxPlayers;
        private string _playerName;
        private ObservableCollection<Tuple<int, string>> _players;

        #endregion

        #region Properties

        public string GamePin
        {
            get => _gamePin.ToString("000000");
        }

        public int MaxPlayers
        {
            get => _maxPlayers;
            set
            {
                SetProperty(ref _maxPlayers, value);
                _server.ChangeMaxPlayers(value);
            }
        }

        public ObservableCollection<Tuple<int, string>> Players
        {
            get => _players;
            set => SetProperty(ref _players, value);
        }

#if DEBUG
        public bool CanStartGame => _server.Players.Count >= 1;
#else
        public bool CanStartGame => Server.Players.Count >= MIN_PLAYERS - 1;
#endif

        #endregion

        #region Commands

        public IMvxCommand RefreshGamePinCommand => new MvxCommand(GenerateGamePin);
        public IMvxCommand NavigateBackCommand => new MvxCommand(NavigateBack);
        public IMvxCommand StartGameCommand => new MvxCommand(StartGame);
        public IMvxCommand KickPlayerCommand => new MvxCommand<int>((i) => _server.Kick(i));

        #endregion

        public StartGameViewModel(IMvxNavigationService navigationService, INetServerService server, IIntraMessenger messenger)
            : base(navigationService)
        {
            _server = server;
            _messenger = messenger;

            MaxPlayers = 20;
            Players = new ObservableCollection<Tuple<int, string>>();

            GenerateGamePin();
            _server.Players.CollectionChanged += OnPlayerCollectionChanged;
            _server.Start(MaxPlayers, GamePin);
        }

        private async void StartGame()
        {
            if (!CanStartGame)
                return;

            INetClientService client = Mvx.IoCProvider.Resolve<INetClientService>();
            client.Start(GamePin, _playerName);
            await Task.Run(async () =>
            {
                while (!client.IsConnected)
                    await Task.Delay(15).ConfigureAwait(false);
            }).ConfigureAwait(false);
            Mvx.IoCProvider.Resolve<IGameService>().Start();

            await NavigationService.Navigate<GameViewModel, string>(_playerName).ConfigureAwait(false);
        }

        private void NavigateBack()
        {
            if (_server.Players.Count > 0)
            {
                void callback(DialogMessage.Button button)
                {
                    if (button == DialogMessage.Button.Yes)
                        UnsafeNavigateBack();
                }

                _messenger.Send(new DialogMessage
                {
                    Title = "Oh, come on...",
                    Content = "People are already queueing up to play! Are you sure you want to deprive them of this wonderful opportunity by closing the server?",
                    Buttons = DialogMessage.Button.Yes | DialogMessage.Button.No,
                    Callback = callback
                });
            }
            else
            {
                UnsafeNavigateBack();
            }
        }

        private void UnsafeNavigateBack()
        {
            if (_server.IsRunning)
                _server.Stop();
            NavigationService.Navigate<HomeViewModel>();
        }

        private void GenerateGamePin()
        {
            Random r = new Random();
            _gamePin = r.Next(100000, 999999);
            RaisePropertyChanged(nameof(GamePin));
            _server.ChangeConnectPin(GamePin);
        }

        private async void OnPlayerCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (NetPeer element in e.NewItems)
                {
                    await AsyncDispatcher.ExecuteOnMainThreadAsync(() => Players.Add(new Tuple<int, string>(element.Id, (string)element.Tag))).ConfigureAwait(false);
                }
            } else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                foreach (NetPeer element in e.OldItems)
                {
                    await AsyncDispatcher.ExecuteOnMainThreadAsync(() => Players.Remove(Players.First(p => p.Item1 == element.Id))).ConfigureAwait(false);
                }
            }
            await RaisePropertyChanged(nameof(CanStartGame));
        }

        public override void Prepare(string parameter)
        {
            _playerName = parameter;
        }
    }
}
