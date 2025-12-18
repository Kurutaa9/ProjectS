************************************
*        DYNAMIC FOG & MIST        *
* (C) Copyright 2015-2023 Kronnect * 
*           README FILE            *
************************************


How to use this asset
---------------------
Dynamic Fog & Mist includes 3 operation modes:

1) Traditional camera-based script: just add "DynamicFog" or "DynamicFog Exclusive" script to your camera. It will show up in the Game view. Customize it using the custom inspector.
- Dynamic Fog & Mist: recommended script for most situations.
- Dynamic Fog & Mist Exclusive: variant of the main script with Render Scale option which results in better performance on mobile.

2) Post-Processing Extensions for LWRP and URP
Support for Post-Processing Stack is included. This option requires Unity 2018.1 or later.
You also need to have PostProcessing Stack V2 package installed (you can install it from the Package Manager).
Then import the package named "PostProcessingSupport.unitypackage" included in DynamicFog folder.
If you opt for the post-processing extensions, don't add the script to the camera!
Dynamic Fog effects will be available as pluggable effects in Post-Processing Profiles and Volumes.

Check this video for setting up Dynamic Fog & Mist on URP: https://youtu.be/QiTM4YjLQT8

3) Custom Fog materials
A collection of materials and shaders are provided that have the fog effect integrated in their shaders so they offer the fastest performance available! However, if you choose this path, then you need to use these materials and not the image effects or Post-Processing Extensions above. Check the documentation for details.


Demo Scenes
-----------
There're at several demo scenes, located in "Demos" folder. Just go there from Unity, open them and run it. Remember to remove the Demos folder from your project to reduce size.
Important: since Unity 5.4, the Game View allows to scale the window. Choose "Free Aspect" to easily read the text of some of the demo scenes, otherwise due to the scaling feature demo texts could not be visible inside the window depending on the chosen resolution and your screen size.


Documentation/API reference
---------------------------
The PDF is located in the Documentation folder. It contains additional instructions on how to use this asset as well as a useful Frequent Asked Question section.


Support
-------
Please read the documentation PDF and browse/play with the demo scene and sample source code included before contacting us for support :-)

* Support-Web: https://kronnect.com/support
* Support-Discord: https://discord.gg/EH2GMaM
* Email: contact@kronnect.com
* Twitter: @Kronnect


Future updates
--------------

All our assets follow an incremental development process by which a few beta releases are published on our support forum (kronnect.com).
We encourage you to signup and engage our forum. The forum is the primary support and feature discussions medium.

Of course, all updates of Dynamic Fog & Mist will be eventually available on the Asset Store.


Version history
---------------

Current version
- [Fix] Fixed issue when switching from perspective to orthographic camera at runtime

V7.0
- Minimum Unity version required is now Unity 2020.3.16
- Minor inspector GUI improvements

V6.9
- Added "Dynamic Fog of War Hole" command to top menu. See manual for details
- Material property setting optimizations

V6.8
- Added Skybox Blending option

V6.7.2
- [Fix] VR fixes

V6.7.1
- API: added GetFogOfWarAlpha

V6.7
- Refactored fogColor function to make integrations easier
- Included a copy of Standard Assets to run the demos since the package is no longer available on the Asset Store

V6.6
- Added "Sync with Profile" option to inspector. By default it's disabled which allows you to assign a fog profile but keep changes to the fog inspector settings. If enabled, profile settings will replace inspector settings when entering into playmode.
- [Fix] Fixed an issue which could prevent setting proper camera depthTextureMode

V6.5.5
- [Fix] Fixed horizon glitches with fog materials in Unity 2019.1
- [Fix] Fixed compiler error with LWRP on Mac

V6.5.4
- Added compatibility with LWRP VR in Unity 2018.3.x

V6.5.3
- [Fix] Fixed issue with Post-Processing extensions in build

V6.5.2
- [Fix] Fixed hard edges of fog of war plane effect

V6.5.1
- Added support for Unity 2018.3

V6.5
- Added support for Post-Processing Stack V2 (Unity 2018.1 and up)

V6.0
- New Exclusive Mode (add 'Dynamic Fog Exclusive' script' to camera instead of normal Dynamic Fog script)

V5.2
- Added 3 new fog materials: Standard + Normal, Standard + Normal + Occlusion Map and Standard + Normal + Occlusion integrated
- [Fix] Fixed racing condition when preset and other values are set at runtime
- [Fix] Workaround for an error in Unity 2018.1 beta

V5.1
- VR: support for Single Pass Instanced (Unity 2017.2+)
- Support for Timeline animations
- Added wind direction

V5.0
- New Fog Profiles
- Fog volumes now handle full set of fog properies by setting target fog profile
- Dithering option extended to all fog variants

V4.3
- Added light scattering option to Desktop Plus variant

V4.2
- New fog variant: orthogonal - takes depth and height separately for computing fog
- New fog variant: desktop plus orthogonal - takes depth and height separately for computing fog
- Added noise scale parameter to some fog variants

V4.1
- Fog volumes: added option to specify custom fog colors

V4.0.1
- [Fix] Fixed issue with Post-Processing Stack

V4.0
- New fog of war prefab and mode!
- Updated demo scene "Orthographic" with fog of war functionality

V3.1:
- New fog variant "Basic" for low end mobile devices
- [Fix] Fixed Single Pass Stereo with OpenVR SDK

V3.0:
- Support for orthographic camera

V2.4:
- Compatibility with Unity 5.5
- VR: Compatibility with Single Pass Stereo Rendering

V2.3:
- Additional fog materials for reflection support
- Improved scene 5 (game) with sounds
- Fixed baseline height issue with fog materials
- Added distance falloff to Desktop Fog Plus, Mobile (Simplified) and fog material shaders

V2.2:
- New fog materials for using on geometry (see demo scene 5)
- Updated documentation

V2.0:
- New Desktop Fog Plus variant which improves fog effect
- New custom inspector

V1.8:
- Added Sun property to assign a directional light and implement day light cycle

V1.7:
- Enhanced fog blending for desktop variant
- Fog is now clipped above fog height, saving GPU cycles when using low fog
- Added option “Clip under baseline height”
- Fixed “fog soup” issue when using low camera far clip values

V1.6.1:
- Added compatibility with Gaia Extension System

V1.6:
- Added max distance and max distance falloff parameters.
- Added support for second color to mobile shaders.

V1.5:
- Support for custom void areas (“Fog of War”). See demo scene 4.
- New secondary color to create artistic/gradient fog effects.

V1.4:
- Compatibility with RenderTexture targets

V1.3:
- New option to specify a baseline for the height of the fog

V1.2:
- Support for fog volumes
- Improved performance (even more!)
- Fixed non-advanced fog shader to take into account new sky alpha setting

V1.1:
- Fixed opaque sky haze issue with billboard trees

V1.0
- Initial release

