# Cobalt Core Archipelago

A Cobalt Core mod for [the Archipelago multi-game randomizer system](https://archipelago.gg/).

- Version: 1.0.0
- Archipelago version: 0.6.5
- Cobalt Core version: 1.2.8
- Nickel Version: 1.20.3
- Nickel API Version: 1.5.7

The mod may work with other versions, but has not been tested on them.

## Current Status (January 2026)

Just released! I have tested it on my own with some friends but there may still be
some bugs I couldn't find yet. If you encounter any issues while playing,
please ping me on Discord (`saltyisaac`) in the Cobalt Core channel on the
[Archipelago Server](https://discord.gg/8Z65BR2), or make an issue on this
repository. (Please ensure that no one has reported it yet, to the best
of your ability!)

I am also looking for feedback on how much fun the mod is to play, balance details
and other kinds of suggestions. Look at the [Planned Features](#planned-features-no-promises)
and [Known Issues](#known-issues) sections for more details.

I mostly did this for fun, so I make no promises as to how responsive I will be
nor how long I will keep working on this mod. If I were to become inactive, I give
my blessing to anyone who wants to fork the repository.


## What is an "Archipelago Randomizer", and why would I want one?

Let's say I'm playing Cobalt Core, and my friend is playing Ocarina of Time.
I may encounter a card saying it unlocks my friend's Hookshot. When I play it,
it allows them to reach several OoT chests they couldn't before.
In one of those chests they find Drake, meaning I can now select her when starting
a run and find new cards and artifacts I couldn't before.
One of these artifacts unlocks my friend's Ocarina when picked up.
This continues until we both find enough of our items to finish our games.

In essence, a multi-game randomizer system like Archipelago allows a group of
friends to each bring whatever games they want (if they have an Archipelago mod)
and smush them all together into one big cooperative multiplayer experience.

### What this mod changes

On default settings, you will start with three randomly chosen characters and
a randomly chosen ship. Starting cards for each character are randomized as well.
Aside from the starting cards, most of the cards and artifacts you will find will
be unplayable until they are unlocked by you or another player.

Usual character and ship unlock conditions are removed.
In order to unlock ships, characters, cards, artifacts, or items for other
games, you will find special cards and artifacts which unlock an item when played.
Which items you find will depend on which characters you have in your run.

The goal is to accumulate a certain amount of memories (1 for each character by
default), which unlocks the Future Memory sequence. Finishing this sequence
completes the game.

There are more details in the YAML options file as well as in the in-game
mod settings (such as Deathlink).

## Installation

### Prerequisites and dependencies

- Make sure Cobalt Core is installed in the current version.
- Install [Nickel](https://github.com/Shockah/Nickel). You can use [this setup guide](https://github.com/Shockah/Nickel/blob/master/docs/player-guide.md).
- Run the nickel launcher at least once and close the game.
- Install the core Archipelago tools (at least version 0.6.4,
but preferably the latest version) from
[Archipelago's GitHub Releases page](https://github.com/ArchipelagoMW/Archipelago/releases).
On that page, scroll down to the "Assets" section for the release you want,
click on the appropriate installer for your system to start downloading it
(for most Windows users, that will be the file called `Setup.Archipelago.X.Y.Z.exe`),
then run it.
- Go to the [Releases page of this repository](https://github.com/Isaac-SOL/CobaltCoreArchipelagoMod/releases)
and look at the latest release.
There should be three files: A .zip, an .apworld and a .yaml.
Download all three.

### Archipelago tools setup

- Go to your Archipelago installation folder. Typically, that will be `C:\ProgramData\Archipelago`.
- Put the `Cobalt.Core.yaml` file in `Archipelago\Players`.
You may leave the `.yaml` unchanged to play on default settings, or use your
favorite text editor to read and change the settings in it.
- Double-click on the `cobalt_core.apworld` file. Archipelago should display a
popup saying it installed the apworld. Optionally, you can double-check that
there's now an `cobalt_core.apworld` file in `Archipelago\custom_worlds\`.

#### I've never used Archipelago before. How do I generate a multiworld?

Let's create a randomized "multiworld" with only a single Cobalt Core world in it.

- Make sure `Cobalt.Core.yaml` is the only file in `Archipelago\Players`
(subfolders here are fine).
- Double-click on `Archipelago\ArchipelagoGenerate.exe`. You should see a
console window appear and then disappear after a few seconds.
- In `Archipelago\output\` there should now be a file with a name similar to
`AP_95887452552422108902.zip`.
- Open https://archipelago.gg/uploads in your favorite web browser, and upload
the output .zip you just generated. Click "Create New Room".
- The room page should give you a hostname and port number to connect to, e.g.
"archipelago.gg:12345".

For a more complex multiworld, you'd put one `.yaml` file in the `\Players`
folder for each world you want to generate. You can have multiple worlds of
the same game (each with different options), as well as several different games,
as long as each `.yaml` file has a unique player/slot name. It also doesn't
matter who plays which game; it's common for one human player to play more than
one game in a multiworld.

### Installing and running the Cobalt Core mod

- Extract the .zip file you downloaded at the end of the [Prerequisites section](#prerequisites-and-dependencies)
in the `ModLibrary` folder inside your `Nickel` folder.
- Run the game using the Nickel Launcher. Cobalt Core mods only work when the 
game is run this way.
- If the mod is correctly installed, the game will always start on the save
selection screen. If it does not, there was an issue during installation.
- You cannot run already-existing saves with the Archipelago mod. These will
be marked with a crossed AP icon and will not be clickable.
Click on an empty save (delete one if necessary), and you will be prompted
for your connection info: hostname, port number, slot name and password.
- Use the hostname and port number from the previous section. Your slot name will be
CAT1 by default, but you can change it in `Cobalt.Core.yaml`. If you didn't set
a password you can leave it empty.
- Press "Connect", and, if everything went right, you should be in the main menu.
You are now connected and ready to start playing with other people!

## Planned features (no promises!)

- In-game tracker: A way to know how many cards and artifacts are left to be
found on each character
- In-game console: A way to use commands directly from in-game.
- Hint system integration: A way to hint any character, ship, card or artifact
by pressing a button.
- Actual dialogue for the additional Books and CAT memories. (If you're interested
in writing those, hit me up!)
- Additional in-combat dialogue between characters when getting new items from other players,
or unlocking items for other players. Lots of dialogue if possible!
(Also interested in writers for these!)
- Potentially additional cards or artifacts which give the game more interactivity
with the multiworld.

### Will other mods be supported?

I don't know for sure yet. If there are compatibility issues with "utility"
or "quality of life" type mods, I will try my best to fix them. However, the AP
protocol cannot support added characters or cards as a generic thing, as far as
I am aware. They would have to each be added individually. If someone can work
out a solution for this we can discuss it, but if it proves to be too big of
an undertaking I will probably not do it myself.

## Known issues

- Using Nickel's import function to import a save from the vanilla game will
immediately throw you onto the main menu without connecting to a host. This is not
supported and will probably crash quickly, but it's fine as long as you close
the game immediately.
- Sometimes, after finishing a run, you may see a notifications for new
unlocked characters or ships that aren't actually unlocked yet. This does not
affect gameplay, the unlocks work correctly, it's the notifications that are bugged.

## Credits

- Shockah for making Nickel
- rft50 for making the DemoMod that eased me into modding Cobalt Core
considering I had no idea what I was doing
- Ixrec for making the great Nine Sols AP mod whose code helped
me figure out how an AP integration is supposed to work considering I *also* had no
idea what I was doing on that front either
- Landmaster who first started a Cobalt Core Archipelago project which was never
finished but can be found [here](https://github.com/Landmaster/CobaltCoreArchipelago)
- All the people in the CC modding community who answered my many questions
- All the Archipelago contributors for making this frankly absurd project
- Everyone at Rocket Rat Games for making a very cool and cute game that is not only
very fun to play but also very fun to mod
