# ED:Bindings

## Fork related details

This repo was forked in January 2025. Given the fact that the original repo was not touched in a few years, I do not intend to do pull requests for the time being. That might change though in case the origial author gets active again on this tools.

The initial changes I did are:
* updating project files to compile with .NET 8.0 from within Visual Studio Code.
* customizing column behaviour to hide selected columns
* Adding a "Notes" column to be fileld with in-game control help texts
* Allowing for more than one device mapping file to be used

I personally use a VPC MongoosT-50CM3 Throttle as well as a VKB Modern Combat II Grip. Naturally I want to keep their Device Maps seperated. That is why I needed more than one Devie Map to be active.

**DISCLAIMER:** I am not a C# expert, I am a Python/C/C++ coder at heart. All changes I made were done using the help of either ChatGPT or Github CoPilot.

# Original README content (forked 01/2025)

This is a little windows application that reads the key bindings file
so that it can be searchable and mappable to keys.

<img src="https://raw.githubusercontent.com/ghorsey/EdBindings/main/assets/edbindings.screenshot.gif">

## Features

* Support mapping device codes in Elite to actual labels of device (For example [X56 Bindings](https://www.edrefcard.info/device/SaitekX56)).
* Allows filtering bindings.
* Includes [VoiceAttack/BindED](https://github.com/alterNERDtive/bindED) variable names.

## Device Mapping Files
1. The device mapping files are found in the `DeviceMappings` folder.
2. The files are in JSON format using the following Schema:

```
{
  "name": "{{Friendly Device Name, will appear in menu}}", 
  "controls": [
    {
      "deviceId": "{{The device from the ED binds file}}",
      "deviceName": "{{Friendly Device Name to show in the table}}",
      "controlLabel": "{{Friendly name for the key to show in the table}}",
      "controlValue": "{{The key value from the ED binds file}}"
    },
    // additional controls
  ]
}
```

See [X56.json](https://github.com/ghorsey/EdBindings/blob/main/src/EdBindings/DeviceMappings/X56.json) for a complete example.

## Please Send a PR with additional Devince Mapping Files
Please open a PR or start a discussion with your mappings

## Credits

* App Icon made by [Nikita Golubev](https://www.flaticon.com/authors/nikita-golubev) from [www.flaticon.com](https://www.flaticon.com/)
