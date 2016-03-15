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


# Create compressed packages for distribution
echo "Compressing binary packages..."

echo "Compressing ZIP package..."
zip -r9 "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION.zip" "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION/"

echo "Compressing tarball..."
tar cfJ "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION.tar.xz" "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION/"

# Create simple debian package
echo "Building Debian package..."
mkdir "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION-all/"
cp -r "$LAUNCHPAD_ROOT/Packaging/Debian/template/." "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION-all/"
cp -r "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION/." "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION-all/usr/lib/Launchpad/"
rm "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION-all/usr/lib/Launchpad/readme.md"

# Update the package version in the control file
sed -i "s/\(version *: \).*/\1$LAUNCHPAD_ASSEMBLY_VERSION/" "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION-all/DEBIAN/control"

dpkg -b "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION-all/"

echo "Cleaning up residual build files..."
rm -r "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION-all/"
rm -r "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION/"

