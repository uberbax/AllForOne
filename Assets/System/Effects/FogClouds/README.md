# Area clouds

Scene: Game_Exp_Rumble, FogAreaActivator1/Fog/rootCloud/AnimatedClouds.

The cloud layer uses the existing Assets/!Images/mapcloud.png atlas. Its 35 animated cards are combined in one mesh. The shader confines the clouds to the local rectangle between loFog and hiFog.

Call FogActivator.Activate() to disperse the clouds outward from the player position projected into that rectangle. Duration is configurable in the FogActivator Inspector (default 2.4 seconds). Repeated calls while dispersing or cleared do nothing. ResetFog() restores the cloud layer and cancels current dispersal. ClearProgress, IsDispersing and IsCleared expose its state.

The cloud material controls density and colors. Runtime animation uses a MaterialPropertyBlock and retains the shared material. Completion disables only the generated MeshRenderer.

After moving loFog/hiFog in Edit Mode, run Tools > Effects > Build FogAreaActivator1 Clouds to rebuild the mesh bounds. The command saves the scene automatically only if it had no unsaved changes before building.
