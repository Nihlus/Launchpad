Launchpad
=========

An open-source launcher for your games.
Launchpad was originally made for Unreal Engine 4, but supports arbitrary software and games. 

[![Build Status](https://travis-ci.org/Nihlus/Launchpad.svg?branch=master)](https://travis-ci.org/Nihlus/Launchpad)
[![codecov](https://codecov.io/gh/Nihlus/Launchpad/branch/master/graph/badge.svg)](https://codecov.io/gh/Nihlus/Launchpad)

![Launchpad (GTK# on Linux)](https://i.imgur.com/Xq1mtRl.png "Launchpad (GTK# on Linux)")

## Features

* Self-updating
* Can install, update and verify the game installation
* Support for a number of widespread protocols (currently FTP and HTTP/HTTPS)

## Usage guide
[Game Developer Quickstart](https://github.com/Nihlus/Launchpad/wiki/Game-Developer-Quickstart)

Note for users installing on Unix - you may need to install some additional libraries for Launchpad to run.
Simply run /Scripts/launchpad-dependencies.sh if your system is Debian or Debian-based, and it'll install them for you.

If you are not on a Debian-based system, you have to install these packages:
* libwebkitgtk-dev

If you are on Windows, you'll also need the GTK# runtime.
http://www.mono-project.com/docs/gui/gtksharp/installer-for-net-framework/

## Contributing
If you want to contribute code back to the project, great! Open a pull request with your changes based on the `master` branch and I'll gladly take a look.

If you're not a developer, but want to contribute anyway, or if you just want to say thank you by buying me lunch, you can toss me some loose change.

[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=jarl%2egullberg%40gmail%2ecom&lc=SE&item_name=Launchpad&item_number=pad%2dgithub&no_note=0&currency_code=EUR&bn=PP%2dDonationsBF%3abtn_donate_LG%2egif%3aNonHostedGuest)

## Code contributors
* Jarl Gullberg
* Mentos
* Neur0t1c
