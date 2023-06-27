using Shiny.BluetoothLE;

namespace Models
{
	public class BLEScanModel : ReactiveObject
    {
      public IPeripheral Peripheral { get; set; }
      public string Name { get; set; }
      public int Rssi { get; set; }

      public BLEScanModel(IPeripheral peripheral) 
      { 
         Peripheral = peripheral;
      }

		public override bool Equals(object? obj)
		{
			return Peripheral.Equals(obj);
		}

		public void Update(ScanResult scanResult)
      {
         Rssi = scanResult.Rssi;
         Name = Peripheral.Name;

			this.RaisePropertyChanged(String.Empty);
		}
    }
}
