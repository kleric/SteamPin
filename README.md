# SteamPin
Steam Tile creator for Windows 8.

Just a heads up, I never wrote anything for Windows 8 before. So if some things aren't quite right, that's probably why.

I hacked this together in about a day. Just for fun, and because I wanted something simpler than Steam Tile. The ads bothered me, and I felt the launch time was rather slow. 

How do I use this?
====================
 - Run it
 - Enter your Steam ID (not username, but your vanity URL or actual steam id)
 - Click the games you want to pin
 - Close it when you're done

How does it work?
===================
 I originally wanted to implement tiles like OblyTile, but because I didn't want to spend that much time actually making this, I opted for Secondary Tiles.
 
 - Resolve Vanity URL to Steam ID
 - Get list of owned games
 - Download banner images for owned games
 - Populate Grid View
 - Create Secondary Tile on click

Then when launched with a Secondary Tile it checks for an argument (the App ID), and if it is there it tries to launch the game through the Steam URL (steam://runapp).

The launching is a bit hacky, as I was having some trouble with actually getting the game to start. So that just continuously tries to start the game until it launches. 


