# VR2Gather - Building the application

> xxxjack This session still needs to be written. Because it is likely to change in the near future.
> 
> This section should also explain the CI/CD.


Here is old stuff from the toplevel readme:


## Quick build instructions

- check out the repository.
- install Unity, preferably Unity Hub.
- Install prerequisites:
	- cwipc from <https://github.com/cwi-dis/cwipc>
	- (optionally) bin2dash and sub (to be provided)
	- probably more that I forgot
- open the toplevel directory in Unity.
- select `LoginManager` scene
- play

## Re-installing nuget packages

Mainly ffmpeg. Check whether newer versions are available on `nuget.org`. Do the following steps:

- Update toplevel `packages.config`
- Run `nuget restore`
- `git rm` and `git add` of the subdirectories in `Assets/packages`.

## Github Actions

- Needs a license. See <https://game.ci/docs/github/activation> for how to get one and install it.
