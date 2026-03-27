# Rebound

Throw something. Press grip. It comes back.

Rebound is a Blade & Sorcery PC mod that lets you recall thrown items back to your hand with an arc, some spin, and just enough drama.

## What it does

- Lets you throw an item and call it back
- Returns it to whichever empty hand you use to recall it
- Adds arc motion instead of a boring straight-line snap
- Tries to orient the handle so the catch feels clean
- Exposes a few tuning options in mod settings

## How to use it

1. Throw an item hard enough
2. Leave one hand empty
3. Press grip on that hand
4. Catch the item when it comes back

That is it.

## Mod options

Current options include:

- minimum throw velocity
- return speed
- arc strength
- weighted feel

If it feels too floaty or too stiff, that is where to start.

## Install

Drop the `Rebound` folder into:

`BladeAndSorcery_Data/StreamingAssets/Mods/`

This is for Blade & Sorcery PC.

## Build from source

Set `GAME_FOLDER` in `.env`, then run:

```bat
build.bat
```

The build script outputs a ready-to-install mod folder.

## Notes

- Right now this tracks one returning item at a time
- If an item gets grabbed again, tracking clears
- If you are testing feel, start with return speed and arc strength

## Discord

If you want updates, feedback, or bug reports, join the Discord:

[discord.gg/WQ628mcr4w](https://discord.gg/WQ628mcr4w)
