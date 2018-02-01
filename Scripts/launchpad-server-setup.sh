#!/bin/bash
#check if we're running as root - this is required.

bIsRoot=false
if [ $(id -u) -eq 0 ]
then
    bIsRoot=true
else
    bIsRoot=false
    echo "This script needs to be run as root. Please run it with <sudo> or <gksudo>."
    exit 1
fi

#check if vsftpd is installed
#assume it is installed.
bIsVsftpdInstalled=true
echo "Checking for vsftpd..."

#if no,
#ask user if it should be installed.
hash vsftpd 2>/dev/null || { echo >&2 "vsftpd is required software for this automated setup."; bIsVsftpdInstalled=false; }
if [ "$bIsVsftpdInstalled" = false ]
then
    read -p "Would you like to install it? [y/n] " -r
    if [[ $REPLY =~ ^[Yy]$ ]]
    then
        apt-get install vsftpd
    else
        exit 1;
    fi
else
    echo "Vsftpd was installed, proceeding."

fi
#if yes,
#modify the config options, uncommenting each
    echo "Allowing anonymous downloading of files..."
    sed -i "s/^#anonymous_enable/anonymous_enable/" /etc/vsftpd.conf
    sed -i "s/\(anonymous_enable *= *\).*/\1YES/" /etc/vsftpd.conf

    echo "Allowing local accounts to log in and write files..."
    sed -i "s/^#write_enable/write_enable/" /etc/vsftpd.conf
    sed -i "s/\(write_enable *= *\).*/\1YES/" /etc/vsftpd.conf

    echo "Setting local umask..."
    sed -i "s/^#local_umask/local_umask/" /etc/vsftpd.conf
    sed -i "s/\(local_umask *= *\).*/\1022/" /etc/vsftpd.conf

    echo "Prohibiting anonymous uploading of files..."
    sed -i "s/^#anon_upload_enable/anon_upload_enable/" /etc/vsftpd.conf
    sed -i "s/\(anon_upload_enable *= *\).*/\1NO/" /etc/vsftpd.conf

    echo "Changing service PAM name.."
    sed -i "s/^#pam_service_name/pam_service_name/" /etc/vsftpd.conf
    sed -i "s/\(pam_service_name *= *\).*/\1ftp/" /etc/vsftpd.conf
    service vsftpd restart

#create folders

    echo "Creating folder structure in /srv/ftp..."

    cd /srv/ftp
    mkdir game
    mkdir game/Win64
    mkdir game/Win32
    mkdir game/Linux
    mkdir game/Mac
    mkdir game/Win64/bin
    mkdir game/Win32/bin
    mkdir game/Linux/bin
    mkdir game/Mac/bin
    mkdir launcher
    mkdir launcher/bin

    chown -R root:ftp game
    chown -R root:ftp launcher

    #vsftpd will fail voluntarily if the FTP root is writable, if we're jailing users.
    chmod ugo-w /srv/ftp

    chmod -R g+rwX game
    chmod -R o+r game
    chmod -R g+rwX launcher
    chmod -R o+r launcher

    echo "Folder structure created and permissions set."

    read -p "You will need an account in the ftp group to upload files to the server. Would you like to use an existing account, or create a new one? Note that you may need to log out and back in if you use an existing account. [Create - y/ Existing - n] " -r
    if [[ $REPLY =~ ^[Yy]$ ]]
    then
        useradd -d /srv/ftp --comment Launchpad-system -G ftp launchpad
        passwd launchpad
    else
        read -p "Input account name: " -r
        usermod -a -G ftp $REPLY
    fi


    echo "Setup successful. You can now start uploading your game and/or launcher via your selected accounts."
