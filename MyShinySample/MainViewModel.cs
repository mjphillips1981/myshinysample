using Helpers;
using Models;
using Shiny.BluetoothLE;
using System.Diagnostics;
using System.Reflection;

namespace MyShinySample
{
	public class MainViewModel : ViewModel
	{
		public MainViewModel(BaseServices services, IBleManager bleManager, TimerHelper timerHelper) : base(services)
		{
			_timer = timerHelper;

			IsScanning = bleManager?.IsScanning ?? false;

			this.WhenAnyValueSelected(x => x.SelectedPeripheral, async x =>
			{
				StopScan();

				await Navigation.Navigate("DataPage", new NavigationParameters
				{
					{"Peripheral", x.Peripheral }
				});
			});

			ScanToggle = ReactiveCommand.CreateFromTask(
				 async () =>
				 {
					 if (bleManager == null)
					 {
						 await Dialogs.Alert("Platform Not Supported");
						 return;
					 }
					 if (IsScanning)
					 {
						 StopScan();
					 }
					 else
					 {
						 Title = "Scanning...";

						 _timer.Start(StopScan, deviceType + nameof(StopScan), 7000, 0);

						 Peripherals.Clear();
						 IsScanning = true;

						 scanSub = bleManager
								 .Scan(new ScanConfig
								 {
									 ServiceUuids = new[] { "6e400001-b5a3-f393-e0a9-e50e24dcca9e" }
								 })
								 .Buffer(TimeSpan.FromSeconds(1))
								 .Where(x => x?.Any() ?? false)
								 .SubOnMainThread(
									  results =>
									  {
										  var list = new List<BLEScanModel>();
										  foreach (var result in results)
										  {
											  if (!string.IsNullOrWhiteSpace(result.Peripheral.Name))
											  {
												  Debug.WriteLine("BLE Device Found: " + result.Peripheral.Name);

												  //check if found peripheral is already in Peripherals
												  var peripheral = Peripherals.FirstOrDefault(x => x.Equals(result.Peripheral));
												  if (peripheral == null)
													  //if not then check to see if it is in the temp list
													  peripheral = list.FirstOrDefault(x => x.Equals(result.Peripheral));

												  if (peripheral != null)
												  {
													  //if already in the list UPDATE
													  peripheral.Update(result);
												  }
												  else
												  {
													  //if not in list, create a new one and add it
													  peripheral = new BLEScanModel(result.Peripheral);
													  peripheral.Update(result);
													  list.Add(peripheral);
												  }

											  }
										  }
										  if (list.Any())
										  {
											  // XF is not able to deal with an observablelist/addrange properly
											  foreach (var item in list)
												  Peripherals.Add(item);
										  }
									  },
									  ex => Dialogs.Alert(ex.ToString(), "ERROR")
								 );
					 }
				 }
			);
		}


		IDisposable? scanSub;

		string deviceType;
		TimerHelper _timer;

		public ICommand NavToTest { get; }
		public ICommand ScanToggle { get; }
		public ObservableCollection<BLEScanModel> Peripherals { get; } = new();

		[Reactive] public BLEScanModel? SelectedPeripheral { get; set; }
		[Reactive] public bool IsScanning { get; private set; }


		void StopScan()
		{
			Title = "Click Scan";

			if (Peripherals.Count < 1)
			{
				Dialogs.Snackbar($"No {deviceType} found");
			}

			scanSub?.Dispose();
			scanSub = null;
			IsScanning = false;

			_timer.Stop(true);
		}

		public override void OnNavigatedFrom(INavigationParameters parameters)
		{
			Debug.WriteLine(GetType().Name + " " + MethodBase.GetCurrentMethod().Name);
			foreach (var key in parameters.Keys)
			{
				Debug.WriteLine("key: " + key + "--value: " + parameters.GetValue<object>(key));
			}

			_timer.Stop(true);
		}

		public override void OnNavigatedTo(INavigationParameters parameters)
		{
			base.OnNavigatedTo(parameters);

			Peripherals.Clear();

			this.RaisePropertyChanged(nameof(SelectedPeripheral));
		}
	}
}