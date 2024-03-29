# Unity-Stuff
Different unity scripts and functions created by me at one place. Readme contains description of all scripts in this repository.

<br>

***

<br>

## Network/MulticastDiscovery.cs
UDP Multicast discovery service. Class with similar function like Unity NetworkDiscovery component, but my works with multicast, not a broadcast. It's helpful if you have more network adapters.

<br>

***

<br>

## File/Share.cs
## File/Editor/ShareInspector.cs

### Usage
You need to create GameObject in scene and add FileShare.cs script as component. Then you can call it from other scripts.

```c#
FileShare.RegisterReceiveAction("someAction", (file) => { Debug.Log("received file: " + file); });
FileShare.Send("someFile.xml", "someAction");
```

You can set target directory for received files. For example:
```c#
FileShare.SetTempDirectory(Application.dataPath);
```

### Explanation
At the start you need action (ex. lambda function) after receive file on client side. Identifier of this action is string. Also you have a option to remove (UnregisterReceiveAction) this action. Progress of sending a file is starting with Unity message sended to all clients, to be prepared to receive file on specified port. Client (sender of file) ask server for client addresses and send the file to all of them. Each client after receiving a file invokes action by specified identifier.

### Warning
Unity message are identified by "short" value. This script is using Unity highest (47) +1 and +2. If you are using those message numbers, you can change it directly in script region "network message stuff".

While you are testing sharing files, use two computers. Windows not allowing open same port for multiple instances.

Tested on Unity 5.5 and 2017.2. Tested only on Windows.

<br>

***

<br>

## Editor/SkyboxEditor.cs
Help utility to setup 6 Sided skybox material. You can find it in top menu Window > Skybox Editor.

<br>

***

<br>

## Development/Invoker.cs
## Development/InvokerInspector.cs
Extension for Unity3D editor to have a option to invoke any method from any component script on any GameObject

One video should say more then a lot of words. Tested on Unity 5.5 and 2017.2.

[![Youtube](http://img.youtube.com/vi/JZ4mGmtQTvA/0.jpg)](http://www.youtube.com/watch?v=JZ4mGmtQTvA)

Available method argument types:
* Base types also available in array and List
  * int
  * float
  * bool
  * string
  * long
* Unity struct types
  * Vector2
  * Vector3
  * Vector4
  * Quaternion
  * Rect
  * Color
  * Bounds
  * AnimationCurve
* all classes inherited from UnityEngine.Object like GameObject, Transform, etc.

_How to render this arguments is defined in Invoker.cs, you have option to extend it with your own. Same render way is used for render method output value, if it's not void._

<br>

***

<br>

## MouseOverUI.cs
Verify if mouse cursor or any finger touch is over any UI element. It's important that UI elements needs to have turned on "Raycast Target". Static approach, just ask for property "Verify".

<br>

***

<br>

## GyroControl.cs
Script to move camera with gyro sensor (usually at mobile device). Device can have 3 different sensors to recognize rotation and not all are always installed. This script uses most used installed sensor (usually if device support auto display rotation it has this sensor).

<br>

## Thanks and support

<a href='https://ko-fi.com/Z8Z5ABMLW' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://cdn.ko-fi.com/cdn/kofi1.png?v=3' border='0' alt='Buy Me a Coffee at ko-fi.com' /></a>

