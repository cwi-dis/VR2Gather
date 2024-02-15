# VR2Gather - Comparison to standard Unity

In this section we will try to give a very quick overview in how using VR2Gather for development differs to standard Unity practices for VR experiences. It is intended to be read in parallel to [Prefabs](04-prefabs.md), or before, or after.

VR2Gather is used to create shared experiences, and the intention is that all users share the same experience. So, if user _Jack_ presses a switch that makes a light turn on, a user _Jill_ in the same session should also see the light turn on.

And if user _Jill_ moves to a different location user _Jack_ should see the representation of user _Jill_ moving. No matter whether they go down a hill or not.

The low-level implementation of this is passing messages between the currently running instances within the session, and is handled by the orchestrator code, but there are a few things that you need to implement differently from standard Unity.

## Implementing user actions

Normally in Unity, you connect to be provided.

## VRRig and P\_Player\_self

to be provided

## P_Player

to be provided

## Scene changes

to be provided


