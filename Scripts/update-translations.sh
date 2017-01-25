#!/bin/bash

# Move to the base folder where the script is located.
cd $(dirname $0)

LAUNCHPAD_TRANSLATIONS_ROOT="../Launchpad.Translations/"
cd $LAUNCHPAD_TRANSLATIONS_ROOT

mkdir "translations_update"
cd "translations_update"

wget https://crowdin.com/download/project/launchpad.zip
unzip launchpad.zip

for D in `find . -type d`
do
	LOCALE_NAME=${D/./}
	LOCALE_NAME=${LOCALE_NAME///}
	LOCALE_NAME=${LOCALE_NAME/-/_}

	if [ ! -z "$LOCALE_NAME" ];
	then
		cp "$D/messages.po" "../$LOCALE_NAME.po"
		
		mkdir -p "../../Launchpad.Launcher/Content/locale/$LOCALE_NAME/LC_MESSAGES/"
		cp "$D/messages.po" "../../Launchpad.Launcher/Content/locale/$LOCALE_NAME/LC_MESSAGES/messages.po"
	fi	
done

rm launchpad.zip

for D in `find . -type d`
do
	if [ ! $D == "." ]; then
		rm -r $D
	fi	
done

cd ..

rm -r "translations_update"
