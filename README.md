Launchpad
=========

An open-souce launcher for your games.
Launchpad was originally made for Unreal Engine 4, but supports arbitrary software and games. 

[![Build Status](https://travis-ci.org/Nihlus/Launchpad.svg?branch=master)](https://travis-ci.org/Nihlus/Launchpad)

![Launchpad (GTK# on Linux)](https://i.imgur.com/Xq1mtRl.png "Launchpad (GTK# on Linux)")

Features:

* Self-updating
* Can install, update and verify the game installation
* Support for a number of widespread protocols (currently FTP and HTTP/HTTPS)

Usage guide: [Launchpad Quickstart]

Note for users installing on Unix - you may need to install some additional libraries for Launchpad to run.
Simply run /Scripts/launchpad-dependencies.sh if your system is Debian or Debian-based, and it'll install them for you.

If you are not on a Debian-based system, you have to install these packages:
* libwebkitgtk-dev

If you are on Windows, you'll also need the GTK# runtime.
http://www.mono-project.com/docs/gui/gtksharp/installer-for-net-framework/

# Code contributors
* Jarl Gullberg
* Mentos
* Neur0t1c
