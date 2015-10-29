using System;
using Launchpad.Utilities.Events.Arguments;

namespace Launchpad.Utilities.Events.Delegates
{
	public delegate void ManifestGenerationProgressChangedEventHandler (object sender, ManifestGenerationProgressChangedEventArgs e);
	public delegate void ManifestGenerationFinishedEventHandler (object sender, EventArgs e);
}

