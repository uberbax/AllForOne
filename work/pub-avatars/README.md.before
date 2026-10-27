# Pub

Prefab: Assets/System/UI/Pub/Prefabs/UI_Pub.prefab
Scene: Assets/Scenes/Game_Exp_Rumble.unity
Panel: Canvas/UI_Pub (closed by default)
Entry button: Canvas/Button_Pub

The panel reuses Theme_Dark fonts, frames, buttons and icons already used by the project. PubPortraits.asset contains cropped Sprite subassets referencing existing avatar textures; source sprites/textures are not modified.

Balance is exposed on PubController: recruitPrice=100 gold per attempt, refreshPrice=25 gold, refreshMinutes=30, bonusEvery=20. 1x/2x/3x perform independent equal-chance rolls from the current three distinct offers. Each attempt grants one individual unequipped companion; every 20 attempts grants an extra companion. Rewards go into the main player's existing inventory; nothing is spawned in the world.

The pool comes from ConfigLoader.heroes (Heroes) and corresponding DatabaseAll.heroes definitions. IDs beginning with hero_ or tree are excluded case-insensitively. Invalid/missing definitions are omitted; fewer than three valid heroes are handled without duplicate offers.

Offers, UTC refresh deadline and recruitment progress live in existing PlayerData.playerStats entries: pub_offer_0..2, pub_next_refresh, pub_recruits_total. Saves keep the existing player key/format and respect MainStates.useSave. Reopening the panel preserves a valid lineup. Automatic and paid refreshes restart the 30-minute timer.

Currency and inventory capacity, including bonus rewards, are checked before payment. Multiple gold stacks are supported. Hero creation failure rolls back new MainStates.all registrations and does not charge gold. Recruitment commits before the reveal animation; closing during reveal keeps granted rewards. UI actions are locked while revealing.

Build/rebuild: Tools > UI > Pub > Build and Place in Canvas. An already-dirty scene is left unsaved. Public runtime entry points: PubController.Show(), Hide(), Recruit(), InstantRefresh(), SelectMultiplier(int).

Validation completed:
- Full project C# compile.
- Isolated transaction/filter/save model checks.
- Native Unity Play Mode: 76 eligible heroes, 10 excluded rows; real 3x hero creation, correct ownership and unequipped state, gold charges, bonus at 20, paid refresh, offer restore, prefab bindings/portraits, double-click lock, closing during reveal. Original player/data restored after the test.
- Native preview rendered to work/pub/pub-preview.png.
