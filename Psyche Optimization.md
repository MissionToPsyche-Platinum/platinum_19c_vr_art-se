Check for any unneeded Update() calls.

The quickest way to check is to make sure that anything that is being updated on a frame by frame basis is actually needing to be updated that often.

Scan for any poorly optimized colliders.

Scan for any poorly optimized object references or issues or potential leftover artifacts from previous runs.

Scan the memory profiler and see what is using the most memory.

See if there is a way to improve the quality of the images.
    This might be a stretch, but it’s worth a shot

Reoptimize the collision handler 
    I expect that this is causing some of the issues with the lag. There are a lot of wasted calls for things that really have no relevance to the overall system.

Double check to ensure that any assets being loaded are fully loaded by the time the museum is ready for the player to enter it.

Clean up any un-used scripts
    I seriously doubt this is causing any issues, but worth a shot.

Occlusion Culling
    This is the one I am most iffy on, just because I have no idea if it will cause issues with the way we currently call stuff. If it breaks things, it is the first to go.

Sources:
https://docs.unity3d.com/6000.0/Documentation/Manual/profiler-introduction.html
https://medium.com/@lemapp09/beginning-game-development-vr-performance-optimization-78553530ca83
