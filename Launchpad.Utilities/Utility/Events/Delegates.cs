using System;

namespace Launchpad.Utilities.Utility.Events
{
	public delegate void ManifestGenerationProgressChangedEventHandler(object sender,ManifestGenerationProgressChangedEventArgs e);
	public delegate void ManifestGenerationFinishedEventHandler(object sender,EventArgs e);
}

