# Party management clearing

Scene: Assets/Scenes/Game_Exp_Rumble.unity. Entry: Canvas/Button_PartyCamp. Scene root: PartyCamp. Prefab: Prefabs/PartyCamp.prefab.

The clearing is placed beyond the map renderer bounds with its own orthographic camera. It instantiates full hero prefabs from ResourceHolder.monsters[id], preserving their hierarchy and idle animation. Camp instances disable gameplay scripts and physics; they never register as world companions. Animator and Spriter idle frames use unscaled time while gameplay is paused. The forest plants and campfire reuse existing project art.

PartyCampController.Show switches cameras, hides gameplay canvases/raycasters, releases the mobile joystick and pauses scaled gameplay time by default. Hide, Back, Escape, and disabling the controller restore the previous camera/UI/joystick/time state. pauseWorld is configurable in Inspector.

Equipment uses the same mainPlayer inventory and MainStates.EquipSkills/petsLoIndex..petsHiIndex convention as UI_AssignPets. Click an inventory card to assign/remove a hero, or click a camp hero's minus button to remove. Auto Assign prioritizes rarity, ascension, normal level, then attack+magic. Clear removes heroes only; skills/resources/shards are not assigned. Changes are saved when MainStates.useSave is enabled and synchronize through PartyController.RefreshParty.

Tools/UI/Party Camp/Build and Place rebuilds the prefab and places it in the active Game_Exp_Rumble scene. Tools/UI/Party Camp/Validate and Preview uses isolated temporary data and does not write player saves.
