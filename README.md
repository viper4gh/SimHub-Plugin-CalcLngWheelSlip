# SimHub-Plugin: Calculate Rotational Tyre Slip
This is a plugin for [SimHub](http://www.simhubdash.com/)

It calculates tyre slip by the relationship between Tyre RPS and Car Speed and provides the result as new properties.\
It is working for **Project CARS2** and **Race Room Racing Experience only**.

## Installation
Put the DLL file Viper.PluginCalcRotTyreSlip.dll into the SimHub folder parallel to the SimHubWPF.exe and start SimHub.\
SimHub detects the new plugin, confirm the question for enabling.\
If SimHub does not start, please check the log files in the "Logs" folder.
If you go to Settings -> Plugins tab now you should see the new plugin.

<img src="docs/Plugins_View.jpg" width="70%" height="70%">

Further there is a settings screen under "Additional Plugins"-> "Calculate Rotational Tyre Slip" where you can configure the limits when a tyre diameter calculation is triggered.

<img src="docs/Plugin_settings_view.jpg" width="70%" height="70%">

### Why do we need a tyre diameter calculation and what about these limits?
For the tyre slip calculation we have to calculate the tyre surface speed and compare it to the car speed.\
And for the tyre surface speed calculation we need the tyre diameter, which is not available in the game API.\
That means we have to calculate it first, but this works only if the tyre slip is nearly zero (no locking/spinning tyres, no cornering, no side slip).\
To automatically detect such a moment I defined 4 limits:
- minimum Car Speed - The higher the speed, the more accurate the result (default: 20 km/h)
- maximum Brake input - prevent tyre locking (default: 0%)
- maximum Throttle input - prevent tyre spinning (default: 5%)
- maximum ratio value between lateral car speed (sideways) and car speed - prevent cornering and side slides (default: 0.001)

As soon as all 4 limits are met for the first time, the tyre diameter is calculated.\
And only when the diameter is calculated, the slip can be calculated.\
The tighter the limits are set, the more accurate the result, but the longer it takes that this moment happens.

### How to use the plugin
The plugin provides new properties and actions.\
The new properties provide the calculated diameter(m) and the of rotational slip value of every tyre.\
The slip value can be understood as follows:
-  0 = no slip, the tyre surface speed is the same as the car speed
-  1 = 100% tyre lock, the tyre surface speed is 0 and the car still moving
- -1 = 100% tyre spin, the tyre surface speed is at least twice that of the car speed. The value can be lower than -1.

<img src="docs/Properties.jpg" width="70%" height="70%">

There are further two new actions:
- CalcTyreDiameter - manual trigger the tyre diameter calculation, the detection limits are ignored
- ResetTyreDiameter - reset the tyre diameter, the automatic detection begins again

<img src="docs/Control_Actions.jpg" width="70%" height="70%">
