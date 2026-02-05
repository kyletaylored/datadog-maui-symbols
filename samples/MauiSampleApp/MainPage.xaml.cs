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
		ServiceNameLabel.Text = $"Service: {BuildInfo.ServiceName}";
		VersionLabel.Text = $"Version: {BuildInfo.Version}";
		VariantLabel.Text = $"Variant: {BuildInfo.Variant}";
		ConfigurationLabel.Text = $"Configuration: {BuildInfo.Configuration}";
		BuildIdLabel.Text = $"Build ID: {BuildInfo.BuildId}";
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
