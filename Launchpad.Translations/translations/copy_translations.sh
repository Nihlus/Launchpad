#!/bin/bash
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
