# FlickrExtractor
Quick and dirty project to extract all of a user's Flickr photos to a directory on the local file system.

This isn't designed for mainstream use, I designed it for a one-shot extraction of my photos from Flickr. I'm sharing in case anyone else may have a use for it. Feel free to do what you like with it.

As it stands, it should probably be run from within Visual Studio.

## Instructions

Once you have downloaded the source code, open it in Visual Studio (It was created with Visual Studio 2017 community edition).

Alter the app.config file and update the three values in the `appSettings` section.

* **location** : The folder the data will be placed into. Please note the application also requires a sub-folder called "photos" to be created in advance.
* **apiKey** : The API key you got from Flickr when registister you app. https://www.flickr.com/services/apps/create/apply/
* **sharedSecret**: The Shared Secret you get when you register your app.

When you run the application for the first time you will need to authenticate yourself with Flickr. It will launch a web browser and you can authorise the app to read your account. Whe it is finished it will give you a code that you need to type into the application. When you do this and your user account it authorised to use the application then it will store the token it received in a JSON file in the location given in the config file.

If you run the application subsequent times it will use the token it stored earlier.
