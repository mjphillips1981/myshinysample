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

		switch (peripheral.Status)
		{
			case ConnectionState.Connected:
				_ = _dialogs.Snackbar($"{peripheral.Name} Connected", 1000);
				break;
			case ConnectionState.Disconnected:
				_ = _dialogs.Snackbar($"{peripheral.Name} Disconnected", 1000);
				break;
			case ConnectionState.Disconnecting:
				_ = _dialogs.Snackbar($"{peripheral.Name} Disconnecting", 1000);
				break;
			case ConnectionState.Connecting:
				_ = _dialogs.Snackbar($"{peripheral.Name} Connecting", 1000);
				break;
		}

		return Task.CompletedTask;
	}
}