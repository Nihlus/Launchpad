#!/bin/bash

wget https://crowdin.com/download/project/launchpad.zip launchpad.zip
unzip launchpad.zip

for D in `find . -type d`
do
	LOCALE_NAME=${D/./}
	LOCALE_NAME=${LOCALE_NAME///}
	LOCALE_NAME=${LOCALE_NAME/-/_}
	echo $LOCALE_NAME
	if [ ! -z "$LOCALE_NAME" ];
	then
		cp "$D/messages.po" "../$LOCALE_NAME.po"
	fi	
done

rm launchpad.zip
rm index.html
for D in `find . -type d`
do
	rm -r $D
done
