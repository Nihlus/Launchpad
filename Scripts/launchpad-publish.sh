#!/bin/bash

# This script assumes that it is launched from the /Scripts/ directory in the Launchpad development tree.
# If it is not, your results may be unexpected.

# Move to the base folder where the script is located.
cd $(dirname $0)

LAUNCHPAD_ROOT=".."
OUTPUT_ROOT="$LAUNCHPAD_ROOT/release"

RED='\033[0;31m'
GREEN='\033[0;32m'
ORANGE='\033[0;33m'
LOG_PREFIX="${GREEN}[Launchpad]:"
LOG_PREFIX_ORANGE="${ORANGE}[Launchpad]:"
LOG_PREFIX_RED="${RED}[Launchpad]:"
LOG_SUFFIX='\033[0m'

# Build a release version of launchpad
echo -e "$LOG_PREFIX Building Release configuration of Launchpad Launcher... $LOG_SUFFIX"
xbuild /p:Configuration="Release" "$LAUNCHPAD_ROOT/Launchpad.Launcher/Launchpad.Launcher.csproj"

echo -e "$LOG_PREFIX Building Release configuration of Launchpad Utilities... $LOG_SUFFIX"
xbuild /p:Configuration="Release" "$LAUNCHPAD_ROOT/Launchpad.Utilities/Launchpad.Utilities.csproj"

LAUNCHPAD_ASSEMBLY_VERSION=$(monodis --assembly "$LAUNCHPAD_ROOT/Launchpad.Launcher/bin/Release/Launchpad.exe" | grep Version | egrep -o '[0-9]*\.[0-9]*\.[0-9]*\.[0-9]*d*')

echo -e "$LOG_PREFIX Copying files to output directory... $LOG_SUFFIX"

# Create the root output directory
if [ ! -d "$OUTPUT_ROOT" ]; then
	mkdir "$OUTPUT_ROOT"
fi

# Create a staging folder for this version
mkdir "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION"
mkdir "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION/bin"

# Copy the neccesary files
echo ""
echo -e "$LOG_PREFIX Updating and copying translation files... $LOG_SUFFIX"
bash "$LAUNCHPAD_ROOT/Scripts/update-translations.sh"
cp -r "$LAUNCHPAD_ROOT/Extras/locale" "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION/bin"

cp -r "$LAUNCHPAD_ROOT/Launchpad.Launcher/bin/Release/." "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION/bin"

# Generate launcher binary manifest
echo ""
echo -e "$LOG_PREFIX Generating launcher binary manifest... $LOG_SUFFIX"
mono "$LAUNCHPAD_ROOT/Launchpad.Utilities/bin/Release/Launchpad.Utilities.exe" -b -d "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION/bin" -m Launchpad

echo "$LAUNCHPAD_ASSEMBLY_VERSION" > "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION/LauncherVersion.txt"

# Create compressed packages for distribution
echo ""
echo -e "$LOG_PREFIX Compressing binary packages... $LOG_SUFFIX"

# Move to the release directory for compatibility purposes
cd "$LAUNCHPAD_ROOT/release"

echo -e "$LOG_PREFIX Compressing ZIP package... $LOG_SUFFIX"
zip -r9 "launchpad-$LAUNCHPAD_ASSEMBLY_VERSION.zip" "launchpad-$LAUNCHPAD_ASSEMBLY_VERSION/"

echo ""
echo -e "$LOG_PREFIX Compressing tarball... $LOG_SUFFIX"
tar cfJ "launchpad-$LAUNCHPAD_ASSEMBLY_VERSION.tar.xz" "launchpad-$LAUNCHPAD_ASSEMBLY_VERSION/"

# Move back to the previous working directory
cd -

# Create simple debian package
echo ""
echo -e "$LOG_PREFIX Building Debian package... $LOG_SUFFIX"
mkdir "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION-all/"
cp -r "$LAUNCHPAD_ROOT/Packaging/Debian/template/." "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION-all/"
cp -r "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION/." "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION-all/usr/lib/Launchpad/"
rm "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION-all/usr/lib/Launchpad/readme.md"

# Update the package version in the control file
sed -i "s/\(version *: \).*/\1$LAUNCHPAD_ASSEMBLY_VERSION/" "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION-all/DEBIAN/control"

dpkg -b "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION-all/"


# Offer remote upload capabilities
echo ""
echo -e "$LOG_PREFIX_ORANGE Would you like to upload the new version to a remote FTP server for distribution? $LOG_SUFFIX"
echo -e "$LOG_PREFIX_ORANGE Note: You will be prompted for login information and a path to the upload directory. $LOG_SUFFIX"
echo -e "$LOG_PREFIX_ORANGE The directory should look something like this (on Debian): '/srv/ftp/launcher/' $LOG_SUFFIX"
echo ""
echo -e "$LOG_PREFIX_RED Selecting this option will replace and publish this build. $LOG_SUFFIX"
read -p "[y/n]" -r
echo ""  # (optional) move to a new line
if [[ $REPLY =~ ^[Yy]$ ]]; then
	read -p "Enter remote host: " -r REMOTEHOST
	read -p "Enter remote username: " -r REMOTEUSER
	read -p "Enter full path to remote upload directory [/srv/ftp/launcher/]: " -r REMOTEUPLOAD
	read -p "Enter full path to remote binary directory [/var/www/files/public/Launchpad/Binaries]: " -r REMOTEUPLOADBINARIES
	
	# Give the remote launcher dir a default value if no input was provided
	if [ -z "$REMOTEUPLOAD" ]; then
		REMOTEUPLOAD="/srv/ftp/launcher/"
	fi
	
	# Give the remote binary dir a default value if no input was provided
	if [ -z "$REMOTEUPLOADBINARIES" ]; then
		REMOTEUPLOADBINARIES="/var/www/files/public/Launchpad/Binaries"
	fi
	
	# Make sure the remote launcher dir ends with a slash
	if [[ ! "$REMOTEUPLOADBINARIES" == */ ]]; then
		REMOTEUPLOAD+="/"
	fi
	
	# Make sure the remote binary dir ends with a slash
	if [[ ! "$REMOTEUPLOADBINARIES" == */ ]]; then
		REMOTEUPLOAD+="/"
	fi
	
	echo ""
	echo -e "$LOG_PREFIX Uploading files to remote server... $LOG_SUFFIX"
	
	# Upload the binaries used by the launcher to update itself
	ssh $REMOTEUSER@$REMOTEHOST "mkdir -p $REMOTEUPLOAD"		
    #scp -r "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION/." "$REMOTEUSER@$REMOTEHOST:$REMOTEUPLOAD"
    
    # Upload new packaged binaries (zip, tarball, debian)
    ssh $REMOTEUSER@$REMOTEHOST "mkdir -p $REMOTEUPLOADBINARIES"
    scp "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION.zip" "$REMOTEUSER@$REMOTEHOST:$REMOTEUPLOADBINARIES"
    scp "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION.tar.xz" "$REMOTEUSER@$REMOTEHOST:$REMOTEUPLOADBINARIES"
    scp "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION-all.deb" "$REMOTEUSER@$REMOTEHOST:$REMOTEUPLOADBINARIES"
        
    echo ""
	echo -e "$LOG_PREFIX Upload successful! $LOG_SUFFIX"
fi

echo ""
echo -e "$LOG_PREFIX Cleaning up residual build files... $LOG_SUFFIX"
rm -r "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION-all/"
rm -r "$OUTPUT_ROOT/launchpad-$LAUNCHPAD_ASSEMBLY_VERSION/"

echo ""
echo -e "$LOG_PREFIX Done! $LOG_SUFFIX"
