using Shiny.BluetoothLE;
using System.Diagnostics;

namespace MyBleClient;


public class MyBleDelegate : BleDelegate
{
	readonly IDialogs _dialogs;
	public MyBleDelegate(IDialogs dialogs) => this._dialogs = dialogs;

	public override Task OnAdapterStateChanged(AccessState state)
	{
		Debug.WriteLine($"Adapter Status: {state}");

		return Task.CompletedTask;
	}


	public override Task OnPeripheralStateChanged(IPeripheral peripheral)
	{
		Debug.WriteLine($"MyBleDelegate: BLE Device Status {peripheral.Status}");

		if (peripheral.Status == ConnectionState.Connected)
			_ = _dialogs.Snackbar($"{peripheral.Name} Connected", 1000);
		else
			_ = _dialogs.Snackbar($"{peripheral.Name} Disconnected", 1000);

		return Task.CompletedTask;
	}
}