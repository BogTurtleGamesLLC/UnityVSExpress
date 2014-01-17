UnityVSExpress
==============

This is a simple application that acts as a middleman between Unity and Visual Studio Express for C# editing.

It allows the user to double-click on files in Unity (or errors) and have the file be opened in Visual Studio Express at the correct line number.

To install this application as your Unity editor:

1. Place the compiled UnityVSExpress.exe somewhere.

2. In Unity, select Edit&#8594;Preferences&#8594;External Tools&#8594;External Script Editor.

3. Browse for and select UnityVSExpress.exe.

4. In Unity, fill out the External Script Editor Args field with:

    ```
    "$(File)" $(Line)
    ```

    The default Visual Studio Express year to run is 2010, i.e., Visual C# Express 2010. If you would like to run a different year, you can add that as a third parameter:

    ```
    "$(File)" $(Line) 2013
    ```

    In this case, Unity would run Visual Studio Express 2013, i.e., Windows Desktop Express 2013.

5. Make sure that by default .cs files are opened with the Visual Studio Express version you want.
To do this, find a .cs file, right click on it, select Open with&#8594;Choose default program...
Then select the version of Visual Studio Express that you are using for Unity scripts.

If you are having problems...
-----------------------------

1. From within Unity, select Assets&#8594;Sync MonoDevelop Project. Then try again.

2. Close all instances of Visual Studio Express. Then try again.

3. Recheck the syntax for the External Script Editor Args field.

4. Make sure the External Script Editor Args field is specifying the Visual Studio Express year that you would like to run.

5. Make sure that by default .cs files are opened with this same Visual Studio Express version.

If you like or use this application, feel free to check out [some of our games](http://www.bogturtlegames.com).
