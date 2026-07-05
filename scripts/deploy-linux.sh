#!/bin/bash

# Sayra Server Linux systemd Installation Script

SERVICE_NAME="sayra-server"
INSTALL_DIR="/opt/sayra-server"
BINARY_NAME="Sayra.Server.Core"

if [ "$EUID" -ne 0 ]; then
  echo "Please run as root"
  exit 1
fi

echo "Creating installation directory: $INSTALL_DIR"
mkdir -p $INSTALL_DIR

# Assume current directory contains the build artifacts
echo "Copying files..."
cp -r ./* $INSTALL_DIR/

echo "Creating systemd service file..."
cat <<EOF > /etc/systemd/system/$SERVICE_NAME.service
[Unit]
Description=Sayra Server Management System
After=network.target

[Service]
Type=notify
WorkingDirectory=$INSTALL_DIR
ExecStart=$INSTALL_DIR/$BINARY_NAME
Restart=always
RestartSec=10
SyslogIdentifier=sayra-server
User=root

[Install]
WantedBy=multi-user.target
EOF

echo "Reloading systemd and starting service..."
systemctl daemon-reload
systemctl enable $SERVICE_NAME
systemctl start $SERVICE_NAME

echo "Installation Complete."
