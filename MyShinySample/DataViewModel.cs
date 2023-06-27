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

		CompositeDisposable? deactivateWith;
		protected internal CompositeDisposable DeactivateWith => deactivateWith ?? (deactivateWith = new CompositeDisposable());


		void LoadCommandExecuted()
		{
			_peripheral
				.WhenDisconnected()
				.SubOnMainThread(async x =>
				{
					if (DeviceIsConnected)
					{
						await Navigation.GoBackToRootAsync();
					}
				}).DisposeWith(DeactivateWith);
		}

		async Task ConnectToDeviceExecuted()
		{
			IList<BleServiceInfo> Services;
			BleServiceInfo myservice = null!;
			List<BleCharacteristicInfo> Characteristics;

			IsBusy = true;
			Title = "Connecting...";

			try
			{
				var services = await _peripheral.WithConnectIf()
					.Select(x => x.GetServices())
					.Switch()
					.ToTask();

				Services = services.ToList();

				foreach (var service in Services)
				{
					if (service.Uuid.Equals(_uartGattServiceId))
					{
						myservice = service;
						break;
					}
				}

				if (myservice != null)
				{
					Characteristics = (await _peripheral!.GetCharacteristicsAsync(myservice.Uuid)).ToList();

					foreach (var characteristic in Characteristics)
					{
						if (characteristic.Uuid.Equals(_uartGattCharacteristicSendId))
						{
							_tXCharacteristic = characteristic;

							NegotMTU.Execute(null);

							Debug.WriteLine("got tx");
						}
						else if (characteristic.Uuid.Equals(_uartGattCharacteristicReceiveId))
						{
							_peripheral
								.NotifyCharacteristic(characteristic)
								.SubOnMainThread(x =>
								{
									DataFromDevice = x.Data == null ? "NO DATA" : Encoding.UTF8.GetString(x.Data);
								}, ex => Debug.WriteLine("GATT ERROR: " + ex.Message))
								.DisposeWith(DeactivateWith);

							Debug.WriteLine("got rx");
						}
					}
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

				if (results < 57)
				{
					results = await _peripheral.TryRequestMtuAsync(60);
				}

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
			deactivateWith.Dispose();
			deactivateWith = null;
			_tXCharacteristic = null;
			DeviceIsConnected = false;
			_peripheral.CancelConnection();
			_peripheral = null;
		}

		async void OnButtonCommandExecuted(string cmd)
		{
			switch (cmd)
			{
				case "RequestStatus":
					WriteToDevice($"RS=1\0");
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

