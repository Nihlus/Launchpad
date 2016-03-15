#!/bin/bash

# This script assumes that it is launched from the /Scripts/ directory in the Launchpad development tree.
# If it is not, your results may be unexpected.

# Move to the base folder where the script is located.
cd $(dirname $0)

LAUNCHPAD_ROOT=".."
OUTPUT_ROOT="$LAUNCHPAD_ROOT/release"

# Build a release version of launchpad
echo "Building Release configuration of Launchpad Launcher..."
xbuild /p:Configuration="Release" "$LAUNCHPAD_ROOT/Launchpad.Launcher/Launchpad.Launcher.csproj"

LAUNCHPAD_ASSEMBLY_VERSION=$(monodis --assembly "$LAUNCHPAD_ROOT/Launchpad.Launcher/bin/Release/Launchpad.exe" | grep Version | egrep -o '[0-9]*\.[0-9]*\.[0-9]*\.[0-9]*d*')

echo "Copying files to output directory..."

# Create the root output directory
if [ ! -d "$OUTPUT_ROOT" ]; then
	mkdir "$OUTPUT_ROOT"
fi

# Create a staging folder for this version
mkdir "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION"

# Copy the neccesary files
cp -r "$LAUNCHPAD_ROOT/Launchpad.Launcher/bin/Release/." "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION"
cp -r "$LAUNCHPAD_ROOT/Extras/Linux/." "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION"


echo "Compressing binary packages..."



