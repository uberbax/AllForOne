# Pickup notifications

XDtake sends pickups to PickupNotificationStack.Show(itemId, localizedName, amount, icon), using ResourceHolder.GetIcon. Other FlyTextUI callers keep their existing stat feedback.

Resources/PickupNotifications.prefab is created lazily on the first pickup. It contains an independent overlay Canvas (no GraphicRaycaster), theme TMP font, SafeArea and non-interactive compact rows. Runtime scene wiring is not required.

Defaults: right side, 380 x 42 UI-unit bars, 6-unit gap, max 5 visible, 2.8 s hold, 0.22 s fade. Timers use unscaled time. Queue order is FIFO; duplicates in visible or pending entries merge by item ID. Layout uses fixed vertical slots and single-line ellipsis, so messages do not pass through one another. Visible rows are pooled and reused. Amount values are read before AddItem mutates the inventory stack.

Edit Max Visible, Hold Duration and Fade Duration on the prefab; adjust SafeArea/Rows anchoring for placement and RowTemplate for style. Rebuild from Tools/UI/Build Pickup Notifications to restore the generated default layout.
