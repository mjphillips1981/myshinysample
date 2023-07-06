using Shiny.BluetoothLE;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reflection;
using System.Text;

namespace MyShinySample
{
	public class DataViewModel : ViewModel
	{
		public DataViewModel(BaseServices services) : base(services)
		{
			ConnectToDeviceCommand = ReactiveCommand.CreateFromTask(ConnectToDeviceExecuted);
			LoadCommand = ReactiveCommand.Create(LoadCommandExecuted);
			NegotMTU = ReactiveCommand.CreateFromTask(NegotiateMTU);
			ButtonCommand = ReactiveCommand.Create<string>(OnButtonCommandExecuted);
		}

		static readonly string _uartGattServiceId = "6e400001-b5a3-f393-e0a9-e50e24dcca9e";
		static readonly string _uartGattCharacteristicSendId = "6e400002-b5a3-f393-e0a9-e50e24dcca9e";
		static readonly string _uartGattCharacteristicReceiveId = "6e400003-b5a3-f393-e0a9-e50e24dcca9e";

		[Reactive] public bool DeviceIsConnected { get; set; } = false;
		[Reactive] public string? DataFromDevice { get; set; }

		public ICommand ButtonCommand { get; }
		public ICommand LoadCommand { get; }
		public ICommand ConnectToDeviceCommand { get; }

		public ICommand NegotMTU { get; }

		IPeripheral? _peripheral;
		BleCharacteristicInfo? _tXCharacteristic;
		BleCharacteristicInfo? _rXCharacteristic;

		//CompositeDisposable? deactivateWith;
		//protected internal CompositeDisposable DeactivateWith => deactivateWith ?? (deactivateWith = new CompositeDisposable());

		IDisposable? _disposable;
		IDisposable _disposable2;


		void LoadCommandExecuted()
		{
			//_peripheral
			//	.WhenDisconnected()
			//	.SubOnMainThread(async x =>
			//	{
			//		if (DeviceIsConnected)
			//		{
			//			//await Navigation.GoBackToRootAsync();
			//			Stop();
			//		}
			//	}).DisposeWith(DeactivateWith);
		}

		async Task ConnectToDeviceExecuted()
		{
			IsBusy = true;
			Title = "Connecting...";
			try
			{
				await _peripheral.WithConnectIf(new ConnectionConfig { AutoConnect = false });

				_rXCharacteristic = await _peripheral.GetCharacteristicAsync(_uartGattServiceId, _uartGattCharacteristicReceiveId);

				if (_rXCharacteristic != null)
				{
					//var _rx = await _peripheral.NotifyCharacteristic(_rXCharacteristic);

					Debug.WriteLine("made it here");

					_disposable = _peripheral
						.NotifyCharacteristic(_rXCharacteristic)
						.SubOnMainThread(x =>
						{
							DataFromDevice = x.Data == null ? "NO DATA" : Encoding.UTF8.GetString(x.Data);

							Debug.WriteLine(DataFromDevice);
						}, ex => Debug.WriteLine("GATT ERROR: " + ex.Message));
				}

				_tXCharacteristic = await _peripheral.GetCharacteristicAsync(_uartGattServiceId, _uartGattCharacteristicSendId);

				if (_tXCharacteristic != null)
				{
					NegotMTU.Execute(null);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);

				await Dialogs.Snackbar("BLE Connection Error", 1000);

				await Navigation.GoBackToRootAsync();
			}
		}

		async Task NegotiateMTU()
		{
			try
			{
				Thread.Sleep(500);

				var results = _peripheral.Mtu;

				//if (results < 57)
				//{
					results = await _peripheral.TryRequestMtuAsync(60);
				//}

				Debug.WriteLine($"result: {results}");

				if (results >= 57)
				{
					DeviceIsConnected = true;

					this.RaisePropertyChanged(nameof(DeviceIsConnected));

					//ok to start sending message
					WriteToDevice("SM=1\0");
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"NegotiateMTU: {ex.Message}");
			}
		}

		public async void WriteToDevice(string datatosend)
		{
			try
			{
				if (_peripheral.Status == ConnectionState.Connected && _tXCharacteristic != null)
				{
					Debug.WriteLine($"Writing: {datatosend}");

					var result = await _peripheral.WriteCharacteristicAsync(_tXCharacteristic, Encoding.UTF8.GetBytes(datatosend));

					Debug.WriteLine(result.Event.ToString());
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}

		void Stop()
		{
			Debug.WriteLine("stopping");

			_peripheral.CancelConnection();

			_disposable.Dispose();
			_disposable = null;

			_tXCharacteristic = null;

			if (_rXCharacteristic.IsNotifying)
			{
				_rXCharacteristic = null;
			}

			DeviceIsConnected = false;

			DataFromDevice = "";
			this.RaisePropertyChanged(nameof(DataFromDevice));

			//_peripheral = null;
		}

		async void OnButtonCommandExecuted(string cmd)
		{
			switch (cmd)
			{
				case "RequestStatus":
					if (DeviceIsConnected)
					{
						WriteToDevice($"RS=1\0");
					}
					break;

				case "Connect":
					if (!DeviceIsConnected)
					{
						ConnectToDeviceCommand.Execute(null);
					}
					break;
				case "Disconnect":
					if (DeviceIsConnected)
					{
						Stop();
					}
					break;
			}
		}


		//override section

		public override Task InitializeAsync(INavigationParameters parameters)
		{
			_peripheral = parameters.GetValue<IPeripheral>("Peripheral");

			Title = _peripheral.Name;

			LoadCommand.Execute(null);

			return base.InitializeAsync(parameters);
		}

		public override void OnNavigatedTo(INavigationParameters parameters)
		{
			Debug.WriteLine(GetType().Name + " " + MethodBase.GetCurrentMethod().Name);
			foreach (var key in parameters.Keys)
			{
				Debug.WriteLine("key: " + key + "--value: " + parameters.GetValue<object>(key));
			}

			if (_peripheral != null && _peripheral.Status != ConnectionState.Connected)
			{
				ConnectToDeviceCommand.Execute(null);
			}
		}

		public override void OnNavigatedFrom(INavigationParameters parameters)
		{
			Debug.WriteLine(GetType().Name + " " + MethodBase.GetCurrentMethod().Name);
			foreach (var key in parameters.Keys)
			{
				Debug.WriteLine("key: " + key + "--value: " + parameters.GetValue<object>(key));
			}

			Stop();
		}
	}
}

