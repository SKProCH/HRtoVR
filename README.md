# HRtoVR

Stream your Heart Rate onto your VR applications.  
Over-simplified <sub>from user perspective</sub>  and over-complicated <sub>from developer perspective</sub>

[![license](https://img.shields.io/github/license/SKProCH/HRtoVR?style=for-the-badge)](https://github.com/SKProCH/HRtoVR/blob/main/LICENSE)
![downloads](https://img.shields.io/github/downloads/SKProCH/HRtoVR/total?style=for-the-badge)
[![lastcommit](https://img.shields.io/github/last-commit/SKProCH/HRtoVR?style=for-the-badge)](https://github.com/SKProCH/HRtoVR/commits/main)
[![issues](https://img.shields.io/github/issues/SKProCH/HRtoVR?style=for-the-badge)](https://github.com/SKProCH/HRtoVR/issues)

## Installation

HRtoVR is now a unified cross-platform application.

1. Download the latest version for your platform from the [Releases](https://github.com/SKProCH/HRtoVR/releases) page.
2. Extract the files to a folder of your choice.
3. Launch the `HRtoVR` executable.
    - **Linux**: You may need to mark the file as executable (`chmod +x HRtoVR`).

### Supported Devices & Services

Configure your preferred service in the **Listeners** tab within the UI.

| Device        | Info                                                                                           |
|---------------|------------------------------------------------------------------------------------------------|
| Bluetooth LE  | Native bluetooth support from your PC                                                          |
| FitbitHRtoWS  | https://github.com/200Tigersbloxed/FitbitHRtoWS                                                |
| HRProxy       | HRProxy Custom Reader                                                                          |
| HypeRate      | https://www.hyperate.io/                                                                       |
| Pulsoid       | https://pulsoid.net/ https://www.stromno.com/                                                  |
| PulsoidSocket | https://github.com/200Tigersbloxed/HRtoVRChat_OSC/wiki/Upgrading-from-Pulsoid-to-PulsoidSocket |
| Stromno       | https://www.stromno.com/                                                                       |
| TextFile      | A .txt file containing only a number                                                           |
| Omnicept      | https://www.hp.com/us-en/vr/reverb-g2-vr-headset-omnicept-edition.html                         |

## Configuration

All the configuration is done within the UI, you don't need the edit config anymore.  
But if you need it, you can find config near the app (or in the HRToVR folder in your config folder on Mac).

## UI Features

The application provides a real-time **Dashboard** to monitor your heart rate and connection status at a glance. In the
**Listeners** tab, you can manage all data sources and use the dedicated **Restart** button to troubleshoot specific
services without restarting the entire app. For advanced users, the **Logs** tab offers a detailed view of application
events, while the **System Tray** integration allows the app to run unobtrusively in the background.

## Avatar Setup

Avatar-specific setup is required to receive the OSC data.

I can't get the original prefab works, but you try to
use [original avatar setup guide](https://github.com/200Tigersbloxed/HRtoVRChat_OSC/blob/main/AvatarSetup.md) if you
want.

I've had success with the [ImLunaUwU prefab](https://github.com/ImLunaUwU/LunaHR), but make sure that you adjust your
parameter names in the app.
