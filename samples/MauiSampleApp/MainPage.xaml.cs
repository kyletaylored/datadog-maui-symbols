namespace MauiSampleApp;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
		LoadBuildConfiguration();
	}

	private void LoadBuildConfiguration()
	{
		ServiceNameLabel.Text = $"Service: {Datadog.MAUI.Symbols.DatadogBuildInfo.ServiceName}";
		VersionLabel.Text = $"Version: {Datadog.MAUI.Symbols.DatadogBuildInfo.Version}";
		VariantLabel.Text = $"Variant: {Datadog.MAUI.Symbols.DatadogBuildInfo.Variant}";
		BuildIdLabel.Text = $"Build ID: {Datadog.MAUI.Symbols.DatadogBuildInfo.BuildId}";
	}

	private void OnCounterClicked(object? sender, EventArgs e)
	{
		count++;

		if (count == 1)
			CounterBtn.Text = $"Clicked {count} time";
		else
			CounterBtn.Text = $"Clicked {count} times";

		SemanticScreenReader.Announce(CounterBtn.Text);
	}
}
