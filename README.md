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

## Requirements
### Building
#### Every OS
* .NET Core SDK (>=2.0.0)
* JetBrains Rider (or any IDE supporting the modern C# tooling)

### Running
#### Linux & Mac
* Mono (or 32-bit .NET Core)
* libgtk-3-0

#### Windows
* .NET 4.6.2

## Contributing
If you want to contribute code back to the project, great! Open a pull request with your changes based on the `master` branch and I'll gladly take a look.

If you're not a developer, but want to contribute anyway, or if you just want to say thank you by buying me lunch, you can toss me some loose change via PayPal or Ko-Fi.

[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=jarl%2egullberg%40gmail%2ecom&lc=SE&item_name=Launchpad&item_number=pad%2dgithub&no_note=0&currency_code=EUR&bn=PP%2dDonationsBF%3abtn_donate_LG%2egif%3aNonHostedGuest)

<a href='https://ko-fi.com/H2H176VD' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://az743702.vo.msecnd.net/cdn/kofi2.png?v=0' border='0' alt='Buy Me a Coffee at ko-fi.com' /></a>

## Code contributors
* Jarl Gullberg
