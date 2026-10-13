using RPGCharacterAnims.Actions;
using RPGCharacterAnims.Extensions;
using RPGCharacterAnims.Lookups;
using UnityEngine;
#if !UNITY_EDITOR
using UnityEngine.SceneManagement;
#endif
#if ENABLE_INPUT_SYSTEM
// Requires installing the InputSystem Package from the Package Manager: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.5/manual/Installation.html
using UnityEngine.InputSystem;
#endif

namespace RPGCharacterAnims
{
    public class GUIControls : MonoBehaviour
    {
	    public int dxGui = 0;
	    public int dyGui = 0;
	    
	    
        private RPGCharacterController rpgCharacterController;
        private RPGCharacterWeaponController rpgCharacterWeaponController;
        private float charge;
		private float idleStatic;
        private bool useHips;
        private bool useDual;
        private bool useInstant;
        private bool isTalking;
        private bool isAiming;
        private bool useNavigation;
        private float swimTimeout;
        private Vector3 jumpInput;
        public GameObject nav;

        public enum Button
        {
            AttackL, AttackR, Attack, AttackDual, SpecialAttack1, EndSpecial,
            LeftKick, LeftKick2, RightKick, RightKick2, GetHit, Knockback1, Knockback2, Knockdown,
            RollForward, RollBackward, RollLeft, RollRight, DodgeLeft, DodgeRight, DodgeBack,
            TurnLeft, TurnRight, TurnLeft180, TurnRight180, DiveRoll,
            CastAtkLeft, CastAtkRight, CastAtkDual, CastAOE, CastBuff, CastSummon, StopCasting,
            Jump, JumpFlip, Sleep, Sit, Drink, Bow, Yes, No, Pickup, Activate, Boost, Stand,
            StartTalking, StopTalking, ClimbLadder, SwimUp, SwimDown, Crawl,
            Death, Revive, DeathRevive, Relax, Unarmed, SheathLeft, SheathRight,
            DebugController, DebugAnimator, ChangeMap
        }

        public enum ToggleControl { Navigation, Sprint, Crouch, Block, HipShooting, Hips, Instant }

        private bool EnsureController()
        {
            if (rpgCharacterController == null) rpgCharacterController = GetComponent<RPGCharacterController>();
            if (rpgCharacterWeaponController == null) rpgCharacterWeaponController = GetComponent<RPGCharacterWeaponController>();
            return rpgCharacterController != null;
        }

        private bool StartControlAction(string action, object context = null)
        {
            return rpgCharacterController.HandlerExists(action) && rpgCharacterController.TryStartAction(action, context);
        }

        private bool EndControlAction(string action)
        {
            return rpgCharacterController.HandlerExists(action) && rpgCharacterController.TryEndAction(action);
        }

        private bool CanUseGroundControls => !useNavigation && !rpgCharacterController.isDead
            && !rpgCharacterController.isSwimming && !rpgCharacterController.isCasting
            && rpgCharacterController.maintainingGround;

        public void CrossFade(string anim, float tm = 0.1f)
        {
	        if (anim == "attack")
		        PressButton(Button.AttackL);
        }
        
        /// <summary>Executes a demo button immediately, without an OnGUI event. False means unavailable.</summary>
        public bool PressButton(Button button)
        {
            if (!EnsureController()) return false;
            var c = rpgCharacterController;
            // These controls are also available outside the normal grounded panel.
            switch (button)
            {
                case Button.DebugController: c.DebugController(); return true;
                case Button.DebugAnimator:
                    if (c.animator == null) return false;
                    c.animator.DebugAnimatorParameters(); return true;
                case Button.ChangeMap:
                    #if !UNITY_EDITOR
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex == 0 ? 1 : 0);
                    return true;
                    #else
                    return false;
                    #endif
                case Button.Death:
                case Button.Revive:
                case Button.DeathRevive:
                    if (useNavigation || c.isClimbing || c.isCasting || c.isSitting || !c.maintainingGround) return false;
                    bool revive = button == Button.Revive || (button == Button.DeathRevive && c.isDead);
                    return revive ? EndControlAction(HandlerTypes.Death) : StartControlAction(HandlerTypes.Death);
                case Button.SwimUp: return Swim(Vector3.up);
                case Button.SwimDown: return Swim(Vector3.down);
                case Button.Jump: return Jump(HandlerTypes.Jump);
                case Button.JumpFlip: return Jump(HandlerTypes.DoubleJump);
                case Button.StopCasting:
                    if (useNavigation || !c.isCasting) return false;
                    bool ended = EndControlAction(HandlerTypes.Cast);
                    return EndControlAction(HandlerTypes.AttackCast) || ended;
            }
            if (!CanUseGroundControls) return false;
            switch (button)
            {
                case Button.AttackL:
                    return (c.hasLeftWeapon || (c.leftWeapon == Weapon.Unarmed && c.rightWeapon == Weapon.Unarmed))
                        && StartControlAction(HandlerTypes.Attack, new AttackContext("Attack", Side.Left));
                case Button.AttackR:
                    return (c.hasRightWeapon || (c.leftWeapon == Weapon.Unarmed && c.rightWeapon == Weapon.Unarmed))
                        && StartControlAction(HandlerTypes.Attack, new AttackContext("Attack", Side.Right));
                case Button.Attack:
                    return c.hasTwoHandedWeapon && StartControlAction(HandlerTypes.Attack, new AttackContext("Attack", Side.None));
                case Button.AttackDual:
                    return c.hasDualWeapons && c.rightWeapon != Weapon.RightItem && c.leftWeapon != Weapon.LeftItem
                        && c.leftWeapon != Weapon.Shield && StartControlAction(HandlerTypes.Attack, new AttackContext("Attack", Side.Dual));
                case Button.SpecialAttack1:
                    if (c.hasTwoHandedWeapon && !c.hasAimedWeapon)
                        return StartControlAction(HandlerTypes.Attack, new AttackContext("Special", Side.None));
                    return (c.leftWeapon == Weapon.LeftSword || c.leftWeapon == Weapon.LeftMace)
                        && (c.rightWeapon == Weapon.RightSword || c.rightWeapon == Weapon.RightMace)
                        && StartControlAction(HandlerTypes.Attack, new AttackContext("Special", Side.Right));
                case Button.EndSpecial: return c.isSpecial && EndControlAction(HandlerTypes.Attack);
                case Button.LeftKick: return StartControlAction(HandlerTypes.Attack, new AttackContext("Kick", Side.Left, (int)KickType.LeftKick1));
                case Button.LeftKick2: return StartControlAction(HandlerTypes.Attack, new AttackContext("Kick", Side.Left, (int)KickType.LeftKick2));
                case Button.RightKick: return StartControlAction(HandlerTypes.Attack, new AttackContext("Kick", Side.Right, (int)KickType.RightKick1));
                case Button.RightKick2: return StartControlAction(HandlerTypes.Attack, new AttackContext("Kick", Side.Right, (int)KickType.RightKick2));
                case Button.GetHit: return !c.isCrawling && StartControlAction(HandlerTypes.GetHit, new HitContext());
                case Button.Knockback1: return !c.isCrawling && StartControlAction(HandlerTypes.Knockback, new HitContext((int)KnockbackType.Knockback1, Vector3.back));
                case Button.Knockback2: return !c.isCrawling && StartControlAction(HandlerTypes.Knockback, new HitContext((int)KnockbackType.Knockback2, Vector3.back));
                case Button.Knockdown: return !c.isCrawling && StartControlAction(HandlerTypes.Knockdown, new HitContext((int)KnockdownType.Knockdown1, Vector3.back));
                case Button.RollForward: return StartControlAction(HandlerTypes.Roll, RollType.Forward);
                case Button.RollBackward: return StartControlAction(HandlerTypes.Roll, RollType.Backward);
                case Button.RollLeft: return StartControlAction(HandlerTypes.Roll, RollType.Left);
                case Button.RollRight: return StartControlAction(HandlerTypes.Roll, RollType.Right);
                case Button.DodgeLeft: return StartControlAction(HandlerTypes.Dodge, DodgeType.Left);
                case Button.DodgeRight: return StartControlAction(HandlerTypes.Dodge, DodgeType.Right);
                case Button.DodgeBack: return StartControlAction(HandlerTypes.Dodge, DodgeType.Backward);
                case Button.TurnLeft: return StartControlAction(HandlerTypes.Turn, TurnType.Left);
                case Button.TurnRight: return StartControlAction(HandlerTypes.Turn, TurnType.Right);
                case Button.TurnLeft180: return StartControlAction(HandlerTypes.Turn, TurnType.Left180);
                case Button.TurnRight180: return StartControlAction(HandlerTypes.Turn, TurnType.Right180);
                case Button.DiveRoll: return StartControlAction(HandlerTypes.DiveRoll, DiveRollType.DiveRoll1);
                case Button.CastAtkLeft: return CastAttack(Side.Left);
                case Button.CastAtkRight: return CastAttack(Side.Right);
                case Button.CastAtkDual: return CastAttack(Side.Dual);
                case Button.CastAOE: return CanCastSpell() && StartControlAction(HandlerTypes.Cast, new CastContext(AnimationVariations.AOE.TakeRandom(), Side.Dual));
                case Button.CastBuff: return CanCastSpell() && StartControlAction(HandlerTypes.Cast, new CastContext(AnimationVariations.Buff.TakeRandom(), Side.Dual));
                case Button.CastSummon: return CanCastSpell() && StartControlAction(HandlerTypes.Cast, new CastContext(AnimationVariations.Summon.TakeRandom(), Side.Dual));
                case Button.Sleep: return PlayEmote(EmoteType.Laydown, false);
                case Button.Sit: return PlayEmote(EmoteType.Sit, false);
                case Button.Drink: return PlayEmote(EmoteType.Drink, false);
                case Button.Bow: return PlayEmote(EmoteType.Bow1, false);
                case Button.Yes: return PlayEmote(EmoteType.Yes, false);
                case Button.No: return PlayEmote(EmoteType.No, false);
                case Button.Pickup: return PlayEmote(EmoteType.Pickup, false) || PlayEmote(EmoteType.Pickup, true);
                case Button.Activate: return PlayEmote(EmoteType.Activate, false) || PlayEmote(EmoteType.Activate, true);
                case Button.Boost: return PlayEmote(EmoteType.Boost, true);
                case Button.Stand: return c.isSitting && EndControlAction(HandlerTypes.Emote);
                case Button.StartTalking: return StartControlAction(HandlerTypes.Talk, AnimationVariations.Conversations.TakeRandom());
                case Button.StopTalking: return c.isTalking && EndControlAction(HandlerTypes.Talk);
                case Button.ClimbLadder: return StartControlAction(HandlerTypes.ClimbLadder);
                case Button.Crawl:
                    if (c.isCrawling) return StartControlAction(HandlerTypes.Crouch) && StartControlAction(HandlerTypes.Idle);
                    if (!c.isCrouching || !c.HandlerExists(HandlerTypes.Crawl) || !c.CanStartAction(HandlerTypes.Crawl)) return false;
                    EndControlAction(HandlerTypes.Crouch);
                    return StartControlAction(HandlerTypes.Crawl);
                case Button.Relax:
                    return CanSwitchWeapon() && !c.isRelaxed && StartControlAction(HandlerTypes.Relax, useInstant ? (object)true : null);
                case Button.Unarmed: return EquipWeapon(Weapon.Unarmed);
                case Button.SheathLeft: return c.leftWeapon.HasEquippedWeapon() && SwitchWeapon(new SwitchWeaponContext { type = "Sheath", side = "Left", leftWeapon = Weapon.Unarmed, rightWeapon = Weapon.Relax });
                case Button.SheathRight: return c.rightWeapon.HasEquippedWeapon() && SwitchWeapon(new SwitchWeaponContext { type = "Sheath", side = "Right", leftWeapon = Weapon.Relax, rightWeapon = Weapon.Unarmed });
                default: return false;
            }
        }

        private bool CastAttack(Side side)
        {
            var c = rpgCharacterController;
            return (side == Side.Right || c.leftWeapon == Weapon.Unarmed)
                && (side == Side.Left || c.rightWeapon == Weapon.Unarmed)
                && c.HandlerExists(HandlerTypes.Cast) && c.CanStartAction(HandlerTypes.Cast)
                && StartControlAction(HandlerTypes.AttackCast, new AttackCastContext(AnimationVariations.AttackCast.TakeRandom(), side));
        }

        private bool CanCastSpell() => !rpgCharacterController.hasDualWeapons
            && (rpgCharacterController.leftWeapon.IsCastableWeapon() || rpgCharacterController.rightWeapon.IsCastableWeapon());

        private bool PlayEmote(EmoteType emote, bool combat)
        {
            if (!EnsureController() || !CanUseGroundControls) return false;
            if (combat && (emote == EmoteType.Pickup || emote == EmoteType.Activate) && rpgCharacterController.hasDualWeapons) return false;
            return StartControlAction(combat ? HandlerTypes.EmoteCombat : HandlerTypes.Emote, emote);
        }

        private bool Jump(string action)
        {
            if (useNavigation || rpgCharacterController.isDead || rpgCharacterController.isSwimming
                || !rpgCharacterController.HandlerExists(action)
                || !rpgCharacterController.CanStartAction(action)) return false;
            rpgCharacterController.SetJumpInput(Vector3.up);
            return StartControlAction(action);
        }

        private bool Swim(Vector3 direction)
        {
            if (useNavigation || !rpgCharacterController.isSwimming) return false;
            swimTimeout = Time.time + 0.5f;
            jumpInput = direction;
            var legacy = GetComponent<RPGCharacterInputController>();
            if (legacy != null) legacy.PauseInput(0.5f);
            #if ENABLE_INPUT_SYSTEM
            var inputSystem = GetComponent<RPGCharacterInputSystemController>();
            if (inputSystem != null) inputSystem.PauseInput(0.5f);
            #endif
            rpgCharacterController.SetJumpInput(direction);
            return true;
        }

        /// <summary>Sets a toggle to the requested value; repeated calls do not flip the action.</summary>
        public bool SetToggle(ToggleControl toggle, bool value)
        {
            if (!EnsureController()) return false;
            var c = rpgCharacterController;
            switch (toggle)
            {
                case ToggleControl.Hips: useHips = value; return true;
                case ToggleControl.Instant: useInstant = value; return true;
                case ToggleControl.Navigation:
                    if (value && (nav == null || !c.HandlerExists(HandlerTypes.Navigation) || c.isCasting || !c.maintainingGround)) return false;
                    if (!value && c.isNavigating && !EndControlAction(HandlerTypes.Navigation)) return false;
                    useNavigation = value;
                    if (nav != null) foreach (var renderer in nav.GetComponentsInChildren<MeshRenderer>(true)) renderer.enabled = value;
                    return true;
                case ToggleControl.Sprint:
                    if (value && (!CanUseGroundControls || !c.hasNoWeapon || c.isCrouching)) return false;
                    return SetActionToggle(HandlerTypes.Sprint, value);
                case ToggleControl.Crouch:
                    if (value && (!CanUseGroundControls || c.isSprinting)) return false;
                    return SetActionToggle(HandlerTypes.Crouch, value);
                case ToggleControl.Block:
                    if (value && (!CanUseGroundControls || c.isSpecial || c.isSitting || c.IsActive(HandlerTypes.Relax))) return false;
                    return SetActionToggle(HandlerTypes.Block, value);
                case ToggleControl.HipShooting:
                    if (value && !CanSwitchWeapon()) return false;
                    return SetActionToggle(HandlerTypes.HipShoot, value);
                default: return false;
            }
        }

        private bool SetActionToggle(string action, bool value)
        {
            if (!rpgCharacterController.HandlerExists(action)) return false;
            if (rpgCharacterController.IsActive(action) == value) return true;
            return value ? StartControlAction(action) : EndControlAction(action);
        }

        public bool GetToggle(ToggleControl toggle)
        {
            if (!EnsureController()) return false;
            switch (toggle)
            {
                case ToggleControl.Navigation: return useNavigation;
                case ToggleControl.Sprint: return rpgCharacterController.isSprinting;
                case ToggleControl.Crouch: return rpgCharacterController.isCrouching;
                case ToggleControl.Block: return rpgCharacterController.isBlocking;
                case ToggleControl.HipShooting: return rpgCharacterController.isHipShooting;
                case ToggleControl.Hips: return useHips;
                case ToggleControl.Instant: return useInstant;
                default: return false;
            }
        }

        public bool Toggle(ToggleControl toggle) => SetToggle(toggle, !GetToggle(toggle));

        private void DrawToggle(Rect rect, ToggleControl toggle, string label)
        {
            bool before = GetToggle(toggle);
            bool after = GUI.Toggle(rect, before, label);
            if (before != after) SetToggle(toggle, after);
        }

        /// <summary>Selects the same weapon as a single weapon button in the demo.</summary>
        public bool EquipWeapon(Weapon weapon)
        {
            if (!EnsureController() || !CanSwitchWeapon()) return false;
            var context = new SwitchWeaponContext { type = "Switch" };
            if (weapon == Weapon.Unarmed)
            {
                if (rpgCharacterController.hasNoWeapon) return false;
                context.side = "Dual"; context.leftWeapon = Weapon.Unarmed; context.rightWeapon = Weapon.Unarmed;
            }
            else if (System.Array.IndexOf(WeaponGroupings.TwoHandedWeapons, weapon) >= 0)
            {
                if (rpgCharacterController.rightWeapon == weapon) return false;
                context.side = "None"; context.leftWeapon = Weapon.Relax; context.rightWeapon = weapon;
            }
            else if (System.Array.IndexOf(WeaponGroupings.LeftHandedWeapons, weapon) >= 0)
            {
                if (rpgCharacterController.leftWeapon == weapon) return false;
                context.side = "Left"; context.leftWeapon = weapon; context.rightWeapon = Weapon.Relax;
            }
            else if (System.Array.IndexOf(WeaponGroupings.RightHandedWeapons, weapon) >= 0)
            {
                if (rpgCharacterController.rightWeapon == weapon) return false;
                context.side = "Right"; context.leftWeapon = Weapon.Relax; context.rightWeapon = weapon;
            }
            else return false;
            return SwitchWeapon(context);
        }

        public bool EquipDualWeapons(Weapon leftWeapon, Weapon rightWeapon)
        {
            if (!EnsureController() || !CanSwitchWeapon() || leftWeapon == Weapon.Shield) return false;
            bool valid = false;
            foreach (var pair in WeaponGroupings.LeftRightWeaponPairs)
                if (pair.Item1 == leftWeapon && pair.Item2 == rightWeapon) valid = true;
            if (!valid || (rpgCharacterController.leftWeapon == leftWeapon && rpgCharacterController.rightWeapon == rightWeapon)) return false;
            return SwitchWeapon(new SwitchWeaponContext { type = "Switch", side = "Dual", leftWeapon = leftWeapon, rightWeapon = rightWeapon });
        }

        private bool CanSwitchWeapon() => CanUseGroundControls && !rpgCharacterController.isBlocking
            && !rpgCharacterController.isCrawling && !rpgCharacterController.isSpecial;

        private bool SwitchWeapon(SwitchWeaponContext context)
        {
            if (!CanSwitchWeapon()) return false;
            context.sheathLocation = useHips ? "Hips" : "Back";
            if (useInstant) context.type = "Instant";
            return StartControlAction(HandlerTypes.SwitchWeapon, context);
        }

        public bool SetCharge(float value)
        {
            if (!EnsureController() || !CanUseGroundControls || !rpgCharacterController.hasShield || rpgCharacterController.animator == null) return false;
            charge = Mathf.Clamp01(value);
            rpgCharacterController.animator.SetFloat(HandlerTypes.Charge, charge);
            return true;
        }

        public bool SetIdleAmount(float value)
        {
            if (!EnsureController() || !CanUseGroundControls || rpgCharacterController.animator == null) return false;
            idleStatic = Mathf.Clamp01(value);
            rpgCharacterController.animator.SetFloat(AnimationParameters.Idle, idleStatic);
            return true;
        }

        public bool NavigateTo(Vector3 destination)
        {
            if (!EnsureController() || !useNavigation) return false;
            return StartControlAction(HandlerTypes.Navigation, destination);
        }

        private void Update()
        {
            if (!EnsureController()) return;
            if (swimTimeout > 0f)
            {
                if (Time.time < swimTimeout && rpgCharacterController.isSwimming) rpgCharacterController.SetJumpInput(jumpInput);
                else { swimTimeout = 0f; rpgCharacterController.SetJumpInput(Vector3.zero); }
            }
            if (!useNavigation || nav == null || rpgCharacterController.isCasting || !rpgCharacterController.maintainingGround) return;
            var mainCamera = Camera.main;
            if (mainCamera == null) return;
            #if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null) return;
            var screenPoint = Mouse.current.position.ReadValue();
            bool clicked = Mouse.current.leftButton.wasPressedThisFrame;
            #else
            var screenPoint = Input.mousePosition;
            bool clicked = Input.GetMouseButtonDown(0);
            #endif
            if (Physics.Raycast(mainCamera.ScreenPointToRay(screenPoint), out RaycastHit hit, 100))
            {
                nav.transform.position = hit.point;
                if (clicked) NavigateTo(hit.point);
            }
        }


        private void Awake()
        {
            EnsureController();
        }

        private void OnGUI()
        {
            if (!EnsureController()) { return; }
			// Allow Navigation if character is not Casting and is on the ground.
			if (!rpgCharacterController.isCasting && rpgCharacterController.maintainingGround) { Navigation(); }

			// Character is not using Navmesh Navigation.
			if (!useNavigation) {

				// Character is dead.
				if (rpgCharacterController.isDead) {
					Misc();
					return;
				}
				// Character is swimming.
				if (rpgCharacterController.isSwimming) {
					Swimming();
					return;
				}
				// Character is not on the ground.
				if (!rpgCharacterController.maintainingGround) {
					Jumping();
					return;
				}
				// Character is not Casting.
				if (!rpgCharacterController.isCasting) {
					Idle();
					Charging();
					Emotes();
					Attacks();
					RollDodgeTurn();

					// Character is not Crawling, show Damage UI.
					if (!rpgCharacterController.isCrawling) { Damage(); }

					// Character is not doing Special Attack or Crawling, show Weapon Switching UI.
					if (!rpgCharacterController.isBlocking && !rpgCharacterController.isCrawling) { WeaponSwitching(); }
				}
				// Character is not doing Special Attack or Crawling, show Blocking UI.
				if (!rpgCharacterController.isSpecial && !rpgCharacterController.isCrawling) { Blocking(); }

				Climbing();
				Casting();
			}

			// Character is not Casting.
			if (!rpgCharacterController.isCasting) {
				if (rpgCharacterController.hasNoWeapon && !rpgCharacterController.isCrouching) { Sprinting(); }
				if (!rpgCharacterController.isSprinting) Crouching();
			}

			DebugRPGCharacter();
			ChangeMap();
		}

        private void Charging()
        {
			// Only available if character has shield.
	        if (!rpgCharacterController.hasShield) { return; }
	        GUI.Button(new Rect(dxGui + 620, dyGui + 140, 100, 30), "Charge");
	        charge = GUI.HorizontalSlider(new Rect(dxGui + 620, dyGui + 170, 100, 30), charge, 0.0F, 1f);
	        SetCharge(charge);
        }

		private void Idle()
		{
			GUI.Button(new Rect(dxGui + 540, dyGui + 140, 60, 30), "Idle");
			idleStatic = GUI.HorizontalSlider(new Rect(dxGui + 540, dyGui + 170, 60, 30), idleStatic, 0.0F, 1f);
			SetIdleAmount(idleStatic);
		}

		private void Navigation()
        {
            if (nav != null && rpgCharacterController.HandlerExists(HandlerTypes.Navigation))
                DrawToggle(new Rect(dxGui + 550, dyGui + 105, 100, 30), ToggleControl.Navigation, "Navigation");
        }

		private void Attacks()
		{
			// Check if Attack Action exists.
			if (!rpgCharacterController.HandlerExists(HandlerTypes.Attack)) { return; }

			// End special attack.
			if (rpgCharacterController.CanEndAction(HandlerTypes.Attack) && rpgCharacterController.isSpecial) {
				if (GUI.Button(new Rect(dxGui + 235, dyGui + 85, 100, 30), "End Special"))
				{ PressButton(Button.EndSpecial); }
			}
			// Check if can start Attack Action.
			if (!rpgCharacterController.CanStartAction(HandlerTypes.Attack)) { return; }

			// Left weapon.
			if (rpgCharacterController.hasLeftWeapon
				|| (rpgCharacterController.leftWeapon == Weapon.Unarmed && rpgCharacterController.rightWeapon == Weapon.Unarmed)) {
				if (GUI.Button(new Rect(dxGui + 25, dyGui + 85, 100, 30), "Attack L"))
				{ PressButton(Button.AttackL); }
			}
			// Right weapon.
			if (rpgCharacterController.hasRightWeapon
				|| (rpgCharacterController.rightWeapon == Weapon.Unarmed && rpgCharacterController.leftWeapon == Weapon.Unarmed)) {
				if (GUI.Button(new Rect(dxGui + 130, dyGui + 85, 100, 30), "Attack R"))
				{ PressButton(Button.AttackR); }
			}
			// Two-handed weapon.
			if (rpgCharacterController.hasTwoHandedWeapon) {
				if (GUI.Button(new Rect(dxGui + 130, dyGui + 85, 100, 30), "Attack"))
				{ PressButton(Button.Attack); }
			}
			// Dual weapons.
			if (rpgCharacterController.hasDualWeapons) {

				// Can't Dual Attack with Item weapons or with a Shield.
				if (rpgCharacterController.rightWeapon != Weapon.RightItem && rpgCharacterController.leftWeapon != Weapon.LeftItem
					&& rpgCharacterController.leftWeapon != Weapon.Shield) {
					if (GUI.Button(new Rect(dxGui + 235, dyGui + 85, 100, 30), "Attack Dual"))
					{ PressButton(Button.AttackDual); }
				}
			}
			//Special Attack.
			if (rpgCharacterController.hasTwoHandedWeapon && !rpgCharacterController.hasAimedWeapon) {
				if (GUI.Button(new Rect(dxGui + 335, dyGui + 85, 100, 30), "Special Attack1"))
				{ PressButton(Button.SpecialAttack1); }
			}
			// Sword + Mace Special Attack.
			if ((rpgCharacterController.leftWeapon == Weapon.LeftSword || rpgCharacterController.leftWeapon == Weapon.LeftMace)
				&& (rpgCharacterController.rightWeapon == Weapon.RightSword || rpgCharacterController.rightWeapon == Weapon.RightMace)) {
				if (GUI.Button(new Rect(dxGui + 335, dyGui + 85, 100, 30), "Special Attack1"))
				{ PressButton(Button.SpecialAttack1); }
			}
			// Kicking.
			if (GUI.Button(new Rect(dxGui + 25, dyGui + 115, 100, 30), "Left Kick"))
			{ PressButton(Button.LeftKick); }
			if (GUI.Button(new Rect(dxGui + 25, dyGui + 145, 100, 30), "Left Kick2"))
			{ PressButton(Button.LeftKick2); }
			if (GUI.Button(new Rect(dxGui + 130, dyGui + 115, 100, 30), "Right Kick"))
			{ PressButton(Button.RightKick); }
			if (GUI.Button(new Rect(dxGui + 130, dyGui + 145, 100, 30), "Right Kick2"))
			{ PressButton(Button.RightKick2); }
		}

		private void Damage()
        {
			// Check if GetHit Action exists.
			if (rpgCharacterController.HandlerExists(HandlerTypes.GetHit)
				&& rpgCharacterController.CanStartAction(HandlerTypes.GetHit)) {
					if (GUI.Button(new Rect(dxGui + 30, dyGui + 240, 100, 30), "Get Hit"))
					{ PressButton(Button.GetHit); }
			}
			// Check if Knockback Action exists.
			if (rpgCharacterController.HandlerExists(HandlerTypes.Knockback)
				&& rpgCharacterController.CanStartAction(HandlerTypes.Knockback)) {
					if (GUI.Button(new Rect(dxGui + 130, dyGui + 240, 100, 30), "Knockback1"))
					{ PressButton(Button.Knockback1); }
					if (GUI.Button(new Rect(dxGui + 230, dyGui + 240, 100, 30), "Knockback2"))
					{ PressButton(Button.Knockback2); }
			}
			// Check if Knockdown Action exists.
			if (rpgCharacterController.HandlerExists(HandlerTypes.Knockdown)
				&& rpgCharacterController.CanStartAction(HandlerTypes.Knockdown)) {
					if (GUI.Button(new Rect(dxGui + 130, dyGui + 270, 100, 30), "Knockdown"))
					{ PressButton(Button.Knockdown); }
			}
        }

		private void Sprinting()
		{
            if (rpgCharacterController.HandlerExists(HandlerTypes.Sprint))
                DrawToggle(new Rect(dxGui + 640, dyGui + 90, 100, 30), ToggleControl.Sprint, "Sprint");
        }

		private void Crouching()
        {
            if (!rpgCharacterController.HandlerExists(HandlerTypes.Crouch)) { return; }
            DrawToggle(new Rect(dxGui + 640, dyGui + 115, 100, 30), ToggleControl.Crouch, "Crouch");
            if ((rpgCharacterController.isCrouching || rpgCharacterController.isCrawling)
                && rpgCharacterController.HandlerExists(HandlerTypes.Crawl)
                && GUI.Button(new Rect(dxGui + 640, dyGui + 140, 100, 30), "Crawl"))
                PressButton(Button.Crawl);
        }

        private void Blocking()
        {
            if (!rpgCharacterController.HandlerExists(HandlerTypes.Block)
                || rpgCharacterController.isCasting || rpgCharacterController.isSitting
                || rpgCharacterController.IsActive(HandlerTypes.Relax)) { return; }
            DrawToggle(new Rect(dxGui + 25, dyGui + 215, 100, 30), ToggleControl.Block, "Block");
            if (rpgCharacterController.isBlocking && rpgCharacterController.HandlerExists(HandlerTypes.GetHit)
                && GUI.Button(new Rect(dxGui + 30, dyGui + 240, 100, 30), "Get Hit")) PressButton(Button.GetHit);
        }

		private void RollDodgeTurn()
		{
			// Check if Roll Action exists.
			if (rpgCharacterController.HandlerExists(HandlerTypes.Block)) {
				if (rpgCharacterController.CanStartAction(HandlerTypes.Roll)) {
					if (GUI.Button(new Rect(dxGui + 25, dyGui + 15, 100, 30), "Roll Forward"))
					{ PressButton(Button.RollForward); }
					if (GUI.Button(new Rect(dxGui + 130, dyGui + 15, 100, 30), "Roll Backward"))
					{ PressButton(Button.RollBackward); }
					if (GUI.Button(new Rect(dxGui + 25, dyGui + 45, 100, 30), "Roll Left"))
					{ PressButton(Button.RollLeft); }
					if (GUI.Button(new Rect(dxGui + 130, dyGui + 45, 100, 30), "Roll Right"))
					{ PressButton(Button.RollRight); }
				}
			}
			// Check if Dodge Action exists.
			if (rpgCharacterController.HandlerExists(HandlerTypes.Dodge)) {
				if (rpgCharacterController.CanStartAction(HandlerTypes.Dodge)) {
					if (GUI.Button(new Rect(dxGui + 235, dyGui + 15, 100, 30), "Dodge Left"))
					{ PressButton(Button.DodgeLeft); }
					if (GUI.Button(new Rect(dxGui + 235, dyGui + 45, 100, 30), "Dodge Right"))
					{ PressButton(Button.DodgeRight); }
					if (GUI.Button(new Rect(dxGui + 235, dyGui + 75, 100, 30), "Dodge Back"))
					{ PressButton(Button.DodgeBack); }
				}
			}
			// Check if Turn Action exists.
			if (rpgCharacterController.HandlerExists(HandlerTypes.Turn)) {
				if (rpgCharacterController.CanStartAction(HandlerTypes.Turn)) {
					if (GUI.Button(new Rect(dxGui + 340, dyGui + 15, 100, 30), "Turn Left"))
					{ PressButton(Button.TurnLeft); }
					if (GUI.Button(new Rect(dxGui + 340, dyGui + 45, 100, 30), "Turn Right"))
					{ PressButton(Button.TurnRight); }
					if (GUI.Button(new Rect(dxGui + 445, dyGui + 15, 100, 30), "Turn Left 180"))
					{ PressButton(Button.TurnLeft180); }
					if (GUI.Button(new Rect(dxGui + 445, dyGui + 45, 100, 30), "Turn Right 180"))
					{ PressButton(Button.TurnRight180); }
				}
			}
			// Check if DiveRoll Action exists.
			if (rpgCharacterController.HandlerExists(HandlerTypes.DiveRoll)) {
				if (rpgCharacterController.CanStartAction(HandlerTypes.DiveRoll)) {
					if (GUI.Button(new Rect(dxGui + 445, dyGui + 75, 100, 30), "Dive Roll"))
					{ PressButton(Button.DiveRoll); }
				}
			}
		}

        private void Casting()
        {
			// Check if Cast Action exists.
			if (!rpgCharacterController.HandlerExists(HandlerTypes.Cast)) { return; }

			// If Casting, allow to stop.
			if (rpgCharacterController.CanEndAction(HandlerTypes.Cast) && rpgCharacterController.isCasting) {
                if (GUI.Button(new Rect(dxGui + 25, dyGui + 330, 100, 30), "Stop Casting"))
                { PressButton(Button.StopCasting); }
            }
			// If AttackCasting, allow to stop.
			if (rpgCharacterController.CanEndAction(HandlerTypes.AttackCast) && rpgCharacterController.isCasting) {
				if (GUI.Button(new Rect(dxGui + 25, dyGui + 330, 100, 30), "Stop Casting"))
				{ PressButton(Button.StopCasting); }
			}

			// Check if Casting Action can be started.
            if (!rpgCharacterController.CanStartAction(HandlerTypes.Cast)) { return; }

            var leftUnarmed = rpgCharacterController.leftWeapon == Weapon.Unarmed;
            var rightUnarmed = rpgCharacterController.rightWeapon == Weapon.Unarmed;
            var wieldStaff = rpgCharacterController.rightWeapon == Weapon.TwoHandStaff;

            if (leftUnarmed && GUI.Button(new Rect(dxGui + 25, dyGui + 330, 100, 30), "Cast Atk Left"))
			{ PressButton(Button.CastAtkLeft); }
            if (rightUnarmed && GUI.Button(new Rect(dxGui + 125, dyGui + 330, 100, 30), "Cast Atk Right"))
			{ PressButton(Button.CastAtkRight); }
            if (leftUnarmed && rightUnarmed && GUI.Button(new Rect(dxGui + 80, dyGui + 365, 100, 30), "Cast Atk Dual"))
			{ PressButton(Button.CastAtkDual); }
            if (!rpgCharacterController.hasDualWeapons
				&& (rpgCharacterController.leftWeapon.IsCastableWeapon() || rpgCharacterController.rightWeapon.IsCastableWeapon())) {
                if (GUI.Button(new Rect(dxGui + 25, dyGui + 425, 100, 30), "Cast AOE"))
                { PressButton(Button.CastAOE); }
                if (GUI.Button(new Rect(dxGui + 25, dyGui + 400, 100, 30), "Cast Buff"))
                { PressButton(Button.CastBuff); }
                if (GUI.Button(new Rect(dxGui + 25, dyGui + 450, 100, 30), "Cast Summon"))
                { PressButton(Button.CastSummon); }
            }
        }

        private void Jumping()
        {
			// Check if Jump Action exists.
			if (!rpgCharacterController.HandlerExists(HandlerTypes.Jump)) { return; }

			// Check if Jump Action can be started.
			if (rpgCharacterController.CanStartAction(HandlerTypes.Jump)) {
                if (GUI.Button(new Rect(dxGui + 25, dyGui + 175, 100, 30), "Jump")) {
                    PressButton(Button.Jump);
                }
            }
			// Check if Double Jump Action can be started.
            if (rpgCharacterController.CanStartAction(HandlerTypes.DoubleJump)) {
                if (GUI.Button(new Rect(dxGui + 25, dyGui + 175, 100, 30), "Jump Flip")) {
                    PressButton(Button.JumpFlip);
                }
            }
        }

        private void Emotes()
        {
			// Check if Emote Action exists.
			if (rpgCharacterController.HandlerExists(HandlerTypes.Emote)) {
				if (rpgCharacterController.CanStartAction(HandlerTypes.Emote)) {
					var emote = EmoteType.Unknown;
					if (GUI.Button(new Rect(dxGui + 665, dyGui + 680, 100, 30), "Sleep")) { emote = EmoteType.Laydown; }
					if (GUI.Button(new Rect(dxGui + 770, dyGui + 680, 100, 30), "Sit")) { emote = EmoteType.Sit; }
					if (GUI.Button(new Rect(dxGui + 770, dyGui + 650, 100, 30), "Drink")) { emote = EmoteType.Drink; }
					if (GUI.Button(new Rect(dxGui + 665, dyGui + 650, 100, 30), "Bow")) { emote = EmoteType.Bow1; }
					if (GUI.Button(new Rect(dxGui + 560, dyGui + 650, 100, 30), "Yes")) { emote = EmoteType.Yes; }
					if (GUI.Button(new Rect(dxGui + 455, dyGui + 650, 100, 30), "No")) { emote = EmoteType.No; }
					if (GUI.Button(new Rect(dxGui + 130, dyGui + 175, 100, 30), "Pickup")) { emote = EmoteType.Pickup; }
					if (GUI.Button(new Rect(dxGui + 235, dyGui + 175, 100, 30), "Activate")) { emote = EmoteType.Activate; }

					if (emote != EmoteType.Unknown) { PlayEmote(emote, false); }
				}
				// Check if Emote action can be stopped.
				if (rpgCharacterController.CanEndAction(HandlerTypes.Emote)) {
					if (rpgCharacterController.isSitting) {
						if (GUI.Button(new Rect(dxGui + 795, dyGui + 680, 100, 30), "Stand"))
						{ PressButton(Button.Stand); }
					}
				}
			}
			// Check if character has Talk Action.
			if (rpgCharacterController.HandlerExists(HandlerTypes.Talk)) {

				// Check if character can start Talk Action.
				if (rpgCharacterController.CanStartAction(HandlerTypes.Talk))
				{
					if (GUI.Button(new Rect(dxGui + 560, dyGui + 680, 100, 30), "Start Talking"))
					{ PressButton(Button.StartTalking); }
				}
				// Check if character can stop Talk Action.
				if (rpgCharacterController.CanEndAction(HandlerTypes.Talk)) {
					if (rpgCharacterController.isTalking) {
						if (GUI.Button(new Rect(dxGui + 795, dyGui + 680, 100, 30), "Stop Talking"))
						{ PressButton(Button.StopTalking); }
					}
				}
			}
			// Check if Emote Combat Action exists.
			if (rpgCharacterController.HandlerExists(HandlerTypes.EmoteCombat)) {
				if (rpgCharacterController.CanStartAction(HandlerTypes.EmoteCombat)) {
					var emote = EmoteType.Unknown;
					if (GUI.Button(new Rect(dxGui + 480, dyGui + 650, 100, 30), "Boost")) { emote = EmoteType.Boost; }

					// Pickup and Activate require a free hand.
					if (!rpgCharacterController.hasDualWeapons) {
						if (GUI.Button(new Rect(dxGui + 130, dyGui + 175, 100, 30), "Pickup")) { emote = EmoteType.Pickup; }
						if (GUI.Button(new Rect(dxGui + 235, dyGui + 175, 100, 30), "Activate")) { emote = EmoteType.Activate; }
					}
					// Start the Action if Emote found.
					if (emote != EmoteType.Unknown) { PlayEmote(emote, true); }
				}
			}
        }

        private void Climbing()
        {
	        // Check if ClimbLadder Action exists.
	        if (!rpgCharacterController.HandlerExists(HandlerTypes.ClimbLadder)) { return; }

			// Check if can start ClimbLadder Action.
	        if (rpgCharacterController.CanStartAction(HandlerTypes.ClimbLadder)) {
				if (GUI.Button(new Rect(dxGui + 640, dyGui + 360, 100, 30), "Climb Ladder"))
				{ PressButton(Button.ClimbLadder); }
			}
        }

        private void Swimming()
        {
            if (GUI.Button(new Rect(dxGui + 25, dyGui + 175, 100, 30), "Swim Up")) PressButton(Button.SwimUp);
            if (GUI.Button(new Rect(dxGui + 25, dyGui + 225, 100, 30), "Swim Down")) PressButton(Button.SwimDown);
        }

        // Death / Debug.
        private void Misc()
        {
            var deathReviveLabel = rpgCharacterController.isDead ? "Revive" : "Death";
            if (!rpgCharacterController.isClimbing && !rpgCharacterController.isCasting
				&& !rpgCharacterController.isSitting && rpgCharacterController.maintainingGround) {

				// Check if Death Action exists.
				if (rpgCharacterController.HandlerExists(HandlerTypes.Death)) {
					if (GUI.Button(new Rect(dxGui + 30, dyGui + 270, 100, 30), deathReviveLabel)) {
						PressButton(Button.DeathRevive);
					}
				}
            }
        }

		private void DebugRPGCharacter()
		{
			// Output all controller variables.
			if (GUI.Button(new Rect(dxGui + 600, dyGui + 20, 120, 30), "Debug Controller"))
			{ PressButton(Button.DebugController); }

			// Output all Animator parameters.
			if (GUI.Button(new Rect(dxGui + 600, dyGui + 50, 120, 30), "Debug Animator"))
			{ PressButton(Button.DebugAnimator); }
		}

		private void ChangeMap()
		{
            #if !UNITY_EDITOR
            if (GUI.Button(new Rect(dxGui + 780, dyGui + 130, 80, 80), "Change\n \nMap")) PressButton(Button.ChangeMap);
            #endif
        }

		private void WeaponSwitching()
		{
			// Check if SwitchWeapon Action exists.
			if (!rpgCharacterController.HandlerExists(HandlerTypes.SwitchWeapon)) { return; }

			var doSwitch = false;
			var context = new SwitchWeaponContext();

			// Switch to Relax.
			if (rpgCharacterController.HandlerExists(HandlerTypes.Relax)) {
				if (!rpgCharacterController.isRelaxed) {
					if (GUI.Button(new Rect(dxGui + 1115, dyGui + 240, 100, 30), "Relax")) {
						PressButton(Button.Relax);
					}
				}
			}
			// Switch to Unarmed.
			if (rpgCharacterController.rightWeapon != Weapon.Unarmed
				|| rpgCharacterController.leftWeapon != Weapon.Unarmed) {
				if (GUI.Button(new Rect(dxGui + 1115, dyGui + 280, 100, 30), "Unarmed")) {
					doSwitch = true;
					context.type = "Switch";
					context.side = "Dual";
					context.leftWeapon = Weapon.Unarmed;
					context.rightWeapon = Weapon.Unarmed;
				}
			}

			var offset = 310;

			// 2 Handed Weapons.
			foreach (var weapon in WeaponGroupings.TwoHandedWeapons) {
				if (rpgCharacterController.rightWeapon != weapon) {
					var label = weapon.ToString();
					if (label.StartsWith("TwoHand")) { label = label.Replace("TwoHand", "2H "); }
					if (GUI.Button(new Rect(dxGui + 1115, dyGui + offset, 100, 30), label)) {
						doSwitch = true;
						context.type = "Switch";
						context.side = "None";
						context.leftWeapon = Weapon.Relax;
						context.rightWeapon = weapon;
					}
				}

				offset += 30;
			}

			offset = 530;

			// One Handed Weapons.
			foreach (var pair in WeaponGroupings.LeftRightWeaponPairs) {
				bool missingOneSide = false;

				// Left weapons.
				if (rpgCharacterController.leftWeapon != pair.Item1) {
					missingOneSide = true;
					if (GUI.Button(new Rect(dxGui + 1065, dyGui + offset, 100, 30), pair.Item1.ToString())) {
						doSwitch = true;
						context.type = "Switch";
						context.side = "Left";
						context.leftWeapon = pair.Item1;
						context.rightWeapon = Weapon.Relax;
					}
				}
				// Right weapons.
				if (rpgCharacterController.rightWeapon != pair.Item2) {
					missingOneSide = true;
					if (GUI.Button(new Rect(dxGui + 1165, dyGui + offset, 100, 30), pair.Item2.ToString())) {
						doSwitch = true;
						context.type = "Switch";
						context.side = "Right";
						context.leftWeapon = Weapon.Relax;
						context.rightWeapon = pair.Item2;
					}
				}
				// If at least one side isn't carrying this weapon, show the Dual switch.
				if (missingOneSide) {
					string label = pair.Item1.ToString();
					if (!label.Contains("Shield")) {
						label = label.Replace("Left", "Dual ") + "s";
						if (GUI.Button(new Rect(dxGui + 965, dyGui + offset, 100, 30), label)) {
							doSwitch = true;
							context.type = "Switch";
							context.side = "Dual";
							context.leftWeapon = pair.Item1;
							context.rightWeapon = pair.Item2;
						}
					}
				}

				offset += 30;
			}
			// Sheath Left Weapon.
			if (rpgCharacterController.leftWeapon.HasEquippedWeapon()) {
				if (GUI.Button(new Rect(dxGui + 750, dyGui + offset - 150, 100, 30), "Sheath Left")) {
					doSwitch = true;
					context.type = "Sheath";
					context.side = "Left";
					context.leftWeapon = Weapon.Unarmed;
					context.rightWeapon = Weapon.Relax;
				}
			}
			// Sheath Right Weapon.
			if (rpgCharacterController.rightWeapon.HasEquippedWeapon()) {
				if (GUI.Button(new Rect(dxGui + 850, dyGui + offset - 150, 100, 30), "Sheath Right")) {
					doSwitch = true;
					context.type = "Sheath";
					context.side = "Right";
					context.leftWeapon = Weapon.Relax;
					context.rightWeapon = Weapon.Unarmed;
				}
			}

			offset += 30;

			// Check if HipShoot Action exists.
			if (rpgCharacterController.HandlerExists(HandlerTypes.HipShoot)) {
				DrawToggle(new Rect(dxGui + 1000, dyGui + 495, 100, 30), ToggleControl.HipShooting, "Hip Shooting");
			}

			// Sheath/Unsheath Hips.
			DrawToggle(new Rect(dxGui + 1000, dyGui + 260, 100, 30), ToggleControl.Hips, "Hips");
			if (useHips) { context.sheathLocation = "Hips"; }
			else { context.sheathLocation = "Back"; }

			// Instant weapon toggle.
			DrawToggle(new Rect(dxGui + 1000, dyGui + 310, 100, 30), ToggleControl.Instant, "Instant");
			if (useInstant) { context.type = "Instant"; }

			// Perform the weapon switch.
			if (doSwitch) { SwitchWeapon(context); }
		}
	}
}


